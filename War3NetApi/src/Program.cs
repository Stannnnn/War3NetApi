#nullable enable

namespace War3NetMPQApi
{
    internal class Program
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

                    case "extractall":
                        mpqEditor.ExtractAll(parts[1], parts[2]);
                        break;

                    case "list":
                        mpqEditor.List(parts[1], parts[2]);
                        break;

                    case "add":
                        mpqEditor.Add(parts[1], parts[2]);
                        break;

                    case "addall":
                        mpqEditor.AddAll(parts[1]);
                        break;

                    case "remove":
                        mpqEditor.Remove(parts[1]);
                        break;

                    case "save":
                        mpqEditor.Save();
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