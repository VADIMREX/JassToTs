
import sys
import pathlib
import io

import JassException
import JassTranslatorException
from Language import Language
from JassLexer import JassLexer
from JassParser import JassParser
from JassToTs import JassToTs

help = "usable arguments:\n" + \
    "-input           read arguments from user input\n" + \
    "-i   <file path> set input file\n" + \
    "-o   <file path> set output file \n" + \
    "-ot  <file path> set output tree file\n" + \
    "-nt              for saving tree file\n" + \
    "-ydwe            for compatibility with YDWE jass\n" + \
    "-op              for optimiztion\n" + \
    "-dts             d.ts mode, make *.d.ts instead of *.ts file\n" + \
    "-t               tree mode, will save tree file\n" + \
    "-h               show this message\n" + \
    "-lenient         less strict mode\n" + \
    ""
inPath = ""
outPath = ""
outTree = ""
language = Language.TypeScript
isTreeNeeded = False
isOptimizationNeeded = False
isYdweCompatible = False


def TranslateFile(ipath, opath, tpath):
    print("JASS to TypeScript translator (by VADIMREX)\n")

    lexer = JassLexer(isYdweCompatible)
    parser = JassParser(isYdweCompatible)

    print("reading file %s", ipath)

    source = ""

    try:
        f = io.open(ipath, mode="r", encoding="utf-8")
        source = f.read()
    except:
        print("file not found")

    print("lexing")

    tokens = lexer.Tokenize(source)

    print("parsing")
    tree = parser.Parse(tokens)

    if "" != tpath:
        print("saving tree into %s", tpath)
        try:
            f = io.open(tpath, mode="w", encoding="utf-8")
            f.write(tree)
        except Exception as e:
            print("file not found")

    print("translating")
    script = ""
    match language:
        case Language.TypeScript | Language.TypeScriptDeclaration:
            tsConverter = JassToTs(isOptimizationNeeded, isYdweCompatible, language == Language.TypeScriptDeclaration)
            script = tsConverter.Convert(tree)
        case Language.Lua:
            #
            pass
        case Language.GalaxyRaw:
            #
            pass

    print("saving into %s", opath)
    try:
        f = io.open(opath, mode="w", encoding="utf-8")
        f.write(script)
    except Exception as e:
        print("file not found")

i = 0
while i < len(sys.argv):
    match sys.argv[i]:
        case "-input":
            print("please enter arguments, empty line for continue")
            sys.argv.clear()
            j = 0
            while j < 2:
                s = input()
                if "" == s:
                    j += 1
                    i += 1
                    continue
                j = 0
                sys.argv.append(s)
            i = 0
            continue
        case "-i":
            if i + 1 == len(sys.argv):
                break
            inPath = sys.argv[i + 1]
            i += 2
            continue
        case "-o":
            if i + 1 == len(sys.argv):
                break
            outPath = sys.argv[i + 1]
            i += 2
            continue
        case "-ot":
            if i + 1 == len(sys.argv):
                break
            outTree = sys.argv[i + 1]
            i += 2
            continue
        case "-nt":
            isTreeNeeded = True
            i += 1
            continue
        case "-ydwe":
            isYdweCompatible = True
            i += 1
            continue
        case "-op":
            isOptimizationNeeded = True
            i += 1
            continue
        case "-dts":
            language = Language.TypeScriptDeclaration
            i += 1
            continue
        case "-t":
            language = Language.TypeScriptDeclaration
            i += 1
            continue
        case "-lua":
            language = Language.Lua
            i += 1
            continue
        case "-galaxy-raw":
            language = Language.GalaxyRaw
            i += 1
            continue
        case "-lenient":
            JassException.IsStrict = False
            JassTranslatorException.IsStrict = False
            i += 1
            continue
        case "-h":
            print(help)
            sys.exit(0)
        case _:
            i += 1
            continue
    sys.exit(-1)

if "" == inPath:
    cwd = pathlib.Path().resolve()

if "" == outPath:
    outPath = pathlib.Path(inPath)

if isTreeNeeded and "" == outTree:
    outTree = pathlib.Path(inPath)

try:
    TranslateFile(inPath, outPath, outTree)
except Exception as e:
    print(str(e))

sys.exit(0)
