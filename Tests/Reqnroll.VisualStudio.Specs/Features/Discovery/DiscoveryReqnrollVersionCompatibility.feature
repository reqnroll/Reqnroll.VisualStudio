Feature: Discovery - Reqnroll version compatibility

Scenario Outline: Discover bindings from a Reqnroll project on .NET Framework
	Given there is a simple Reqnroll project for <version>
	And the target framework is net481
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples:
	| case      | version               |
	| line-v1.0 | v1.0.0-pre20240125-60 |

Scenario Outline: Discover bindings from a Reqnroll project on .NET Core
	Given there is a simple Reqnroll project for <version>
	And the project uses the new project format
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples:
	| case      | version               |
	| line-v1.0 | v1.0.0-pre20240125-60 |

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

Scenario Outline: Discover bindings with the right Reqnroll connector
	Given there is a simple Reqnroll project for <version>
	And the project uses the new project format
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples:
	| case     | version               | framework |
	| G-net8.0 | v1.0.0-pre20240125-60 | net8.0    |
	| G-net7.0 | v1.0.0-pre20240125-60 | net7.0    |
	| G-net6.0 | v1.0.0-pre20240125-60 | net6.0    |
