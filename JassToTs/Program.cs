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
            try
            {
                Console.WriteLine("start lexer");
                var tokens = lexer.Tokenize(source);
                Console.WriteLine("start parser");
                var ast = parser.Parse(tokens);
                //foreach (var tok in ast)
                //    Console.WriteLine(tok);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }
    }
}
