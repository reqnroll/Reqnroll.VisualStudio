// ReSharper disable once CheckNamespace

public static class FunctionalExtensions
{
    public static TResult Map<TSource, TResult>(this TSource @this, Func<TSource, TResult> fn) => fn(@this);

    public static T Tie<T>(this T @this, Action<T> act)
    {
        act(@this);
        return @this;
    }
}
