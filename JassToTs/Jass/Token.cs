using System;
using System.Collections.Generic;
using System.Text;

namespace Jass
{
    /// <summary> Виды токенов </summary>
    class TokenKind
    {
        #region виды токенов

        /// <summary> однострочный комментарий </summary>
        public const string lcom = "lcom";
        /// <summary> многострочный комментарий (в JASS отсутствуют)</summary>
        [Obsolete("в JASS только однострочные комментарии начинающиеся с //")]
        public const string mcom = "mcom";

        /// <summary> десятичное целое </summary>
        public const string ndec = "ndec";
        /// <summary> восьмеричное целое </summary>
        public const string oct = "oct";
        /// <summary> шеснадцатиричное целое в формате 0xNN </summary>
        public const string xhex = "xhex";
        /// <summary> шеснадцатиричное целое в формате $NN </summary>
        public const string dhex = "dhex";
        /// <summary> действительное </summary>
        public const string real = "real";
        /// <summary> шеснадцатиричное из 4х ASCII символов, записавыается в апостравах </summary>
        public const string adec = "adec";

        /// <summary> оператор </summary>
        public const string oper = "oper";

        /// <summary> строка в кавычках (в JASS единственный тип строк) </summary>
        public const string dstr = "dstr";
        /// <summary> строка в апострофах (, в JASS в апострафах хранятся целые, смотри <see cref="adec"/>) </summary>
        [Obsolete("не используется, в JASS единственный тип строк: заключённые в кавычках")]
        public const string sstr = "sstr";

        /// <summary> идентификатор </summary>
        public const string name = "name";

        /// <summary> базовый тип </summary>
        public const string btyp = "btyp";
        /// <summary> ключевое слово </summary>
        public const string kwd = "kwd";

        /// <summary> null значение </summary>
        public const string @null = "null";
        /// <summary> булевое значение </summary>
        public const string @bool = "bool";

        /// <summary> перевод строки </summary>
        public const string ln = "ln";

        /// <summary> левая скобка </summary>
        public const string lbra = "lbra";
        /// <summary> правая скобка </summary>
        public const string rbra = "rbra";
        /// <summary> левая квадратная скобка </summary>
        public const string lind = "lind";
        /// <summary> правая квадратная скобка </summary>
        public const string rind = "rind";

        /// <summary> макросы YDWE, пока что преобразуются в комментарий </summary>
        public const string ymacr = "ymacr";

        #endregion

        /// <summary> связь между видом и типом </summary>
        static Dictionary<string, string> TypeByKind = new Dictionary<string, string>
        {
            { lcom,  TokenType.comm },
            { mcom,  TokenType.comm },
            { ndec,  TokenType.val },
            { oct,   TokenType.val },
            { xhex,  TokenType.val },
            { dhex,  TokenType.val },
            { real,  TokenType.val },
            { adec,  TokenType.val },
            { oper,  TokenType.oper },
            { dstr,  TokenType.val },
            { sstr,  TokenType.val },
            { name,  TokenType.name },
            { btyp,  TokenType.name },
            { kwd,   TokenType.kwd },
            { @null, TokenType.val },
            { @bool, TokenType.val },
            { ln,    TokenType.br },
            { lbra,  TokenType.par },
            { rbra,  TokenType.par },
            { lind,  TokenType.par },
            { rind,  TokenType.par },
            // макросы считаем за коментарии
            { ymacr, TokenType.comm },
        };

        /// <summary> получить тип </summary>
        /// <param name="kind"> вид токена </param>
        public static string GetType(string kind) => TypeByKind[kind];
    }

    /// <summary> Типы токенов </summary>
    class TokenType
    {
        /// <summary> разделитель </summary>
        public const string br = "br";
        /// <summary> значение (безымянная константа) </summary>
        public const string val = "val";
        /// <summary> ключевое слово </summary>
        public const string kwd = "kwd";
        /// <summary> идентификатор </summary>
        public const string name = "name";
        /// <summary> оператор </summary>
        public const string oper = "oper";
        /// <summary> скобки </summary>
        public const string par = "par";
        /// <summary> комментарий </summary>
        public const string comm = "comm";
    }

    /// <summary> Токен </summary>
    class Token
    {
        public string Type => TokenKind.GetType(Kind);
        public string Kind = "";
        public int Line = 0;
        public int Col = 0;
        public int Pos = 0;
        public string Text = "";
        public override string ToString() => $"{Line},{Col} [{Type}|{Kind}]: {Text}";
        public Token Clone() => new Token { Kind = Kind, Line = Line, Col = Col, Pos = Pos, Text = Text };
    }
}
