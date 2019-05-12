using System;
using System.IO;

namespace JassToTs
{
    class Program
    {
        static void Main(string[] args)
        {
            var source = "";
            using (var sr = new StreamReader("common.j"))
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
