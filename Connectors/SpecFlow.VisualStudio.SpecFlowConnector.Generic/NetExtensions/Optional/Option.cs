﻿// ReSharper disable once CheckNamespace

public abstract class Option<T>
{
    public static implicit operator Option<T>(T? value) =>
        value is null
            ? new None<T>()
            : new Some<T>(value);

    public static implicit operator Option<T>(None _) => new None<T>();

    public abstract Option<TResult> Map<TResult>(Func<T, TResult> map);
    public abstract Option<TResult> MapOptional<TResult>(Func<T, Option<TResult>> map);
    public abstract Option<T> Or(Func<Option<T>> or);
    public abstract Option<T> Tie(Action<T> act);
    public abstract T Reduce(T whenNone);
    public abstract T Reduce(Func<T> whenNone);

    public abstract Option<TNew> OfType<TNew>() where TNew : class;
}
