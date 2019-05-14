using System;
using System.Collections.Generic;
using System.Text;

namespace Jass
{
    class Statement
    {
        public static string language = "jass";

        public Token Start;
        public string Type;
        public Statement Parent;
        public List<Statement> Childs = new List<Statement>();

        public virtual string ToJass() => $"// {Type} {Start}\n";
        public virtual string ToTypeScript() => $"// {Type} {Start}\n";

        public override string ToString() => "jass" == language ? ToJass() :
                                             "ts" == language ? ToJass() :
                                             base.ToString();
    }

    class TypeDeclaration : Statement
    {
        public Token BaseType;
        public Token Name;
        public override string ToJass() => $"type {Name.Text} extends {BaseType.Text}\n";
        public override string ToTypeScript() => $"class {Name.Text} : {BaseType.Text} {'{'} {'}'}\n";
    }

    class Globals : Statement
    {
        public override string ToJass() {
            var str = "globals\n";
            foreach (var s in Childs)
                str += s.ToJass() + "\n";
            str += "endglobals\n";
            return str;
        }
        public override string ToTypeScript() {
            var str = "// globals\n";
            foreach (var s in Childs)
                str += s.ToTypeScript() + "\n";
            str += "// endglobals\n";
            return str;
        }
    }

    class VarDeclaration : Statement
    {
        public Token VarType;
        public Token Name;
        public bool IsConst;
        public Statement InitialValue;

        public override string ToJass() => (IsConst ? "constant " : "") + $"{VarType.Text} {Name.Text}" + (null != InitialValue ? " = " + InitialValue.ToJass() : "") + "\n";
        public override string ToTypeScript() => (IsConst ? "const " : "") + $"var {Name.Text}: {VarType.Text}" + (null != InitialValue ? " = " + InitialValue.ToJass() : "") + ";\n";
    }

    class JassParser
    {
        List<Token> tokens;
        /// <summary> глобальная позиция в списке токенов </summary>
        int i;

        List<string> types;

        #region type keyword
        
        /// <summary> Попытаться распарсить объявление типа </summary>
        /// <param name="parent"> Родительский узел </param>
        Statement TryParseTypeDecl(Statement parent)
        {
            var stat = new TypeDeclaration { Type = "type", Parent = parent };

            int j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (Token.lcom == tokens[i].Type) continue;
                if (j < 4 && Token.ln == tokens[i].Type)
                    throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: linebreak");
                switch (j)
                {
                    case 0:
                        if (Token.kwd != tokens[i].Type || "type" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
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
                    case 4:
                        if (Token.ln != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: new line expected");
                        j++;
                        break;
                }
            }
            if (i < tokens.Count) i--;
            return stat;
        }

        #endregion

        #region globals keyword

        /// <summary> Попытаться распарсить раздел с глобальными переменными </summary>
        /// <param name="parent"> Родительский узел </param>
        Statement TryParseGlobals(Statement parent)
        {
            var stat = new Globals { Type = "globals", Parent = parent };

            int j = 0;
            for (; i < tokens.Count && j < 4; i++)
            {
                if (Token.lcom == tokens[i].Type) continue;
                switch (j)
                {
                    case 0:
                        if (Token.kwd != tokens[i].Type || "globals" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
                        j++;
                        break;
                    case 1:
                    case 3:
                        if (Token.ln != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong globals declaration: new line expected");
                        j++;
                        break;
                    case 2:
                        if (Token.ln == tokens[i].Type) continue;
                        if (Token.kwd == tokens[i].Type && "endglobals" == tokens[i].Text)
                        {
                            j++;
                            continue;
                        }
                        var vardecl = TryParseGlobalVarDecl(stat);
                        stat.Childs.Add(vardecl);
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсить объявление глобальной переменной или константы </summary>
        /// <param name="parent"> Родительский узел </param>
        Statement TryParseGlobalVarDecl(Statement parent)
        {
            Statement stat;
            //('constant' type id '=' expr newline | var_declr newline)*
            if (Token.kwd == tokens[i].Type && "constant" == tokens[i].Text)
            {
                stat = TryParseGlobalConst(parent);
                stat.Type = "gconst";
            }
            else
            {
                stat = TryParseVarDecl(parent);
                stat.Type = "gvar";
            }
            return stat;
        }

        /// <summary> Попытаться распарсить глобальную константу </summary>
        /// <param name="parent"> Родительский узел </param>
        Statement TryParseGlobalConst(Statement parent)
        {
            // 'constant' type id '=' expr newline
            var stat = new VarDeclaration { Type = "var decl", Parent = parent };

            int j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (Token.lcom == tokens[i].Type) continue;
                switch (j)
                {
                    case 0:
                        if (Token.kwd != tokens[i].Type || "constant" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
                        stat.IsConst = true;
                        j++;
                        break;
                    case 1:
                        if (Token.name != tokens[i].Type && Token.btyp != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: type identifier expected");
                        stat.VarType = tokens[i];
                        j++;
                        break;
                    case 2:
                        if (Token.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: identifier expected");
                        stat.Name = tokens[i];
                        j++;
                        break;
                    case 3:
                        if (Token.oper != tokens[i].Type || "=" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: = operator expected");
                        j++;
                        break;
                    case 4:
                        if (Token.ln == tokens[i].Type)
                            //throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: new line expected");
                        j++;
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        #endregion

        /// <summary> Попытаться распарсить объявление переменной </summary>
        /// <param name="parent"> Родительский узел </param>
        Statement TryParseVarDecl(Statement parent)
        {
            // type id ('=' expr)? | type 'array' id 
            var stat = new VarDeclaration { Type = "var decl", Parent = parent };

            for (; i < tokens.Count; i++)
            {
                if (Token.ln == tokens[i].Type) break;
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        Statement TryParseExpression(Statement parent)
        {
            //('constant' type id '=' expr newline | var_declr newline)*
            var stat = new VarDeclaration { Type = "var decl", Parent = parent };

            for (; i < tokens.Count; i++)
            {
                if (Token.ln == tokens[i].Type) break;
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        Statement TryParseNative(Statement parent)
        {
            var stat = new Statement { Type = "native", Parent = parent };

            for (; i < tokens.Count; i++)
            {
                if (Token.ln == tokens[i].Type) break;
            }

            return stat;
        }

        Statement TryParseFunction(Statement parent)
        {
            var stat = new Statement { Type = "function", Parent = parent };

            for (; i < tokens.Count; i++)
            {
                if (Token.ln == tokens[i].Type) break;
            }

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
                    if (isDeclPassed) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: not in declaration block");
                    stat = TryParseTypeDecl(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
                if ("globals" == tokens[i].Text)
                {
                    if (isDeclPassed) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong global declaration: not in declaration block");
                    stat = TryParseGlobals(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
                if ("native" == tokens[i].Text)
                {
                    if (isDeclPassed) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong native declaration: not in declaration block");
                    stat = TryParseNative(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
                
                //if ("constant" == tokens[i].Text)
                //{
                //    if (!isDeclPassed) isDeclPassed = true;
                //    stat = TryParseFunction(ast);
                //    ast.Childs.Add(stat);
                //    continue;
                //}
                if ("function" == tokens[i].Text)
                {
                    if (!isDeclPassed) isDeclPassed = true;
                    stat = TryParseFunction(ast);
                    ast.Childs.Add(stat);
                    continue;
                }
            }
            return ast;
        }
    }
}
