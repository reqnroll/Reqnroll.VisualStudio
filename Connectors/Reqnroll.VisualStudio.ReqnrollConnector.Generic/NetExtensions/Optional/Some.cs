// ReSharper disable once CheckNamespace

public sealed class Some<T> : Option<T>, IEquatable<Some<T>>
{
    public Some(T value)
    {
        Content = value;
    }

    public T Content { get; }

    private string ContentToString =>
        Content?.ToString() ?? "<null>";

    public bool Equals(Some<T>? other) =>
        other?.GetType() == typeof(Some<T>) &&
        EqualityComparer<T>.Default.Equals(Content, other.Content);

    public static implicit operator T(Some<T> some) =>
        some.Content;

    public static implicit operator Some<T>(T value) =>
        new(value);

    public override Option<TResult> Map<TResult>(Func<T, TResult> map) =>
        map(Content);

    public override Option<TResult> MapOptional<TResult>(Func<T, Option<TResult>> map) =>
        map(Content);

    public override Option<T> Or(Func<Option<T>> or) => this;

    public override Option<T> Tie(Action<T> act)
    {
        act(Content);
        return this;
    }

    public override T Reduce(T whenNone) =>
        Content;

    public override T Reduce(Func<T> whenNone) =>
        Content;

    public override Option<TNew> OfType<TNew>() =>
        typeof(T).IsAssignableFrom(typeof(TNew))
            ? new Some<TNew>((Content as TNew)!)
            : new None<TNew>();

    public override string ToString() =>
        $"Some({ContentToString})";

    public override bool Equals(object? obj) =>
        Equals(obj as Some<T>);

    public override int GetHashCode() =>
        Content?.GetHashCode() ?? 0;

    public static bool operator ==(Some<T>? a, Some<T>? b) =>
        a?.Equals(b) ?? b is null;

    public static bool operator !=(Some<T> a, Some<T> b) => !(a == b);
}
