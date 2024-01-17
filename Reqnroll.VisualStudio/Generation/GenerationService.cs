#nullable disable

namespace Reqnroll.VisualStudio.Generation;

public class GenerationService
{
    private readonly IDeveroomLogger _logger;
    private readonly IProjectScope _projectScope;

    public GenerationService(IProjectScope projectScope)
    {
        _projectScope = projectScope;
        _logger = projectScope.IdeScope.Logger;
    }

    private IMonitoringService MonitoringService => _projectScope.IdeScope.MonitoringService;

    public static bool CheckReqnrollToolsFolder(IProjectScope projectScope)
    {
        var toolsFolder = GetReqnrollToolsFolderSafe(projectScope, projectScope.GetProjectSettings(), out _);
        return toolsFolder != null;
    }

    private static string GetReqnrollToolsFolderSafe(IProjectScope projectScope, ProjectSettings projectSettings,
        out string toolsFolderErrorMessage)
    {
        toolsFolderErrorMessage = null;
        try
        {
            var reqnrollToolsFolder = projectSettings.ReqnrollGeneratorFolder;
            if (string.IsNullOrEmpty(reqnrollToolsFolder))
            {
                projectScope.IdeScope.Actions.ShowProblem(
                    "Unable to generate feature-file code behind, because Reqnroll NuGet package folder could not be detected. For configuring Reqnroll tools folder manually, check http://speclink.me/deveroomsfassref.");
                toolsFolderErrorMessage =
                    "Folder is not configured. See http://speclink.me/deveroomsfassref for details.";
                return null;
            }

            if (!projectScope.IdeScope.FileSystem.Directory.Exists(reqnrollToolsFolder))
            {
                projectScope.IdeScope.Actions.ShowProblem(
                    $"Unable to find Reqnroll tools folder: '{reqnrollToolsFolder}'. Build solution to ensure that all packages are restored. The feature file has to be re-generated (e.g. by saving) after the packages have been restored.");
                toolsFolderErrorMessage = "Folder does not exist";
                return null;
            }

            return reqnrollToolsFolder;
        }
        catch (Exception ex)
        {
            projectScope.IdeScope.Logger.LogException(projectScope.IdeScope.MonitoringService, ex);
            toolsFolderErrorMessage = ex.Message;
            return null;
        }
    }

    public GenerationResult GenerateFeatureFile(string featureFilePath, string targetExtension, string targetNamespace)
    {
        var projectSettings = _projectScope.GetProjectSettings();
        var reqnrollToolsFolder =
            GetReqnrollToolsFolderSafe(_projectScope, projectSettings, out var toolsFolderErrorMessage);
        if (reqnrollToolsFolder == null)
            return CreateErrorResult(featureFilePath,
                $"Unable to use Reqnroll tools folder '{projectSettings.ReqnrollGeneratorFolder}': {toolsFolderErrorMessage}");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        try
        {
            var connector = OutProcReqnrollConnectorFactory.Create(_projectScope);

            var result = connector.RunGenerator(featureFilePath, projectSettings.ReqnrollConfigFilePath,
                targetExtension, targetNamespace, _projectScope.ProjectFolder, reqnrollToolsFolder);

            _projectScope.IdeScope.MonitoringService.MonitorReqnrollGeneration(result.IsFailed, projectSettings);

            if (result.IsFailed)
            {
                _logger.LogWarning(result.ErrorMessage);
                SetErrorContent(featureFilePath, result);
                _logger.LogVerbose(() => result.FeatureFileCodeBehind.Content);
            }
            else
            {
                _logger.LogInfo(
                    $"code-behind file generated for file {featureFilePath} in project {_projectScope.ProjectName}");
                _logger.LogVerbose(() =>
                    result.FeatureFileCodeBehind.Content.Substring(0,
                        Math.Min(450, result.FeatureFileCodeBehind.Content.Length)));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogException(MonitoringService, ex);
            return CreateErrorResult(featureFilePath, ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogVerbose($"Generation: {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private GenerationResult CreateErrorResult(string featureFilePath, string errorMessage)
    {
        var result = new GenerationResult
        {
            ErrorMessage = errorMessage
        };
        SetErrorContent(featureFilePath, result);
        return result;
    }

    private void SetErrorContent(string featureFilePath, GenerationResult result)
    {
        result.FeatureFileCodeBehind = new FeatureFileCodeBehind
        {
            FeatureFilePath = featureFilePath,
            Content = GetErrorContent(result.ErrorMessage)
        };
    }

    private string GetErrorContent(string resultErrorMessage)
    {
        var errorLines = resultErrorMessage.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        return
            "#error " + errorLines[0] + Environment.NewLine +
            string.Join(Environment.NewLine,
                errorLines.Skip(1)
                    .Select(l => l.StartsWith("(") ? "#error " + l : "//" + l));
    }
}
