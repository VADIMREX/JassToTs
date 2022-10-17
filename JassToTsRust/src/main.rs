use std::env;
use std::io::stdin;

mod Token;
mod JassLexer;

const help: &str = "usable arguments:
-input           read arguments from user input
-i   <file path> set input file
-o   <file path> set output file 
-ot  <file path> set output tree file
-nt              for saving tree file
-ydwe            for compatibility with YDWE jass
-op              for optimiztion
-dts             d.ts mode, make *.d.ts instead of *.ts file
-t               tree mode, will save tree file
-lua             set output language to lua (default language typescript)
-galaxy-raw      set output language to galaxy (not final version)
-h               show this message
-lenient         less strict mode
";

enum Language {
    TypeScript,
    TypeScriptDeclaration,
    Lua,
    GalaxyRaw,
}

fn TranslateFile(ipath: &String, opath: &String, tpath: &String, language: Language, isTreeNeeded: bool, isOptimizationNeeded: bool, isYdweCompatible: bool) {
    println!("JASS to TypeScript translator (by VADIMREX)\n");

    let mut lexer = JassLexer::JassLexer::new(isYdweCompatible);

    //var parser = new Jass.JassParser(isYdweCompatible);
            
    println!("reading file {}", ipath);
    let source: String = String::new();
    /*using (var sr = new StreamReader(ipath))
        source = sr.ReadToEnd();*/

    println!("lexeing");
    let tokens = lexer.Tokenize(&source);
}

fn main() {
    let mut inPath =  String::new();
    let mut outPath = String::new();
    let mut outTree = String::new();

    let mut language = Language::TypeScript;
    let mut isTreeNeeded = false;
    let mut isOptimizationNeeded = false;
    let mut isYdweCompatible = false;

    let mut args: Vec<_> = env::args().collect();
    
    let mut i = 1;
    while i < args.len() {
        match &args[i] as &str {
            "-input" => {
                println!("please enter arguments, empty line for continue");
                let mut j = 0;
                let mut lst = vec![];
                while j < 2 {
                    let mut s = String::new();
                    stdin().read_line(&mut s);

                    if "" == s {
                        j = j + 1;
                        continue;
                    }
                    j = 0;
                    lst.push(s);
                }
                args = lst;
                i = 0;
                continue;
            },
            "-i" => if i + 1 != args.len() { inPath = String::from(args[i + 1].as_str()); i = i + 2; continue; },
            "-o" => if i + 1 != args.len() { outPath = String::from(args[i + 1].as_str()); i = i + 2; continue; },
            "-ot" => if i + 1 != args.len() { outTree = String::from(args[i + 1].as_str()); i = i + 2; continue; },
            "-nt" => { isTreeNeeded = true; i = i + 1; continue;},
            "-ydwe" => { isYdweCompatible = true; i = i + 1; continue;},
            "-op" => { isOptimizationNeeded = true; i = i + 1; continue;},
            "-dts" => { language = Language::TypeScriptDeclaration; i = i + 1; continue;},
            "-t" => { language = Language::TypeScriptDeclaration; i = i + 1; continue;},
            "-lua" => { language = Language::Lua; i = i + 1; continue;},
            "-galaxy-raw" => { language = Language::GalaxyRaw; i = i + 1; continue;},
            "-lenient" => { 
                /*Jass.JassException.IsStrict = false;
                JassTranslatorException.IsStrict = false;*/
                i = i + 1;
                continue;
            },
            "-h" => {
                println!("{}", help);
                return;
            },
            _ => { i = i + 1; continue; }
        }
        return;
    }

    if "" == inPath {
        
    }
        
    if "" == outPath {
    
    }

    if isTreeNeeded && "" == outTree { /* outTree = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + ".tree");*/ }

    /*
    
    */
    {
        TranslateFile(&inPath, &outPath, &outTree, language, isTreeNeeded, isOptimizationNeeded, isYdweCompatible);
    }

}
