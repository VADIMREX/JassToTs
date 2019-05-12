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
            var helper = new Jass.JassLexer();
            try
            {
                var tokens = helper.Tokenize(source);
                foreach (var tok in tokens)
                    Console.WriteLine(tok);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }
    }
}
