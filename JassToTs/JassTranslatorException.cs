using System;

namespace JassToTs {
    public class JassTranslatorException : Exception {
        public JassTranslatorException(string message): base(message) {}
    }
}