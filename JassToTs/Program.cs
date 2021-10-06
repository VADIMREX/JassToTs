using System;
using System.Collections.Generic;
using System.IO;

namespace JassToTs
{
    enum Language
    {
        TypeScript,
        TypeScriptDeclaration,
        Lua,
        GalaxyRaw
    }

    class Program
    {
        const string help =
@"usable arguments:
-input           read arguments from user input
-i   <file path> set input file
-o   <file path> set output file 
-ot  <file path> set output tree file
-nt              for saving tree file
-ydwe            for compatibility with YDWE jass
-op              for optimiztion
-dts             d.ts mode, make *.d.ts instead of *.ts file
-t               tree mode, will save tree file
-lua             set output language to lua (default language typescript)
-galaxy-raw      set output language to galaxy (not final version)
-h               show this message
-lenient         less strict mode
";

        static string inPath = "";
        static string outPath = "";
        static string outTree = "";
        static Language language;
        static bool isTreeNeeded = false;
        static bool isOptimizationNeeded = false;
        static bool isYdweCompatible = false;

        static void TranslateFile(string ipath, string opath, string tpath)
        {
            Console.WriteLine("JASS to TypeScript translator (by VADIMREX)\n");

            var lexer = new Jass.JassLexer(isYdweCompatible);
            var parser = new Jass.JassParser(isYdweCompatible);
            
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
            var script = "";
            switch(language)
            {
                case Language.TypeScript:
                case Language.TypeScriptDeclaration:
                    var tsConverter = new JassToTs(isOptimizationNeeded, language == Language.TypeScriptDeclaration);
                    script = tsConverter.Convert(tree);
                    break;
                case Language.Lua:
                    var luaConverter = new JassToLua(isOptimizationNeeded);
                    script = luaConverter.Convert(tree);
                    break;
                case Language.GalaxyRaw:
                    var galaxyRawConverter = new JassToGalaxyRaw(isOptimizationNeeded);
                    script = galaxyRawConverter.Convert(tree);
                    break;
            }
            Console.WriteLine($"saving into {opath}");
            using (var sw = new StreamWriter(opath))
                sw.WriteLine(script);
        }

        static int Main(string[] args)
        {
            language = Language.TypeScript;
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
                    case "-nt": isTreeNeeded = true; continue;
                    case "-ydwe": isYdweCompatible = true; continue;
                    case "-op": isOptimizationNeeded = true; continue;
                    case "-dts": language = Language.TypeScriptDeclaration; continue;
                    case "-t": language = Language.TypeScriptDeclaration; continue;
                    case "-lua": language = Language.Lua; continue;
                    case "-galaxy-raw": language = Language.GalaxyRaw; continue;
                    case "-lenient": 
                        Jass.JassException.IsStrict = false;
                        JassTranslatorException.IsStrict = false;
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
                    var oPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath));
                    switch (language)
                    {
                        case Language.TypeScript: outPath += ".ts"; break;
                        case Language.TypeScriptDeclaration: outPath += ".d.ts"; break;
                        case Language.Lua: outPath += ".lua"; break;
                        case Language.GalaxyRaw: outPath += ".galaxy"; break;
                    }
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

            if ("" == outPath)
            {
                outPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath));
                switch (language)
                {
                    case Language.TypeScript: outPath += ".ts"; break;
                    case Language.TypeScriptDeclaration: outPath += ".d.ts"; break;
                    case Language.Lua: outPath += ".lua"; break;
                    case Language.GalaxyRaw: outPath += ".galaxy"; break;
                }
            }
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
