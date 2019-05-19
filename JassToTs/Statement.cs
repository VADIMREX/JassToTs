using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jass
{
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