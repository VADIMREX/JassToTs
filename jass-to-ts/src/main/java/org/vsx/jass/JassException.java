package org.vsx.jass;

public class JassException extends Exception {
    public JassException(int line, int col, String message) {
        super(String.format("Line %d, Col %d: %s", line, col, message));
    }
}
