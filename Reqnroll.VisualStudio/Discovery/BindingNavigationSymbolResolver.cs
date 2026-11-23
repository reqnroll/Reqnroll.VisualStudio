using Reqnroll.VisualStudio.ReqnrollConnector.Models;

namespace Reqnroll.VisualStudio.Discovery;

public static class BindingNavigationSymbolResolver
{
    public static IMethodSymbol? Resolve(Compilation compilation, BindingNavigationTarget? target)
    {
        if (target == null || string.IsNullOrEmpty(target.AssemblyIdentity) ||
            string.IsNullOrEmpty(target.DocumentationCommentId) ||
            !target.DocumentationCommentId.StartsWith("M:", StringComparison.Ordinal) ||
            !AssemblyIdentity.TryParseDisplayName(target.AssemblyIdentity, out var identity))
            return null;

        var symbols = DocumentationCommentId.GetSymbolsForDeclarationId(target.DocumentationCommentId, compilation)
            .OfType<IMethodSymbol>()
            .Where(s => s.ContainingAssembly.Identity.Equals(identity))
            .Take(2)
            .ToArray();
        return symbols.Length == 1 ? symbols[0] : null;
    }
}
