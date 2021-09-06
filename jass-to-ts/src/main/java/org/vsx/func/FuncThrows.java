package org.vsx.func;

import java.util.function.Function;

@FunctionalInterface
public interface FuncThrows<TArg, TRes, TException extends Throwable> {
    TRes run(TArg arg) throws TException;

    public static <TArg, TRes, TException extends Throwable> Function<TArg, TRes> RunOrThrow(FuncThrows<TArg, TRes, TException> fnc) {
        return (x) -> {
            try {
                return fnc.run(x);
            }
            catch(Throwable e) {
                throw new RuntimeException(e);
            }
        };
    }
}
