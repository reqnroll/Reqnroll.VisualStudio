using Reqnroll.Bindings.Provider.Data;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using ReqnrollConnector.Logging;

namespace ReqnrollConnector.Discovery;

/// <summary>
/// Transforms discovery results from <see cref="BindingData"/> class to <see cref="Reqnroll.VisualStudio.ReqnrollConnector.Models.DiscoveryResult"/>.
/// </summary>
internal class DiscoveryResultTransformer
{
    public InternalDiscoveryResult Transform(BindingData bindingData, ISourceLocationProvider sourceLocationProvider, IAnalyticsContainer analytics)
    {
        var typeNamesToKey = new Dictionary<string, string>();
        var sourceFilesToKey = new Dictionary<string, string>();

        string? GetSourceLocation(BindingSourceData? sourceData)
        {
            if (sourceData?.SourceLocation != null) return sourceData.SourceLocation;
            if (sourceData is null) return null;
            var sourceLocation = sourceLocationProvider.GetSourceLocation(sourceData.Method);
            return sourceLocation == null ? null :
                $"#{GetKey(sourceFilesToKey, sourceLocation.SourcePath)}|{sourceLocation.StartLine}|{sourceLocation.StartColumn}|{sourceLocation.EndLine}|{sourceLocation.EndColumn}";
        }

        string GetTypeNameKey(string typeName) => GetKey(typeNamesToKey, typeName);

        var stepDefinitions = bindingData.StepDefinitions
                                         .Select(sdb => CreateStepDefinition(sdb, GetTypeNameKey, GetSourceLocation))
                                         .OrderBy(sd => sd.SourceLocation)
                                         .ToArray();

        var hooks = bindingData.
                    Hooks
                    .Select(sdb => CreateHook(sdb, GetSourceLocation))
                    .OrderBy(sd => sd.SourceLocation)
                    .ToArray();


        analytics.AddAnalyticsProperty("TypeNames", typeNamesToKey.Count.ToString());
        analytics.AddAnalyticsProperty("SourcePaths", sourceFilesToKey.Count.ToString());
        analytics.AddAnalyticsProperty("StepDefinitions", stepDefinitions.Length.ToString());
        analytics.AddAnalyticsProperty("Hooks", hooks.Length.ToString());

        return new InternalDiscoveryResult(
            stepDefinitions,
            hooks,
            ReverseDictionary(sourceFilesToKey),
            ReverseDictionary(typeNamesToKey)
        );
    }

    private static IDictionary<string, string> ReverseDictionary(IDictionary<string, string> sourceFilesToKey)
    {
        return sourceFilesToKey.ToDictionary(e => e.Value, e => e.Key);
    }

    private StepDefinition CreateStepDefinition(
        StepDefinitionData stepDefinitionData,
        Func<string, string> getTypeNameKey,
        Func<BindingSourceData?, string?> getSourceLocation)
    {
        var stepDefinition = new StepDefinition
        {
            Type = stepDefinitionData.Type,
            Regex = stepDefinitionData.Regex,
            Method = GetMethodReference(stepDefinitionData.Source?.Method),
            ParamTypes = GetParamTypes(stepDefinitionData.ParamTypes, getTypeNameKey),
            Scope = GetScope(stepDefinitionData.Scope),
            Expression = GetSourceExpression(stepDefinitionData),
            Error = stepDefinitionData.Error,
            SourceLocation = getSourceLocation(stepDefinitionData.Source)
        };

        return stepDefinition;
    }

    private Hook CreateHook(HookData sdb, Func<BindingSourceData?, string?> getSourceLocation)
    {
        var stepDefinition = new Hook
        {
            Type = sdb.Type,
            HookOrder = sdb.HookOrder,
            Method = GetMethodReference(sdb.Source?.Method),
            Scope = GetScope(sdb.Scope),
            SourceLocation = getSourceLocation(sdb.Source),
            Error = sdb.Error
        };
        return stepDefinition;
    }

    private string GetMethodReference(BindingSourceMethodData? method)
    {
        if (method == null) return "???";

        var declaringTypeName = method.Type?.Split('.').Last();
        var methodSignatureWithoutReturnType = method.FullName?.Split(new[] { ' ' }, 2).ElementAtOrDefault(1)?.Trim();
        return $"{declaringTypeName}.{methodSignatureWithoutReturnType}";
    }

    private string GetKey(IDictionary<string, string> valueToKey, string value)
    {
        if (valueToKey.TryGetValue(value, out var key))
            return key;

        key = valueToKey.Count.ToString();
        valueToKey.Add(value, key);
        return key;
    }

    private string? GetParamTypes(string[] parameterTypeNames, Func<string, string> getTypeNameKey)
    {
        var paramTypes = string.Join("|",
            parameterTypeNames.Select(parameterTypeName => GetParamType(parameterTypeName, getTypeNameKey)));
        return paramTypes.Length == 0 ? null : paramTypes;
    }

    private string GetParamType(string parameterTypeName, Func<string, string> getTypeNameKey)
    {
        if (TypeShortcuts.FromType.TryGetValue(parameterTypeName, out var shortcut))
            return shortcut;

        var key = getTypeNameKey(parameterTypeName);
        return $"#{key}";
    }

    private StepScope? GetScope(BindingScopeData? scopeData)
    {
        if (scopeData == null)
            return null;

        return new StepScope
        {
            Tag = scopeData.Tag,
            FeatureTitle = scopeData.FeatureTitle,
            ScenarioTitle = scopeData.ScenarioTitle,
            Error = scopeData.Error
        };
    }

    private string? GetSourceExpression(StepDefinitionData stepDefinitionData)
        => stepDefinitionData.Expression ?? GetSpecifiedExpressionFromRegex(stepDefinitionData);

    private string? GetSpecifiedExpressionFromRegex(StepDefinitionData stepDefinitionData)
    {
        if (stepDefinitionData.Regex == null)
            return null;

        string regexString = stepDefinitionData.Regex;
        if (regexString.StartsWith("^"))
            regexString = regexString.Substring(1);
        if (regexString.EndsWith("$"))
            regexString = regexString.Substring(0, regexString.Length - 1);
        return regexString;
    }
}