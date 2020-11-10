#nullable enable

using System.IO;
using War3Net.IO.Mpq;

namespace War3NetMPQApi
{
    class MPQEditor
    {
        private MpqArchiveFiles? mpqArchiveFiles;
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

            mpqArchiveFiles = new MpqArchiveFiles(copy);
        }

        public void Extract(string fileName, string fileOut)
        {
            if (mpqArchiveFiles != null)
            {
                try
                {
                    using (MpqStream fileStreamIn = mpqArchiveFiles.MpqArchive.OpenFile(fileName))
                    {
                        using (FileStream fileStreamOut = File.Create(fileOut))
                        {
                            fileStreamIn.CopyTo(fileStreamOut);
                        }
                    }
                }
                catch (FileNotFoundException) { }
            }
        }

        public void Unh3x(string file)
        {
            using (MemoryStream stream = MpqArchive.Restore(file))
            {
                using (FileStream streamOut = File.Create(file))
                {
                    stream.CopyTo(streamOut);
                }
            }
        }

        public void Replace(string inputFile, string replaceFile)
        {
            mpqArchiveFiles?.ModifiedFiles.Add(MpqFile.New(File.OpenRead(inputFile), replaceFile));
        }

        public void Remove(string file)
        {
            mpqArchiveFiles?.RemovedFiles.Add(MpqHash.GetHashedFileName(file));
        }

        public void Save()
        {
            if (mpqArchivePath != null)
            {
                mpqArchiveFiles?.SaveTo(mpqArchivePath);
            }
        }

        public void Close()
        {
            if (mpqArchivePath != null)
            {
                mpqArchivePath = null;
            }

            mpqArchiveFiles?.Dispose();
        }
    }
}