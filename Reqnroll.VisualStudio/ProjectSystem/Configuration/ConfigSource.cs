namespace Reqnroll.VisualStudio.ProjectSystem.Configuration;

public record ConfigSource(string FilePath, DateTimeOffset LastChangeTime, string? ErrorMessage)
{
    public bool IsValid => !string.IsNullOrEmpty(FilePath);

    public static ConfigSource TryGetConfigSource(string filePath, IFileSystemForVs fileSystem, IDeveroomLogger logger)
    {
        if (string.IsNullOrEmpty(filePath))
            return CreateInvalid("Test assembly path could not be detected, therefore some Reqnroll Visual Studio Extension features are disabled. Try to rebuild the project or restart Visual Studio.");
        if (!fileSystem.File.Exists(filePath))
            return CreateInvalid("Test assembly not found. Please build the project to enable the Reqnroll Visual Studio Extension features.");
        try
        {
            return CreateValid(filePath, fileSystem.File.GetLastWriteTimeUtc(filePath));
        }
        catch (Exception ex)
        {
            logger.LogDebugException(ex);
            return CreateInvalid($"Test assembly could not be accessed: {ex.Message}. Please rebuild the project to enable the Reqnroll Visual Studio Extension features.");
        }
    }
    public static ConfigSource CreateInvalid(string errorMessage)
    {
        return new(string.Empty, DateTimeOffset.MinValue, errorMessage);
    }

    public static ConfigSource CreateValid(string filePath, DateTimeOffset lastChangeTime)
    {
        return new(filePath, lastChangeTime, null);
    }
}
