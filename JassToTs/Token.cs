using System;
using System.Collections.Generic;
using System.Text;

namespace Jass
{
    class Token
    {
        public string Type = "";
        public int Line = 0;
        public int Col = 0;
        public int Pos = 0;
        public string Text = "";
        public override string ToString() => $"{Line},{Col} [{Type}]: {Text}";
    }
}
