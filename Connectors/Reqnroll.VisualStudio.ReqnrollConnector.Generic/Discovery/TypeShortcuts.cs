namespace ReqnrollConnector.Discovery;

public static class TypeShortcuts
{
    public const string ReqnrollTableType = "Reqnroll.Table";
    public const string StringType = "System.String";
    public const string Int32Type = "System.Int32";

    public static readonly Dictionary<string, string> FromShortcut = new()
    {
        {"s", StringType},
        {"c", typeof(char).FullName!},
        {"b", typeof(bool).FullName!},
        {"bt", typeof(byte).FullName!},
        {"i", Int32Type},
        {"sh", typeof(short).FullName!},
        {"l", typeof(long).FullName!},
        {"f", typeof(float).FullName!},
        {"d", typeof(double).FullName!},
        {"m", typeof(decimal).FullName!},
        {"st", ReqnrollTableType}
    };

    public static readonly Dictionary<string, string> FromType = FromShortcut.ToDictionary(p => p.Value, p => p.Key);
}
