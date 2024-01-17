namespace ReqnrollConnector.Logging;

[DebuggerDisplay("{_analyticsProperties}")]
public class AnalyticsContainer : IDictionary<string, string>, IAnalyticsContainer
{
    private readonly IDictionary<string, string> _analyticsProperties;

    public AnalyticsContainer()
    {
        _analyticsProperties = new Dictionary<string, string>();
    }

    public AnalyticsContainer(IDictionary<string, string> analyticsProperties)
    {
        _analyticsProperties = analyticsProperties;
    }

    public void AddAnalyticsProperty(string key, string value)
    {
        _analyticsProperties.Add(key, value);
    }

    public ImmutableSortedDictionary<string, string> ToImmutable() => this;

    public void Add(string key, string value)
    {
        AddAnalyticsProperty(key, value);
    }

    public void Add(KeyValuePair<string, string> item)
    {
        var (key, value) = item;
        AddAnalyticsProperty(key, value);
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _analyticsProperties.GetEnumerator();


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(KeyValuePair<string, string> item) => throw new NotImplementedException();

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        using var enumerator = GetEnumerator();
        for (int i = 0; i <= Math.Min(_analyticsProperties.Count, array.Length) && enumerator.MoveNext(); ++i)
            if (i >= arrayIndex)
                array[i - arrayIndex] = enumerator.Current;
    }

    public bool Remove(KeyValuePair<string, string> item) => throw new NotImplementedException();

    public int Count => _analyticsProperties.Count;

    public bool IsReadOnly => false;

    public bool ContainsKey(string key) => _analyticsProperties.ContainsKey(key);

    public bool Remove(string key) => throw new NotImplementedException();

    public bool TryGetValue(string key, out string value) => _analyticsProperties.TryGetValue(key, out value!);

    public string this[string key]
    {
        get => _analyticsProperties[key];
        set => AddAnalyticsProperty(key, value);
    }

    public ICollection<string> Keys => _analyticsProperties.Keys;
    public ICollection<string> Values => _analyticsProperties.Values;

    public static implicit operator ImmutableSortedDictionary<string, string>(AnalyticsContainer container)
        => container.ToImmutableSortedDictionary();
}
