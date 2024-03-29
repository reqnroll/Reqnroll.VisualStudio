﻿// ReSharper disable once CheckNamespace

public static class EnumerableExtensions
{
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> sequence) =>
        sequence.Select(x => (Option<T>) new Some<T>(x))
            .DefaultIfEmpty(new None<T>())
            .First();

    public static Option<T> FirstOrNone<T>(this IEnumerable<T> sequence, Func<T, bool> predicate) =>
        sequence.Where(predicate).FirstOrNone();

    public static IEnumerable<TResult> SelectOptional<T, TResult>(
        this IEnumerable<T> sequence, Func<T, Option<TResult>> map) =>
        sequence.Select(map)
            .OfType<Some<TResult>>()
            .Select(some => some.Content);
}
