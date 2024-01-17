global using Reqnroll;$if$ ('$unittestframework$' == 'NUnit')
global using NUnit;$endif$$if$ ('$unittestframework$' == 'MSTest')
global using Microsoft.VisualStudio.TestTools.UnitTesting;$endif$$if$ ('$unittestframework$' == 'xUnit')
global using Xunit;$endif$$if$ ('$fluentassertionsincluded$' == 'True')
global using FluentAssertions;$endif$
