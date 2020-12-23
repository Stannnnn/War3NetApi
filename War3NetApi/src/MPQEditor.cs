#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Extensions;
using War3Net.IO.Mpq;

namespace War3NetMPQApi
{
    internal class MPQEditor
    {
        private MpqArchive? originalMpqArchive;
        private MpqArchiveBuilder? mpqArchiveBuilder;
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
            mpqArchiveBuilder = new MpqArchiveBuilder(originalMpqArchive);
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
                            System.Console.WriteLine(mpqEntry.Filename + " " + mpqEntry.FilePosition + " " + mpqEntry.FileSize);
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

        public void ExtractAll(string listFile, string dirOut)
        {
            if (originalMpqArchive != null)
            {
                string[] files = File.ReadAllLines(listFile);

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
                foreach ((var fileName, var _, var stream) in FileProvider.EnumerateFiles(folder))
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
    }
}