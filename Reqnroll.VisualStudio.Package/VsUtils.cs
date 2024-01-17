#nullable disable
using Microsoft.VisualStudio.OLE.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IServiceProvider = System.IServiceProvider;

namespace Reqnroll.VisualStudio;

public static class VsUtils
{
    public static ProjectItem GetProjectItemFromTextBuffer(ITextBuffer textBuffer)
    {
        try
        {
            if (!textBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter) ||
                bufferAdapter == null)
                return null;

            var extensibleObject = bufferAdapter as IExtensibleObject;
            if (extensibleObject != null)
            {
                extensibleObject.GetAutomationObject("Document", null, out object documentObj);
                var document = (Document) documentObj;
                if (document != null) return document.ProjectItem;
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetProjectItemFromTextBuffer)}");
            return null;
        }
    }

    public static IWpfTextView GetWpfTextViewFromFilePath(string filePath, IServiceProvider serviceProvider,
        IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
    {
        if (GetVsWindowFrame(filePath, serviceProvider, out var windowFrame))
        {
            // Get the IVsTextView from the windowFrame.
            IVsTextView textView = VsShellUtilities.GetTextView(windowFrame);
            if (!IsInitialized(textView))
                return null;

            return editorAdaptersFactoryService.GetWpfTextView(textView);
        }

        return null;
    }

    private static IVsWindowFrame GetVsWindowFrame(string filePath, IServiceProvider serviceProvider,
        bool openIfNotOpened)
    {
        if (VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty,
                out var _, out var _, out var windowFrame))
            return windowFrame;

        if (!openIfNotOpened)
            return null;

        VsShellUtilities.OpenDocument(serviceProvider, filePath, Guid.Empty,
            out var _, out var _, out windowFrame);
        return windowFrame;
    }

    private static bool GetVsWindowFrame(string filePath, IServiceProvider serviceProvider,
        out IVsWindowFrame windowFrame) =>
        VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty,
            out _, out _, out windowFrame);

    public static void OpenIfNotOpened(string filePath, IServiceProvider serviceProvider)
    {
        if (VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty,
                out _, out _, out _))
            return;

        VsShellUtilities.OpenDocument(serviceProvider, filePath, Guid.Empty,
            out _, out _, out _);
    }

    /// <summary>
    ///     IVsEditorAdaptersFactoryService.GetWpfTextView brings the text view into an inconsistent state when it is not fully
    ///     initialized (open project with files opened but not activated yet)
    /// </summary>
    private static bool IsInitialized(IVsTextView textView)
    {
        if (textView == null)
            return false;
        var propertyInfo = textView.GetType()
            .GetProperty("CurrentInitializationState", BindingFlags.Instance | BindingFlags.Public);
        if (propertyInfo == null)
            return true; // actually we don't know
        var value = propertyInfo.GetValue(textView).ToString();
        return value == "TextViewAvailable";
    }

    public static Project GetProject(ProjectItem projectItem)
    {
        try
        {
            return projectItem?.ContainingProject;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetProject)}");
            return null;
        }
    }

    public static bool IsSolutionProject(Project project)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(project.FullName) &&
                   Path.GetDirectoryName(project.FullName) != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(IsSolutionProject)}");
            return false;
        }
    }

    public static string GetProjectFolder(Project project)
    {
        try
        {
            return project.Properties.Item("FullPath").Value.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetProjectFolder)}");
            return string.IsNullOrEmpty(project.FullName) ? null : Path.GetDirectoryName(project.FullName);
        }
    }

    public static string GetPlatformName(Project project)
    {
        try
        {
            return project.ConfigurationManager.ActiveConfiguration.PlatformName;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetPlatformName)}");
            return null;
        }
    }

    public static string GetPlatformTargetName(Project project)
    {
        try
        {
            if (project.ConfigurationManager.ActiveConfiguration.Properties == null)
                return null;
            var platformTargetName = project.ConfigurationManager.ActiveConfiguration.Properties.Item("PlatformTarget").Value.ToString();
            if (string.IsNullOrEmpty(platformTargetName))
                return null;
            return platformTargetName;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetPlatformTargetName)}");
            return null;
        }
    }

    public static string GetOutputFileName(Project project)
    {
        try
        {
            var result = project.Properties.Item("OutputFileName").Value.ToString();
            if (string.IsNullOrWhiteSpace(result)) result = project.Properties.Item("AssemblyName").Value + ".dll";
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetOutputFileName)}");
            return null;
        }
    }

    public static string GetOutputPath(Project project)
    {
        try
        {
            if (project.ConfigurationManager.ActiveConfiguration.Properties == null)
                return null;
            return project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetOutputPath)}");
            return null;
        }
    }

    public static string GetOutputAssemblyPath(Project project)
    {
        try
        {
            //DumpProperties(project.Properties, "project");
            //DumpProperties(project.ConfigurationManager.ActiveConfiguration.Properties, "ActiveConfiguration");
            var outputFileName = GetOutputFileName(project);
            if (outputFileName == null)
                return null;
            var projectFolder = GetProjectFolder(project);
            if (projectFolder == null)
                return null;
            var outputPath = GetOutputPath(project);
            if (outputPath == null)
            {
                var outputAssemblyPath = GetOutputAssemblyPathFromOutputGroups(project, outputFileName, projectFolder);
                if (outputAssemblyPath != null)
                    return outputAssemblyPath;
                return null;
            }

            return Path.Combine(projectFolder, outputPath, outputFileName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetOutputAssemblyPath)}");
            return null;
        }
    }

    private static string GetOutputAssemblyPathFromOutputGroups(Project project, string outputFileName, string projectFolder)
    {
        try
        {
            DumpOutputGroups(project.ConfigurationManager.ActiveConfiguration.OutputGroups);
            var primaryOutputGroup = project.ConfigurationManager.ActiveConfiguration.OutputGroups.Item("Built");
            if (primaryOutputGroup == null)
                return null;
            var fileUrls = (primaryOutputGroup.FileURLs as object[])?.OfType<string>();
            if (fileUrls == null)
                return null;
            string GetUriPath(string url)
            {
                if (url.StartsWith("file://") && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return uri.LocalPath;
                return url;
            }
            var filePath = fileUrls.Select(GetUriPath).FirstOrDefault(path => path.EndsWith(Path.DirectorySeparatorChar + outputFileName));
            if (filePath == null)
                return null;

            var objPathSegment = @"\obj\";
            var objIndex = filePath.LastIndexOf(objPathSegment, StringComparison.CurrentCultureIgnoreCase);
            if (objIndex >= 0)
            {
                filePath = filePath
                    .Remove(objIndex, objPathSegment.Length)
                    .Insert(objIndex, @"\bin\");
            }

            return filePath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetOutputAssemblyPathFromOutputGroups)}");
            return null;
        }
    }

    public static string GetTargetFrameworkMoniker(Project project)
    {
        try
        {
            return project.Properties.Item("TargetFrameworkMoniker").Value.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetTargetFrameworkMoniker)}");
            return null;
        }
    }

    public static string GetTargetFrameworkMonikers(Project project)
    {
        try
        {
            return project.Properties.Item("TargetFrameworkMonikers").Value.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"{nameof(VsUtils)}.{nameof(GetTargetFrameworkMonikers)}");
            return null;
        }
    }

    private static void DumpProperties(Properties props, string category)
    {
        if (props == null)
            return;
        var result = new StringBuilder();
        result.AppendLine("START PROPS: " + category);
        foreach (Property prop in props)
        {
            result.Append($"{prop.Name} = ");
            try
            {
                result.Append(prop.Value);
            }
            catch (Exception e)
            {
                result.Append(e.Message);
            }

            result.AppendLine();
        }

        result.AppendLine("END PROPS: " + category);
        Debug.WriteLine(result);
    }

    private static void DumpOutputGroups(OutputGroups outputGroups)
    {
        void DumpOutputGroup(OutputGroup outputGroup)
        {
            var canonicalName = outputGroup.CanonicalName;
            var displayName = outputGroup.DisplayName;
            var description = outputGroup.Description;
            var fileCount = outputGroup.FileCount;
            var fileNames = outputGroup.FileNames as object[];
            var fileUrls = outputGroup.FileURLs as object[];

            string GetArrayValue(object[] array)
                => array == null ? "" : string.Join(Environment.NewLine, array.Select((o, i) => $"[{i}]: {o}"));

            Debug.WriteLine($"Output group: {canonicalName}, {displayName}, {description}, {fileCount}");
            Debug.WriteLine($"File names: {Environment.NewLine}{GetArrayValue(fileNames)}");
            Debug.WriteLine($"File URLs: {Environment.NewLine}{GetArrayValue(fileUrls)}");
        }

        foreach (OutputGroup outputGroup in outputGroups)
        {
            DumpOutputGroup(outputGroup);
        }
    }

    public static T SafeResolveMefDependency<T>(DTE dte) where T : class
    {
        try
        {
            var oleServiceProvider = dte as IOleServiceProvider;
            if (oleServiceProvider == null)
                return null;
            return ResolveMefDependency<T>(new ServiceProvider(oleServiceProvider));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    public static T ResolveMefDependency<T>(IServiceProvider serviceProvider) where T : class
    {
        var componentModel = (IComponentModel) serviceProvider.GetService(typeof(SComponentModel));
        return componentModel?.GetService<T>();
    }

    public static string GetMefCatalogCacheFolder(IServiceProvider serviceProvider)
    {
        var componentModelHost = serviceProvider.GetService(typeof(SVsComponentModelHost)) as IVsComponentModelHost;
        if (componentModelHost == null)
            return null;

        componentModelHost.GetCatalogCacheFolder(out var folderPath);
        return folderPath;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern int ClientToScreen(IntPtr hWnd, [In] [Out] User32Point pt);

    public static Point? GetCaretPosition(IServiceProvider serviceProvider)
    {
        try
        {
            var vsTextManager = (IVsTextManager) serviceProvider.GetService(typeof(SVsTextManager));
            if (vsTextManager == null)
                return null;

            ErrorHandler.ThrowOnFailure(vsTextManager.GetActiveView(Convert.ToInt32(true), null, out var activeView));
            if (activeView == null)
                return null;

            ErrorHandler.ThrowOnFailure(activeView.GetCaretPos(out var caretLine, out var caretColumn));

            var interopPoint = new POINT[1];
            ErrorHandler.ThrowOnFailure(activeView.GetPointOfLineColumn(caretLine + 1, caretColumn + 1, interopPoint));

            var p = new User32Point(interopPoint[0].x, interopPoint[0].y);
            ErrorHandler.ThrowOnFailure(ClientToScreen(activeView.GetWindowHandle(), p));

            var wpfTextView = GetWpfTextView(serviceProvider, activeView) as Visual;
            if (wpfTextView != null)
            {
                var target = PresentationSource.FromVisual(wpfTextView)?.CompositionTarget;
                if (target != null)
                {
                    var transformedPoint = target.TransformFromDevice.Transform(new Point(p.x, p.y));
                    return transformedPoint;
                }
            }

            return new Point(p.x, p.y);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    private static IWpfTextView GetWpfTextView(IServiceProvider serviceProvider, IVsTextView activeView)
    {
        var editorAdaptersFactoryService = ResolveMefDependency<IVsEditorAdaptersFactoryService>(serviceProvider);
        return editorAdaptersFactoryService?.GetWpfTextView(activeView);
    }

    public static IVsHierarchy GetHierarchyFromProject(Project project)
    {
        try
        {
            var serviceProvider = new ServiceProvider(project.DTE as IOleServiceProvider);
            if (!(serviceProvider.GetService(typeof(SVsSolution)) is IVsSolution solution))
                return null;
            if (!ErrorHandler.Succeeded(solution.GetProjectOfUniqueName(project.FullName, out var hierarchy)))
                return null;
            return hierarchy;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    public static Project GetProjectFromHierarchy(IVsHierarchy hierarchy)
    {
        try
        {
            if (!ErrorHandler.Succeeded(hierarchy.GetProperty(
                    VSConstants.VSITEMID_ROOT,
                    (int) __VSHPROPID.VSHPROPID_ExtObject,
                    out var projectObj)))
                return null;

            return projectObj as Project;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    public static string SafeGetProjectFilePath(IVsHierarchy vsHierarchy)
    {
        var project = GetProjectFromHierarchy(vsHierarchy);
        return project?.FullName;
    }

    public static string GetMsBuildPropertyValue(Project project, string propertyName)
    {
        try
        {
            var hierarchy = GetHierarchyFromProject(project);
            if (!(hierarchy is IVsBuildPropertyStorage propertyStorage))
                return null;

            if (ErrorHandler.Succeeded(
                    propertyStorage.GetPropertyValue(propertyName,
                        project.ConfigurationManager.ActiveConfiguration.ConfigurationName,
                        (uint) _PersistStorageType.PST_PROJECT_FILE, out var propValue)))
                return propValue;

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    public static IEnumerable<ProjectItem> GetProjectItems(Project project) => GetProjectItems(project.ProjectItems);

    private static IEnumerable<ProjectItem> GetProjectItems(ProjectItems projectItems)
    {
        foreach (ProjectItem projectItem in projectItems)
        {
            yield return projectItem;
            if (projectItem.ProjectItems != null)
                foreach (var subProjectItem in GetProjectItems(projectItem.ProjectItems))
                    yield return subProjectItem;
        }
    }

    public static IEnumerable<ProjectItem> GetPhysicalFileProjectItems(Project project) =>
        GetProjectItems(project).Where(IsPhysicalFile);

    public static ProjectItem FindProjectItemByFilePath(Project project, string filePath)
    {
        return GetPhysicalFileProjectItems(project)
            .FirstOrDefault(pi => string.Equals(filePath, GetFilePath(pi), StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsPhysicalFile(ProjectItem projectItem) => string.Equals(projectItem.Kind,
        VSConstants.GUID_ItemType_PhysicalFile.ToString("B"), StringComparison.InvariantCultureIgnoreCase);

    public static string GetFilePath(ProjectItem projectItem)
    {
        if (!IsPhysicalFile(projectItem))
            return null;

        return projectItem.FileNames[1];
    }

    public static IEnumerable<Project> GetAllProjects(DTE dte)
    {
        var projects = dte.Solution.Projects.OfType<Project>().ToArray();
        return EnumerateProjectHierarchy(projects);
    }

    private static IEnumerable<Project> EnumerateProjectHierarchy(IEnumerable<Project> projects)
    {
        foreach (var project in projects)
        {
            yield return project;
            var subProjects = project.ProjectItems.OfType<ProjectItem>().Select(x => x.SubProject).OfType<Project>()
                .ToArray();
            foreach (var subProject in EnumerateProjectHierarchy(subProjects)) yield return subProject;
        }
    }

    public static string GetVsMainVersion(IServiceProvider serviceProvider)
    {
        const string defaultMainVersion = "17.0";
        try
        {
            var dte = (DTE) serviceProvider.GetService(typeof(DTE));
            return dte?.Version ?? defaultMainVersion;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return defaultMainVersion;
        }
    }

    // https://stackoverflow.com/a/55039958
    public static string GetVsProductDisplayVersionSafe(IServiceProvider serviceProvider)
    {
        try
        {
            var vsAppId = serviceProvider.GetService<IVsAppId>(typeof(SVsAppId));
            vsAppId.GetProperty((int) VSAPropID.VSAPROPID_ProductDisplayVersion, out var productDisplayVersion);

            var displayVersion = productDisplayVersion as string;
            return displayVersion ?? GetVsMainVersion(serviceProvider);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return GetVsMainVersion(serviceProvider);
        }
    }

    public static IEnumerable<NuGetInstalledPackage> GetInstalledNuGetPackages(IServiceProvider serviceProvider,
        string projectFullName)
    {
        return ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            var solution = serviceProvider.GetService<SVsSolution, IVsSolution>();
            int result = solution.GetProjectOfUniqueName(projectFullName, out IVsHierarchy project);
            if (result != VSConstants.S_OK)
                throw new Exception(
                    $"Error calling {nameof(IVsSolution)}.{nameof(IVsSolution.GetProjectOfUniqueName)}: {result}");

            result = solution.GetGuidOfProject(project, out Guid projectGuid);
            if (result != VSConstants.S_OK)
                throw new Exception(
                    $"Error calling {nameof(IVsSolution)}.{nameof(IVsSolution.GetGuidOfProject)}: {result}");

            var serviceBrokerContainer =
                serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
            var serviceBroker = serviceBrokerContainer.GetFullAccessServiceBroker();

            var projectService =
                await serviceBroker.GetProxyAsync<INuGetProjectService>(NuGetServices.NuGetProjectServiceV1);
            using (projectService as IDisposable)
            {
                var packagesResult =
                    await projectService.GetInstalledPackagesAsync(projectGuid, CancellationToken.None);
                if (packagesResult.Status != InstalledPackageResultStatus.Successful)
                    throw new Exception("Unexpected result from GetInstalledPackagesAsync: " + packagesResult.Status);
                return packagesResult.Packages;
            }
        });
    }

    [StructLayout(LayoutKind.Sequential)]
    public class User32Point
    {
        public int x;
        public int y;

        public User32Point()
        {
            x = 0;
            y = 0;
        }

        public User32Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
