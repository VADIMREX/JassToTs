package org.vsx.jassToTs;

public class JassTranslatorException extends Exception {
    static boolean isStrict = true;
    public static boolean getIsStrict() {
        return isStrict;
    }
    public static void setIsStrict(boolean value) {
        isStrict = value;
    }
    public static void Error(String message) throws JassTranslatorException {
        if (isStrict) throw new JassTranslatorException(message);
        System.out.println(message);
    }
    public JassTranslatorException(String message) {
        super(message);
    }
}
