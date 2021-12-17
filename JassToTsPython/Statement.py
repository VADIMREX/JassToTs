import enum

class StatementType(enum.Enum):
    Prog = "Prog"

    TypeDecl   = "TypeDecl"
    Glob       = "Glob"
    GConst     = "GConst"
    GVar       = "GVar"
    GArr       = "GArr"
    LVar       = "LVar"
    LArr       = "LArr"
    Expr       = "Expr"
    RVar       = "RVar"
    FCall      = "FCall"
    RArr       = "RArr"
    RFunc      = "RFunc"
    Native     = "Native"
    CNative    = "CNative"
    FuncDecl   = "FuncDecl"
    Params     = "Params"
    Func       = "Func"
    CFunc      = "CFunc"
    FuncLocals = "FuncLocals"
    FuncBody   = "FuncBody"
    Comm       = "Comm"
    Debug      = "Debug"
    Set        = "Set"
    ASet       = "ASet"
    If         = "If"
    Cond       = "Cond"
    Then       = "Then"
    ElseCond   = "ElseCond"
    Else       = "Else"
    Loop       = "Loop"
    Exit       = "Exit"
    Return     = "Return"

    TypeName = "TypeName"
    BaseType = "BaseType"
    Type     = "Type"
    Name     = "Name"
    Val      = "Val"
    Oper     = "Oper"
    Result   = "Result"
    Param    = "Param"
    Ind      = "Ind"
    Par      = "Par"

    YdweMacro = "YdweMacro"

class Statement:
    def __init__(self, parent = None, type = None, start = None):
        self.Parent = parent
        self.Type = type
        self.Start = start
        self.Childs = []
    
    # Создать и добавить дочернюю инструкцию
    # тип инструкции
    # токен с которого начинается потомок
    # дочернюю инструкцию
    def MakeChild(self, type, token):
        child = Statement(self, type, token)
        self.Childs.append(child)
        return child

    # <summary> Создать и добавить дочернюю инструкцию </summary>
    # <param name="type"> тип инструкции </param>
    # <param name="token"> токен с которого начинается потомок </param>
    # <returns> себя </returns>
    # <seealso cref="MakeChild(string,Token)"/>
    def AddChild(self, type, token):
        self.MakeChild(type, token)
        return self

    # <summary> Добавить дочернюю инструкцию </summary>
    # <param name="child"> дочерняя инструкция </param>
    # <returns> себя </returns>
    def AddChildStatement(self, child):
        child.Parent = self
        self.Childs.append(child)
        return self

    def __str__(self): return self.ToString(0)
    
    def ToString(self, ident) -> str:
        res = "// {}{} {}\n".format("".ljust(ident, ' '), self.Type, self.Start)
        if len(self.Childs) > 0:
            res += "".join(map(lambda x: x.ToString(ident + 2), self.Childs))
                  #"".join(x.ToString(ident + 2) for x in self.Childs)
        return res
            