package org.vsx.jass;

import java.util.HashMap;
import java.util.Map;

/** Виды токенов */
public class TokenKind {
    /** однострочный комментарий */
    public static final String lcom = "lcom";
    /** 
     * многострочный комментарий (в JASS отсутствуют)
     * @deprecated в JASS только однострочные комментарии начинающиеся с //
     */
    @Deprecated
    public static final String mcom = "mcom";

    /** десятичное целое */
    public static final String ndec = "ndec";
    /** восьмеричное целое */
    public static final String oct = "oct";
    /** шеснадцатиричное целое в формате 0xNN */
    public static final String xhex = "xhex";
    /** шеснадцатиричное целое в формате $NN */
    public static final String dhex = "dhex";
    /** действительное */
    public static final String real = "real";
    /** шеснадцатиричное из 4х ASCII символов, записавыается в апостравах */
    public static final String adec = "adec";

    /** оператор */
    public static final String oper = "oper";

    /** строка в кавычках (в JASS единственный тип строк) */
    public static final String dstr = "dstr";
    /** 
     * строка в апострофах (, в JASS в апострафах хранятся целые, смотри <see cref="adec"/>) 
     * @deprecated не используется, в JASS единственный тип строк: заключённые в кавычках
     */
    @Deprecated
    public static final String sstr = "sstr";

    /** идентификатор */
    public static final String name = "name";

    /** базовый тип */
    public static final String btyp = "btyp";
    /** ключевое слово */
    public static final String kwd = "kwd";

    /** null значение */
    public static final String _null = "null";
    /** булевое значение */
    public static final String _bool = "bool";

    /** перевод строки */
    public static final String ln = "ln";

    /** левая скобка */
    public static final String lbra = "lbra";
    /** правая скобка */
    public static final String rbra = "rbra";
    /** левая квадратная скобка */
    public static final String lind = "lind";
    /** правая квадратная скобка */
    public static final String rind = "rind";

     /** макросы YDWE, пока что преобразуются в комментарий */
     public static final String ymacr = "ymacr";

    static HashMap<String, String> TypeByKind = new HashMap<String, String>(Map.ofEntries(
        Map.entry(lcom,  TokenType.comm),
        Map.entry(mcom,  TokenType.comm),
        Map.entry(ndec,  TokenType.val),
        Map.entry(oct,   TokenType.val),
        Map.entry(xhex,  TokenType.val),
        Map.entry(dhex,  TokenType.val),
        Map.entry(real,  TokenType.val),
        Map.entry(adec,  TokenType.val),
        Map.entry(oper,  TokenType.oper),
        Map.entry(dstr,  TokenType.val),
        Map.entry(sstr,  TokenType.val),
        Map.entry(name,  TokenType.name),
        Map.entry(btyp,  TokenType.name),
        Map.entry(kwd,   TokenType.kwd),
        Map.entry(_null, TokenType.val),
        Map.entry(_bool, TokenType.val),
        Map.entry(ln,    TokenType.br),
        Map.entry(lbra,  TokenType.par),
        Map.entry(rbra,  TokenType.par),
        Map.entry(lind,  TokenType.par),
        Map.entry(rind,  TokenType.par),

        Map.entry(ymacr, TokenType.comm)
    ));
        

    // static {

    // }

    /**
     * получить тип
     * @param kind вид токена
     * @return
     */
    public static String GetType(String kind) {
        return TypeByKind.get(kind);
    }
    
}
