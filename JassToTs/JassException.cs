using System;

namespace Jass
{
    public class JassException : Exception
    {
        static bool isStrict = true;
        public static bool IsStrict { get => isStrict; set => isStrict = value; }
        static string formatMessage(int line, int col, string message) => $"Line {line}, Col {col}: {message}";

        public static void Error(int line, int col, string message) 
        {
            if (isStrict) throw new JassException(line, col, message);
            Console.WriteLine(formatMessage(line, col, message));
        }
        
        public JassException(int line, int col, string message) : base(formatMessage(line, col, message)) {}
    }
}