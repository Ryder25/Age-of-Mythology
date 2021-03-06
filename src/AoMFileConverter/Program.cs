﻿using AoMEngineLibrary.Anim;
using AoMEngineLibrary.Graphics.Brg;
using AoMEngineLibrary.Graphics.Prt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AoMFileConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("--- " + Properties.Resources.AppTitleLong + " ---");
                if (args.Length == 0)
                {
                    Console.WriteLine("No input arguments were found!");
                    Console.WriteLine($"Drag and drop one or more files on the EXE to convert.{Environment.NewLine}Supported: anim.txt, ddt, bti, cub, prt, mtrl, brg");
                }

                foreach (string f in args)
                {
                    try
                    {
                        Console.WriteLine("Processing " + Path.GetFileName(f) + "...");

                        Convert(f);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to convert the file!");
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                }
            }
            finally
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }


        private static void Convert(string f)
        {
            string magic;
            using (FileStream fs = File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BrgBinaryReader reader = new BrgBinaryReader(fs);
                magic = reader.ReadString(4);
            }

            if (f.EndsWith("anim.txt"))
            {
                AnimFile.ConvertToXml(File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read), File.Open(f + ".xml", FileMode.Create, FileAccess.Write, FileShare.Read));
                Console.WriteLine("Success! Anim converted.");
            }
            else if (f.EndsWith(".ddt"))
            {
                TextureDecoder.Decode(f, Path.GetDirectoryName(f) ?? string.Empty);
                Console.WriteLine("Success! Ddt converted.");
            }
            else if (f.EndsWith(".bti"))
            {
                TextureEncoder.Encode(f, Path.GetDirectoryName(f) ?? string.Empty);
                Console.WriteLine("Success! Bti converted.");
            }
            else if (f.EndsWith(".cub"))
            {
                TextureEncoder.Encode(f, Path.GetDirectoryName(f) ?? string.Empty);
                Console.WriteLine("Success! Cub converted.");
            }
            else if (f.EndsWith(".prt"))
            {
                PrtFile file = new PrtFile(File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read));
                file.SerializeAsXml(File.Open(f + ".xml", FileMode.Create, FileAccess.Write, FileShare.Read));
                Console.WriteLine("Success! Prt converted.");
            }
            else if (magic == "MTRL")
            {
                MtrlFile file = new MtrlFile();
                file.Read(File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read));
                file.SerializeAsXml(File.Open(f + ".xml", FileMode.Create, FileAccess.Write, FileShare.Read));
                Console.WriteLine("Success! Mtrl converted.");
            }
            else if (magic == "BANG")
            {
                string brgMtrlOutputPath = Path.Combine(Path.GetDirectoryName(f) ?? string.Empty, "materials");
                if (!Directory.Exists(brgMtrlOutputPath))
                {
                    Directory.CreateDirectory(brgMtrlOutputPath);
                }

                BrgFile file = new BrgFile(File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read));
                for (int i = 0; i < file.Materials.Count; ++i)
                {
                    MtrlFile mtrl = new MtrlFile(file.Materials[i]);
                    using (var fs = File.Open(Path.Combine(brgMtrlOutputPath, Path.GetFileNameWithoutExtension(f) + "_" + i + ".mtrl"),
                        FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        mtrl.Write(fs);
                    }
                }
                Console.WriteLine("Success! Mtrl files created.");
            }
            else
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(f);

                if (xmlDoc?.DocumentElement?.Name == "AnimFile")
                {
                    AnimFile.ConvertToAnim(File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read), File.Open(f + ".txt", FileMode.Create, FileAccess.Write, FileShare.Read));
                    Console.WriteLine("Success! Anim converted.");
                }
                else if (xmlDoc?.DocumentElement?.Name == "ParticleFile")
                {
                    PrtFile file = PrtFile.DeserializeAsXml(File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read));
                    file.Write(File.Open(f + ".prt", FileMode.Create, FileAccess.Write, FileShare.Read));
                    Console.WriteLine("Success! Prt converted.");
                }
                else
                {
                    MtrlFile file = MtrlFile.DeserializeAsXml(File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read));
                    using (var fs = File.Open(f + ".mtrl", FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        file.Write(fs);
                    }
                    Console.WriteLine("Success! Mtrl converted.");
                }
            }
        }
    }
}
