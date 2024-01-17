using System;
using Reqnroll.VisualStudio.Generator.Infrastructure;
using VSLangProj80;

namespace Reqnroll.VisualStudio.Generator;

[ComVisible(true)]
[Guid("58D27450-CAE5-4472-84B2-EF19E3485770")]
[CodeGeneratorRegistration(typeof(DeveroomSingleFileGenerator), Description, vsContextGuids.vsContextGuidVCSProject,
    GeneratesDesignTimeSource = true, GeneratorRegKeyName = Name)]
[CodeGeneratorRegistration(typeof(DeveroomSingleFileGenerator), Description, vsContextGuids.vsContextGuidVBProject,
    GeneratesDesignTimeSource = true, GeneratorRegKeyName = Name)]
[ProvideObject(typeof(DeveroomSingleFileGenerator))]
public class DeveroomSingleFileGenerator : BaseCodeGeneratorWithSite
{
    public const string Name = "ReqnrollSingleFileGenerator";
    public const string Description = "Feature file generator for Reqnroll";

    protected override string GetDefaultExtension() => ".feature" + base.GetDefaultExtension();

    protected override byte[] GenerateCode(string inputFileContent)
    {
        var projectItem = GetProjectItem();
        if (!projectItem.Saved)
            projectItem.Save();

        string extension = base.GetDefaultExtension();

        var projectSystem = (IVsIdeScope) VsUtils.ResolveMefDependency<IIdeScope>(ServiceProvider.GlobalProvider);
        var projectScope = projectSystem.GetProjectScope(GetProject());

        var generationService = projectScope.GetGenerationService();
        if (generationService == null)
            throw new NotSupportedException("Generation for non-Reqnroll projects is not possible.");
        var generationResult = generationService.GenerateFeatureFile(InputFilePath, extension, FileNameSpace);
        string content;
        if (generationResult.IsFailed)
        {
            GeneratorError(0, generationResult.ErrorMessage, 1, 1);
            content = generationResult.FeatureFileCodeBehind?.Content ?? generationResult.ErrorMessage;
        }
        else
        {
            content = generationResult.FeatureFileCodeBehind.Content;
        }

        return GetBytes(content);
    }

    private byte[] GetBytes(string content)
    {
        Encoding enc = Encoding.UTF8;

        //Get the preamble (byte-order mark) for our encoding
        byte[] preamble = enc.GetPreamble();
        int preambleLength = preamble.Length;

        //Convert the writer contents to a byte array
        byte[] body = enc.GetBytes(content);

        //Prepend the preamble to body (store result in resized preamble array)
        Array.Resize(ref preamble, preambleLength + body.Length);
        Array.Copy(body, 0, preamble, preambleLength, body.Length);

        //Return the combined byte array
        return preamble;
    }
}
