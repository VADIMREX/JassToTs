using System;
using System.IO;

namespace JassToTs
{
    class Program
    {
        static void Main(string[] args)
        {
            if (0 == args.Length) args = new[] { Console.ReadLine() };
            var source = "";
            using (var sr = new StreamReader(args[0]))
                source = sr.ReadToEnd();
            var lexer = new Jass.JassLexer();
            var parser = new Jass.JassParser();
#if !DEBUG
            try
            {
#endif
                Console.WriteLine("start lexer");
                var tokens = lexer.Tokenize(source);
                Console.WriteLine("start parser");
                var ast = parser.Parse(tokens);
                //foreach (var tok in ast)
                //    Console.WriteLine(tok);
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
#endif
            Console.ReadKey();
        }
    }
}
