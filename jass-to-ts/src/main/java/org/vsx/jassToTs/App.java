package org.vsx.jassToTs;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Scanner;

import org.vsx.jass.JassException;
import org.vsx.jass.JassLexer;
import org.vsx.jass.JassParser;

public class App 
{
    final static String help =
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
    
    static String inPath = "";
    static String outPath = "";
    static String outTree = "";
    static int language;
    static boolean isTreeNeeded = false;
    static boolean isOptimizationNeeded = false;
    static boolean isYdweCompatible = false;

    static void TranslateFile(Path ipath, Path opath, Path tpath) throws Exception
    {
        System.out.println("JASS to TypeScript translator (by VADIMREX)\n");

        var lexer = new JassLexer(isYdweCompatible);
        var parser = new JassParser(isYdweCompatible);
        
        System.out.println(String.format("reading file %s", ipath));
        String source = "";

        try {
            source = Files.readString(ipath, StandardCharsets.UTF_8);
        } catch (IOException e) {
            System.out.println("file not found");
        }
        
        System.out.println("lexeing");
        var tokens = lexer.Tokenize(source);

        System.out.println("parsing");
        var tree = parser.Parse(tokens);

        if (!"".equals(tpath.toString()))
        {
            System.out.println(String.format("saving tree into %s", tpath));
            try {
                Files.writeString(tpath, tree.toString(), StandardCharsets.UTF_8);
            } catch (IOException e) {
                System.out.println("file not found");
            }
        }

        System.out.println("translating");
        var script = "";
        switch(language)
        {
            case Language.TypeScript:
            case Language.TypeScriptDeclaration:
                var tsConverter = new JassToTs(isOptimizationNeeded, isYdweCompatible, language == Language.TypeScriptDeclaration);
                script = tsConverter.Convert(tree);
                break;
            case Language.Lua:
                // var luaConverter = new JassToLua(isOptimizationNeeded);
                // script = luaConverter.Convert(tree);
                break;
            case Language.GalaxyRaw:
                // var galaxyRawConverter = new JassToGalaxyRaw(isOptimizationNeeded);
                // script = galaxyRawConverter.Convert(tree);
                break;
        }
        System.out.println(String.format("saving into %s", opath));
        try {
            Files.writeString(opath, script, StandardCharsets.UTF_8);
        } catch (IOException e) {
            System.out.println("file not found");
        }
    }

    public static void main( String[] args )
    {
        language = Language.TypeScript;
            for (var i = 0; i < args.length; i++)
            {
                switch (args[i])
                {
                    case "-input":
                        System.out.println("please enter arguments, empty line for continue");
                        var scan = new Scanner(System.in);
                        var lst = new ArrayList<String>();
                        int j = 0;
                        while (j < 2)
                        {
                            var s = scan.nextLine();
                            if ("" == s)
                            {
                                j++;
                                continue;
                            }
                            j = 0;
                            lst.add(s);
                        }
                        args = lst.toArray(new String[0]);
                        i = -1;
                        continue;
                    case "-i": if (i + 1 == args.length) break; inPath = args[i + 1]; i++; continue;
                    case "-o": if (i + 1 == args.length) break; outPath = args[i + 1]; i++; continue;
                    case "-ot": if (i + 1 == args.length) break; outTree = args[i + 1]; i++; continue;
                    case "-nt": isTreeNeeded = true; continue;
                    case "-ydwe": isYdweCompatible = true; continue;
                    case "-op": isOptimizationNeeded = true; continue;case "-dts": language = Language.TypeScriptDeclaration; continue;
                    case "-t": language = Language.TypeScriptDeclaration; continue;
                    case "-lua": language = Language.Lua; continue;
                    case "-galaxy-raw": language = Language.GalaxyRaw; continue;
                    case "-lenient": 
                        JassException.setIsStrict(false); 
                        JassTranslatorException.setIsStrict(false);
                        continue;
                    case "-h":
                        System.out.println(help);
                        System.exit(0);
                        return;
                    default: continue;
                }
                System.exit(-1);
                return;
            }

            if ("".equals(inPath))
            {
                var cwd = Paths.get(".");
                try (var stream = Files.newDirectoryStream(cwd, "*.{j,ai}")) {
                    for (var iPath: stream) {
                        var fileName = iPath.getFileName().toString();
                        var extensionPos = fileName.lastIndexOf(".");
                        if (extensionPos > 0) fileName = fileName.substring(0, extensionPos); 

                        Path tPath = null;
                        if (isTreeNeeded)
                            tPath = iPath.getParent()
                                         .resolve(fileName + ".tree");
                        
                        switch (language)
                        {
                            case Language.TypeScript: fileName += ".ts"; break;
                            case Language.TypeScriptDeclaration: fileName += ".d.ts"; break;
                            case Language.Lua: fileName += ".lua"; break;
                            case Language.GalaxyRaw: fileName += ".galaxy"; break;
                        }

                        var oPath = iPath.getParent()
                                         .resolve(fileName);
                        
                        try
                        {
                            TranslateFile(iPath, oPath, tPath);
                        }
                        catch (Exception e)
                        {
                            System.out.println(e.getMessage());
                        }
                    }
                } catch (IOException x) {
                    System.out.println(x.getMessage());
                    // throw new RuntimeException(String.format("error reading folder %s: %s",
                    // dir,
                    // x.getMessage()),
                    // x);
                }

                System.exit(0);
                return;
            }

            if ("".equals(outPath))
            {
                outPath = Path.of(inPath)
                              .getFileName()
                              .toString();

                var extensionPos = outPath.lastIndexOf(".");
                if (extensionPos > 0) outPath = outPath.substring(0, extensionPos); 

                switch (language)
                {
                    case Language.TypeScript: outPath += ".ts"; break;
                    case Language.TypeScriptDeclaration: outPath += ".d.ts"; break;
                    case Language.Lua: outPath += ".lua"; break;
                    case Language.GalaxyRaw: outPath += ".galaxy"; break;
                }

                outPath = Path.of(inPath)
                              .getParent()
                              .resolve(outPath)
                              .toString();
            }
            if (isTreeNeeded && "".equals(outTree)) { 
                outTree = Path.of(inPath)
                              .getFileName()
                              .toString();
                var extensionPos = outTree.lastIndexOf(".");
                if (extensionPos > 0) outTree = outTree.substring(0, extensionPos); 

                outTree = Path.of(inPath)
                              .getParent()
                              .resolve(outTree + ".tree")
                              .toString();
            }

            try
            {
                TranslateFile(Path.of(inPath), Path.of(outPath), Path.of(outTree));
            }
            catch (Exception e)
            {
                System.out.println(e.getMessage());
                for (var a : e.getStackTrace())
                    System.out.println(a);
            }
            System.exit(0);
            return;
    }
}
