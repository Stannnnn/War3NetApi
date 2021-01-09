using System;
using System.Collections.Generic;
using System.Linq;

namespace War3Net.IO.Mpq
{
    public class CustomMpqArchiveBuilder : MpqArchiveBuilder
    {
        public CustomMpqArchiveBuilder(MpqArchive originalMpqArchive) : base(originalMpqArchive)
        {
        }

        override protected IEnumerable<MpqFile> GetMpqFiles()
        {
            return OriginalFiles.Where(mpqFile => !RemovedFiles.Contains(mpqFile.Name)).Concat(ModifiedFiles);
        }
    }
}