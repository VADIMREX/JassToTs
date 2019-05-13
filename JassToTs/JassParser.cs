using System;
using System.Collections.Generic;
using System.Text;

namespace Jass
{
    class Statement
    {
        public string Type;
        public Statement Parent;
        public List<Statement> Childs = new List<Statement>();
    }

    class TypeDeclaration : Statement
    {
        public Token BaseType;
        public Token Name;
        public override string ToString() => $"type {Name.Text} extends {BaseType.Text}";
    }

    class JassParser
    {
        List<Token> tokens;
        /// <summary> глобальная позиция в списке токенов </summary>
        int i;

        List<string> types;

        /// <summary> Попытаться распарсить объявление типа </summary>
        /// <param name="parent"> Родительский узел </param>
        Statement TryParseTypeDecl(Statement parent)
        {
            var stat = new TypeDeclaration { Type = "type", Parent = parent };

            int j = 0;
            for (; i < tokens.Count && j < 4; i++)
            {
                if (Token.lcom == tokens[i].Type) continue;
                if (Token.ln == tokens[i].Type)
                    throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: linebreak");
                switch (j)
                {
                    case 0:
                        if (Token.kwd != tokens[i].Type || "type" != tokens[i].Text) return null;
                        j++;
                        break;
                    case 1:
                        if (Token.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: identifier expected");
                        stat.Name = tokens[i];
                        j++;
                        break;
                    case 2:
                        if (Token.kwd != tokens[i].Type || "extends" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: extends keyword expected");
                        j++;
                        break;
                    case 3:
                        if (Token.name != tokens[i].Type && Token.btyp != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: type identifier expected");
                        j++;
                        stat.BaseType = tokens[i];
                        break;
                }
            }
            if (i < tokens.Count) i--;
            return stat;
        }

        Statement TryParseGlobals(Statement parent)
        {
            var stat = new Statement { Type = "globals", Parent = parent };
            return stat;
        }

        Statement TryParseNative(Statement parent)
        {
            var stat = new Statement { Type = "native", Parent = parent };
            return stat;
        }

        Statement TryParseFunction(Statement parent)
        {
            var stat = new Statement { Type = "function", Parent = parent };
            return stat;
        }

        public Statement Parse(List<Token> tokens)
        {
            this.tokens = tokens;
            types = new List<string>();
            var ast = new Statement { Type = "program" };
            bool isDeclPassed = false;
            Statement stat = null;
            for (i = 0; i < tokens.Count; i++)
            {
                if (Token.lcom == tokens[i].Type) continue;
                if (Token.ln == tokens[i].Type) continue;

                if (Token.kwd != tokens[i].Type) new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: error: keyword expected");

                if ("type" == tokens[i].Text)
                {
                    if (!isDeclPassed) new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: not in declaration block");
                    stat = TryParseTypeDecl(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
                if ("globals" == tokens[i].Text)
                {
                    if (!isDeclPassed) new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong global declaration: not in declaration block");
                    stat = TryParseTypeDecl(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
                if ("native" == tokens[i].Text)
                {
                    if (!isDeclPassed) new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong native declaration: not in declaration block");
                    stat = TryParseTypeDecl(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
                
                if ("constant" == tokens[i].Text)
                {
                    if (!isDeclPassed) isDeclPassed = true;
                    stat = TryParseTypeDecl(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
                if ("function" == tokens[i].Text)
                {
                    if (!isDeclPassed) isDeclPassed = true;
                    stat = TryParseTypeDecl(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
            }
            return ast;
        }
    }
}
