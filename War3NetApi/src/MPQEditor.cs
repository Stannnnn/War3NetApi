#nullable enable

using CSharpLua;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using War3Net.Build;
using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.Build.Providers;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Transpilers;
using War3Net.IO.Mpq;

namespace War3NetMPQApi
{
    internal class MPQEditor
    {
        private MpqArchive? originalMpqArchive;
        private CustomMpqArchiveBuilder? mpqArchiveBuilder;
        private string? mpqArchivePath;

        public void Open(string fileName)
        {
            mpqArchivePath = fileName;

            var copy = new MemoryStream();
            using (var filestream = File.OpenRead(fileName))
            {
                filestream.CopyTo(copy);
            }
            copy.Position = 0;

            originalMpqArchive = MpqArchive.Open(copy);
            mpqArchiveBuilder = new CustomMpqArchiveBuilder(originalMpqArchive);
        }

        private static FileStream CreateOrOpenFileAndFolder(string path)
        {
            var directory = new FileInfo(path).Directory;

            if (!Directory.Exists(directory.FullName))
            {
                Directory.CreateDirectory(directory.FullName);
            }

            if (File.Exists(path))
            {
                return File.OpenWrite(path);
            }
            else
            {
                return File.Create(path);
            }
        }

        public void Extract(string fileName, string fileOut)
        {
            if (originalMpqArchive != null)
            {
                try
                {
                    var mpqEntries = originalMpqArchive.GetMpqEntries(fileName);

                    if (mpqEntries.Count() > 1)
                    {
                        foreach (var mpqEntry in mpqEntries)
                        {
                            System.Console.WriteLine(mpqEntry.FileName + " " + mpqEntry.FilePosition + " " + mpqEntry.FileSize);
                        }
                    }

                    using (MpqStream fileStreamIn = originalMpqArchive.OpenFile(mpqEntries.Last()))
                    {
                        using (FileStream fileStreamOut = CreateOrOpenFileAndFolder(fileOut))
                        {
                            fileStreamIn.CopyTo(fileStreamOut);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                }
                catch (MpqParserException)
                {
                    System.Console.WriteLine("Exception for: " + fileName);
                    throw;
                }
            }
        }

        public IEnumerable<string> ReadLines(Func<Stream> streamProvider)
        {
            using (var stream = streamProvider())
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        public void ExtractAll(string listFile, string dirOut)
        {
            if (originalMpqArchive != null)
            {
                var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                files.AddRange(File.ReadAllLines(listFile));

                if (originalMpqArchive.FileExists("(listfile)"))
                {
                    using (MpqStream fileStreamIn = originalMpqArchive.OpenFile("(listfile)"))
                    {
                        files.AddRange(ReadLines(() => fileStreamIn));
                    }
                }

                foreach (var file in files)
                {
                    if (originalMpqArchive.FileExists(file))
                    {
                        Extract(file, dirOut + Path.DirectorySeparatorChar + file);
                    }
                }
            }
        }

        public void List(string listFile, string fileOut)
        {
            if (originalMpqArchive != null)
            {
                string[] files = File.ReadAllLines(listFile);
                List<string> list = new List<string>();

                foreach (var file in files)
                {
                    if (originalMpqArchive.FileExists(file))
                    {
                        list.Add(file);
                    }
                }

                File.WriteAllLines(fileOut, list.ToArray());
            }
        }

        public void Add(string inputFile, string fileName)
        {
            mpqArchiveBuilder?.RemoveFile(fileName);
            mpqArchiveBuilder?.AddFile(MpqFile.New(File.OpenRead(inputFile), fileName));
        }

        public void AddAll(string folder)
        {
            if (mpqArchiveBuilder != null)
            {
                foreach ((var fileName, var _, var stream) in EnumerateFiles(folder))
                {
                    mpqArchiveBuilder.RemoveFile(fileName);
                    mpqArchiveBuilder.AddFile(MpqFile.New(stream, fileName));
                }
            }
        }

        public void Remove(string file)
        {
            mpqArchiveBuilder?.RemoveFile(MpqHash.GetHashedFileName(file));
        }

        public void Save()
        {
            if (mpqArchivePath != null && mpqArchiveBuilder != null)
            {
                using (var fileStream = File.Create(mpqArchivePath))
                {
                    mpqArchiveBuilder.SaveArchive(fileStream);
                }
            }
        }

        public void Restore(string file)
        {
            using (MemoryStream stream = MpqArchive.Restore(file))
            {
                using (FileStream streamOut = File.Create(file))
                {
                    stream.CopyTo(streamOut);
                }
            }
        }

        public void Close()
        {
            if (mpqArchivePath != null)
            {
                mpqArchivePath = null;
            }

            originalMpqArchive?.Dispose();
        }

        public void TranspileAndSaveTest(string inputFile, string outputFile, ScriptLanguage targetLanguage)
        {
            var targetFileName = targetLanguage switch
            {
                ScriptLanguage.Jass => "war3map.j",
                ScriptLanguage.Lua => "war3map.lua",
                _ => throw new InvalidEnumArgumentException(nameof(targetLanguage), (int)targetLanguage, typeof(ScriptLanguage)),
            };

            using var mapArchive = MpqArchive.Open(inputFile, true);

            var map = Map.Open(mapArchive);
            var sourceLanguage = map.Info.ScriptLanguage;
            var mpqArchiveBuilder = new MpqArchiveBuilder(mapArchive);

            using var mapInfoStream = new MemoryStream();
            using var mapInfoWriter = new BinaryWriter(mapInfoStream);

            var mapInfo = map.Info;
            mapInfo.ScriptLanguage = targetLanguage;
            if (mapInfo.FormatVersion < MapInfoFormatVersion.Lua)
            {
                mapInfo.FormatVersion = MapInfoFormatVersion.Lua;
                if (mapInfo.GameVersion is null)
                {
                    mapInfo.GameVersion = GamePatchVersionProvider.GetGameVersion(War3Net.Build.Common.GamePatch.v1_31_1);
                }
            }

            mapInfoWriter.Write(mapInfo);
            mapInfoStream.Position = 0;
            mpqArchiveBuilder.AddFile(MpqFile.New(mapInfoStream, MapInfo.FileName));

            if (sourceLanguage == ScriptLanguage.Jass)
            {
                if (targetLanguage != ScriptLanguage.Lua)
                {
                    throw new NotSupportedException($"Unable to transpile from {sourceLanguage} to {targetLanguage}.");
                }

                mpqArchiveBuilder.RemoveFile("war3map.j");
                mpqArchiveBuilder.RemoveFile(@"Scripts\war3map.j");

                using var reader = new StreamReader(@"C:\Users\Stan\Google Drive\PHP Projects\Files\common.j");
                var commonJ = JassSyntaxFactory.ParseCompilationUnit(reader.ReadToEnd());

                using var reader2 = new StreamReader(@"C:\Users\Stan\Google Drive\PHP Projects\Files\blizzard.j");
                var blizzardJ = JassSyntaxFactory.ParseCompilationUnit(reader2.ReadToEnd());

                var transpiler = new JassToLuaTranspiler();
                transpiler.RegisterJassFile(commonJ);
                transpiler.RegisterJassFile(blizzardJ);

                var script = mapArchive.OpenFile("war3map.j");

                using var reader3 = new StreamReader(script);
                var luaCompilationUnit = transpiler.Transpile(JassSyntaxFactory.ParseCompilationUnit(reader3.ReadToEnd()));
                script.Close();

                var tempFileName = Path.GetTempFileName();
                try
                {
                    using (var writer = new StreamWriter(tempFileName))
                    {
                        var luaRenderOptions = new LuaSyntaxGenerator.SettingInfo
                        {
                            // Indent = 4,
                            // IsCommentsDisabled = true,
                        };

                        var luaRenderer = new LuaRenderer(luaRenderOptions, writer);
                        luaRenderer.RenderCompilationUnit(luaCompilationUnit);
                    }

                    using var fileStream = File.OpenRead(tempFileName);
                    mpqArchiveBuilder.AddFile(MpqFile.New(fileStream, targetFileName));

                    var mpqArchiveCreateOptions = new MpqArchiveCreateOptions
                    {
                        AttributesCreateMode = MpqFileCreateMode.Prune,
                    };

                    mpqArchiveBuilder.SaveTo(outputFile, mpqArchiveCreateOptions);
                }
                finally
                {
                    File.Delete(tempFileName);
                }
            }
            else if (sourceLanguage == ScriptLanguage.Lua)
            {
                throw new NotSupportedException($"Unable to transpile from {sourceLanguage} to {targetLanguage}.");
            }
            else
            {
                throw new NotSupportedException($"Unable to transpile from {sourceLanguage} to {targetLanguage}.");
            }
        }

        public IEnumerable<(string fileName, MpqLocale locale, Stream stream)> EnumerateFiles(string path)
        {
            if (Directory.Exists(path))
            {
                var pathPrefixLength = path.Length + (path.EndsWith(@"\", StringComparison.Ordinal) ? 0 : 1);
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    var fileName = new FileInfo(file).ToString().Substring(pathPrefixLength);
                    // var memoryStream = new MemoryStream();
                    // File.OpenRead(file).CopyTo(memoryStream);

                    var locale = MpqLocaleProvider.GetPathLocale(fileName, out var filePath);

                    yield return (filePath, locale, File.OpenRead(file));
                }
            }
        }
    }
}