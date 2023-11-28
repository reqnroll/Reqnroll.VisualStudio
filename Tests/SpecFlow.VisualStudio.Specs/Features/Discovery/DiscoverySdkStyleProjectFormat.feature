Feature: Discovery - SDK-style project format

Scenario Outline: Discover bindings from SDK-style project format
	Given there is a simple SpecFlow project for <version>
	And the project uses the new project format
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples:
	| label        | framework     | version |
	| .NET new prj | net48         | v3.*    |
	| .NET Core    | netcoreapp2.1 | v3.*    |
	| .NET 6       | net6.0        | v3.*    |
	| .NET 7       | net7.0        | v3.*    |
	| .NET 8       | net8.0        | v3.*    |
	#| .NET SpecFlow v2 | net45         | v2.*    |
