var log = new ConsoleLogger();

Assembly TestAssemblyFactory(AssemblyLoadContext context, string testAssemblyPath)
{
    return context.LoadFromAssemblyPath(testAssemblyPath);
}

return (int)new Runner(log).Run(args, TestAssemblyFactory);
