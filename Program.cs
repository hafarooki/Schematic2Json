using fNbt;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schematic2Json
{
    class Program
    {
        static void Main(string[] args)
        {
            string file;

            if (args.Length > 0)
            {
                file = args[0];
            }
            else
            {
                Console.Write("Schematic file path: ");
                file = Console.ReadLine();
                Console.WriteLine();
            }

            if (!File.Exists(file))
            {
                Console.WriteLine("File not found.");
                if (args.Contains("nopause")) return;
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
                Environment.Exit(1);
                return;
            }

            Console.WriteLine("Converting...");

            try
            {
                var nbt = new NbtFile();
                nbt.LoadFromFile(file);
                var root = nbt.RootTag;
                var width = root.Get<NbtShort>("Width").ShortValue;
                var height = root.Get<NbtShort>("Height").ShortValue;
                var length = root.Get<NbtShort>("Length").ShortValue;
                var blocks = root.Get<NbtByteArray>("Blocks").ByteArrayValue;
                var blockdata = root.Get<NbtByteArray>("Data").ByteArrayValue;

                Model model = new Model();
                model.__comment = "Converted with Schematic2Json by MicleBrick. NBT data read with https://github.com/fragmer/fNbt. JSON written with https://json.net";
                model.textures = new Dictionary<string, string>();
                foreach (WoolColor color in Enum.GetValues(typeof(WoolColor)))
                {
                    model.textures.Add(((byte)color).ToString(), "blocks/wool_colored_" + color.ToString());
                }

                var elements = new List<Model.Element>();

                var random = new Random();

                for (var x = 0; x < width; ++x)
                {
                    for (var y = 0; y < height; ++y)
                    {
                        for (var z = 0; z < length; ++z)
                        {
                            var index = y * width * length + z * width + x;
                            var block = blocks[index];
                            if (block == 0) continue;
                            var data = blockdata[index];

                            byte texture = 0;

                            if (block == 35 || block == 159) texture = data;
                            var rnd = (float) random.NextDouble() * 16;
                            if (args.Contains("nonoise")) rnd = 0;
                            var face = new Model.Element.Face { texture = "#" + texture, uv = new float[] { rnd, rnd, rnd, rnd } };

                            elements.Add(new Model.Element
                            {
                                from = new float[] { x, y, z },
                                to = new float[] { x + 1, y + 1, z + 1 },
                                faces = new Dictionary<string, Model.Element.Face> {
                                { "North", face },
                                { "East", face },
                                { "South", face },
                                { "West", face },
                                { "Up", face },
                                { "Down", face },
                                }
                            });
                        }
                    }
                }
                model.elements = elements.ToArray();
                Console.WriteLine("Serializing...");
                var extension = Path.GetExtension(file);
                var newFile = file.Replace(extension, ".json");
                File.WriteAllText(newFile, JsonConvert.SerializeObject(model, Formatting.Indented));
                Console.WriteLine("Written json to file: " + newFile);
                if (args.Contains("nopause")) return;
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            catch (Exception exception)
            {
                Console.WriteLine("There was an error while trying to convert the schematic file.");
                File.WriteAllText(DateTime.Now.ToFileTime() + "-crash.txt", exception.ToString());
                Console.WriteLine(exception);
                if (args.Contains("nopause")) return;
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
                Environment.Exit(2);
                return;
            }
        }
    }
}
