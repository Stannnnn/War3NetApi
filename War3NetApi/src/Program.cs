#nullable enable

namespace War3NetMPQApi
{
    class Program
    {
        private static void Main(string[] args)
        {
            MPQEditor mpqEditor = new MPQEditor();

            foreach (string arg in args)
            {
                var parts = arg.Split('>');

                switch (parts[0])
                {
                    case "open":
                        mpqEditor.Close();
                        mpqEditor.Open(parts[1]);
                        break;

                    case "extract":
                        mpqEditor.Extract(parts[1], parts[2]);
                        break;

                    case "restore":
                        mpqEditor.Unh3x(parts[1]);
                        break;

                    case "replace":
                        mpqEditor.Replace(parts[1], parts[2]);
                        break;

                    case "remove":
                        mpqEditor.Remove(parts[1]);
                        break;

                    case "close":
                        mpqEditor.Close();
                        break;
                }
            }

            mpqEditor.Close();
        }
    }
}