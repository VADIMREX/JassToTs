package JassToTs

import (
	"fmt"
	"os"
)

const (
    TypeScript = iota
    TypeScriptDeclaration
    Lua
    GalaxyRaw
)

const help =
    "usable arguments:\n" +
    "-input           read arguments from user input\n" +
    "-i   <file path> set input file\n" +
    "-o   <file path> set output file \n" +
    "-ot  <file path> set output tree file\n" +
    "-nt              for saving tree file\n" +
    "-ydwe            for compatibility with YDWE jass\n" +
    "-op              for optimiztion\n" +
    "-dts             d.ts mode, make *.d.ts instead of *.ts file\n" +
    "-t               tree mode, will save tree file\n" +
    "-h               show this message\n" +
    "-lenient         less strict mode\n" +
    "";

var inPath = "";
var outPath = "";
var outTree = "";
var language int;
var isTreeNeeded = false;
var isYdweCompatible = false;
var isOptimizationNeeded = false;

func TranslateFile(ipath string, opath string, tpath string) error {
    fmt.Println("JASS to TypeScript translator (by VADIMREX)\n");

    var lexer = JassLexer(isYdweCompatible);
    /*var parser = new Jass.JassParser(isYdweCompatible);
    
    Console.WriteLine($"reading file {ipath}");
    string source;
    using (var sr = new StreamReader(ipath))
        source = sr.ReadToEnd();

    Console.WriteLine("lexeing");
    var tokens = lexer.Tokenize(source);

    Console.WriteLine("parsing");
    var tree = parser.Parse(tokens);

    if ("" != tpath)
    {
        Console.WriteLine($"saving tree into {tpath}");
        using (var sw = new StreamWriter(tpath))
            sw.WriteLine(tree.ToString());
    }

    Console.WriteLine("translating");
    var script = "";
    switch(language)
    {
        case Language.TypeScript:
        case Language.TypeScriptDeclaration:
            var tsConverter = new JassToTs(isOptimizationNeeded, isYdweCompatible, language == Language.TypeScriptDeclaration);
            script = tsConverter.Convert(tree);
            break;
        case Language.Lua:
            var luaConverter = new JassToLua(isOptimizationNeeded);
            script = luaConverter.Convert(tree);
            break;
        case Language.GalaxyRaw:
            var galaxyRawConverter = new JassToGalaxyRaw(isOptimizationNeeded);
            script = galaxyRawConverter.Convert(tree);
            break;
    }
    Console.WriteLine($"saving into {opath}");
    using (var sw = new StreamWriter(opath))
        sw.WriteLine(script);*/
    return nil
}

func main() {

	language = TypeScript;
	//for i, arg := range w.Args {
    for i := 0; i < len(os.Args); i++ {
        switch os.Args[i] {
            case "-input":
                fmt.Println("please enter arguments, empty line for continue");
            case "-i": 
                if i + 1 == len(os.Args) {
                    break
                }
                inPath = os.Args[i + 1]; i++; continue;
            case "-o": 
                if i + 1 == len(os.Args) {
                    break
                }
                outPath = os.Args[i + 1]; i++; continue;
            case "-ot": 
                if i + 1 == len(os.Args) {
                    break
                }
                outTree = os.Args[i + 1]; i++; continue;
            case "-nt": isTreeNeeded = true; continue;
            case "-ydwe": isYdweCompatible = true; continue;
            case "-op": isOptimizationNeeded = true; continue;
            case "-dts": language = TypeScriptDeclaration; continue;
            case "-t": language = TypeScriptDeclaration; continue;
            case "-lua": language = Lua; continue;
            case "-galaxy-raw": language = GalaxyRaw; continue;
            /*case "-lenient": 
                JassException.setIsStrict(false); 
                JassTranslatorException.setIsStrict(false);
                continue;*/
            case "-h":
                fmt.Println(help);
                os.Exit(0);
                return;
            default: 
                continue;
        }
        os.Exit(-1);
        return;
	}
    if "" == inPath {

    }

    if "" == outPath {

    }
    if isTreeNeeded && "" == outTree { 

    }

    // if TranslateFile(Path.of(inPath), Path.of(outPath), Path.of(outTree)) != nil {

    // }
    os.Exit(0);
}
