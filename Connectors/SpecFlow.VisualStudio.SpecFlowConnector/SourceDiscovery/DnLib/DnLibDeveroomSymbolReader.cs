#nullable disable
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;

namespace SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery.DnLib;

public class DnLibDeveroomSymbolReader : IDeveroomSymbolReader
{
    private readonly ModuleDefMD _moduleDefMd;

    public DnLibDeveroomSymbolReader(string assemblyPath)
    {
        _moduleDefMd = ModuleDefMD.Load(assemblyPath);
    }

    public MethodSymbol ReadMethodSymbol(int token)
    {
        var method = _moduleDefMd.ResolveMethod((uint) (token & 0x00FFFFFF));

        var stateClassType = GetStateClassType(method);
        if (stateClassType != null)
        {
            var sequencePoints = new List<SequencePoint>();
            if (stateClassType != null)
                foreach (var typeMethod in stateClassType.Methods)
                    sequencePoints.AddRange(GetSequencePointsFromMethodBody(typeMethod));

            sequencePoints.AddRange(GetSequencePointsFromMethodBody(method));
            sequencePoints.Sort((sp1, sp2) => Comparer<int>.Default.Compare(sp1.StartLine, sp2.StartLine));
            return new MethodSymbol(token, sequencePoints.ToArray());
        }

        return new MethodSymbol(token, GetSequencePointsFromMethodBody(method).ToArray());
    }

    public void Dispose()
    {
        _moduleDefMd.Dispose();
    }

    private TypeDef GetStateClassType(MethodDef method)
    {
        var stateMachineDebugInfo =
            method.CustomDebugInfos?.OfType<PdbStateMachineTypeNameCustomDebugInfo>().FirstOrDefault();
        if (stateMachineDebugInfo != null)
            return stateMachineDebugInfo.Type;
        var stateMachineAttr = method.CustomAttributes.FirstOrDefault(ca =>
            ca.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");
        return stateMachineAttr?.ConstructorArguments.Select(ca => ca.Value).OfType<TypeDefOrRefSig>().FirstOrDefault()
            ?.TypeDef;
    }

    private IEnumerable<SequencePoint> GetSequencePointsFromMethodBody(MethodDef methodDef)
    {
        var methodBody = methodDef?.MethodBody as CilBody;
        if (methodBody == null)
            yield break;

        var relevantInstructions = methodBody.Instructions.Where(i => i.SequencePoint != null);
        var sequencePoints = relevantInstructions.Select(i => new SequencePoint((int) i.Offset,
            GetSourcePath(i.SequencePoint.Document), i.SequencePoint.StartLine,
            i.SequencePoint.EndLine, i.SequencePoint.StartColumn, i.SequencePoint.EndColumn));
        foreach (var sequencePoint in sequencePoints)
            yield return sequencePoint;
    }

    private string GetSourcePath(PdbDocument document) => document.Url;
}
