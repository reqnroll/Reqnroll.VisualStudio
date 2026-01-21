using System.Collections.Immutable;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using ReqnrollConnector.Logging;
using ILogger = ReqnrollConnector.Logging.ILogger;

namespace ReqnrollConnector.SourceDiscovery.DnLib;

public class DnLibDeveroomSymbolReader : DeveroomSymbolReader
{
    private readonly ModuleDefMD _moduleDefMd;

    public DnLibDeveroomSymbolReader(ModuleDefMD moduleDefMd)
    {
        _moduleDefMd = moduleDefMd;
    }

    public static DeveroomSymbolReader Create(ILogger log, string assemblyPath)
    {
        log.Info($"Creating {nameof(DnLibDeveroomSymbolReader)}");
        var moduleDefMd = ModuleDefMD.Load(assemblyPath);
        return new DnLibDeveroomSymbolReader(moduleDefMd);
    }

    public override IEnumerable<MethodSymbolSequencePoint> ReadMethodSymbol(int token)
    {
        var resolvedMethod = _moduleDefMd.ResolveMethod((uint) (token & 0x00FFFFFF));

        if (resolvedMethod == null)
            return ImmutableArray<MethodSymbolSequencePoint>.Empty;

        var stateClassType = GetStateClassType(resolvedMethod);

        if (stateClassType != null)
        {
            var stateClassSequencePoints = stateClassType.Methods
                .SelectMany(GetSequencePointsFromMethodBody)
                .ToList();
            var methodSequencePoints = GetSequencePointsFromMethodBody(resolvedMethod).ToList();

            var allSequencePoints = stateClassSequencePoints
                .Union(methodSequencePoints)
                .OrderBy(sp => sp.StartLine)
                .ToList();

            return allSequencePoints;
        }
        else
        {
            return GetSequencePointsFromMethodBody(resolvedMethod);
        }
    }

    private static TypeDef? GetStateClassType(MethodDef method)
    {
        var stateMachineDebugInfos = method
            .CustomDebugInfos
            .OfType<PdbStateMachineTypeNameCustomDebugInfo>()
            .ToList();

        if (stateMachineDebugInfos.Count > 0)
        {
            return stateMachineDebugInfos[0].Type;
        }

        var asyncAttributes = method
            .CustomAttributes
            .Where(ca => ca.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute")
            .ToList();

        if (asyncAttributes.Count > 0)
        {
            var stateMachineAttr = asyncAttributes[0];
            var typeDefOrRefSigs = stateMachineAttr
                .ConstructorArguments
                .Select(ca => ca.Value)
                .OfType<TypeDefOrRefSig>()
                .ToList();

            if (typeDefOrRefSigs.Count > 0)
            {
                var typeDef = typeDefOrRefSigs[0].TypeDef;
                if (typeDef != null)
                    return typeDef;
            }
        }

        return null;
    }

    private IEnumerable<MethodSymbolSequencePoint> GetSequencePointsFromMethodBody(MethodDef methodDef)
    {
        if (methodDef == null)
            return ImmutableArray<MethodSymbolSequencePoint>.Empty;

        var cilBody = methodDef.MethodBody as CilBody;
        if (cilBody == null)
            return ImmutableArray<MethodSymbolSequencePoint>.Empty;

        var sequencePoints = cilBody
            .Instructions
            .Where(IsRelevant)
            .Select(i => new MethodSymbolSequencePoint(
                (int) i.Offset,
                GetSourcePath(i.SequencePoint.Document),
                i.SequencePoint.StartLine,
                i.SequencePoint.EndLine,
                i.SequencePoint.StartColumn,
                i.SequencePoint.EndColumn)
            )
            .ToList();

        return sequencePoints;
    }

    public static bool IsRelevant(Instruction instruction)
        => instruction.SequencePoint is not null &&
           instruction.SequencePoint.StartLine != SequencePointConstants.HIDDEN_LINE &&
           instruction.SequencePoint.StartColumn != SequencePointConstants.HIDDEN_COLUMN;

    private string GetSourcePath(PdbDocument document) => document.Url;

    public override string ToString() => $"{nameof(DnLibDeveroomSymbolReader)}({_moduleDefMd})";
}
