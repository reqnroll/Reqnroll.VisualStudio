#nullable disable
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace Reqnroll.SampleProjectGenerator;

public abstract class ProjectChanger
{
    protected readonly string _configJsonFilePath;
    protected readonly JObject _configJson;
    protected readonly string _projectFilePath;
    protected readonly string _projectFolder;
    protected readonly XDocument _projXml;
    protected string _targetPlatform;

    protected ProjectChanger(string projectFilePath, string targetPlatform = null)
    {
        _projectFilePath = projectFilePath;
        _targetPlatform = targetPlatform;
        _projectFolder = Path.GetDirectoryName(_projectFilePath);
        _configJsonFilePath = Path.Combine(_projectFolder, "reqnroll.json");
        _projXml = Load(projectFilePath);
        _configJson = LoadJson(_configJsonFilePath);
    }

    private JObject LoadJson(string configJsonFilePath)
    {
        return JObject.Parse(File.ReadAllText(configJsonFilePath));
    }

    protected XDocument Load(string path)
    {
        using (var reader = new StreamReader(path))
        {
            return XDocument.Load(reader);
        }
    }

    protected void Save(XDocument xDoc, string path)
    {
        using (var writer = new StreamWriter(path, false, Encoding.UTF8))
        {
            xDoc.Save(writer);
        }
    }

    public abstract void SetPlatformTarget(string platformTarget);

    public abstract NuGetPackageData InstallNuGetPackage(string packagesFolder, string packageName,
        string sourcePlatform = "net462", string packageVersion = null, bool dependency = false);

    public virtual void SetReqnrollConfig(string name, string attr, string value)
    {
        var settingObj = _configJson as JToken;
        foreach (var nameSection in name.Split('/'))
        {
            settingObj = EnsureReqnrollSetting(nameSection, settingObj);
        }

        ((JObject)settingObj).Add(attr, new JValue(value));
    }

    private static JToken EnsureReqnrollSetting(string name, JToken baseObj)
    {
        var isArray = name.EndsWith("[]");
        name = name.TrimEnd('[', ']');

        if (baseObj is JArray baseArray)
        {
            var settingObj = isArray ? (JToken)new JArray() : new JObject();
            baseArray.Add(settingObj);
            return settingObj;
        }
        else
        {
            var settingObj = baseObj[name];
            if (settingObj == null)
            {
                settingObj = isArray ? new JArray() : new JObject();
                ((JObject)baseObj).Add(name, settingObj);
            }

            return settingObj;
        }
    }

    public void AddAssemblyReferencesFromFolder(string folderPath)
    {
        AddAssemblyReferences(Directory.GetFiles(folderPath, "*.dll"));
    }

    public void AddAssemblyReferences(params string[] assemblyPaths)
    {
        /*
        <ItemGroup>
            <Reference Include="Newtonsoft.Json">
                <HintPath>packages\Newtonsoft.Json.9.0.1\lib\net45\Newtonsoft.Json.dll</HintPath>
            </Reference>
        </ItemGroup>
        */

        var lastItemGroup = DescendantsSimple(_projXml, "ItemGroup").Last();
        var itemGroup = CreateElement("ItemGroup");
        lastItemGroup.AddAfterSelf(itemGroup);

        foreach (var assemblyPath in assemblyPaths)
        {
            var refElement = CreateElement("Reference");
            refElement.SetAttributeValue("Include", Path.GetFileNameWithoutExtension(assemblyPath));
            refElement.Add(CreateElement("HintPath", GetRelativePath(assemblyPath, _projectFolder)));
            itemGroup.Add(refElement);
        }
    }

    protected abstract IEnumerable<XElement> DescendantsSimple(XContainer me, string simpleName);
    protected abstract XElement CreateElement(string simpleName, object content);

    public XElement CreateElement(string simpleName) => CreateElement(simpleName, null);

    public static string GetRelativePath(string path, string basePath)
    {
        path = Path.GetFullPath(path);
        basePath = Path.GetFullPath(basePath);
        if (string.Equals(path, basePath, StringComparison.OrdinalIgnoreCase))
            return "."; // the "this folder"

        if (path.StartsWith(basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return path.Substring(basePath.Length + 1);

        //handle different drives
        string pathRoot = Path.GetPathRoot(path);
        if (!string.Equals(pathRoot, Path.GetPathRoot(basePath), StringComparison.OrdinalIgnoreCase))
            return path;

        //handle ".." pathes
        string[] pathParts = path.Substring(pathRoot.Length)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string[] basePathParts = basePath.Substring(pathRoot.Length)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        int commonFolderCount = 0;
        while (commonFolderCount < pathParts.Length && commonFolderCount < basePathParts.Length &&
               string.Equals(pathParts[commonFolderCount], basePathParts[commonFolderCount],
                   StringComparison.OrdinalIgnoreCase))
            commonFolderCount++;

        StringBuilder result = new StringBuilder();
        for (int i = 0; i < basePathParts.Length - commonFolderCount; i++)
        {
            result.Append("..");
            result.Append(Path.DirectorySeparatorChar);
        }

        if (pathParts.Length - commonFolderCount == 0)
            return result.ToString().TrimEnd(Path.DirectorySeparatorChar);

        result.Append(string.Join(Path.DirectorySeparatorChar.ToString(), pathParts, commonFolderCount,
            pathParts.Length - commonFolderCount));
        return result.ToString();
    }

    public virtual void Save()
    {
        Save(_projXml, _projectFilePath);
        SaveJson(_configJson, _configJsonFilePath);
    }

    private void SaveJson(JObject jsonObj, string filePath)
    {
        File.WriteAllText(filePath, jsonObj.ToString(Formatting.Indented));
    }

    public abstract void AddFile(string filePath, string action, string generator = null,
        string generatedFileExtension = ".cs");

    protected XElement CreateActionElm(string filePath, string action)
    {
        var lastOfAction = DescendantsSimple(_projXml, action).LastOrDefault();
        XElement itemGroup;
        if (lastOfAction != null)
        {
            itemGroup = lastOfAction.Parent;
        }
        else
        {
            var lastItemGroup = DescendantsSimple(_projXml, "ItemGroup").LastOrDefault() ??
                                DescendantsSimple(_projXml, "PropertyGroup").Last();
            itemGroup = CreateElement("ItemGroup");
            lastItemGroup.AddAfterSelf(itemGroup);
        }

        var elm = CreateElement(action);
        var includeValue = Path.IsPathRooted(filePath) ? GetRelativePath(filePath, _projectFolder) : filePath;
        elm.SetAttributeValue("Include", includeValue);
        itemGroup.Add(elm);
        return elm;
    }

    public abstract IEnumerable<NuGetPackageData> GetInstalledNuGetPackages(string packagesFolder);

    public virtual void SetTargetFramework(string targetFramework)
    {
        throw new NotSupportedException();
    }
}
