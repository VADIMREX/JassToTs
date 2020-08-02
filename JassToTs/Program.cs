using System;
using System.Collections.Generic;
using System.IO;

namespace JassToTs
{
    class Program
    {
        const string help =
@"usable arguments:
-input           read arguments from user input
-i   <file path> set input file
-o   <file path> set output file 
-ot  <file path> set output tree file
-dts             d.ts mode, make *.d.ts instead of *.ts file
-t               tree mode, will save tree file
-h               show this message
";

        static string inPath = "";
        static string outPath = "";
        static string outTree = "";
        static bool isDTS = false;
        static bool isLua = true;
        static bool isTreeNeeded = false;

        static void TranslateFile(string ipath, string opath, string tpath)
        {
            Console.WriteLine("JASS to TypeScript translator (by VADIMREX)\n");

            var lexer = new Jass.JassLexer();
            var parser = new Jass.JassParser();
            
            Console.WriteLine($"reading file {ipath}");
            string source;
            using (var sr = new StreamReader(ipath))
                source = sr.ReadToEnd();

            Console.WriteLine("lexeing");
            var tokens = lexer.Tokenize(source);

            Console.WriteLine("parsing");
            var tree = parser.Parse(tokens);

            if ("" != tpath)
            {
                Console.WriteLine($"saving tree into {tpath}");
                using (var sw = new StreamWriter(tpath))
                    sw.WriteLine(tree.ToString());
            }

            Console.WriteLine("translating");
            var ts = "";
            if (isLua)
            {
                var converter = new JassToLua();
                ts = converter.Convert(tree);
            }
            else
            {
                var converter = new JassToTs(isDTS);
                ts = converter.Convert(tree);
            }
            Console.WriteLine($"saving into {opath}");
            using (var sw = new StreamWriter(opath))
                sw.WriteLine(ts);
        }

        static int Main(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-input":
                        Console.WriteLine("please enter arguments, empty line for continue");
                        var lst = new List<string>();
                        int j = 0;
                        while (j < 2)
                        {
                            var s = Console.ReadLine();
                            if ("" == s)
                            {
                                j++;
                                continue;
                            }
                            j = 0;
                            lst.Add(s);
                        }
                        args = lst.ToArray();
                        i = -1;
                        continue;
                    case "-i": if (i + 1 == args.Length) break; inPath = args[i + 1]; i++; continue;
                    case "-o": if (i + 1 == args.Length) break; outPath = args[i + 1]; i++; continue;
                    case "-ot": if (i + 1 == args.Length) break; outTree = args[i + 1]; i++; continue;
                    case "-dts": isDTS = true; continue;
                    case "-t": isDTS = true; continue;
                    case "-lua":
                        isLua = true;
                        continue;
                    case "-h":
                        Console.WriteLine(help);
                        return 0;
                    default: continue;
                }
                return -1;
            }

            if ("" == inPath)
            {
                var di = new DirectoryInfo(AppContext.BaseDirectory);
                foreach (var fi in di.GetFiles("*.j|*.ai"))
                {
                    var iPath = fi.FullName;
                    var oPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + (isDTS ? ".d.ts" : ".ts"));
                    var tPath = "";
                    if (isTreeNeeded)
                        tPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + ".tree");

                    try
                    {
                        TranslateFile(iPath, oPath, tPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                return 0;
            }

            if ("" == outPath) outPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + (isDTS ? ".d.ts" : ".ts"));
            if (isTreeNeeded && "" == outTree) outTree = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + ".tree");

#if !DEBUG
            try
#endif
            {
                TranslateFile(inPath, outPath, outTree);
            }
#if !DEBUG
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
#endif
            return 0;
        }
    }
}
