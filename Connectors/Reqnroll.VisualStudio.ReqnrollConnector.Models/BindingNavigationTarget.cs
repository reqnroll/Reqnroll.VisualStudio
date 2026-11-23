using System;
using System.Linq;
using System.Reflection;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Models;

public class BindingNavigationTarget : IEquatable<BindingNavigationTarget>
{
    public string AssemblyIdentity { get; set; } = "";
    public string DocumentationCommentId { get; set; } = "";

    public bool Equals(BindingNavigationTarget? other) => other != null &&
        AssemblyIdentity == other.AssemblyIdentity && DocumentationCommentId == other.DocumentationCommentId;

    public override bool Equals(object? obj) => obj is BindingNavigationTarget other && Equals(other);

    public override int GetHashCode() => (AssemblyIdentity?.GetHashCode() ?? 0) * 397 ^
                                         (DocumentationCommentId?.GetHashCode() ?? 0);

    public static BindingNavigationTarget? FromMethod(MethodInfo method)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (method.DeclaringType == null || method.DeclaringType.IsGenericType ||
            method.IsGenericMethod || method.IsSpecialName)
            return null;

        var parameterTypes = method.GetParameters().Select(p => GetParameterTypeName(p.ParameterType)).ToArray();
        if (parameterTypes.Any(p => p == null))
            return null;

        var parameters = parameterTypes.Length == 0 ? "" : "(" + string.Join(",", parameterTypes) + ")";
        return new BindingNavigationTarget
        {
            AssemblyIdentity = method.DeclaringType.Assembly.FullName!,
            DocumentationCommentId = "M:" + method.DeclaringType.FullName!.Replace('+', '.') + "." +
                                     method.Name.Replace('.', '#') + parameters
        };
    }

    private static string? GetParameterTypeName(Type type)
    {
        if (type.IsGenericParameter || type.IsGenericType)
            return null;
        if (type.HasElementType)
        {
            var elementType = GetParameterTypeName(type.GetElementType()!);
            if (elementType == null)
                return null;
            if (type.IsByRef) return elementType + "@";
            if (type.IsPointer) return elementType + "*";
            if (type.IsArray && type.GetArrayRank() == 1 && type == type.GetElementType()!.MakeArrayType())
                return elementType + "[]";
            // Non-vector arrays and generic signatures are deliberately unsupported rather than guessed.
            return null;
        }

        return type.FullName?.Replace('+', '.');
    }
}
