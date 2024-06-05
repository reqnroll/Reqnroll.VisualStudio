Feature: Discovery - Reqnroll version compatibility

Scenario Outline: Discover bindings from a Reqnroll project on .NET Framework
	Given there is a simple Reqnroll project for <version>
	And the target framework is net481
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples:
	| case    | version |
	| line-v1 | v1.0.1  |
	| line-v2 | v2.0.2  |

Scenario Outline: Discover bindings from a Reqnroll project on .NET Core
	Given there is a simple Reqnroll project for <version>
	And the project uses the new project format
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples:
	| case    | version |
	| line-v1 | v1.0.1  |
	| line-v2 | v2.0.2  |

Scenario Outline: Discover bindings from Reqnroll using different test runners
	Given there is a simple Reqnroll project with test runner "<test runner tool>" for the latest version
	And the project uses the new project format
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples:
	| test runner tool |
	| NUnit            |
	| xUnit            |
	| MsTest           |
