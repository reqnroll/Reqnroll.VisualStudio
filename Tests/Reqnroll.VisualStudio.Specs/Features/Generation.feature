Feature: Generation

Scenario: Generate code-behind for a feature file
	Given there is a simple Reqnroll project for the latest version
	When the code-behind file is generated for a feature file in the project
	Then the generation succeeds
	And the code-behind file is updated

Scenario: Generate error pragmas for invalid feature file
	Given there is a simple Reqnroll project for the latest version
	And there is a syntax error in a feature file
	When the code-behind file is generated for the feature file in the project
	Then the generation fails
	And the code-behind file contains errors

Scenario Outline: Generate code-behind for a feature file with different Reqnroll versions
	Given there is a simple Reqnroll project for <version>
	When the code-behind file is generated for a feature file in the project
	Then the generation succeeds
	And the code-behind file is updated
	And the code-behind file contains "Generator Version:<generator version>"
Examples: 
	| case      | version | generator version |
	| latest    | v2.4.1  | 2.4.0             |
	| recent    | v2.4.0  | 2.4.0             |
	| line-v2.3 | v2.3.2  | 2.3.0             |
	| line-v2.2 | v2.2.1  | 2.2.0             |
	| line-v2.1 | v2.1.0  | 2.0.0             |
	| line-v2.0 | v2.0.0  | 2.0.0             |
	| line-v1.9 | v1.9.0  | 1.9.0             |

Scenario Outline: Generate code-behind for a feature file with different unit test providers
	Given there is a simple Reqnroll project with test runner "<test runner tool>" for the latest version
	When the code-behind file is generated for a feature file in the project
	Then the generation succeeds
	And the code-behind file is updated
	And the code-behind file contains "<attribute>"
Examples: 
	| test runner tool | attribute   |
	| NUnit            | TestFixture |
	| xUnit            | Fact        |
	| MsTest           | TestClass   |

Scenario: Generate code-behind for a feature file from Reqnroll project with plugin
	Given there is a simple Reqnroll project with plugin for the latest version
	When the code-behind file is generated for a feature file in the project
	Then the generation succeeds
	And the code-behind file is updated
	And the code-behind file contains "Deveroom.SampleGeneratorPlugin"

Scenario: Generate code-behind with Unicode steps
	Given there is a simple Reqnroll project with unicode bindings for the latest version
	When the code-behind file is generated for a feature file in the project
	Then the generation succeeds
	And the code-behind file contains Unicode step

