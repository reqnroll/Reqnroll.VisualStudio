// ReSharper disable once CheckNamespace

public static class EitherAdapters
{
    public static Either<TLeft, TNewRight> Map<TLeft, TRight, TNewRight>(this Either<TLeft, TRight> either,
        Func<TRight, TNewRight> map) =>
        either is Right<TLeft, TRight> right
            ? map(right)
            : (TLeft) (Left<TLeft, TRight>) either;

    public static Either<TLeft, TNewLeft> MapLeft<TLeft, TRight, TNewLeft>(this Either<TLeft, TRight> either,
        Func<TLeft, TNewLeft> map) =>
        either is Left<TLeft, TRight> left
            ? map(left)
            : (TLeft) (Left<TLeft, TRight>) either;

    public static Either<TLeft, TNewRight> Map<TLeft, TRight, TNewRight>(this Either<TLeft, TRight> either,
        Func<TRight, Either<TLeft, TNewRight>> map) =>
        either is Right<TLeft, TRight> right
            ? map(right)
            : (TLeft) (Left<TLeft, TRight>) either;

    public static Either<TLeft, TRight> Tie<TLeft, TRight>(this Either<TLeft, TRight> either,
        Action<TRight> tie)
    {
        if (either is Right<TLeft, TRight> right)
            tie(right);
        return either;
    }

    public static TRight Reduce<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TLeft, TRight> map) =>
        either is Left<TLeft, TRight> left
            ? map(left)
            : (Right<TLeft, TRight>) either;

    public static Either<Exception, T> Try<T>(this Func<T> act)
    {
        try
        {
            return act();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
