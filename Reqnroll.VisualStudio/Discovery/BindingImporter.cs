#nullable disable
using Reqnroll.VisualStudio.Discovery.TagExpressions;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;

namespace Reqnroll.VisualStudio.Discovery;

public class BindingImporter
{
    private static readonly string[] EmptyParameterTypes = new string[0];
    private static readonly string[] SingleStringParameterTypes = {TypeShortcuts.StringType};
    private static readonly string[] DoubleStringParameterTypes = {TypeShortcuts.StringType, TypeShortcuts.StringType};
    private static readonly string[] SingleIntParameterTypes = {TypeShortcuts.Int32Type};
    private static readonly string[] SingleDataTableParameterTypes = {TypeShortcuts.ReqnrollTableType};
    private readonly Dictionary<string, ProjectBindingImplementation> _implementations = new();

    private readonly IDeveroomLogger _logger;
    private readonly Dictionary<string, string> _sourceFiles;
    private readonly TagExpressionParser _tagExpressionParser = new();
    private readonly Dictionary<string, string> _typeNames;

    public BindingImporter(Dictionary<string, string> sourceFiles, Dictionary<string, string> typeNames,
        IDeveroomLogger logger)
    {
        _sourceFiles = sourceFiles;
        _typeNames = typeNames;
        _logger = logger;
    }

    public ProjectStepDefinitionBinding ImportStepDefinition(StepDefinition stepDefinition)
    {
        try
        {
            var stepDefinitionType = Enum.TryParse<ScenarioBlock>(stepDefinition.Type, out var parsedHookType)
                ? parsedHookType
                : ScenarioBlock.Unknown;
            var regex = ParseRegex(stepDefinition);
            var sourceLocation = ParseSourceLocation(stepDefinition.SourceLocation);
            var scope = ParseScope(stepDefinition.Scope);
            var parameterTypes = ParseParameterTypes(stepDefinition.ParamTypes);

            if (!_implementations.TryGetValue(stepDefinition.Method, out var implementation))
            {
                implementation =
                    new ProjectBindingImplementation(stepDefinition.Method, parameterTypes, sourceLocation);
                _implementations.Add(stepDefinition.Method, implementation);
            }

            return new ProjectStepDefinitionBinding(stepDefinitionType, regex, scope, implementation,
                stepDefinition.Expression, GetBindingError(stepDefinition.Error, scope, "step definition"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Invalid step definition binding: {ex.Message}");
            return null;
        }
    }

    public ProjectHookBinding ImportHook(Hook hook)
    {
        try
        {
            var hookType = Enum.TryParse<HookType>(hook.Type, out var parsedHookType)
                ? parsedHookType
                : HookType.Unknown;
            var sourceLocation = ParseSourceLocation(hook.SourceLocation);
            var scope = ParseScope(hook.Scope);

            if (!_implementations.TryGetValue(hook.Method, out var implementation))
            {
                implementation =
                    new ProjectBindingImplementation(hook.Method, null, sourceLocation);
                _implementations.Add(hook.Method, implementation);
            }

            return new ProjectHookBinding(implementation, scope, hookType, hook.HookOrder, GetBindingError(hook.Error, scope, "hook"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Invalid hook binding: {ex.Message}");
            return null;
        }
    }

    private string GetBindingError(string error, Scope scope, string bindingType)
    {
        if (!string.IsNullOrWhiteSpace(error))
            return $"Invalid {bindingType}: {error}";
        if (!string.IsNullOrWhiteSpace(scope?.Error))
            return $"Invalid scope for {bindingType}: {scope.Error}";
        return null;
    }

    private static Regex ParseRegex(StepDefinition stepDefinition) =>
        string.IsNullOrEmpty(stepDefinition.Regex)
            ? null
            : new Regex(stepDefinition.Regex, RegexOptions.CultureInvariant);

    private string[] ParseParameterTypes(string paramTypes)
    {
        if (string.IsNullOrWhiteSpace(paramTypes))
            return EmptyParameterTypes;
        switch (paramTypes)
        {
            case "s":
                return SingleStringParameterTypes;
            case "i":
                return SingleIntParameterTypes;
            case "s|s":
                return DoubleStringParameterTypes;
            case "st":
                return SingleDataTableParameterTypes;
        }

        var parts = paramTypes.Split('|');
        return parts.Select(ParseParameterType).ToArray();
    }

    private string ParseParameterType(string paramType)
    {
        paramType = paramType.Trim();

        if (TypeShortcuts.FromShortcut.TryGetValue(paramType, out var shortcutTypeName))
            return shortcutTypeName;

        if (paramType.StartsWith("#") && _typeNames != null)
            if (_typeNames.TryGetValue(paramType.Substring(1), out var typeNameAtIndex))
                paramType = typeNameAtIndex;

        return paramType;
    }

    private SourceLocation ParseSourceLocation(string sourceLocation)
    {
        if (string.IsNullOrWhiteSpace(sourceLocation))
            return null;
        var parts = sourceLocation.Split('|');
        if (parts.Length <= 1 || !int.TryParse(parts[1], out var line))
            line = 1;
        if (parts.Length <= 2 || !int.TryParse(parts[2], out var column))
            column = 1;
        int? endLineOrNull = null;
        if (parts.Length > 3 && int.TryParse(parts[3], out var endLine))
            endLineOrNull = endLine;
        int? endColumnOrNull = null;
        if (parts.Length > 4 && int.TryParse(parts[4], out var endColumn))
            endColumnOrNull = endColumn;

        string sourceFile = parts[0];
        if (sourceFile.StartsWith("#") && _sourceFiles != null)
            if (_sourceFiles.TryGetValue(sourceFile.Substring(1), out var sourceFileAtIndex))
                sourceFile = sourceFileAtIndex;

        return new SourceLocation(sourceFile, line, column, endLineOrNull, endColumnOrNull);
    }

    private Scope ParseScope(StepScope bindingScope)
    {
        if (bindingScope == null)
            return null;

        try
        {
            return new Scope
            {
                FeatureTitle = bindingScope.FeatureTitle,
                ScenarioTitle = bindingScope.ScenarioTitle,
                Tag = string.IsNullOrWhiteSpace(bindingScope.Tag)
                    ? null
                    : _tagExpressionParser.Parse(bindingScope.Tag),
                Error = bindingScope.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogVerbose($"Invalid tag expression '{bindingScope.Tag}': {ex.Message}");
            return new Scope
            {
                FeatureTitle = bindingScope.FeatureTitle,
                ScenarioTitle = bindingScope.ScenarioTitle,
                Tag = null,
                Error = $"Invalid tag expression '{bindingScope.Tag}': {ex.Message}"
            };
        }
    }
}
