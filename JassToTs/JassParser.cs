using System;
using System.Collections.Generic;
using System.Text;

namespace Jass
{
    /// <summary> Базовый класс инструкции </summary>

    class JassParser
    {
        /// <summary> список токенов по которому идёт разбор </summary>
        List<Token> tokens;
        /// <summary> глобальная позиция в списке токенов </summary>
        int i;

        bool isYdweCompatible;
        public JassParser(bool isYdweCompatible = false) => this.isYdweCompatible = isYdweCompatible;

        /// <summary> Добавить комментарий </summary>
        /// <param name="parent"> Инструкция содержащая комментарий </param>
        /// <returns> true если был добавлен комментарий </returns>
        bool AddComment(Statement parent)
        {
            if (TokenType.comm != tokens[i].Type) return false;
            parent.AddChild("Comm", tokens[i]);
            return true;
        }

        #region type/global

        /// <summary> Попытаться распарсить объявление типа </summary>
        Statement TryParseTypeDecl()
        {
            var stat = new Statement { Type = "TypeDecl" };

            int j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (AddComment(stat)) continue;
                if (j < 4 && TokenKind.ln == tokens[i].Kind)
                    throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: linebreak");
                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "type" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
                        j++;
                        break;
                    case 1:
                        if (TokenKind.name != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: identifier expected");
                        stat.AddChild("TypeName", tokens[i]);
                        j++;
                        break;
                    case 2:
                        if (TokenKind.kwd != tokens[i].Kind || "extends" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: extends keyword expected");
                        j++;
                        break;
                    case 3:
                        if (TokenKind.name != tokens[i].Kind && TokenKind.btyp != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: type identifier expected");
                        j++;
                        stat.AddChild("BaseType", tokens[i]);
                        break;
                    case 4:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: new line expected");
                        j++;
                        break;
                }
            }
            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсить раздел с глобальными переменными </summary>
        Statement TryParseGlobals()
        {
            var stat = new Statement { Type = "Glob" };

            int j = 0;
            for (; i < tokens.Count && j < 4; i++)
            {
                if (TokenKind.lcom == tokens[i].Kind) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "globals" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
                        j++;
                        break;
                    case 1:
                    case 3:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong globals declaration: new line expected");
                        j++;
                        break;
                    case 2:
                        if (TokenKind.ln == tokens[i].Kind) continue;
                        if (TokenKind.kwd == tokens[i].Kind && "endglobals" == tokens[i].Text)
                        {
                            i++;
                            j++;
                            continue;
                        }
                        var vardecl = TryParseGlobalVarDecl();
                        stat.AddChild(vardecl);
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсить объявление глобальной переменной или константы </summary>
        Statement TryParseGlobalVarDecl()
        {
            Statement stat = TokenKind.kwd == tokens[i].Kind && "constant" == tokens[i].Text ?
                TryParseGlobalConst() :
                TryParseVarDecl();
            stat.Type = $"G{stat.Type}";
            return stat;
        }

        #endregion

        #region var/const declaration

        /// <summary> Попытаться распарсить глобальную константу </summary>
        Statement TryParseGlobalConst()
        {
            // 'constant' type id '=' expr newline
            var stat = new Statement { Type = "Const" };

            int j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "constant" != tokens[i].Text) return null;
                        stat.Start = tokens[i];
                        j++;
                        break;
                    case 1:
                        if (TokenKind.name != tokens[i].Kind && TokenKind.btyp != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong const declaration: type identifier expected");
                        stat.AddChild("Type", tokens[i]);
                        j++;
                        break;
                    case 2:
                        if (TokenKind.name != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong const declaration: identifier expected");
                        stat.AddChild("Name", tokens[i]);
                        j++;
                        break;
                    case 3:
                        if (TokenKind.oper != tokens[i].Kind || "=" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong const declaration: initialization expected");
                        i++;
                        var expr = TryParseExpression();
                        stat.AddChild(expr);
                        j++;
                        break;
                    case 4:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong const declaration: new line expected");
                        j++;
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсить объявление переменной </summary>
        Statement TryParseVarDecl()
        {
            // type id ('=' expr)? | type 'array' id 
            var stat = new Statement();

            var IsLocal = false;
            var IsArray = false;

            int j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    // local не обязательно
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "local" != tokens[i].Text)
                        {
                            j++;
                            goto case 1;
                        }
                        stat.Start = tokens[i];
                        IsLocal = true;
                        j++;
                        break;
                    //   тип
                    case 1:
                        if (TokenKind.name != tokens[i].Kind && TokenKind.btyp != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: type identifier expected");
                        if (null == stat.Start) stat.Start = tokens[i];
                        stat.AddChild("Type", tokens[i]);
                        j++;
                        break;
                    //     array не обязательно
                    case 2:
                        if (TokenKind.kwd != tokens[i].Kind || "array" != tokens[i].Text)
                        {
                            j++;
                            goto case 3;
                        }
                        IsArray = true;
                        j++;
                        break;
                    //       имя
                    case 3:
                        if (TokenKind.name != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: identifier expected");
                        stat.AddChild("Name", tokens[i]);
                        j += IsArray ? 2 : 1;
                        break;
                    //         = значение по умолчанию не обязательно
                    case 4:
                        if (TokenKind.oper != tokens[i].Kind || "=" != tokens[i].Text)
                        {
                            j++;
                            goto case 5;
                        }
                        i++;
                        var expr = TryParseExpression();
                        stat.AddChild(expr);
                        j++;
                        break;
                    //           конец строки
                    case 5:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong var declaration: new line expected");
                        j++;
                        break;
                }
            }

            stat.Type = string.Format("{0}{1}", IsLocal ? "L" : "", IsArray ? "Arr" : "Var");

            if (i < tokens.Count) i--;
            return stat;
        }

        #endregion

        #region expression
        /// <summary> Остановить если встретился перевод строки </summary>
        bool DefStopper(Token token) => TokenType.br == tokens[i].Type;

        /// <summary> Попытаться распарсить выражение </summary>
        /// <param name="stopper"> Предикат используемый для остановки парсинга выражения, по умолчанию <see cref="DefStopper(Token)"/></param>
        Statement TryParseExpression(Func<Token, bool> stopper = null)
        {
            if (null == stopper) stopper = DefStopper;
            //('constant' type id '=' expr newline | var_declr newline)*
            Statement stat = new Statement { Type = "Expr", Start = tokens[i] };
            var par = new Stack<Statement>();
            Statement child;
            for (; i < tokens.Count; i++)
            {
                if (AddComment(stat)) continue;
                if (0 == par.Count && stopper(tokens[i])) break;
                switch (tokens[i].Type)
                {
                    case TokenType.par:
                        switch (tokens[i].Kind)
                        {
                            case TokenKind.lbra:
                            case TokenKind.lind:
                                par.Push(stat);
                                stat = stat.MakeChild("Par", tokens[i]);
                                continue;
                            case TokenKind.rbra:
                            case TokenKind.rind:
                                if ("Par" != stat.Type)
                                    throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: unexpected closing parenthes");
                                if (TokenKind.lbra != stat.Start.Kind && TokenKind.rbra == tokens[i].Kind)
                                    throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: another parenthes expected");
                                if (TokenKind.lind != stat.Start.Kind && TokenKind.rind == tokens[i].Kind)
                                    throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: another parenthes expected");
                                stat = par.Pop();
                                continue;
                        }
                        throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: unexpected parenthes kind");
                    case TokenType.val:
                        stat.AddChild("Val", tokens[i]);
                        continue;
                    case TokenType.oper:
                        stat.AddChild("Oper", tokens[i]);
                        continue;
                    case TokenType.kwd:
                        stat.AddChild(TryParseFuncRef());
                        continue;
                    case TokenType.name:
                        child = TryParseArrayRef();
                        if (null == child)
                            child = TryParseFuncCall();
                        if (null == child)
                            child = new Statement { Type = "RVar", Start = tokens[i] };
                        stat.AddChild(child);
                        continue;
                }
                break;
            }
            //if (0 == stat.Childs.Count) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: empty expression");
            if (i < tokens.Count) i--;
            if (1 == stat.Childs.Count) return stat.Childs[0];
            return stat;
        }

        /// <summary> Остановить если найден перево строки, запятая или закрывающая скобка </summary>
        /// <code>token => TokenKind.ln == token.Kind || (TokenKind.oper == token.Kind && "," == token.Text)</code>
        bool ArgStopper(Token token) =>
            TokenKind.ln == token.Kind ||
            TokenKind.rbra == token.Kind ||
            (TokenKind.oper == token.Kind && "," == token.Text);

        /// <summary> Попытаться распарсить вызов функции </summary>
        Statement TryParseFuncCall()
        {
            Statement stat = new Statement { Type = "FCall", Start = tokens[i] };

            var start = i;
            var j = 0;
            for (; i < tokens.Count && j < 4; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.name != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: identifier expected");
                        stat.AddChild("Name", tokens[i]);
                        j++;
                        continue;
                    case 1:
                        if (TokenKind.lbra != tokens[i].Kind)
                        {
                            i = start;
                            return null;
                        }
                        j++;
                        continue;
                    case 2:
                        var arg = TryParseExpression(ArgStopper);
                        if ("Expr" != arg.Type || arg.Childs.Count > 0) stat.AddChild(arg);
                        else if (stat.Childs.Count > 1)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: argument expected");
                        j++;
                        continue;
                    case 3:
                        if (TokenKind.oper == tokens[i].Kind && "," == tokens[i].Text)
                            j--;
                        else if (TokenKind.rbra == tokens[i].Kind)
                            j++;
                        else
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: comma or parenthes expected");
                        continue;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Остановить если найден перевод строки или закрывающая квадратная скобка </summary>
        /// <code>token => TokenKind.ln == token.Kind || TokenKind.rind == token.Kind</code>
        bool IndStopper(Token token) =>
            TokenKind.ln == token.Kind ||
            TokenKind.rind == token.Kind;

        /// <summary> Попытаться распарсить ссылку на элемент массива </summary>
        Statement TryParseArrayRef()
        {
            Statement stat = new Statement { Type = "RArr", Start = tokens[i] };

            var start = i;
            var j = 0;
            for (; i < tokens.Count && j < 4; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.name != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: identifier expected");
                        stat.AddChild("Name", tokens[i]);
                        j++;
                        continue;
                    case 1:
                        if (TokenKind.lind != tokens[i].Kind)
                        {
                            i = start;
                            return null;
                        }
                        j++;
                        continue;
                    case 2:
                        var ind = TryParseExpression(IndStopper);
                        if ("Expr" == ind.Type && 0 == ind.Childs.Count)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: index expected");
                        stat.MakeChild("Ind", ind.Start)
                            .AddChild(ind);
                        j++;
                        continue;
                    case 3:
                        if (TokenKind.rind != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: comma or parenthes expected");
                        j++;
                        continue;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсить ссылку на функцию </summary>
        Statement TryParseFuncRef()
        {
            Statement stat = new Statement { Type = "RFunc", Start = tokens[i] };

            var start = i;
            var j = 0;
            for (; i < tokens.Count && j < 2; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind && "function" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: function expected");
                        j++;
                        continue;
                    case 1:
                        if (TokenKind.name != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: identifier expected");
                        stat.Start = tokens[i];
                        j++;
                        continue;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        #endregion

        #region functions

        /// <summary> Попытаться распарсить объявление нативной функции </summary>
        Statement TryParseNative()
        {
            var stat = new Statement { Type = "Native" };

            var j = 0;
            bool IsConst = false;
            for (; i < tokens.Count && j < 4; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    // constant не обязательно
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "constant" != tokens[i].Text)
                        {
                            j++;
                            goto case 1;
                        }
                        stat.Start = tokens[i];
                        IsConst = true;
                        j++;
                        break;
                    case 1:
                        if (TokenKind.kwd != tokens[i].Kind || "native" != tokens[i].Text) return null;
                        if (null == stat.Start) stat.Start = tokens[i];
                        j++;
                        break;
                    case 2:
                        stat.AddChild(TryParseFuncDecl());
                        j++;
                        break;
                    case 3:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong native declaration: new line expected");
                        j++;
                        break;
                }
            }

            if (IsConst) stat.Type = "CNative";

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсить объявление функции </summary>
        Statement TryParseFuncDecl()
        {
            var stat = new Statement { Type = "FuncDecl" };

            var j = 0;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.name != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong function declaration: identifier expected");
                        stat.AddChild("Name", tokens[i]);
                        j++;
                        continue;
                    case 1:
                        if (TokenKind.kwd != tokens[i].Kind || "takes" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong function declaration: takes keyword expected");
                        j++;
                        continue;
                    case 2:
                        if (TokenType.name == tokens[i].Type && "nothing" == tokens[i].Text)
                            stat.AddChild("Params", tokens[i]);
                        else
                            stat.AddChild(TryParseParams());
                        j++;
                        continue;
                    case 3:
                        if (TokenKind.kwd != tokens[i].Kind || "returns" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong function declaration: returns keyword expected");
                        j++;
                        continue;
                    case 4:
                        if ((TokenType.name == tokens[i].Type && "nothing" == tokens[i].Text) ||
                            (TokenType.name == tokens[i].Type))
                            stat.AddChild("Result", tokens[i]);
                        else
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong function declaration: nothing or type name expected");
                        j++;
                        continue;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсить параметры функции </summary>
        Statement TryParseParams()
        {
            var stat = new Statement { Type = "Params" };

            var j = 0;

            Token type = null;
            for (; i < tokens.Count && j < 3; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenType.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong function declaration: type name expected");
                        type = tokens[i];
                        j++;
                        continue;
                    case 1:
                        if (TokenType.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong function declaration: param name expected");
                        stat.MakeChild("Param", type)
                            .AddChild("Type", type)
                            .AddChild("Name", tokens[i]);
                        j++;
                        continue;
                    case 2:
                        if (TokenKind.oper == tokens[i].Kind && "," == tokens[i].Text)
                            j -= 2;
                        else if (TokenKind.kwd == tokens[i].Kind && "returns" == tokens[i].Text)
                            j++;
                        else
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong expression: comma or returns expected");
                        continue;
                }
            }

            if (i < tokens.Count + 1) i -= 2;
            return stat;
        }

        /// <summary> Попытаться распарсить функцию </summary>
        Statement TryParseFunc()
        {
            var stat = new Statement { Type = "Func" };

            var j = 0;
            bool IsConst = false;
            for (; i < tokens.Count && j < 7; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    // constant не обязательно
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "constant" != tokens[i].Text)
                        {
                            j++;
                            goto case 1;
                        }
                        stat.Start = tokens[i];
                        IsConst = true;
                        j++;
                        continue;
                    case 1:
                        if (TokenKind.kwd != tokens[i].Kind || "function" != tokens[i].Text) return null;
                        if (null == stat.Start) stat.Start = tokens[i];
                        j++;
                        continue;
                    case 2:
                        stat.AddChild(TryParseFuncDecl());
                        j++;
                        continue;
                    case 3:
                    case 6:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong function declaration: new line expected");
                        j++;
                        continue;
                    case 4:
                        stat.AddChild(TryParseFuncLocals());
                        stat.AddChild(TryParseFuncBody());
                        j++;
                        continue;
                    case 5:
                        if (TokenKind.kwd != tokens[i].Kind || "endfunction" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong function declaration: endfunction expected");
                        j++;
                        continue;
                }
            }

            if (IsConst) stat.Type = "CFunc";

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсит тело функции </summary>
        public Statement TryParseFuncLocals()
        {
            var stat = new Statement { Type = "FuncLocals" };

            for (; i < tokens.Count; i++)
            {
                if (AddComment(stat)) continue;
                if (TokenKind.ln == tokens[i].Kind) continue;

                if (TokenKind.kwd == tokens[i].Kind && "endfunction" == tokens[i].Text)
                    break;
                if (TokenKind.kwd != tokens[i].Kind || "local" != tokens[i].Text)
                    break;
                stat.AddChild(TryParseVarDecl());
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        /// <summary> Попытаться распарсит тело функции </summary>
        public Statement TryParseFuncBody()
        {
            var stat = new Statement { Type = "FuncBody" };

            for (; i < tokens.Count; i++)
            {
                if (AddComment(stat)) continue;
                if (TokenKind.ln == tokens[i].Kind) continue;

                if (TokenKind.kwd == tokens[i].Kind && "endfunction" == tokens[i].Text)
                    break;

                stat.AddChild(TryParseStatement());
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        #endregion

        #region statement

        /// <summary> Попытаться распарсить инструкцию </summary>
        public Statement TryParseStatement()
        {
            for (; i < tokens.Count; i++)
            {
                if (TokenType.comm == tokens[i].Type)
                    return new Statement { Type = "Comm", Start = tokens[i] };

                if (TokenKind.ln == tokens[i].Kind) break;

                if (TokenKind.kwd != tokens[i].Kind) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: statement error: keyword expected");

                switch (tokens[i].Text)
                {
                    case "set":
                        return TryParseSet();
                    case "call":
                        return TryParseCall();
                    case "if":
                        return TryParseIf();
                    case "loop":
                        return TryParseLoop();
                    case "return":
                        return TryParseReturn();
                    case "exitwhen":
                        return TryParseExit();
                    case "debug":
                        i++;
                        var dbg = TryParseStatement();
                        if ("Return" == dbg.Type || "Debug" == dbg.Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: statement error: wrong statement");
                        var stat = new Statement { Type = "Debug" };
                        stat.AddChild(dbg);
                        return stat;
                }
                throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: statement error: unknown keyword");
            }
            return null;
        }

        Statement TryParseSet()
        {
            var stat = new Statement { Type = "Set" };

            var j = 0;
            var IsArray = false;
            for (; i < tokens.Count && j < 5; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    // constant не обязательно
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "set" != tokens[i].Text) return null;
                        if (null == stat.Start) stat.Start = tokens[i];
                        j++;
                        continue;
                    case 1:
                        if (TokenType.name != tokens[i].Type)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong set statement: variable name expected");
                        stat.AddChild("Name", tokens[i]);
                        j++;
                        continue;
                    case 2:
                        if (TokenKind.lind != tokens[i].Kind)
                        {
                            j++;
                            goto case 3;
                        }
                        IsArray = true;
                        i++;
                        var ind = TryParseExpression(IndStopper);
                        stat.MakeChild("Ind", ind.Start)
                            .AddChild(ind);
                        i++;
                        j++;
                        continue;
                    case 3:
                        if (TokenKind.oper != tokens[i].Kind || "=" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong set statement: = expected");
                        i++;
                        var expr = TryParseExpression();
                        stat.AddChild(expr);
                        j++;
                        continue;
                    case 4:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong native declaration: new line expected");
                        j++;
                        break;
                }
            }
            if (IsArray) stat.Type = "ASet";

            if (i < tokens.Count) i--;
            return stat;
        }

        Statement TryParseCall()
        {
            Statement stat = null;

            var j = 0;
            for (; i < tokens.Count && j < 3; i++)
            {
                if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "call" != tokens[i].Text) return null;
                        j++;
                        continue;
                    case 1:
                        stat = TryParseFuncCall();
                        j++;
                        continue;
                    case 2:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong native declaration: new line expected");
                        j++;
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        bool IfStopper(Token token) => TokenKind.ln == token.Kind || (TokenKind.kwd == token.Kind && "then" == token.Text);

        Statement TryParseIf()
        {
            /*
            ifthenelse      := 'if' expr 'then' newline 
                                statement_list else_clause? 'endif' 
            else_clause     := 'else' newline statement_list | 
                               'elseif' expr 'then' newline statement_list else_clause?
            */
            Statement stat = new Statement { Type = "If" };

            Statement cond = new Statement { Type = "Cond" };
            Statement then = null;
            var j = 0;
            for (; i < tokens.Count && j < 8; i++)
            {
                if (AddComment(stat)) continue;

                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "if" != tokens[i].Text) return null;
                        j++;
                        continue;
                    case 1:
                        cond.AddChild(TryParseExpression(IfStopper));
                        stat.AddChild(cond);
                        j++;
                        continue;
                    case 2:
                        if (TokenKind.kwd != tokens[i].Kind || "then" != tokens[i].Text)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong if statement: then expected");
                        then = new Statement { Type = "Then" };
                        stat.AddChild(then);
                        j++;
                        continue;
                    case 3:
                    case 5:
                    case 7:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong if statement: new line expected");
                        j++;
                        continue;
                    case 4:
                        if (TokenKind.ln == tokens[i].Kind) continue;
                        if (TokenKind.kwd == tokens[i].Kind && "elseif" == tokens[i].Text)
                        {
                            cond = new Statement { Type = "ElseCond" };
                            j -= 3;
                            continue;
                        }
                        if (TokenKind.kwd == tokens[i].Kind && "else" == tokens[i].Text)
                        {
                            then = new Statement { Type = "Else" };
                            stat.AddChild(then);
                            j++;
                            continue;
                        }
                        if (TokenKind.kwd == tokens[i].Kind && "endif" == tokens[i].Text)
                        {
                            j += 3;
                            continue;
                        }
                        then.AddChild(TryParseStatement());
                        continue;
                    case 6:
                        if (TokenKind.ln == tokens[i].Kind) continue;
                        if (TokenKind.kwd == tokens[i].Kind && "endif" == tokens[i].Text)
                        {
                            j ++;
                            continue;
                        }
                        then.AddChild(TryParseStatement());
                        continue;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        Statement TryParseLoop()
        {
            Statement stat = new Statement { Type = "Loop" };

            var j = 0;
            for (; i < tokens.Count && j < 3; i++)
            {
                if (AddComment(stat)) continue;

                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "loop" != tokens[i].Text) return null;
                        j++;
                        continue;
                    case 1:
                        if (TokenKind.ln == tokens[i].Kind) continue;
                        if (TokenKind.kwd == tokens[i].Kind && "endloop" == tokens[i].Text) {
                            j++;
                            continue;
                        }
                        //if (TokenKind.kwd == tokens[i].Kind && "exitwhen" == tokens[i].Text)
                        //{
                        //    i++;
                        //    stat.MakeChild("Exit", tokens[i])
                        //        .AddChild(TryParseExpression());
                        //    i++;
                        //    continue;
                        //}
                        stat.AddChild(TryParseStatement());
                        continue;
                    case 2:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong loop statement: new line expected");
                        j++;
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        Statement TryParseExit()
        {
            // *return          := 'return' expr?
            Statement stat = new Statement { Type = "Exit" };

            var j = 0;
            for (; i < tokens.Count && j < 3; i++)
            {
                // if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "exitwhen" != tokens[i].Text) return null;
                        j++;
                        continue;
                    case 1:
                        stat.AddChild(TryParseExpression());
                        j++;
                        continue;
                    case 2:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong exitwhen statement: new line expected");
                        j++;
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        Statement TryParseReturn()
        {
            // *return          := 'return' expr?
            Statement stat = new Statement { Type = "Return" };

            var j = 0;
            for (; i < tokens.Count && j < 3; i++)
            {
                // if (AddComment(stat)) continue;
                switch (j)
                {
                    case 0:
                        if (TokenKind.kwd != tokens[i].Kind || "return" != tokens[i].Text) return null;
                        j++;
                        continue;
                    case 1:
                        stat.AddChild(TryParseExpression());
                        j++;
                        continue;
                    case 2:
                        if (TokenKind.ln != tokens[i].Kind)
                            throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong return statement: new line expected");
                        j++;
                        break;
                }
            }

            if (i < tokens.Count) i--;
            return stat;
        }

        #endregion

        public Statement Parse(List<Token> tokens)
        {
            this.tokens = tokens;

            var prog = new Statement { Type = "Prog" };
            bool isDeclPassed = false;
            Statement stat = null;
            for (i = 0; i < tokens.Count; i++)
            {
                if (AddComment(prog)) continue;
                if (TokenKind.ln == tokens[i].Kind) continue;

                if (TokenKind.kwd != tokens[i].Kind) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: error: keyword expected");

                if ("type" == tokens[i].Text)
                {
                    if (isDeclPassed) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong type declaration: not in declaration block");
                    stat = TryParseTypeDecl();
                    prog.AddChild(stat);
                    continue;
                }
                if ("globals" == tokens[i].Text)
                {
                    if (isDeclPassed) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong global declaration: not in declaration block");
                    stat = TryParseGlobals();
                    prog.Childs.Add(stat);
                    continue;
                }
                if ("constant" == tokens[i].Text || "native" == tokens[i].Text)
                {
                    stat = TryParseNative();
                    if (null != stat)
                    {
                        if (isDeclPassed) throw new Exception($"Line {tokens[i].Line}, Col {tokens[i].Col}: wrong native declaration: not in declaration block");
                        prog.Childs.Add(stat);
                        continue;
                    }
                }

                if ("constant" == tokens[i].Text || "function" == tokens[i].Text)
                {
                    if (!isDeclPassed) isDeclPassed = true;
                    stat = TryParseFunc();
                    prog.Childs.Add(stat);
                    continue;
                }
            }
            return prog;
        }
    }
}
