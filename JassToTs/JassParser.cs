using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Jass
{
    /// <summary> Базовый класс инструкции </summary>
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

    /// <summary> программа </summary>
    class ProgramStatement : Statement
    {
        public ProgramStatement() => Type = "program";
    }

    /// <summary> Объявление типа </summary>
    class TypeDeclaration : Statement
    {
        public string BaseType = null;
        public string Name;
        public override string ToJass() => $"type {Name}" + (null != BaseType ? $" extends {BaseType}\n" : "\n");
        public override string ToTypeScript() => $"class {Name}" + (null != BaseType ? $" extends {BaseType} {'{'} {'}'}\n" : " { }\n");
    }

    /// <summary> Раздел с глобальными переменными </summary>
    class Globals : Statement
    {
        public override string ToJass()
        {
            var str = "globals\n";
            foreach (var s in Childs)
                str += s.ToJass() + "\n";
            str += "endglobals\n";
            return str;
        }
        public override string ToTypeScript()
        {
            var str = "// globals\n";
            foreach (var s in Childs)
                str += s.ToTypeScript() + "\n";
            str += "// endglobals\n";
            return str;
        }
    }

    /// <summary> Объявление переменной </summary>
    class VarDeclaration : Statement
    {
        public string VarType;
        public string Name;
        public bool IsConst;
        public bool IsArray;
        public bool IsLocal;
        public bool IsParam;
        public Expression InitialValue;

        public override string ToJass() => string.Format("{0}{1}{2} {3}{4}\n",
                                                         IsConst ? "constant " : "",
                                                         VarType,
                                                         IsArray ? " array" : "",
                                                         Name,
                                                         null != InitialValue ? " = " + InitialValue.ToJass() : ""
                                           );

        public override string ToTypeScript() => string.Format("{0}{1}: {2}{3}{4};\n",
                                                               IsParam ? "" : IsConst ? "const " : IsLocal ? "let " : "var ",
                                                               Name,
                                                               VarType,
                                                               IsArray ? "[]" : "",
                                                               null != InitialValue ? " = " + InitialValue.ToJass() : ""
                                                 );
    }

    /// <summary> Объявление функции </summary>
    class FunctionDeclaration : Statement
    {
        public bool IsNative;
        public string Name;
        public Dictionary<string, VarDeclaration> Params = new Dictionary<string, VarDeclaration>();
        public string ReturnType;
        public Dictionary<string, VarDeclaration> LocalVariables = new Dictionary<string, VarDeclaration>();
    }

    /// <summary> Скобки </summary>
    class Parens : Expression
    {
        string template => TokenType.lbra == Start.Type ? "({0})" :
                           TokenType.lind == Start.Type ? "[{0}]" :
                           "{0}";
        public override string ToJass() => string.Format(template, base.ToJass());
        public override string ToTypeScript() => string.Format(template, base.ToTypeScript());
    }

    /// <summary> Выражение </summary>
    class Expression : Statement
    {
        public override string ToJass() => Childs.Select(x => x.ToJass()).Aggregate((x, y) => $"{x} {y}");
        public override string ToTypeScript() => Childs.Select(x => x.ToJass()).Aggregate((x, y) => $"{x} {y}");
    }

    /// <summary> наименьная еденица выражения </summary>
    class Atom : Statement
    {
        public override string ToJass() => Start.Text;
        public override string ToTypeScript() => Start.Text;
    }

    class JassParser
    {
        List<Token> tokens;
        /// <summary> глобальная позиция в списке токенов </summary>
        int i;

        Dictionary<string, TypeDeclaration> Types = new Dictionary<string, TypeDeclaration>
        {
            { "integer", new TypeDeclaration { Name = "integer" } },
            { "real",    new TypeDeclaration { Name = "real" } },
            { "boolean", new TypeDeclaration { Name = "boolean" } },
            { "string",  new TypeDeclaration { Name = "string" } },
            { "handle",  new TypeDeclaration { Name = "handle" } },
            { "code",    new TypeDeclaration { Name = "code" } },
        };
        Dictionary<string, VarDeclaration> GlobalVariables = new Dictionary<string, VarDeclaration>();

        Dictionary<string, FunctionDeclaration> Functions = new Dictionary<string, FunctionDeclaration>();

        #region type keyword

        /// <summary> Попытаться распарсить объявление типа </summary>
        /// <param name="parent"> Родительский узел </param>
        TypeDeclaration TryParseTypeDecl(Statement parent)
        {
            var stat = new TypeDeclaration { Type = "type", Parent = parent };

            int j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (TokenType.lcom == tokens[i].Type) continue;
                if (j < 4 && TokenType.ln == tokens[i].Type)
                    throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: linebreak");
                switch (j)
                {
                    case 0:
                        if (TokenType.kwd != tokens[i].Type || "type" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
                        j++;
                        break;
                    case 1:
                        if (TokenType.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: identifier expected");
                        stat.Name = tokens[i].Text;
                        j++;
                        break;
                    case 2:
                        if (TokenType.kwd != tokens[i].Type || "extends" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: extends keyword expected");
                        j++;
                        break;
                    case 3:
                        if (TokenType.name != tokens[i].Type && TokenType.btyp != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: type identifier expected");
                        j++;
                        if (!Types.ContainsKey(tokens[i].Text))
#warning сделать предупреждением
                            new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: base type not found");
                        stat.BaseType = tokens[i].Text;
                        break;
                    case 4:
                        if (TokenType.ln != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: new line expected");
                        j++;
                        break;
                }
            }
            if (i < tokens.Count) i--;
            Types.Add(stat.Name, stat);
            return stat;
        }

        #endregion

        #region globals keyword

        /// <summary> Попытаться распарсить раздел с глобальными переменными </summary>
        /// <param name="parent"> Родительский узел </param>
        Globals TryParseGlobals(Statement parent)
        {
            var stat = new Globals { Type = "globals", Parent = parent };

            int j = 0;
            for (; i < tokens.Count && j < 4; i++)
            {
                if (TokenType.lcom == tokens[i].Type) continue;
                switch (j)
                {
                    case 0:
                        if (TokenType.kwd != tokens[i].Type || "globals" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
                        j++;
                        break;
                    case 1:
                    case 3:
                        if (TokenType.ln != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong globals declaration: new line expected");
                        j++;
                        break;
                    case 2:
                        if (TokenType.ln == tokens[i].Type) continue;
                        if (TokenType.kwd == tokens[i].Type && "endglobals" == tokens[i].Text)
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
        VarDeclaration TryParseGlobalVarDecl(Statement parent)
        {
            VarDeclaration stat;
            if (TokenType.kwd == tokens[i].Type && "constant" == tokens[i].Text)
            {
                stat = TryParseGlobalConst(parent);
                stat.Type = "gconst";
            }
            else
            {
                stat = TryParseVarDecl(parent);
                stat.Type = "gvar";
            }
            if (GlobalVariables.ContainsKey(stat.Name))
#warning проверить реакцию jass
                throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: variable already declared");

            GlobalVariables.Add(stat.Name, stat);
            return stat;
        }

        /// <summary> Попытаться распарсить глобальную константу </summary>
        /// <param name="parent"> Родительский узел </param>
        VarDeclaration TryParseGlobalConst(Statement parent)
        {
            // 'constant' type id '=' expr newline
            var stat = new VarDeclaration { Type = "gconst", Parent = parent };

            int j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (TokenType.lcom == tokens[i].Type) continue;
                switch (j)
                {
                    case 0:
                        if (TokenType.kwd != tokens[i].Type || "constant" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
                        stat.IsConst = true;
                        j++;
                        break;
                    case 1:
                        if (TokenType.name != tokens[i].Type && TokenType.btyp != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: type identifier expected");
                        if (!Types.ContainsKey(tokens[i].Text))
#warning сделать предупреждением
                            new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: type not found");
                        stat.VarType = tokens[i].Text;
                        j++;
                        break;
                    case 2:
                        if (TokenType.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: identifier expected");
                        stat.Name = tokens[i].Text;
                        j++;
                        break;
                    case 3:
                        if (TokenType.oper != tokens[i].Type || "=" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: = operator expected");
                        i++;
                        var expr = TryParseExpression(stat);
                        stat.InitialValue = expr;
                        j++;
                        break;
                    case 4:
                        if (TokenType.ln == tokens[i].Type)
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
        VarDeclaration TryParseVarDecl(Statement parent, FunctionDeclaration context = null)
        {
            // type id ('=' expr)? | type 'array' id 
            var stat = new VarDeclaration { Type = "var", Parent = parent };

            int j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (TokenType.lcom == tokens[i].Type) continue;
                switch (j)
                {
                    // local не обязательно
                    case 0:
                        if (TokenType.kwd != tokens[i].Type || "local" != tokens[i].Text)
                        {
                            j++;
                            goto case 1;
                        }
                        stat.Start = tokens[i];
                        stat.IsLocal = true;
                        j++;
                        break;
                    //   тип
                    case 1:
                        if (TokenType.name != tokens[i].Type && TokenType.btyp != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: type identifier expected");
                        if (!Types.ContainsKey(tokens[i].Text))
#warning сделать предупреждением
                             new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: type not found");
                        if (null == stat.Start) stat.Start = tokens[i];
                        stat.VarType = tokens[i].Text;
                        j++;
                        break;
                    //     array не обязательно
                    case 2:
                        if (TokenType.kwd != tokens[i].Type || "array" != tokens[i].Text)
                        {
                            j++;
                            goto case 3;
                        }
                        stat.IsArray = true;
                        j++;
                        break;
                    //       имя
                    case 3:
                        if (TokenType.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: identifier expected");
                        stat.Name = tokens[i].Text;
                        j += stat.IsArray ? 2 : 1;
                        break;
                    //         = значение по умолчанию не обязательно
                    case 4:
                        if (TokenType.oper != tokens[i].Type || "=" != tokens[i].Text)
                        {
                            j++;
                            goto case 5;
                        }
                        i++;
                        var expr = TryParseExpression(stat, context);
                        stat.InitialValue = expr;
                        j++;
                        break;
                    //           конец строки
                    case 5:
                        if (TokenType.ln == tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: new line expected");
                        j++;
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсить выражение </summary>
        /// <param name="parent"> родиетльская инструкция </param>
        /// <param name="context"> функция внутри которой выполняется выражение </param>
        Expression TryParseExpression(Statement parent, FunctionDeclaration context = null)
        {
            //('constant' type id '=' expr newline | var_declr newline)*
            Statement stat = new Expression { Type = "var decl", Parent = parent };

            int level = 0;
            int state = 0;
            string expectBra = "";
            var type = "";
            for (; i < tokens.Count; i++)
            {
                switch (tokens[i].Type)
                {
                    case TokenType.ln: break;
                    case TokenType.lbra:
                    case TokenType.lind:
                        stat = new Parens { Type = $"{tokens[i].Type.Substring(1)}parens", Parent = stat, Start = tokens[i] };
                        stat.Parent.Childs.Add(stat);
                        level++;
                        expectBra = TokenType.lbra == tokens[i].Type ? TokenType.rbra :
                                    TokenType.lind == tokens[i].Type ? TokenType.rind :
                                    "";
                        continue;
                    case TokenType.rbra:
                    case TokenType.rind:
                        if (tokens[i].Type != expectBra)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: another bracer expected");
                        stat = stat.Parent;
                        expectBra = "";
                        {
                            var p = stat.Parent;
                            if (null == p) continue;
                            if (!(p is Parens)) p = p.Parent;
                            if (null == p || !(p is Parens)) continue;
                            expectBra = TokenType.lbra == p.Start.Type ? TokenType.rbra :
                                        TokenType.lind == p.Start.Type ? TokenType.rind :
                                        "";
                            level--;
                        }
                        continue;
                    // int константы
                    case TokenType.adec:
                    case TokenType.ndec:
                    case TokenType.oct:
                    case TokenType.dhex:
                    case TokenType.xhex:
                    // real константы
                    case TokenType.real:
                    // string константы
                    case TokenType.dstr:
                    // null
                    case TokenType.@null:
                    // bool константы
                    case TokenType.@bool:
                        stat.Childs.Add(new Atom { Parent = stat, Start = tokens[i] });
                        continue;
                    case TokenType.oper:
                        stat.Childs.Add(new Atom { Parent = stat, Start = tokens[i] });
                        continue;
                    case TokenType.kwd:
                        if ("function" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: unknown keyword");
                        stat = new Atom { Parent = stat, Start = tokens[i] };
                        stat.Parent.Childs.Add(stat);
                        i++;
                        if (TokenType.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: function identifier expected");
                        if (!Functions.ContainsKey(tokens[i].Text))
#warning сделать предупреждением
                            new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: function not found");
                        stat.Childs.Add(new Atom { Parent = stat, Start = tokens[i] });
                        stat.Parent.Childs.Add(stat);
                        stat = stat.Parent;
                        continue;
                    case TokenType.name:
                        {
                            Statement reference = null;
                            if (null != context)
                                reference = context.Params.ContainsKey(tokens[i].Text) ?
                                                context.Params[tokens[i].Text] as Statement :
                                            context.LocalVariables.ContainsKey(tokens[i].Text) ?
                                                context.LocalVariables[tokens[i].Text] as Statement :
                                                null;
                            if (null == reference)
                                reference = GlobalVariables.ContainsKey(tokens[i].Text) ?
                                                GlobalVariables[tokens[i].Text] as Statement :
                                            Functions.ContainsKey(tokens[i].Text) ?
                                                Functions[tokens[i].Text] as Statement :
                                                null;
                            if (null == reference)
#warning сделать предупреждением
                                new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: unknown indentifier");
                            stat = new Atom { Parent = stat, Start = tokens[i] };
                            stat.Parent.Childs.Add(stat);
                            stat.Childs.Add(reference);
                            stat = stat.Parent;
                            continue;
                        }
                }
                break;
            }

            if (i < tokens.Count) i--;
            return stat as Expression;
        }

        Statement TryParseNative(Statement parent)
        {
            var stat = new Statement { Type = "native", Parent = parent };

            for (; i < tokens.Count; i++)
            {
                if (TokenType.ln == tokens[i].Type) break;
            }

            return stat;
        }

        Statement TryParseFunction(Statement parent)
        {
            var stat = new Statement { Type = "function", Parent = parent };

            for (; i < tokens.Count; i++)
            {
                if (TokenType.ln == tokens[i].Type) break;
            }

            return stat;
        }
        /*
            //----------------------------------------------------------------------
            // Local Declarations
            //----------------------------------------------------------------------

            local_var_list  := ('local' var_declr newline)*

            var_declr       := type id ('=' expr)? | type 'array' id 

            //----------------------------------------------------------------------
            // Statements
            //----------------------------------------------------------------------

            statement_list  :=  (statement newline)*

            statement       := set | call | ifthenelse | loop | exitwhen | return 
                               | debug

            set             := 'set' id '=' expr | 'set' id '[' expr ']' '=' expr 

            call            := 'call' id '(' args? ')'

            args            := expr (',' expr)*

            ifthenelse      := 'if' expr 'then' newline statement_list 
                               else_clause? 'endif' 

            else_clause     := 'else' newline statement_list 
                               | 'elseif' expr 'then' newline statement_list
            else_clause?

            loop            := 'loop' newline statement_list 'endloop'

            exitwhen        := 'exitwhen' expr 
                            // must appear in a loop

            return          := 'return' expr?

            debug           := 'debug' (set | call | ifthenelse | loop)
        */
         

        public Statement Parse(List<Token> tokens)
        {
            this.tokens = tokens;

            var ast = new ProgramStatement();
            bool isDeclPassed = false;
            Statement stat = null;
            for (i = 0; i < tokens.Count; i++)
            {
                if (TokenType.lcom == tokens[i].Type) continue;
                if (TokenType.ln == tokens[i].Type) continue;

                if (TokenType.kwd != tokens[i].Type) new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: error: keyword expected");

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
