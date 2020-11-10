using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.IO.Mpq;

namespace War3NetMPQApi
{
    class MpqArchiveFiles : IDisposable
    {
        public readonly MpqArchive MpqArchive;
        public readonly List<MpqFile> OriginalFiles;
        public readonly List<MpqFile> ModifiedFiles;
        public readonly List<ulong> RemovedFiles;

        public MpqArchiveFiles(Stream stream)
        {
            MpqArchive = new MpqArchive(stream);

            OriginalFiles = new List<MpqFile>(MpqArchive.GetMpqFiles());
            ModifiedFiles = new List<MpqFile>();
            RemovedFiles = new List<ulong>();
        }

        public void SaveTo(string fileName)
        {
            using (var stream = File.Create(fileName))
            {
                SaveTo(stream);
            }
        }

        public void SaveTo(Stream stream)
        {
            MpqArchive.Create(stream, GetMpqFiles().ToArray()).Dispose();
        }

        public void Dispose()
        {
            MpqArchive.Dispose();
        }

        private IEnumerable<MpqFile> GetMpqFiles()
        {
            return ModifiedFiles.Concat(OriginalFiles.Where(originalFile =>
                  !RemovedFiles.Contains(originalFile.Name) &&
                  !ModifiedFiles.Where(modifiedFile => modifiedFile.IsSameAs(originalFile)).Any()));
        }
    }
}
