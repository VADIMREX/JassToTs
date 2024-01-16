package main

import (
	"fmt"
	"jass-to-ts/jass"
	"jass-to-ts/jassToTs"
	"os"
)

const (
	TypeScript = iota
	TypeScriptDeclaration
	Lua
	GalaxyRaw
)

const help = "usable arguments:\n" +
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
	""

var inPath = ""
var outPath = ""
var outTree = ""
var language int
var isTreeNeeded = false
var isOptimizationNeeded = false
var isYdweCompatible = false

func TranslateFile(ipath string, opath string, tpath string) error {
	fmt.Println("JASS to TypeScript translator (by VADIMREX)\n")

	var lexer = jass.NewJassLexer(isYdweCompatible)
	var parser = jass.NewJassParser(isYdweCompatible)

	fmt.Printf("reading file %s\n", ipath)
	var source = ""

	b, err := os.ReadFile(ipath)
	if err != nil {
		fmt.Print(err)
	}
	source = string(b)

	fmt.Println("lexeing")
	var tokens = lexer.Tokenize(source)

	fmt.Println("parsing")
	var tree = parser.Parse(tokens)

	if tpath != "" {
		fmt.Printf("saving tree into %s\n", tpath)
		b = []byte(tree.String())
		err = os.WriteFile(tpath, b, 0)
		if err != nil {
			fmt.Print(err)
		}
	}

	fmt.Println("translating")
	var script = ""
	switch language {
	case TypeScript,
		TypeScriptDeclaration:
		var tsConverter = jassToTs.NewJassToTs(isOptimizationNeeded, isYdweCompatible, language == TypeScriptDeclaration, 4)
		script = tsConverter.Convert(tree)
	case Lua:
		//   var luaConverter = new JassToLua(isOptimizationNeeded);
		//   script = luaConverter.Convert(tree);
	case GalaxyRaw:
		//   var galaxyRawConverter = new JassToGalaxyRaw(isOptimizationNeeded);
		//   script = galaxyRawConverter.Convert(tree);
	}
	fmt.Printf("saving into %s\n", opath)
	b = []byte(script)
	err = os.WriteFile(opath, b, 0)
	if err != nil {
		fmt.Print(err)
	}
	return nil
}

func main() {
	language = TypeScript
	//for i, arg := range w.Args {
	for i := 0; i < len(os.Args); i++ {
		switch os.Args[i] {
		case "-input":
			fmt.Println("please enter arguments, empty line for continue")
		case "-i":
			if i+1 == len(os.Args) {
				break
			}
			inPath = os.Args[i+1]
			i++
			continue
		case "-o":
			if i+1 == len(os.Args) {
				break
			}
			outPath = os.Args[i+1]
			i++
			continue
		case "-ot":
			if i+1 == len(os.Args) {
				break
			}
			outTree = os.Args[i+1]
			i++
			continue
		case "-nt":
			isTreeNeeded = true
			continue
		case "-ydwe":
			isYdweCompatible = true
			continue
		case "-op":
			isOptimizationNeeded = true
			continue
		case "-dts":
			language = TypeScriptDeclaration
			continue
		case "-t":
			language = TypeScriptDeclaration
			continue
		case "-lua":
			language = Lua
			continue
		case "-galaxy-raw":
			language = GalaxyRaw
			continue
		case "-lenient":
			jass.SetIsStrict(false)
			//JassTranslatorException.setIsStrict(false);
			continue
		case "-h":
			fmt.Println(help)
			os.Exit(0)
			return
		default:
			continue
		}
		os.Exit(-1)
		return
	}
	if inPath == "" {
		cwd, err := os.Getwd()
		if err != nil {
			return
		}
		files, err := os.ReadDir(cwd)
		if err != nil {
			return
		}
		for _, iPath := range files {
			info, err := iPath.Info()
			if err != nil {
				continue
			}
			info.Name()
		}
	}

	if outPath == "" {

	}
	if isTreeNeeded && "" == outTree {

	}

	if TranslateFile(inPath, outPath, outTree) != nil {

	}
	os.Exit(0)
}
