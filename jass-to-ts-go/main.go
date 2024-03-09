package main

import (
	"fmt"
	"jass-to-ts/jass"
	"jass-to-ts/translator"
	"os"
	"path"
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
	"\n"

var inPath = ""
var outPath = ""
var outTree = ""
var language int
var isTreeNeeded = false
var isOptimizationNeeded = false
var isYdweCompatible = false

func TranslateFile(ipath string, opath string, tpath string) error {
	fmt.Print("JASS to TypeScript translator (by VADIMREX)\n\n")

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
	tokens, err := lexer.Tokenize(source)
	if err != nil {
		return err
	}

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
		var tsConverter = translator.NewJassToTs(isOptimizationNeeded, isYdweCompatible, language == TypeScriptDeclaration, 4)
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
	for i := 1; i < len(os.Args); i++ {
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
			fmt.Print(help)
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
			fmt.Println(err)
			return
		}
		files, err := os.ReadDir(cwd)
		if err != nil {
			fmt.Println(err)
			return
		}
		for _, file := range files {
			fileName := file.Name()
			iPath := path.Join(cwd, fileName)
			if ext := path.Ext(fileName); ext != "" {
				fileName = fileName[:len(fileName)-len(ext)]
			}

			tPath := ""
			if isTreeNeeded {
				tPath = path.Join(cwd, fileName+".tree")
			}

			switch language {
			case TypeScript:
				fileName += ".ts"
			case TypeScriptDeclaration:
				outPath += ".d.ts"
			case Lua:
				outPath += ".lua"
			case GalaxyRaw:
				outPath += ".galaxy"
			}

			var oPath = path.Join(cwd, fileName)

			err = TranslateFile(iPath, oPath, tPath)
			if err != nil {
				fmt.Println(err)
			}
		}
		return
	}

	if outPath == "" {
		file, err := os.Stat(inPath)
		dPath := path.Dir(inPath)
		if err != nil {
			fmt.Println(err)
			return
		}
		outPath = file.Name()
		if ext := path.Ext(outPath); ext != "" {
			outPath = outPath[:len(outPath)-len(ext)]
		}

		switch language {
		case TypeScript:
			outPath += ".ts"
		case TypeScriptDeclaration:
			outPath += ".d.ts"
		case Lua:
			outPath += ".lua"
		case GalaxyRaw:
			outPath += ".galaxy"
		}

		outPath = path.Join(dPath, outPath)
	}
	if isTreeNeeded && "" == outTree {
		file, err := os.Stat(inPath)
		dPath := path.Base(inPath)
		if err != nil {
			fmt.Println(err)
			return
		}
		outTree = file.Name()
		if ext := path.Ext(outTree); ext != "" {
			outTree = outTree[:len(outTree)-len(ext)]
		}

		outTree = path.Join(dPath, outTree+".tree")
	}

	err := TranslateFile(inPath, outPath, outTree)
	if err != nil {
		fmt.Println(err)
	}
	os.Exit(0)
}
