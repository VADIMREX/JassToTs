from Statement import Statement
from Token import TokenType, TokenKind
from Statement import StatementType
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
        stat = Statement(type = StatementType.TypeDecl)

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
        stat = Statement(type = StatementType.Glob)

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
        # type id ('=' expr)? | type 'array' id 
        stat = Statement()

        flags = { 
            "IsLocal": False,
            "IsArray": False
        }

        # local не обязательно
        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "local" != self.tokens[self.i].Text:
                j += 1
                return state1(j)
            stat.Start = self.tokens[self.i]
            flags["IsLocal"] = True
            return j + 1
        #   тип
        def state1(j):
            if TokenKind.name != self.tokens[self.i].Kind and TokenKind.btyp != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong var declaration: type identifier expected")
            if None == stat.Start: stat.Start = self.tokens[self.i]
            stat.AddChild("Type", self.tokens[self.i])
            return j + 1
        #     array не обязательно
        def state2(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "array" != self.tokens[self.i].Text:
                j += 1
                return state3(j)
            flags["IsArray"] = True
            return j + 1
        #       имя
        def state3(j):
            if TokenKind.name != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong var declaration: identifier expected")
            stat.AddChild("Name", self.tokens[self.i])
            if flags["IsArray"]:
                return j + 2
            else:
                return j + 1
        #         = значение по умолчанию не обязательно
        def state4(j):
            if TokenKind.oper != self.tokens[self.i].Kind or "=" != self.tokens[self.i].Text:
                return state5(j + 1)
            self.i += 1
            expr = self.TryParseExpression()
            stat.AddChildStatement(expr)
            return j + 1
        #           конец строки
        def state5(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong var declaration: new line expected")
            return j + 1

        states = [state0, state1, state2, state3, state4, state5]

        if None == self.moveStates(stat, states): return None

        stat.Type = ""
        if flags["IsLocal"]:
            stat.Type += "L"
        if flags["IsArray"]:
            stat.Type += "Arr"
        else:
            stat.Type += "Var"

        if self.i < len(self.tokens): self.i -= 1
        return stat

    #endregion

    #region expression
    # Остановить если встретился перевод строки 
    def DefStopper(self, token): return TokenType.br == token.Type

    # Попытаться распарсить выражение 
    #/ <param name="stopper"> Предикат используемый для остановки парсинга выражения, по умолчанию <see cref="DefStopper(Token)"/></param>
    def TryParseExpression(self, stopper = None) -> Statement:
        if None == stopper: stopper = self.DefStopper
        #('constant' type id '=' expr newline | var_declr newline)*
        stat = Statement(type = StatementType.Expr, start = self.tokens[self.i])
        par = []
        child = None

        while self.i < len(self.tokens):
            def iteration(stat):
                if self.AddComment(stat): return True, stat
                if 0 == len(par) and stopper(self.tokens[self.i]): return False, stat
                match self.tokens[self.i].Type:
                    case TokenType.par:
                        match self.tokens[self.i].Kind:
                            case TokenKind.lbra | TokenKind.lind:
                                par.append(stat)
                                stat = stat.MakeChild("Par", self.tokens[self.i])
                                return True, stat
                            case TokenKind.rbra | TokenKind.rind:
                                if "Par" != stat.Type:
                                    JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: unexpected closing parenthes")
                                if TokenKind.lbra != stat.Start.Kind and TokenKind.rbra == self.tokens[self.i].Kind:
                                    JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: another parenthes expected")
                                if TokenKind.lind != stat.Start.Kind and TokenKind.rind == self.tokens[self.i].Kind:
                                    JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: another parenthes expected")
                                stat = par.pop()
                                return True, stat
                        JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: unexpected parenthes kind")
                        return True, stat
                    case TokenType.val:
                        stat.AddChild("Val", self.tokens[self.i])
                        return True, stat
                    case TokenType.oper:
                        stat.AddChild("Oper", self.tokens[self.i])
                        return True, stat
                    case TokenType.kwd:
                        stat.AddChildStatement(self.TryParseFuncRef())
                        return True, stat
                    case TokenType._name:
                        # warning Потенциальные дженерики, хз что с ними делать, пока что записываем строкой
                        if self.isYdweCompatible and TokenKind.btyp == self.tokens[self.i].Kind:
                            ctoken = self.tokens[self.i].Clone()
                            ctoken.Text = '"{}"'.format(ctoken.Text)
                            child = Statement(type = StatementType.RVar, start = ctoken)
                            stat.AddChildStatement(child)
                            return True, stat
                        child = self.TryParseArrayRef()
                        if None == child:
                            child = self.TryParseFuncCall()
                        if None == child:
                            child = Statement(type = StatementType.RVar, start = self.tokens[self.i])
                        stat.AddChildStatement(child)
                        return True, stat
                return False
            b, stat = iteration(stat)
            if not b: break
            self.i += 1

        #if (0 == stat.Childs.Count) JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: empty expression")
        if self.i < len(self.tokens): self.i -= 1
        if 1 == len(stat.Childs): return stat.Childs[0]
        return stat

    # Остановить если найден перево строки, запятая или закрывающая скобка 
    #/ <code>token => TokenKind.ln == token.Kind or (TokenKind.oper == token.Kind and "," == token.Text)</code>
    def ArgStopper(self, token):
        return TokenKind.ln == token.Kind or \
               TokenKind.rbra == token.Kind or \
               (TokenKind.oper == token.Kind and "," == token.Text)

    # Попытаться распарсить вызов функции 
    def TryParseFuncCall(self) -> Statement:
        stat = Statement(type = StatementType.FCall, start = self.tokens[self.i])
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
        return TokenKind.ln == token.Kind or \
               TokenKind.rind == token.Kind

    # Попытаться распарсить ссылку на элемент массива 
    def TryParseArrayRef(self) -> Statement:
        stat = Statement(type = StatementType.RArr, start = self.tokens[self.i])

        start = self.i

        def state0(j):
            if TokenKind.name != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: identifier expected")
            stat.AddChild("Name", self.tokens[self.i])
            return j + 1
        def state1(j):
            if TokenKind.lind != self.tokens[self.i].Kind:
                self.i = start
                return None
            return j + 1
        def state2(j):
            ind = self.TryParseExpression(self.IndStopper)
            if "Expr" == ind.Type and 0 == ind.Childs.Count:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: index expected")
            stat.MakeChild("Ind", ind.Start) \
                .AddChildStatement(ind)
            return j + 1
        def state3(j):
            if TokenKind.rind != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: comma or parenthes expected")
            return j + 1
        
        states = [state0, state1, state2, state3]
        
        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat

    # Попытаться распарсить ссылку на функцию 
    def TryParseFuncRef(self) -> Statement:
        stat = Statement(type = StatementType.RFunc, start = self.tokens[self.i])

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind and "function" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: function expected")
            return j + 1
        def state1(j):
            if TokenKind.name != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: identifier expected")
            stat.Start = self.tokens[self.i]
            return j + 1

        states = [state0, state1]
        
        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat

    #endregion

    #region functions

    # Попытаться распарсить объявление нативной функции 
    def TryParseNative(self) -> Statement:
        stat = Statement(type = StatementType.Native)
        flags = { "IsConst": False }

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "constant" != self.tokens[self.i].Text:
                return state1(j + 1)
            stat.Start = self.tokens[self.i]
            flags["IsConst"] = True
            return j + 1
        def state1(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "native" != self.tokens[self.i].Text: return None
            if None == stat.Start: stat.Start = self.tokens[self.i]
            return j + 1
        def state2(j):
            stat.AddChildStatement(self.TryParseFuncDecl())
            return j + 1
        def state3(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong native declaration: new line expected")
            return j + 1
        
        states = [state0, state1, state2, state3]
        
        if None == self.moveStates(stat, states): return None

        if flags["IsConst"]: stat.Type = "CNative"
        
        if self.i < len(self.tokens): self.i -= 1
        return stat

    # Попытаться распарсить объявление функции 
    def TryParseFuncDecl(self) -> Statement:
        stat = Statement(type = StatementType.FuncDecl)

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
            if TokenType._name == self.tokens[self.i].Type and "nothing" == self.tokens[self.i].Text:
                stat.AddChild("Params", self.tokens[self.i])
            else:
                stat.AddChildStatement(self.TryParseParams())
            return j + 1
        def state3(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "returns" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: returns keyword expected")
            return j + 1
        def state4(j):
            if (TokenType._name == self.tokens[self.i].Type and "nothing" == self.tokens[self.i].Text) or \
                TokenType._name == self.tokens[self.i].Type:
                stat.AddChild("Result", self.tokens[self.i])
            else:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: nothing or type name expected")
            return j + 1
        
        states = [state0, state1, state2, state3, state4]

        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat


    # Попытаться распарсить параметры функции 
    def TryParseParams(self) -> Statement:
        stat = Statement(type = StatementType.Params)

        type = None
        def state0(j):
            if TokenType._name != self.tokens[self.i].Type:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: type name expected")
            type = self.tokens[self.i]
            return j + 1
        def state1(j):
            if TokenType._name != self.tokens[self.i].Type:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong function declaration: param name expected")
            stat.MakeChild("Param", type) \
                .AddChild("Type", type) \
                .AddChild("Name", self.tokens[self.i])
            return j + 1
        def state2(j):
            if TokenKind.oper == self.tokens[self.i].Kind and "," == self.tokens[self.i].Text:
                j -= 2
            else: 
                if TokenKind.kwd == self.tokens[self.i].Kind and "returns" == self.tokens[self.i].Text:
                    j += 1
                else:
                    JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong expression: comma or returns expected")
            return j

        states = [state0, state1, state2]

        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens) + 1: self.i -= 2
        return stat

    # Попытаться распарсить функцию 
    def TryParseFunc(self) -> Statement:
        stat = Statement(type = StatementType.Func)
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
    def TryParseFuncLocals(self) -> Statement:
        stat = Statement(type = StatementType.FuncLocals)

        while self.i < len(self.tokens):
            if self.AddComment(stat):
                self.i += 1
                continue
            if not TokenKind.ln == self.tokens[self.i].Kind:
                self.i += 1
                continue
            if TokenKind.kwd == self.tokens[self.i].Kind and "endfunction" == self.tokens[self.i].Text:
                break
            if TokenKind.kwd != self.tokens[self.i].Kind or "local" != self.tokens[self.i].Text:
                break        
            stat.AddChildStatement(self.TryParseVarDecl())
            self.i += 1

        if self.i < len(self.tokens): self.i-=1
        return stat

    # Попытаться распарсит тело функции 
    def TryParseFuncBody(self) -> Statement:
        stat = Statement(type = StatementType.FuncBody)

        while self.i < len(self.tokens):
            if self.AddComment(stat):
                self.i += 1
                continue
            if TokenKind.ln == self.tokens[self.i].Kind:
                self.i += 1
                continue

            if TokenKind.kwd == self.tokens[self.i].Kind and "endfunction" == self.tokens[self.i].Text:
                break

            stat.AddChildStatement(self.TryParseStatement())
            self.i += 1

        if self.i < len(self.tokens): self.i-=1
        return stat

    #endregion

    #region statement

    def TryParseMacroCall(self) -> Statement:
        sb = "#"
        stoken = self.tokens[self.i].Clone()
        while self.i < len(self.tokens):
            if TokenKind.ln == self.tokens[self.i].Kind: break
            sb += " {}".format(self.tokens[self.i].Text)
            self.i += 1
        stoken.Text = sb
        return Statement(type = StatementType.YdweMacro, start = stoken)

    # Попытаться распарсить инструкцию 
    def TryParseStatement(self) -> Statement:
        while self.i < len(self.tokens):
            if TokenType.comm == self.tokens[self.i].Type:
                return Statement(type = StatementType.Comm, start = self.tokens[self.i])

            if TokenKind.ln == self.tokens[self.i].Kind: break

            #warning Вызов потенциального макроса, у парсера макросы должны быть выпилены
            if self.isYdweCompatible and TokenKind.name == self.tokens[self.i].Kind:
                return self.TryParseMacroCall()
            if TokenKind.kwd != self.tokens[self.i].Kind: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "statement error: keyword expected")

            match self.tokens[self.i].Text:
                case "set":
                    return self.TryParseSet()
                case "call":
                    return self.TryParseCall()
                case "if":
                    return self.TryParseIf()
                case "loop":
                    return self.TryParseLoop()
                case "return":
                    return self.TryParseReturn()
                case "exitwhen":
                    return self.TryParseExit()
                case "debug":
                    self.i += 1
                    dbg = self.TryParseStatement()
                    if "Return" == dbg.Type or "Debug" == dbg.Type:
                        JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "statement error: wrong statement")
                    stat = Statement(type = StatementType.Debug)
                    stat.AddChildStatement(dbg)
                    return stat
            JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "statement error: unknown keyword")

            self.i += 1
        return Statement(type = "Error", start = self.tokens[self.i])

    def TryParseSet(self) -> Statement:
        stat = Statement(type = StatementType.Set)

        flags = { "IsArray": False }

        # constant не обязательно
        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "set" != self.tokens[self.i].Text: return None
            if None == stat.Start: stat.Start = self.tokens[self.i]
            return j + 1
        def state1(j):
            if TokenType._name != self.tokens[self.i].Type:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong set statement: variable name expected")
            stat.AddChild("Name", self.tokens[self.i])
            return j + 1
        def state2(j):
            if TokenKind.lind != self.tokens[self.i].Kind:
                state3(j + 1)
            flags["IsArray"] = True
            self.i += 1
            ind = self.TryParseExpression(self.IndStopper)
            stat.MakeChild("Ind", ind.Start) \
                .AddChildStatement(ind)
            self.i += 1
            return j + 1
        def state3(j):
            if TokenKind.oper != self.tokens[self.i].Kind or "=" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong set statement: = expected")
            self.i += 1
            expr = self.TryParseExpression()
            stat.AddChildStatement(expr)
            return j + 1
        def state4(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong native declaration: new line expected")
            return j + 1

        states = [state0, state1, state2, state3, state4]

        if None == self.moveStates(stat, states): return None

        if flags["IsArray"]: stat.Type = StatementType.ASet

        if self.i < len(self.tokens): self.i -= 1
        return stat

    def TryParseCall(self) -> Statement:
        stats = {"stat": None}

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "call" != self.tokens[self.i].Text: return None
            return j + 1
        def state1(j):
            stats["stat"] = self.TryParseFuncCall()
            return j + 1
        def state2(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong native declaration: new line expected")
            return j + 1

        states = [state0, state1, state2]

        if None == self.moveStates(stats["stat"], states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stats["stat"]

    def IfStopper(self, token):
        return TokenKind.ln == token.Kind or \
              (TokenKind.kwd == token.Kind and "then" == token.Text)

    def TryParseIf(self) -> Statement:
        # /*
        # ifthenelse      := 'if' expr 'then' newline 
        #                     statement_list else_clause? 'endif' 
        # else_clause     := 'else' newline statement_list | 
        #                     'elseif' expr 'then' newline statement_list else_clause?
        # */
        stats = {
            "stat": Statement(type = StatementType.If),
            "cond": Statement(type = StatementType.Cond),
            "then": None
        }
        
        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "if" != self.tokens[self.i].Text: return None
            return j + 1
        def state1(j):
            stats["cond"].AddChildStatement(self.TryParseExpression(self.IfStopper))
            stats["stat"].AddChildStatement(stats["cond"])
            return j + 1
        def state2(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "then" != self.tokens[self.i].Text:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong if statement: then expected")
            stats["then"] = Statement(type = StatementType.Then)
            stats["stat"].AddChildStatement(stats["then"])
            return j + 1
        def state3(j): return state7(j)
        def state5(j): return state7(j)
        def state7(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong if statement: new line expected")
            return j + 1
        def state4(j):
            if TokenKind.ln == self.tokens[self.i].Kind: return j
            if TokenKind.kwd == self.tokens[self.i].Kind and "elseif" == self.tokens[self.i].Text:
                stats["cond"] = Statement(type = StatementType.ElseCond)
                return j - 3
            if TokenKind.kwd == self.tokens[self.i].Kind and "else" == self.tokens[self.i].Text:
                stats["then"] = Statement(type = StatementType.Else)
                stats["stat"].AddChildStatement(stats["then"])
                return j + 1
            if TokenKind.kwd == self.tokens[self.i].Kind and "endif" == self.tokens[self.i].Text:
                return j + 3
            stats["then"].AddChildStatement(self.TryParseStatement())
            return j
        def state6(j):
            if TokenKind.ln == self.tokens[self.i].Kind: return j
            if TokenKind.kwd == self.tokens[self.i].Kind and "endif" == self.tokens[self.i].Text:
                return j + 1
            stats["then"].AddChildStatement(self.TryParseStatement())
            return j
        
        states = [state0, state1, state2, state3, state4, state5, state6, state7]

        if None == self.moveStates(stats["stat"], states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stats["stat"]

    def TryParseLoop(self) -> Statement:
        stat = Statement(type = StatementType.Loop)

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "loop" != self.tokens[self.i].Text: return None
            return j + 1
        def state1(j):
            if TokenKind.ln == self.tokens[self.i].Kind: return j
            if TokenKind.kwd == self.tokens[self.i].Kind and "endloop" == self.tokens[self.i].Text:
                return j + 1
            #if (TokenKind.kwd == self.tokens[self.i].Kind and "exitwhen" == self.tokens[self.i].Text)
            #{
            #    self.i += 1
            #    stat.MakeChild("Exit", self.tokens[self.i])
            #        .AddChildStatement(TryParseExpression())
            #    self.i += 1
            #    continue
            #}
            stat.AddChildStatement(self.TryParseStatement())
            return j
        def state2(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong loop statement: new line expected")
            return j + 1

        states = [state0, state1, state2]

        if None == self.moveStates(stat, states): return None

        if self.i < len(self.tokens): self.i -= 1
        return stat

    def TryParseExit(self) -> Statement:
        # *return          := 'return' expr?
        stat = Statement(type = "Exit")

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "exitwhen" != self.tokens[self.i].Text: return None
            return j + 1
        def state1(j):
            stat.AddChildStatement(self.TryParseExpression())
            return j + 1
        def state2(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong exitwhen statement: new line expected")
            return j + 1

        states = [state0, state1, state2]

        while self.i < len(self.tokens) and j < len(states):
            nj = states[j](j)
            if None == nj: return None
            j = nj
        
            self.i += 1

        if self.i < len(self.tokens): self.i -= 1
        return stat

    def TryParseReturn(self) -> Statement:
        # *return          := 'return' expr?
        stat = Statement(type = StatementType.Return)

        def state0(j):
            if TokenKind.kwd != self.tokens[self.i].Kind or "return" != self.tokens[self.i].Text: return None
            return j + 1
        def state1(j):
            stat.AddChildStatement(self.TryParseExpression())
            return j + 1
        def state2(j):
            if TokenKind.ln != self.tokens[self.i].Kind:
                JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong return statement: new line expected")
            return j + 1

        states = [state0, state1, state2]

        while self.i < len(self.tokens) and j < len(states):
            nj = states[j](j)
            if None == nj: return None
            j = nj
        
            self.i += 1

        if self.i < len(self.tokens): self.i -= 1
        return stat


    #endregion
    
    def Parse(self, tokens) -> Statement:
        self.tokens = tokens

        prog = Statement(type="Prog")
        flags = { "isDeclPassed": False }
        stats = { "stat": None }

        self.i = 0
        while self.i < len(self.tokens):
            def iteration(flags):
                if self.AddComment(prog): return True

                if TokenKind.ln == self.tokens[self.i].Kind: return True

                if TokenKind.kwd != self.tokens[self.i].Kind: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "error: keyword expected")

                if "type" == self.tokens[self.i].Text:
                    if flags["isDeclPassed"]: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong type declaration: not in declaration block")
                    stats["stat"] = self.TryParseTypeDecl()
                    prog.AddChildStatement(stats["stat"])
                    return True
                
                if "globals" == self.tokens[self.i].Text:
                    if flags["isDeclPassed"]: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong global declaration: not in declaration block")
                    stats["stat"] = self.TryParseGlobals()
                    prog.Childs.append(stats["stat"])
                    return True
                
                if "constant" == self.tokens[self.i].Text or "native" == self.tokens[self.i].Text:
                    stats["stat"] = self.TryParseNative()
                    if None != stats["stat"]:
                        if flags["isDeclPassed"]: JassException.Error(self.tokens[self.i].Line, self.tokens[self.i].Col, "wrong native declaration: not in declaration block")
                        prog.Childs.append(stats["stat"])
                        return True
                    
                if "constant" == self.tokens[self.i].Text or "function" == self.tokens[self.i].Text:
                    if not flags["isDeclPassed"]: flags["isDeclPassed"] = True
                    stats["stat"] = self.TryParseFunc()
                    prog.Childs.append(stats["stat"])
                    return True

            if not iteration(flags): 
                break
            
            self.i += 1
        
        return prog