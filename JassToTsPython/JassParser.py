from Statement import Statement
from Token import TokenType, TokenKind
import JassException

class JassParser:
    def __init__(self, isYdweCompatible):
        self.isYdweCompatible = isYdweCompatible

    def moveStates(self, stat, states):
        j = 0
        while self.i < len(self.tokens) and j < len(states):
            if not self.AddComment(stat): 
                nj = states[j](j)
                if None == nj: return None
                j = nj
            
            self.i += 1
        return True

    # <summary> Добавить комментарий </summary>
    # <param name="parent"> Инструкция содержащая комментарий </param>
    # <returns> True если был добавлен комментарий </returns>
    def AddComment(self, parent) -> Statement:
        if TokenType.comm != self.tokens[self.i].Type:
            return False
        parent.AddChild("Comm", self.tokens[self.i])
        return True
    
    # Попытаться распарсить объявление типа 
    def TryParseTypeDecl(self) -> Statement:
        pass
        stat = Statement(type = "TypeDecl")

        j = 0

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "type" != self.tokens[self.i].Text: return None
            stat.Start = self.tokens[self.i]
            return j + 1
        def state1(j):
            if TokenKind.name != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong type declaration: identifier expected")
            stat.AddChild("TypeName", self.tokens[self.i])
            return j + 1
        def state2():
            if TokenKind.kwd != self.tokens[self.i].Kind or "extends" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong type declaration: extends keyword expected")
            return j + 1
        def state3():
            if TokenKind.name != self.tokens[self.i].Kind and TokenKind.btyp != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong type declaration: type identifier expected")
            stat.AddChild("BaseType", self.tokens[self.i])
            return j + 1
        def state4():
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong type declaration: new line expected")
            return j + 1

        states = [state0, state1, state2, state3, state4]

        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat

    # Попытаться распарсить раздел с глобальными переменными 
    def TryParseGlobals(self) -> Statement:
        stat = Statement(type = "Glob")

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "globals" != self.tokens[self.i].Text: return None
            stat.Start = self.tokens[self.i]
            return j + 1
        def state1(j): return state3(j)
        def state3(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong globals declaration: new line expected")
            return j + 1
        def state2(j):
            if TokenKind.ln == self.tokens[self.i].Kind: return j
            if TokenKind.kwd == self.tokens[self.i].Kind and "endglobals" == self.tokens[self.i].Text:
                self.i += 1
                return j + 1
            vardecl = self.TryParseGlobalVarDecl()
            stat.AddChildStatement(vardecl)
            return j

        states = [state0, state1, state2, state3]

        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat

    # Попытаться распарсить объявление глобальной переменной или константы 
    def TryParseGlobalVarDecl(self) -> Statement:
        pass
        if TokenKind.kwd == self.tokens[self.i].Kind and "constant" == self.tokens[self.i].Text:
            stat = self.TryParseGlobalConst()
        else:
            stat = self.TryParseVarDecl()
        stat.Type = "G{}".format(stat.Type)
        return stat

    #endregion

    #region var/const declaration

    # Попытаться распарсить глобальную константу 
    def TryParseGlobalConst(self) -> Statement:
        pass
        # 'constant' type id '=' expr newline
        stat = Statement(type = "Const")

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "constant" != self.tokens[self.i].Text: return None
            stat.Start = self.tokens[self.i]
            return j + 1
        def state1(j):
            if TokenKind.name != self.tokens[self.i].Kind and TokenKind.btyp != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong const declaration: type identifier expected")
            stat.AddChild("Type", self.tokens[self.i])
            return j + 1
        def state2(j):
            if TokenKind.name != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong const declaration: identifier expected")
            stat.AddChild("Name", self.tokens[self.i])
            return j + 1
        def state3(j):
            if TokenKind.oper != self.tokens[self.i].Kind or "=" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong const declaration: initialization expected")
            self.i += 1
            expr = self.TryParseExpression()
            stat.AddChildStatement(expr)
            return j + 1
        def state4(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong const declaration: new line expected")
            return j + 1

        states = [state0, state1, state2, state3, state4]

        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat

    # Попытаться распарсить объявление переменной 
    def TryParseVarDecl(self) -> Statement:
        pass
        # # type id ('=' expr)? | type 'array' id 
        # var stat = new Statement()

        # var IsLocal = False
        # var IsArray = False

        # int j = 0
        # for ( i < len(self.tokens) and j < 5 self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         # local не обязательно
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "local" != self.tokens[self.i].Text)
        #             {
        #                 j += 1
        #                 goto def state1
        #             }
        #             stat.Start = self.tokens[self.i]
        #             IsLocal = True
        #             return j + 1
        #         #   тип
        #         def state1:
        #             if (TokenKind.name != self.tokens[self.i].Kind and TokenKind.btyp != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong var declaration: type identifier expected")
        #             if (None == stat.Start) stat.Start = self.tokens[self.i]
        #             stat.AddChild("Type", self.tokens[self.i])
        #             return j + 1
        #         #     array не обязательно
        #         def state2:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "array" != self.tokens[self.i].Text)
        #             {
        #                 j += 1
        #                 goto def state3
        #             }
        #             IsArray = True
        #             return j + 1
        #         #       имя
        #         def state3:
        #             if (TokenKind.name != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong var declaration: identifier expected")
        #             stat.AddChild("Name", self.tokens[self.i])
        #             j += IsArray ? 2 : 1
        #             break
        #         #         = значение по умолчанию не обязательно
        #         def state4:
        #             if (TokenKind.oper != self.tokens[self.i].Kind or "=" != self.tokens[self.i].Text)
        #             {
        #                 j += 1
        #                 goto def state5
        #             }
        #             self.i += 1
        #             var expr = TryParseExpression()
        #             stat.AddChildStatement(expr)
        #             return j + 1
        #         #           конец строки
        #         def state5:
        #             if (TokenKind.ln != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong var declaration: new line expected")
        #             return j + 1
        #     }
        # }

        # stat.Type = string.Format("{0}{1}", IsLocal ? "L" : "", IsArray ? "Arr" : "Var")

        # if (i < len(self.tokens)) i--
        # return stat

    #endregion

    #region expression
    # Остановить если встретился перевод строки 
    def DefStopper(self, token): return TokenType.br == token.Type

    # Попытаться распарсить выражение 
    #/ <param name="stopper"> Предикат используемый для остановки парсинга выражения, по умолчанию <see cref="DefStopper(Token)"/></param>
    def TryParseExpression(self, stopper = None) -> Statement:
        pass
        # if (None == stopper) stopper = DefStopper
        # #('constant' type id '=' expr newline | var_declr newline)*
        # Statement stat = new Statement { Type = "Expr", Start = self.tokens[self.i] }
        # var par = new Stack<Statement>()
        # Statement child
        # for ( i < len(self.tokens) self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     if (0 == par.Count and stopper(self.tokens[self.i])) break
        #     switch (self.tokens[self.i].Type)
        #     {
        #         def stateTokenType.par:
        #             switch (self.tokens[self.i].Kind)
        #             {
        #                 def stateTokenKind.lbra:
        #                 def stateTokenKind.lind:
        #                     par.Push(stat)
        #                     stat = stat.MakeChild("Par", self.tokens[self.i])
        #                     continue
        #                 def stateTokenKind.rbra:
        #                 def stateTokenKind.rind:
        #                     if ("Par" != stat.Type)
        #                         JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: unexpected closing parenthes")
        #                     if (TokenKind.lbra != stat.Start.Kind and TokenKind.rbra == self.tokens[self.i].Kind)
        #                         JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: another parenthes expected")
        #                     if (TokenKind.lind != stat.Start.Kind and TokenKind.rind == self.tokens[self.i].Kind)
        #                         JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: another parenthes expected")
        #                     stat = par.Pop()
        #                     continue
        #             }
        #             JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: unexpected parenthes kind")
        #             continue
        #         def stateTokenType.val:
        #             stat.AddChild("Val", self.tokens[self.i])
        #             continue
        #         def stateTokenType.oper:
        #             stat.AddChild("Oper", self.tokens[self.i])
        #             continue
        #         def stateTokenType.kwd:
        #             stat.AddChildStatement(TryParseFuncRef())
        #             continue
        #         def stateTokenType.name:
        #             # warning Потенциальные дженерики, хз что с ними делать, пока что записываем строкой
        #             if (isYdweCompatible and TokenKind.btyp == self.tokens[self.i].Kind)
        #             {
        #                 var ctoken = self.tokens[self.i].Clone()
        #                 ctoken.Text = $"\"{ctoken.Text}\""
        #                 child = new Statement { Type = "RVar", Start = ctoken }
        #                 stat.AddChildStatement(child)
        #                 continue
        #             }
        #             child = TryParseArrayRef()
        #             if (None == child)
        #                 child = TryParseFuncCall()
        #             if (None == child)
        #                 child = new Statement { Type = "RVar", Start = self.tokens[self.i] }
        #             stat.AddChildStatement(child)
        #             continue
        #     }
        #     break
        # }
        # #if (0 == stat.Childs.Count) JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: empty expression")
        # if (i < len(self.tokens)) i--
        # if (1 == stat.Childs.Count) return stat.Childs[0]
        # return stat

    # Остановить если найден перево строки, запятая или закрывающая скобка 
    #/ <code>token => TokenKind.ln == token.Kind or (TokenKind.oper == token.Kind and "," == token.Text)</code>
    def ArgStopper(self, token):
        return TokenKind.ln == token.Kind or \
               TokenKind.rbra == token.Kind or \
               (TokenKind.oper == token.Kind and "," == token.Text)

    # Попытаться распарсить вызов функции 
    def TryParseFuncCall(self) -> Statement:
        stat = Statement(type = "FCall", start = self.tokens[self.i])
        start = self.i

        def state0(j):
            if TokenKind.name != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: identifier expected")
            stat.AddChild("Name", self.tokens[self.i])
            return j + 1
        def state1(j):
            if TokenKind.lbra != self.tokens[self.i].Kind:
                self.i = start
                return None
            return j + 1
        def state2(j):
            arg = self.TryParseExpression(self.ArgStopper)
            if "Expr" != arg.Type or arg.Childs.Count > 0: stat.AddChildStatement(arg)
            else: 
                if stat.Childs.Count > 1:
                    JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: argument expected")
            return j + 1
        def state3(j):
            if TokenKind.oper == self.tokens[self.i].Kind and "," == self.tokens[self.i].Text:
                j -= 1
            else:
                if TokenKind.rbra == self.tokens[self.i].Kind:
                    j += 1
                else:
                    JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: comma or parenthes expected")
            return j
        
        states = [state0, state1, state2, state3]
        
        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat

    # Остановить если найден перевод строки или закрывающая квадратная скобка 
    #/ <code>token => TokenKind.ln == token.Kind or TokenKind.rind == token.Kind</code>
    def IndStopper(self, token):
        TokenKind.ln == token.Kind or \
        TokenKind.rind == token.Kind

    # Попытаться распарсить ссылку на элемент массива 
    def TryParseArrayRef(self) -> Statement:
        pass
        # Statement stat = new Statement { Type = "RArr", Start = self.tokens[self.i] }

        # var start = i
        # var j = 0
        # for ( i < len(self.tokens) and j < 4 self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         def state0:
        #             if (TokenKind.name != self.tokens[self.i].Kind) 
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: identifier expected")
        #             stat.AddChild("Name", self.tokens[self.i])
        #             return j + 1
        #         def state1:
        #             if (TokenKind.lind != self.tokens[self.i].Kind)
        #             {
        #                 i = start
        #                 return None
        #             }
        #             return j + 1
        #         def state2:
        #             var ind = TryParseExpression(IndStopper)
        #             if ("Expr" == ind.Type and 0 == ind.Childs.Count)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: index expected")
        #             stat.MakeChild("Ind", ind.Start)
        #                 .AddChildStatement(ind)
        #             return j + 1
        #         def state3:
        #             if (TokenKind.rind != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: comma or parenthes expected")
        #             return j + 1
        #     }
        # }

        # if (i < len(self.tokens)) i--
        # return stat

    # Попытаться распарсить ссылку на функцию 
    def TryParseFuncRef(self) -> Statement:
        pass
        # Statement stat = new Statement { Type = "RFunc", Start = self.tokens[self.i] }

        # var j = 0
        # for ( i < len(self.tokens) and j < 2 self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind and "function" != self.tokens[self.i].Text)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: function expected")
        #             return j + 1
        #         def state1:
        #             if (TokenKind.name != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: identifier expected")
        #             stat.Start = self.tokens[self.i]
        #             return j + 1
        #     }
        # }

        # if (i < len(self.tokens)) i--
        # return stat

    #endregion

    #region functions

    # Попытаться распарсить объявление нативной функции 
    def TryParseNative(self) -> Statement:
        pass 
        # var stat = new Statement { Type = "Native" }

        # var j = 0
        # bool IsConst = False
        # for ( i < len(self.tokens) and j < 4 self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         # constant не обязательно
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "constant" != self.tokens[self.i].Text)
        #             {
        #                 j += 1
        #                 goto def state1
        #             }
        #             stat.Start = self.tokens[self.i]
        #             IsConst = True
        #             return j + 1
        #         def state1:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "native" != self.tokens[self.i].Text) return None
        #             if (None == stat.Start) stat.Start = self.tokens[self.i]
        #             return j + 1
        #         def state2:
        #             stat.AddChildStatement(TryParseFuncDecl())
        #             return j + 1
        #         def state3:
        #             if (TokenKind.ln != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong native declaration: new line expected")
        #             return j + 1
        #     }
        # }

        # if (IsConst) stat.Type = "CNative"

        # if (i < len(self.tokens)) i--
        # return stat

    # Попытаться распарсить объявление функции 
    def TryParseFuncDecl(self) -> Statement:
        stat = Statement(type = "FuncDecl")

        def state0(j):
            if TokenKind.name != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: identifier expected")
            stat.AddChild("Name", self.tokens[self.i])
            return j + 1
        def state1(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "takes" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: takes keyword expected")
            return j + 1
        def state2(j):
            if TokenType.name == self.tokens[self.i].Type and "nothing" == self.tokens[self.i].Text:
                stat.AddChild("Params", self.tokens[self.i])
            else:
                stat.AddChildStatement(self.TryParseParams())
            return j + 1
        def state3(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "returns" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: returns keyword expected")
            return j + 1
        def state4(j):
            if (TokenType.name == self.tokens[self.i].Type and "nothing" == self.tokens[self.i].Text) or \
                TokenType.name == self.tokens[self.i].Type:
                stat.AddChild("Result", self.tokens[self.i])
            else:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: nothing or type name expected")
            return j + 1
        
        states = [state0, state1, state2, state3, state4]

        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat


    # Попытаться распарсить параметры функции 
    def TryParseParams() -> Statement:
        pass
        # var stat = new Statement { Type = "Params" }

        # var j = 0

        # Token type = None
        # for ( i < len(self.tokens) and j < 3 self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         def state0:
        #             if (TokenType.name != self.tokens[self.i].Type)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: type name expected")
        #             type = self.tokens[self.i]
        #             return j + 1
        #         def state1:
        #             if (TokenType.name != self.tokens[self.i].Type)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: param name expected")
        #             stat.MakeChild("Param", type)
        #                 .AddChild("Type", type)
        #                 .AddChild("Name", self.tokens[self.i])
        #             return j + 1
        #         def state2:
        #             if (TokenKind.oper == self.tokens[self.i].Kind and "," == self.tokens[self.i].Text)
        #                 j -= 2
        #             else if (TokenKind.kwd == self.tokens[self.i].Kind and "returns" == self.tokens[self.i].Text)
        #                 j += 1
        #             else
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: comma or returns expected")
        #             continue
        #     }
        # }

        # if (i < len(self.tokens) + 1) i -= 2
        # return stat

    # Попытаться распарсить функцию 
    def TryParseFunc(self) -> Statement:
        stat = Statement(type = "Func")
        flags = { "IsConst": False }

        # constant не обязательно
        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "constant" != self.tokens[self.i].Text:
                j += 1
                return state1(j)
            stat.Start = self.tokens[self.i]
            flags["IsConst"] = True
            return j + 1
        def state1(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "function" != self.tokens[self.i].Text: return None
            if None == stat.Start: stat.Start = self.tokens[self.i]
            return j + 1
        def state2(j):
            stat.AddChildStatement(self.TryParseFuncDecl())
            return j + 1
        def state3(j): return state6(j)
        def state6(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: new line expected")
            return j + 1
        def state4(j):
            stat.AddChildStatement(self.TryParseFuncLocals())
            stat.AddChildStatement(self.TryParseFuncBody())
            return j + 1
        def state5(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "endfunction" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: endfunction expected")
            return j + 1
        
        states = [state0, state1, state2, state3, state4, state5, state6]

        if None == self.moveStates(stat, states): return None

        if flags["IsConst"]: stat.Type = "CFunc"

        if self.i < len(self.tokens): self.i -= 1
        return stat

    # Попытаться распарсит тело функции 
    def TryParseFuncLocals() -> Statement:
        pass
        # var stat = new Statement { Type = "FuncLocals" }

        # for ( i < len(self.tokens) self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     if (TokenKind.ln == self.tokens[self.i].Kind) continue

        #     if (TokenKind.kwd == self.tokens[self.i].Kind and "endfunction" == self.tokens[self.i].Text)
        #         break
        #     if (TokenKind.kwd != self.tokens[self.i].Kind or "local" != self.tokens[self.i].Text)
        #         break
        #     stat.AddChildStatement(TryParseVarDecl())
        # }

        # if (i < len(self.tokens)) i--
        # return stat

    # Попытаться распарсит тело функции 
    def TryParseFuncBody() -> Statement:
        pass
        # var stat = new Statement { Type = "FuncBody" }

        # for ( i < len(self.tokens) self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     if (TokenKind.ln == self.tokens[self.i].Kind) continue

        #     if (TokenKind.kwd == self.tokens[self.i].Kind and "endfunction" == self.tokens[self.i].Text)
        #         break

        #     stat.AddChildStatement(TryParseStatement())
        # }

        # if (i < len(self.tokens)) i--
        # return stat

    #endregion

    #region statement

    def TryParseMacroCall() -> Statement:
        pass
        # var sb = new StringBuilder("#")
        # var stoken = self.tokens[self.i].Clone()
        # for ( i < len(self.tokens) self.i += 1)
        # {
        #     if (TokenKind.ln == self.tokens[self.i].Kind) break
        #     sb.AppendFormat(" {0}", self.tokens[self.i].Text)
        # }
        # stoken.Text = sb.ToString()
        # return new Statement { Type = StatementType.YdweMacro, Start = stoken }

    # Попытаться распарсить инструкцию 
    def TryParseStatement() -> Statement:
        pass
        # for ( i < len(self.tokens) self.i += 1)
        # {
        #     if (TokenType.comm == self.tokens[self.i].Type)
        #         return new Statement { Type = "Comm", Start = self.tokens[self.i] }

        #     if (TokenKind.ln == self.tokens[self.i].Kind) break

        #     #warning Вызов потенциального макроса, у парсера макросы должны быть выпилены
        #     if (isYdweCompatible and TokenKind.name == self.tokens[self.i].Kind) {
        #         return TryParseMacroCall()
        #     }
        #     if (TokenKind.kwd != self.tokens[self.i].Kind) JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "statement error: keyword expected")

        #     switch (self.tokens[self.i].Text)
        #     {
        #         def state"set":
        #             return TryParseSet()
        #         def state"call":
        #             return TryParseCall()
        #         def state"if":
        #             return TryParseIf()
        #         def state"loop":
        #             return TryParseLoop()
        #         def state"return":
        #             return TryParseReturn()
        #         def state"exitwhen":
        #             return TryParseExit()
        #         def state"debug":
        #             self.i += 1
        #             var dbg = TryParseStatement()
        #             if ("Return" == dbg.Type or "Debug" == dbg.Type)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "statement error: wrong statement")
        #             var stat = new Statement { Type = "Debug" }
        #             stat.AddChildStatement(dbg)
        #             return stat
        #     }
        #     JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "statement error: unknown keyword")
        # }
        # return new Statement{ Type = "Error", Start = self.tokens[self.i] }

    def TryParseSet() -> Statement:
        pass
        # var stat = new Statement { Type = "Set" }

        # var j = 0
        # var IsArray = False
        # for ( i < len(self.tokens) and j < 5 self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         # constant не обязательно
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "set" != self.tokens[self.i].Text) return None
        #             if (None == stat.Start) stat.Start = self.tokens[self.i]
        #             return j + 1
        #         def state1:
        #             if (TokenType.name != self.tokens[self.i].Type)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong set statement: variable name expected")
        #             stat.AddChild("Name", self.tokens[self.i])
        #             return j + 1
        #         def state2:
        #             if (TokenKind.lind != self.tokens[self.i].Kind)
        #             {
        #                 j += 1
        #                 goto def state3
        #             }
        #             IsArray = True
        #             self.i += 1
        #             var ind = TryParseExpression(IndStopper)
        #             stat.MakeChild("Ind", ind.Start)
        #                 .AddChildStatement(ind)
        #             self.i += 1
        #             return j + 1
        #         def state3:
        #             if (TokenKind.oper != self.tokens[self.i].Kind or "=" != self.tokens[self.i].Text)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong set statement: = expected")
        #             self.i += 1
        #             var expr = TryParseExpression()
        #             stat.AddChildStatement(expr)
        #             return j + 1
        #         def state4:
        #             if (TokenKind.ln != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong native declaration: new line expected")
        #             return j + 1
        #     }
        # }
        # if (IsArray) stat.Type = "ASet"

        # if (i < len(self.tokens)) i--
        # return stat

    def TryParseCall() -> Statement:
        pass
        # Statement stat = None

        # var j = 0
        # for ( i < len(self.tokens) and j < 3 self.i += 1)
        # {
        #     if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "call" != self.tokens[self.i].Text) return None
        #             return j + 1
        #         def state1:
        #             stat = TryParseFuncCall()
        #             return j + 1
        #         def state2:
        #             if (TokenKind.ln != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong native declaration: new line expected")
        #             return j + 1
        #     }
        # }

        # if (i < len(self.tokens)) i--
        # return None == stat ? new Statement { Type = "Error", Start = self.tokens[self.i] } : stat

    def IfStopper(self, token):
        return TokenKind.ln == token.Kind or \
              (TokenKind.kwd == token.Kind and "then" == token.Text)

    def TryParseIf() -> Statement:
        pass
        # /*
        # ifthenelse      := 'if' expr 'then' newline 
        #                     statement_list else_clause? 'endif' 
        # else_clause     := 'else' newline statement_list | 
        #                     'elseif' expr 'then' newline statement_list else_clause?
        # */
        # Statement stat = new Statement { Type = "If" }

        # Statement cond = new Statement { Type = "Cond" }
        # Statement then = None
        # var j = 0
        # for ( i < len(self.tokens) and j < 8 self.i += 1)
        # {
        #     if (AddComment(stat)) continue

        #     switch (j)
        #     {
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "if" != self.tokens[self.i].Text) return None
        #             return j + 1
        #         def state1:
        #             cond.AddChildStatement(TryParseExpression(IfStopper))
        #             stat.AddChildStatement(cond)
        #             return j + 1
        #         def state2:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "then" != self.tokens[self.i].Text)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong if statement: then expected")
        #             then = new Statement { Type = "Then" }
        #             stat.AddChildStatement(then)
        #             return j + 1
        #         def state3:
        #         def state5:
        #         def state7:
        #             if (TokenKind.ln != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong if statement: new line expected")
        #             return j + 1
        #         def state4:
        #             if (TokenKind.ln == self.tokens[self.i].Kind) continue
        #             if (TokenKind.kwd == self.tokens[self.i].Kind and "elseif" == self.tokens[self.i].Text)
        #             {
        #                 cond = new Statement { Type = "ElseCond" }
        #                 j -= 3
        #                 continue
        #             }
        #             if (TokenKind.kwd == self.tokens[self.i].Kind and "else" == self.tokens[self.i].Text)
        #             {
        #                 then = new Statement { Type = "Else" }
        #                 stat.AddChildStatement(then)
        #                 return j + 1
        #             }
        #             if (TokenKind.kwd == self.tokens[self.i].Kind and "endif" == self.tokens[self.i].Text)
        #             {
        #                 j += 3
        #                 continue
        #             }
        #             then.AddChildStatement(TryParseStatement())
        #             continue
        #         def state6:
        #             if (TokenKind.ln == self.tokens[self.i].Kind) continue
        #             if (TokenKind.kwd == self.tokens[self.i].Kind and "endif" == self.tokens[self.i].Text)
        #             {
        #                 j ++
        #                 continue
        #             }
        #             then.AddChildStatement(TryParseStatement())
        #             continue
        #     }
        # }

        # if (i < len(self.tokens)) i--
        # return stat

    def TryParseLoop() -> Statement:
        pass
        # Statement stat = new Statement { Type = "Loop" }

        # var j = 0
        # for ( i < len(self.tokens) and j < 3 self.i += 1)
        # {
        #     if (AddComment(stat)) continue

        #     switch (j)
        #     {
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "loop" != self.tokens[self.i].Text) return None
        #             return j + 1
        #         def state1:
        #             if (TokenKind.ln == self.tokens[self.i].Kind) continue
        #             if (TokenKind.kwd == self.tokens[self.i].Kind and "endloop" == self.tokens[self.i].Text) {
        #                 return j + 1
        #             }
        #             #if (TokenKind.kwd == self.tokens[self.i].Kind and "exitwhen" == self.tokens[self.i].Text)
        #             #{
        #             #    self.i += 1
        #             #    stat.MakeChild("Exit", self.tokens[self.i])
        #             #        .AddChildStatement(TryParseExpression())
        #             #    self.i += 1
        #             #    continue
        #             #}
        #             stat.AddChildStatement(TryParseStatement())
        #             continue
        #         def state2:
        #             if (TokenKind.ln != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong loop statement: new line expected")
        #             return j + 1
        #     }
        # }

        # if (i < len(self.tokens)) i--
        # return stat

    def TryParseExit() -> Statement:
        pass
        # # *return          := 'return' expr?
        # Statement stat = new Statement { Type = "Exit" }

        # var j = 0
        # for ( i < len(self.tokens) and j < 3 self.i += 1)
        # {
        #     # if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "exitwhen" != self.tokens[self.i].Text) return None
        #             return j + 1
        #         def state1:
        #             stat.AddChildStatement(TryParseExpression())
        #             return j + 1
        #         def state2:
        #             if (TokenKind.ln != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong exitwhen statement: new line expected")
        #             return j + 1
        #     }
        # }

        # if (i < len(self.tokens)) i--
        # return stat

    def TryParseReturn() -> Statement:
        pass
        # # *return          := 'return' expr?
        # Statement stat = new Statement { Type = "Return" }

        # var j = 0
        # for ( i < len(self.tokens) and j < 3 self.i += 1)
        # {
        #     # if (AddComment(stat)) continue
        #     switch (j)
        #     {
        #         def state0:
        #             if (TokenKind.kwd != self.tokens[self.i].Kind or "return" != self.tokens[self.i].Text) return None
        #             return j + 1
        #         def state1:
        #             stat.AddChildStatement(TryParseExpression())
        #             return j + 1
        #         def state2:
        #             if (TokenKind.ln != self.tokens[self.i].Kind)
        #                 JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong return statement: new line expected")
        #             return j + 1
        #     }
        # }

        # if (i < len(self.tokens)) i--
        # return stat

    #endregion
    
    def Parse(self, tokens) -> list[Statement]:
        self.tokens = tokens

        prog = Statement(type="Prog")
        flags = { "isDeclPassed": False }
        stat = None

        self.i = 0
        while self.i < len(self.tokens):
            def iteration(flags):
                if self.AddComment(prog): return True

                if TokenKind.ln == self.tokens[self.i].Kind: return True

                if TokenKind.kwd != self.tokens[self.i].Kind: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "error: keyword expected")

                if "type" == self.tokens[self.i].Text:
                    if flags["isDeclPassed"]: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong type declaration: not in declaration block")
                    stat = self.TryParseTypeDecl()
                    prog.AddChildStatement(stat)
                    return True
                
                if "globals" == self.tokens[self.i].Text:
                    if flags["isDeclPassed"]: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong global declaration: not in declaration block")
                    stat = self.TryParseGlobals()
                    prog.Chinds.append(stat)
                    return True
                
                if "constant" == self.tokens[self.i].Text or "native" == self.tokens[self.i].Text:
                    stat = self.TryParseNative()
                    if None != stat:
                        if flags["isDeclPassed"]: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong native declaration: not in declaration block")
                        prog.Chinds.append(stat)
                        return True
                    
                if "constant" == self.tokens[self.i].Text or "function" == self.tokens[self.i].Text:
                    if not flags["isDeclPassed"]: flags["isDeclPassed"] = True
                    stat = self.TryParseFunc()
                    prog.Chinds.append(stat)
                    return True

            if not iteration(flags): 
                break
            
            self.i += 1
        
        return prog
       
