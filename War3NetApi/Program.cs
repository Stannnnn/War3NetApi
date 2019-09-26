using System.IO;
using War3Net.IO.Mpq;

namespace War3NetMPQApi
{
    class Program
    {
        private static MpqArchive mpqArchive;
        private static FileStream mpqArchiveStream;

        private static void Open(string fileName) {
            mpqArchiveStream = File.OpenRead(fileName);
            mpqArchive = new MpqArchive(mpqArchiveStream);
        }

        private static void Extract (string fileName, string fileOut) {
            if (mpqArchive != null)
            {
                using (MpqStream fileStreamIn = mpqArchive.OpenFile(fileName))
                {
                    using (FileStream fileStreamOut = File.OpenWrite(fileOut))
                    {
                        fileStreamIn.CopyTo(fileStreamOut);
                    }
                }
            }
        }

        private static void Unh3x(string file) {
            using (MemoryStream stream = MpqArchive.Restore(file))
            {
                using (FileStream streamOut = new FileStream(file, FileMode.Create))
                {
                    stream.CopyTo(streamOut);
                }
            }
        }

        private static void Replace(string file, string replaceFile)
        {
            using (FileStream stream = File.OpenRead(replaceFile)) { 
                mpqArchive.ReplaceFile(file, stream);
            }
        }

        private static void Close()
        {
            if (mpqArchiveStream != null)
            {
                mpqArchiveStream.Dispose();
            }

            if (mpqArchive != null)
            {
                mpqArchive.Dispose();
            }
        }

        private static void Main(string[] args)
        {
            foreach (string arg in args) {
                var parts = arg.Split('>');

                switch (parts[0])
                {
                    case "open":
                        Close();
                        Open(parts[1]);
                        break;

                    case "extract":
                        Extract(parts[1], parts[2]);
                        break;

                    case "restore":
                        Unh3x(parts[1]);
                        break;

                    case "replace":
                        Replace(parts[1], parts[2]);
                        break;

                    case "close":
                        Close();
                        break;
                }
            }

            Close();
        }
    }
}
