package org.vsx.jass;

public class JassException extends Exception {
    static boolean isStrict = true;
    public static boolean getIsStrict() {
        return isStrict;
    }
    public static void setIsStrict(boolean value) {
        isStrict = value;
    }
    static String formatMessage(int line, int col, String message) {
        return String.format("Line %d, Col %d: %s", line, col, message);
    }

    public static void Error(int line, int col, String message) 
    throws JassException
    {
        if (isStrict) throw new JassException(line, col, message);
        System.out.println(formatMessage(line, col, message));
    }
    
    public JassException(int line, int col, String message) {
        super(formatMessage(line, col, message));
    }
}
