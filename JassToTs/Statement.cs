using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jass
{
    class StatementType
    {
        /// <summary> Программа </summary>
        public const string Prog = "Prog";

        public const string TypeDecl   = "TypeDecl";
        public const string Glob       = "Glob";
        public const string GConst     = "GConst";
        public const string GVar       = "GVar";
        public const string GArr       = "GArr";
        public const string LVar       = "LVar";
        public const string LArr       = "LArr";
        public const string Expr       = "Expr";
        public const string RVar       = "RVar";
        public const string FCall      = "FCall";
        public const string RArr       = "RArr";
        public const string RFunc      = "RFunc";
        public const string Native     = "Native";
        public const string CNative    = "CNative";
        public const string FuncDecl   = "FuncDecl";
        public const string Params     = "Params";
        public const string Func       = "Func";
        public const string CFunc      = "CFunc";
        public const string FuncLocals = "FuncLocals";
        public const string FuncBody   = "FuncBody";
        public const string Comm       = "Comm";
        public const string Debug      = "Debug";
        public const string Set        = "Set";
        public const string ASet       = "ASet";
        public const string If         = "If";
        public const string Cond       = "Cond";
        public const string Then       = "Then";
        public const string ElseCond   = "ElseCond";
        public const string Else       = "Else";
        public const string Loop       = "Loop";
        public const string Exit       = "Exit";
        public const string Return     = "Return";
    }

    class Statement
    {
        public string Type;
        public Token Start;
        public Statement Parent;
        public List<Statement> Childs = new List<Statement>();

        /// <summary> Создать и добавить дочернюю инструкцию </summary>
        /// <param name="type"> тип инструкции </param>
        /// <param name="token"> токен с которого начинается потомок </param>
        /// <returns> дочернюю инструкцию </returns>
        /// <seealso cref="AddChild(string,Token)"/>
        public Statement MakeChild(string type, Token token)
        {
            var child = new Statement { Parent = this, Type = type, Start = token };
            Childs.Add(child);
            return child;
        }

        /// <summary> Создать и добавить дочернюю инструкцию </summary>
        /// <param name="type"> тип инструкции </param>
        /// <param name="token"> токен с которого начинается потомок </param>
        /// <returns> себя </returns>
        /// <seealso cref="MakeChild(string,Token)"/>
        public Statement AddChild(string type, Token token)
        {
            MakeChild(type, token);
            return this;
        }

        /// <summary> Добавить дочернюю инструкцию </summary>
        /// <param name="child"> дочерняя инструкция </param>
        /// <returns> себя </returns>
        public Statement AddChild(Statement child)
        {
            child.Parent = this;
            Childs.Add(child);
            return this;
        }

        public override string ToString() => ToString(0);
        public string ToString(int ident) =>
            $"// {"".PadLeft(ident, ' ')}{Type} {Start}\n" +
            (Childs.Count > 0 ?
                Childs.Select(x => x.ToString(ident + 2)).Aggregate((x, y) => $"{x}{y}") :
                ""
            );
    }

}