using System;

namespace JassToTs {
    public class JassTranslatorException : Exception {
        static bool isStrict = true;
        public static bool IsStrict { get => isStrict; set => isStrict = value; }

        public static void Error(string message)
        {
            if (isStrict) throw new JassTranslatorException(message);
            Console.WriteLine(message);
        }
        public JassTranslatorException(string message): base(message) {}
    }
}