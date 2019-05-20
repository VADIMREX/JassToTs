using System;
using System.IO;

namespace JassToTs
{
    class Program
    {
        static void Main(string[] args)
        {
            if (0 == args.Length) args = new[] { Console.ReadLine(), Console.ReadLine(), Console.ReadLine() };
            var path = args[0];
            var newPath = "";
            var treePath = "";
            if (args.Length > 1) newPath = args[1];
            if (args.Length > 2) treePath = args[2];
            if ("" == newPath) newPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".d.ts");
            if ("" == treePath) treePath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".tree");
            var source = "";
            using (var sr = new StreamReader(path))
                source = sr.ReadToEnd();
            var lexer = new Jass.JassLexer();
            var parser = new Jass.JassParser();
            var converter = new JassToTs();
#if !DEBUG
            try
#endif
            {
                Console.WriteLine("start lexer");
                var tokens = lexer.Tokenize(source);
                Console.WriteLine("start parser");
                var tree = parser.Parse(tokens);
                Console.WriteLine("save tree");
                using (var sw = new StreamWriter(treePath))
                    sw.WriteLine(tree.ToString());
                Console.WriteLine("start converter");
                var ts = converter.Convert(tree);
                Console.WriteLine("save result");
                using (var sw = new StreamWriter(newPath))
                    sw.WriteLine(ts);
            }
#if !DEBUG
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
#endif
            return;
        }
    }
}
