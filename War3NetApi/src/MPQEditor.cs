#nullable enable

using System.Collections.Generic;
using System.IO;
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

        public void Extract(string fileName, string dirOut)
        {
            var fileOut = dirOut + Path.DirectorySeparatorChar + fileName;

            if (originalMpqArchive != null)
            {
                try
                {
                    using (MpqStream fileStreamIn = originalMpqArchive.OpenFile(fileName))
                    {
                        using (FileStream fileStreamOut = FileProvider.CreateFileAndFolder(fileOut))
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
                        Extract(file, dirOut);
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
            mpqArchiveBuilder?.AddFile(MpqFile.New(File.OpenRead(inputFile), fileName));
        }

        public void AddAll(string folder)
        {
            if (mpqArchiveBuilder != null)
            {
                foreach ((var fileName, var _, var stream) in FileProvider.EnumerateFiles(folder))
                {
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