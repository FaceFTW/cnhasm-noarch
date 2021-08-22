using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;


namespace chnasm {
    class Program {
        static int Main(string[] _args) {
            switch(_args.Length) {
                case 0:
                case 1:
                default:
                    Console.Error.WriteLine("Error Not Enough/Too Many Arguments Provided");
                    Help();
                    break;
                case 2:
                    break;
            }

            List<String> asmIn = new List<string>();
            FileInfo asmFileInfo = new FileInfo(_args[0]);
            FileInfo binFileInfo = new FileInfo(_args[1]);
            Parser parser = new Parser(asmIn);
            try {
                StreamReader reader = new StreamReader(asmFileInfo.FullName);
                do {
                    parser.AsmText.Add(reader.ReadLine());
                } while(reader.Peek() != -1);

                //If we made it here, we should have a nice and full list containing lines from source
                parser.RemoveNonCodeData();
                parser.FindKernCode();
                parser.FindProgCode();
                parser.FindSymbols();

                //Now we need to do some actual processing
                parser.DoParse();

                //We should now have an output
                using(BinaryWriter writer = new BinaryWriter(File.OpenWrite(binFileInfo.FullName))) {
                    for(int i = 0; i < parser._kernBin.Length; i++) {
                        //Do some endianness flipping because binarywriter is little endian
                        ushort littleboi = (ushort)((parser._kernBin[i] & 0xFFU) << 8 | (parser._kernBin[i] & 0xFF00U) >> 8);
                        writer.Write(littleboi);
                    }

                    for(int i = 0; i < parser._progBin.Length; i++) {
                        ushort littleboi = (ushort)((parser._progBin[i] & 0xFFU) << 8 | (parser._progBin[i] & 0xFF00U) >> 8);
                        writer.Write(littleboi);
                    }
                }

                //    Console.WriteLine("Wow it actually works :)");
            } catch(Exception e) {
                Console.WriteLine(e);
                throw;
            }

            return 0;

        }

        public static void Help() {
            Console.WriteLine("");
            Console.WriteLine("Usage: chnasm <asm_file> <output_name>");
            Console.WriteLine("Required Arguments");
            Console.WriteLine("==============================");
            Console.WriteLine("<asm_file>: The location of the assembly source file to do magic on");
            Console.WriteLine("<output_name>: Name/Path of the output file (Default Path will be in current directory of assembler)");
            Console.WriteLine();
            //Console.WriteLine("Optional Arguments");
            //Console.WriteLine("==============================");
            //Console.WriteLine("-h: Show this nice little help stub");
            //Console.WriteLine("-v: Show some verbose output (Assembler Steps w/o Specific data");
            //Console.WriteLine("-X: Show extremely verbose output (Exact assembler steps)");
        }
    }




}
