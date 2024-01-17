// ReSharper disable once CheckNamespace

public class Left<TLeft, TRight> : Either<TLeft, TRight>
{
    public Left(TLeft value)
    {
        Value = value;
    }

    private TLeft Value { get; }

    public static implicit operator TLeft(Left<TLeft, TRight> left)
        => left.Value;

    public override string ToString() => $"Left({typeof(TLeft)}, {typeof(TRight)}){Value}";
}
