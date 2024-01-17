using System;
using System.Linq;

namespace Reqnroll.VisualStudio.ProjectSystem;

public class VsDeveroomErrorListServices : VsServiceBase, IDeveroomErrorListServices
{
    private readonly Lazy<ErrorListProvider> _errorListProvider;

    public VsDeveroomErrorListServices(IVsIdeScope vsIdeScope) : base(vsIdeScope)
    {
        _errorListProvider = new Lazy<ErrorListProvider>(() =>
            CreateErrorListProvider(vsIdeScope));
    }

    public void ClearErrors(DeveroomUserErrorCategory category)
    {
        var subCategoryIndex = _errorListProvider.Value.Subcategories.IndexOf(category.ToString());

        RunOnUiThread(() =>
        {
            if (_errorListProvider.Value.Tasks.OfType<TaskListItem>().All(t => t.SubcategoryIndex == subCategoryIndex))
                _errorListProvider.Value.Tasks.Clear();
            else
                foreach (var task in _errorListProvider.Value.Tasks.OfType<TaskListItem>()
                             .Where(t => t.SubcategoryIndex == subCategoryIndex).ToArray())
                    _errorListProvider.Value.Tasks.Remove(task);
        });
    }

    public void AddErrors(IEnumerable<DeveroomUserError> errors)
    {
        var errorTasks = errors.Select(e =>
            new ErrorTask
            {
                Category = TaskCategory.BuildCompile,
                ErrorCategory = e.Type,
                Text = e.Message,
                Document = e.SourceLocation?.SourceFile,
                Line = e.SourceLocation?.SourceFileLine ?? -1,
                Column = e.SourceLocation?.SourceFileColumn ?? -1,
                SubcategoryIndex = _errorListProvider.Value.Subcategories.IndexOf(e.Category.ToString())
            });

        RunOnUiThread(() =>
        {
            foreach (var task in errorTasks)
            {
                task.Navigate += NavigateTask;
                _errorListProvider.Value.Tasks.Add(task);
            }

            _errorListProvider.Value.Show();
        });
    }

    private static ErrorListProvider CreateErrorListProvider(IVsIdeScope vsIdeScope)
    {
        var errorListProvider = new ErrorListProvider(vsIdeScope.ServiceProvider)
        {
            ProviderName = "Reqnroll",
            ProviderGuid = new Guid("{A874372D-0AE1-46D9-88C7-0AAF7DDBACDF}")
        };
        errorListProvider.Subcategories.AddRange(Enum.GetNames(typeof(DeveroomUserErrorCategory)));
        return errorListProvider;
    }

    private void NavigateTask(object sender, EventArgs e)
    {
        if (sender is TaskListItem task)
        {
            if (string.IsNullOrEmpty(task.Document))
                _vsIdeScope.DeveroomOutputPaneServices?.Activate();
            else
                _errorListProvider.Value.Navigate(task, VSConstants.LOGVIEWID_TextView);
        }
    }
}
