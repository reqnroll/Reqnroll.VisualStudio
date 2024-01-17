using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using ILogger = SpecFlowConnector.Logging.ILogger;

namespace SpecFlowConnector.SourceDiscovery.DnLib;

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

    public override IEnumerable<MethodSymbolSequencePoint> ReadMethodSymbol(int token) =>
        _moduleDefMd
            .ResolveMethod((uint) (token & 0x00FFFFFF))
            .AsOption()
            .Map(method => method
                .Map(GetStateClassType)
                .Map(stateClassType => stateClassType.Methods
                        .SelectMany(GetSequencePointsFromMethodBody)
                        .Union(GetSequencePointsFromMethodBody(method))
                        .OrderBy(sp => sp.StartLine)
                    as IEnumerable<MethodSymbolSequencePoint>
                )
                .Reduce(GetSequencePointsFromMethodBody(method))
            )
            .Reduce(ImmutableArray<MethodSymbolSequencePoint>.Empty);

    private static Option<TypeDef> GetStateClassType(MethodDef method) =>
        method
            .CustomDebugInfos
            .OfType<PdbStateMachineTypeNameCustomDebugInfo>()
            .FirstOrNone()
            .Map(stateMachineDebugInfo => stateMachineDebugInfo.Type)
            .Or(() => method
                .CustomAttributes
                .FirstOrNone(ca => ca
                    .AttributeType
                    .FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute")
                .Map(stateMachineAttr => stateMachineAttr
                    .ConstructorArguments
                    .Select(ca => ca.Value)
                    .OfType<TypeDefOrRefSig>()
                    .Select(td => td.TypeDef)
                    .FirstOrNone()
                )
                .Reduce(None<TypeDef>.Value)
            );

    private IEnumerable<MethodSymbolSequencePoint> GetSequencePointsFromMethodBody(MethodDef methodDef) =>
        methodDef
            .AsOption()
            .MapOptional<CilBody>(md => md.MethodBody as CilBody)
            .Map(mb => mb
                .Instructions
                .Where(IsRelevant)
                .Select(i => new MethodSymbolSequencePoint(
                    (int) i.Offset,
                    GetSourcePath(i.SequencePoint.Document),
                    i.SequencePoint.StartLine,
                    i.SequencePoint.EndLine,
                    i.SequencePoint.StartColumn,
                    i.SequencePoint.EndColumn)
                ))
            .Reduce(() => ImmutableArray<MethodSymbolSequencePoint>.Empty);

    public static bool IsRelevant(Instruction instruction)
        => instruction.SequencePoint is not null &&
           instruction.SequencePoint.StartLine != SequencePointConstants.HIDDEN_LINE &&
           instruction.SequencePoint.StartColumn != SequencePointConstants.HIDDEN_COLUMN;

    private string GetSourcePath(PdbDocument document) => document.Url;

    public override string ToString() => $"{nameof(DnLibDeveroomSymbolReader)}({_moduleDefMd})";
}
