var log = new ConsoleLogger();

Assembly TestAssemblyFactory(AssemblyLoadContext context, string testAssemblyPath)
{
    return context.LoadFromAssemblyPath(testAssemblyPath);
}

return new Runner(log)
    .Run(args, TestAssemblyFactory)
    .Map(result=>(int)result);
