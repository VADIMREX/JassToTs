package org.vsx.func;

@FunctionalInterface
public interface Func<TArg, TRes> {
    TRes run(TArg arg);
}
