namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public static class TaskExtensions
{
    public static Func<CancellationToken, Task> ApplyWhen(this Func<CancellationToken, Task> falseFunc, bool condition,
        Func<Func<CancellationToken, Task>, CancellationToken, Task> trueFunc)
    {
        return condition
            ? ct => trueFunc(falseFunc, ct)
            : falseFunc;
    }
}
