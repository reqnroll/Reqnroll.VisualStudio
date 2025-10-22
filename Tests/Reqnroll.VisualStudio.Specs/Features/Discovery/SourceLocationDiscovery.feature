Feature: Source Location Discovery

Scenario: Discover binding source location
	Given there is a small Reqnroll project
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line

Scenario: Discover binding source location from Reqnroll project with external bindings
	Given there is a small Reqnroll project with external bindings
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And there is a "Then" step with source file containing "ExternalBindings"

Scenario Outline: Discover binding source location from Reqnroll project with async bindings
	Given there is a small Reqnroll project with async bindings
	And the project uses the new project format
	And the target framework is <framework>
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And the step definitions contain source file and line
Examples:
	| framework |
	| net481    |
	| net8.0    |
	| net9.0    |
	| net10.0   |
