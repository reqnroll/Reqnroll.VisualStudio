// ReSharper disable once CheckNamespace

public abstract class Either<TLeft, TRight>
{
    public static implicit operator Either<TLeft, TRight>(TLeft left) => new Left<TLeft, TRight>(left);

    public static implicit operator Either<TLeft, TRight>(TRight left) => new Right<TLeft, TRight>(left);
}
