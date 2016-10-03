using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fNbt;
using Newtonsoft.Json;

namespace Schematic2Json
{
    internal class Program
    {
        private static void Main(string[] args)
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

            if (file == null || !File.Exists(file))
            {
                Console.WriteLine("File not found.");
                if (args.Contains("nopause")) return;
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
                Environment.Exit(1);
            }

            Console.WriteLine("Converting...");

            try
            {
                var nbt = new NbtFile();
                nbt.LoadFromFile(file);
                var root = nbt.RootTag;
                // ReSharper disable PossibleNullReferenceException
                var width = root.Get<NbtShort>("Width").ShortValue;
                var height = root.Get<NbtShort>("Height").ShortValue;
                var length = root.Get<NbtShort>("Length").ShortValue;
                var blocks = root.Get<NbtByteArray>("Blocks").ByteArrayValue;
                var blockdata = root.Get<NbtByteArray>("Data").ByteArrayValue;

                var model = new Model
                {
                    __comment =
                        "Converted with Schematic2Json by MicleBrick. NBT data read with https://github.com/fragmer/fNbt. JSON written with https://json.net",
                    textures = new Dictionary<string, string>()
                };
                foreach (WoolColor color in Enum.GetValues(typeof(WoolColor)))
                {
                    model.textures.Add(((byte)color).ToString(), "blocks/wool_colored_" + color);
                    model.textures.Add((color + 16).ToString(), "blocks/hardened_clay_stained_" + color);
                }
                model.textures.Add(32.ToString(), "blocks/stone_slab_top");
                model.textures.Add(33.ToString(), "blocks/sandstone_top");
                model.textures.Add(34.ToString(), "blocks/cobblestone");
                model.textures.Add(35.ToString(), "blocks/brick");
                model.textures.Add(36.ToString(), "blocks/stonebrick");
                model.textures.Add(37.ToString(), "blocks/nether_brick");
                model.textures.Add(38.ToString(), "blocks/quartz_block_top");

                var elements = new List<Model.Element>();

                var random = new Random();

                var usedTextures = new List<string>();

                for (var x = 0; x < width; ++x)
                {
                    for (var y = 0; y < height; ++y)
                    {
                        for (var z = 0; z < length; ++z)
                        {
                            var index = y*width*length + z*width + x;
                            var block = blocks[index];
                            if (block == 0) continue;
                            var data = blockdata[index];

                            byte texture = 0;

                            if (block == 35 || block == 159) texture = data;
                            if (block == 159) texture += 16;

                            var slab = false;
                            var top = false;

                            if (block == 44)
                            {
                                slab = true;

                                switch (data)
                                {
                                    case 8:
                                        texture = 32;
                                        top = true;
                                        break;
                                    case 0:
                                        texture = 32;
                                        break;
                                    case 1:
                                        texture = 33;
                                        break;
                                    case 9:
                                        texture = 33;
                                        top = true;
                                        break;
                                    case 3:
                                        texture = 34;
                                        break;
                                    case 11:
                                        texture = 34;
                                        top = true;
                                        break;
                                    case 4:
                                        texture = 35;
                                        break;
                                    case 12:
                                        texture = 35;
                                        top = true;
                                        break;
                                    case 5:
                                        texture = 36;
                                        break;
                                    case 13:
                                        texture = 36;
                                        top = true;
                                        break;
                                    case 6:
                                        texture = 37;
                                        break;
                                    case 14:
                                        texture = 37;
                                        top = true;
                                        break;
                                    case 7:
                                        texture = 38;
                                        break;
                                    case 15:
                                        texture = 38;
                                        top = true;
                                        break;
                                    default:
                                        texture = 32;
                                        break;
                                }
                            }

                            if (!usedTextures.Contains(texture.ToString()))
                                usedTextures.Add(texture.ToString());

                            var rnd = (float) random.NextDouble()*16;
                            if (args.Contains("nonoise")) rnd = 0;
                            var face = new Model.Element.Face {texture = "#" + texture, uv = new[] {rnd, rnd, rnd, rnd}};

                            elements.Add(new Model.Element
                            {
                                from = new[] {x, y + (slab && top ? 0.5f : 0), z},
                                to = new[] {x + 1, y + (slab && !top ? 0.5f : 1), z + 1},
                                faces = new Dictionary<string, Model.Element.Face>
                                {
                                    {"North", face},
                                    {"East", face},
                                    {"South", face},
                                    {"West", face},
                                    {"Up", face},
                                    {"Down", face}
                                }
                            });
                        }
                    }
                }

                foreach (var key in model.textures.Keys.ToList())
                {
                    if (!usedTextures.Contains(key)) model.textures.Remove(key);
                }

                // Scale it to 32 due to MC maximum
                if (!args.Contains("noscale"))
                {
                    var max = elements.Select(element => element.to.Max()).Concat(new float[] {0}).Max();
                    var changedAmount = (max - 32.0f)/max;
                    if (changedAmount > 0)
                    {
                        Console.WriteLine("Scaling...");
                        foreach (var element in elements)
                        {
                            for (var i = 0; i < 3; i++)
                            {
                                element.from[i] = Math.Max(element.from[i] - changedAmount*element.from[i], 0);
                            }
                            for (var i = 0; i < 3; i++)
                            {
                                element.to[i] = Math.Max(element.to[i] - changedAmount*element.to[i], 0);
                            }
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
            }
        }
    }
}