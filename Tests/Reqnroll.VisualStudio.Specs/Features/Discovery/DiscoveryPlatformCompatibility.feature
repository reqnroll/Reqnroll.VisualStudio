Feature: Discovery - Platform Compatibility

Scenario Outline: Discover bindings from a Reqnroll project in different .NET Core platforms
	Given there is a simple Reqnroll project for the latest version
	And the project uses the new project format
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples: 
	| framework |
	| net6.0    |
	| net8.0    |
	| net9.0    |
