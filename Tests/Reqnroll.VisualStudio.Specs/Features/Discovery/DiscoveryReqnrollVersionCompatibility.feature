Feature: Discovery - Reqnroll version compatibility

Scenario Outline: Discover bindings from a Reqnroll project on .NET Framework
	Given there is a simple Reqnroll project for <version>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples:
	| case      | version  |
	| line-v3.9 | v3.9.40  |
	| line-v3.8 | v3.8.14  |
	| line-v3.7 | v3.7.13  |
	| line-v3.6 | v3.6.23  |
	| line-v3.5 | v3.5.14  |
	| line-v3.4 | v3.4.31  |
	| line-v3.3 | v3.3.74  |
	| line-v3.1 | v3.1.97  |
	#| line-v3.0 | v3.0.225 |
	| line-v2.4 | v2.4.1   |
	| line-v2.3 | v2.3.2   |
	| line-v2.2 | v2.2.1   |
	| line-v2.1 | v2.1.0   |
	| line-v2.0 | v2.0.0   |
	| line-v1.9 | v1.9.0   |

Scenario Outline: Discover bindings from a Reqnroll project on .NET Core
	Given there is a simple Reqnroll project for <version>
	And the project uses the new project format
	And the target framework is netcoreapp2.1
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples:
	| case      | version |
	| line-v3.9 | v3.9.22 |
	| line-v3.8 | v3.8.14 |
	| line-v3.7 | v3.7.13 |
	| line-v3.6 | v3.6.23 |
	| line-v3.5 | v3.5.14 |
	| line-v3.4 | v3.4.31 |
	| line-v3.3 | v3.3.74 |

Scenario Outline: Discover bindings from Reqnroll using different test runners
	Given there is a simple Reqnroll project with test runner "<test runner tool>" for the latest version
	And the project uses the new project format
	And the target framework is net5.0
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples:
	| test runner tool |
	| NUnit            |
	| xUnit            |
	| MsTest           |

Scenario Outline: Regression tests for special discovery combinations
	Given there is a simple Reqnroll project with test runner "<test runner tool>" for <version>
	And the project uses the new project format
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples:
	| case                           | version | framework | test runner tool |
	| v3.8 + MsTest discovery issue  | v3.8.14 | net5.0    | MsTest           |
	| (#18) Netframework with MsTest | v3.9.40 | net48     | MsTest           |

Scenario Outline: Discover bindings with the right Reqnroll connector
	Given there is a simple Reqnroll project for <version>
	And the project uses the new project format
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
Examples:
	| case             | version | framework     |
	| V3-net8.0        | v3.9.74 | net8.0        |
	| V3-net7.0        | v3.9.22 | net7.0        |
	| V3-net6.0        | v3.9.22 | net6.0        |
	| V3-net5.0        | v3.9.22 | net5.0        |
	| V3-netcoreapp3.1 | v3.9.22 | netcoreapp3.1 |
	| V3-netcoreapp2.1 | v3.9.22 | netcoreapp2.1 |
	| V2-net6.0        | v3.9.8  | net6.0        |
	| V2-net5.0        | v3.9.8  | net5.0        |
	| V2-netcoreapp3.1 | v3.9.8  | netcoreapp3.1 |
	| V2-netcoreapp2.1 | v3.9.8  | netcoreapp2.1 |
