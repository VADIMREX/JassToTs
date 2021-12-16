import enum

# Виды токенов
class TokenKind(enum.Enum):
    # однострочный комментарий
    lcom = "lcom"
    # многострочный комментарий (в JASS отсутствуют)</summary>
    #[Obsolete("в JASS только однострочные комментарии начинающиеся с //")]
    mcom = "mcom"

    # десятичное целое
    ndec = "ndec"
    # восьмеричное целое
    oct = "oct"
    # шеснадцатиричное целое в формате 0xNN
    xhex = "xhex"
    # шеснадцатиричное целое в формате $NN
    dhex = "dhex"
    # действительное
    real = "real"
    # шеснадцатиричное из 4х ASCII символов, записавыается в апостравах
    adec = "adec"

    # оператор
    oper = "oper"

    # строка в кавычках (в JASS единственный тип строк)
    dstr = "dstr"
    # строка в апострофах (, в JASS в апострафах хранятся целые, смотри <see cref="adec"/>)
    #[Obsolete("не используется, в JASS единственный тип строк: заключённые в кавычках")]
    sstr = "sstr"

    # идентификатор
    name = "name"

    # базовый тип
    btyp = "btyp"
    # ключевое слово
    kwd = "kwd"

    # null значение
    null = "null"
    # булевое значение
    bool = "bool"

    # перевод строки
    ln = "ln"

    # левая скобка
    lbra = "lbra"
    # правая скобка
    rbra = "rbra"
    # левая квадратная скобка
    lind = "lind"
    # правая квадратная скобка
    rind = "rind"

    # макросы YDWE, пока что преобразуются в комментарий
    ymacr = "ymacr"

class TokenType(enum.Enum):
    # разделитель
    br = "br"
    # значение (безымянная константа)
    val = "val"
    # ключевое слово
    kwd = "kwd"
    # идентификатор
    name = "name"
    # оператор
    oper = "oper"
    # скобки
    par = "par"
    # комментарий
    comm = "comm"

# связь между видом и типом
TypeByKind = {
    TokenKind.lcom:  TokenType.comm,
    TokenKind.mcom:  TokenType.comm,
    TokenKind.ndec:  TokenType.val,
    TokenKind.oct:   TokenType.val,
    TokenKind.xhex:  TokenType.val,
    TokenKind.dhex:  TokenType.val,
    TokenKind.real:  TokenType.val,
    TokenKind.adec:  TokenType.val,
    TokenKind.oper:  TokenType.oper,
    TokenKind.dstr:  TokenType.val,
    TokenKind.sstr:  TokenType.val,
    TokenKind.name:  TokenType.name,
    TokenKind.btyp:  TokenType.name,
    TokenKind.kwd:   TokenType.kwd,
    TokenKind.null:  TokenType.val,
    TokenKind.bool:  TokenType.val,
    TokenKind.ln:    TokenType.br,
    TokenKind.lbra:  TokenType.par,
    TokenKind.rbra:  TokenType.par,
    TokenKind.lind:  TokenType.par,
    TokenKind.rind:  TokenType.par,
    # макросы считаем за коментарии
    TokenKind.ymacr: TokenType.comm
}

# получить тип
# <param name="kind"> вид токена </param>
def GetType(kind): 
    return TypeByKind[kind]

class Token:
    def __init__(self, col = 0, line = 0, pos = 0, text = "", kind = ""):
        self.Col = col
        self.Line = line
        self.Pos = pos
        self.Text = text
        self.Kind = kind

    def getType(self):
        return GetType(self.Kind)

    def __str__(self):
        return "{},{} [{}|{}]: {}".format(self.Line, self.Col, self.getType(), self.Kind, self.Text)

    def Clone(self):
        return Token(self.Col, self.Line, self.Pos, self.Text, self.Kind)
