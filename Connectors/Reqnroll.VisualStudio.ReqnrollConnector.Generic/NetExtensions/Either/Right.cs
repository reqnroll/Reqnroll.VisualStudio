// ReSharper disable once CheckNamespace

public class Right<TLeft, TRight> : Either<TLeft, TRight>
{
    public Right(TRight value)
    {
        Value = value;
    }

    private TRight Value { get; }

    public static implicit operator TRight(Right<TLeft, TRight> right)
        => right.Value;

    public static implicit operator Option<TRight>(Right<TLeft, TRight> right)
        => right.Value;

    public override string ToString() => $"Right({typeof(TLeft)}, {typeof(TRight)}){Value}";
}
