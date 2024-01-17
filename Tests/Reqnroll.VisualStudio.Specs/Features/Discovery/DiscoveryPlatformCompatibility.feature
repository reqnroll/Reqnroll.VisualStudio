Feature: Discovery - Platform Compatibility

Scenario Outline: Discover bindings from a SpecFlow v3 project in different .NET Core platforms
	Given there is a simple Reqnroll project for v3.*
	And the project uses the new project format
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples: 
	| framework     |
	| netcoreapp2.0 |
	| netcoreapp2.1 |
	| netcoreapp2.2 |
	| netcoreapp3.0 |
	| netcoreapp3.1 |
	| net5.0        |
	| net6.0        |
