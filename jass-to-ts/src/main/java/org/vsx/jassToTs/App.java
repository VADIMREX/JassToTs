package org.vsx.jassToTs;

import java.io.BufferedInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileWriter;
import java.util.ArrayList;
import java.util.Scanner;

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
        "";
    
    static String inPath = "";
    static String outPath = "";
    static String outTree = "";
    static int language;
    static boolean isTreeNeeded = false;
    static boolean isOptimizationNeeded = false;
    static boolean isYdweCompatible = false;

    static void TranslateFile(String ipath, String opath, String tpath) throws Exception
    {
        System.out.println("JASS to TypeScript translator (by VADIMREX)\n");

        var lexer = new JassLexer();
        var parser = new JassParser(isYdweCompatible);
        
        System.out.println(String.format("reading file %s", ipath));
        String source = "";

        try {
            var lineBuffer = new StringBuffer(1024);
            var fin = new FileInputStream(new File(ipath));
            var bin = new BufferedInputStream(fin);
            int character;
            while((character=bin.read())!=-1) {
                lineBuffer.append((char) character);
            }
            source = lineBuffer.toString();
            bin.close();
            fin.close();
            
        } catch (FileNotFoundException e) {
            System.out.println("file not found");
        }
        
        System.out.println("lexeing");
        var tokens = lexer.Tokenize(source);

        System.out.println("parsing");
        var tree = parser.Parse(tokens);

        if ("" != tpath)
        {
            System.out.println(String.format("saving tree into {tpath}"));
            try {
                var fout = new FileWriter(tpath);
                fout.write(tree.toString());
                fout.close();
            } catch (FileNotFoundException e) {
                System.out.println("file not found");
            }
        }

        // System.out.println("translating");
        // var script = "";
        // switch(language)
        // {
        //     case Language.TypeScript:
        //     case Language.TypeScriptDeclaration:
        //         var tsConverter = new JassToTs(isOptimizationNeeded, language == Language.TypeScriptDeclaration);
        //         script = tsConverter.Convert(tree);
        //         break;
        //     case Language.Lua:
        //         var luaConverter = new JassToLua(isOptimizationNeeded);
        //         script = luaConverter.Convert(tree);
        //         break;
        //     case Language.GalaxyRaw:
        //         var galaxyRawConverter = new JassToGalaxyRaw(isOptimizationNeeded);
        //         script = galaxyRawConverter.Convert(tree);
        //         break;
        // }
        // System.out.println(String.format("saving into {opath}"));
        // using (var sw = new StreamWriter(opath))
        //     sw.WriteLine(script);
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
                    case "-h":
                        System.out.println(help);
                        System.exit(0);
                        return;
                    default: continue;
                }
                System.exit(-1);
                return;
            }

            // if ("" == inPath)
            // {
            //     var di = new DirectoryInfo(AppContext.BaseDirectory);
            //     foreach (var fi in di.GetFiles("*.j|*.ai"))
            //     {
            //         var iPath = fi.FullName;
            //         var oPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath));
            //         switch (language)
            //         {
            //             case Language.TypeScript: outPath += ".ts"; break;
            //             case Language.TypeScriptDeclaration: outPath += ".d.ts"; break;
            //             case Language.Lua: outPath += ".lua"; break;
            //             case Language.GalaxyRaw: outPath += ".galaxy"; break;
            //         }
            //         var tPath = "";
            //         if (isTreeNeeded)
            //             tPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + ".tree");

            //         try
            //         {
            //             TranslateFile(iPath, oPath, tPath);
            //         }
            //         catch (Exception e)
            //         {
            //             System.out.println(e.Message);
            //         }
            //     }
            //     System.exit(0);
            //     return;
            // }

            // if ("" == outPath)
            // {
            //     outPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath));
            //     switch (language)
            //     {
            //         case Language.TypeScript: outPath += ".ts"; break;
            //         case Language.TypeScriptDeclaration: outPath += ".d.ts"; break;
            //         case Language.Lua: outPath += ".lua"; break;
            //         case Language.GalaxyRaw: outPath += ".galaxy"; break;
            //     }
            // }
            // if (isTreeNeeded && "" == outTree) outTree = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + ".tree");

            try
            {
                TranslateFile(inPath, outPath, outTree);
            }
            catch (Exception e)
            {
                System.out.println(e.getMessage());
            }
            System.exit(0);
            return;
    }
}
