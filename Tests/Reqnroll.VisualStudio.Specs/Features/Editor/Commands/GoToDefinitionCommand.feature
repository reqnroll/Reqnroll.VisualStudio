Feature: Go to definition command

Rule: Jumps to step definition if defined

Scenario: Jumps to the step definition
	Given there is a Reqnroll project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press{caret} add
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Go To Definition" command
	Then the source file of the "I press add" "When" step definition is opened
	And the caret is positioned to the step definition method

Scenario: Lists step definitions if multiple step definitions matching
	e.g. at scenario outline or at background
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type | regex            | 
		| When | I press add      | 
		| When | I press multiply | 
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario Outline: Add two numbers
			When I press <what>{caret}
		Examples: 
			| what     |
			| add      |
			| multiply |
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Go To Definition" command
	Then a jump list "Go to step definitions" is opened with the following items
		| step definition  |
		| I press add      |
		| I press multiply |
	And invoking the first item from the jump list navigates to the "I press add" "When" step definition

Rule: Jumps to hooks when invoked from scenario header

Scenario: Lists hooks related to the scenario
	Given there is a Reqnroll project scope
	And the following hooks in the project:
		| type           | method        | 
		| BeforeScenario | ResetDatabase | 
		| AfterScenario  | SaveLogs      | 
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		@login
		Scenario: Add two{caret} numbers
			When I press add
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Go To Definition" command
	Then a jump list "Go to hooks" is opened with the following items
		| hook          | hook type      |
		| ResetDatabase | BeforeScenario |
		| SaveLogs      | AfterScenario  |
	And invoking the first item from the jump list navigates to the "ResetDatabase" hook

Rule: Do not do anything if cursor is not standing on a step

Scenario: Cursor stands in a scenario header line
	Given there is a Reqnroll project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two {caret}numbers
			When I press add
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Go To Definition" command
	Then there should be no navigation actions performed

Rule: Offers copying step definition skeleton to clipboard if undefined

Scenario: Navigate from an undefined step
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press multiply{caret}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Go To Definition" command
	Then the step definition skeleton for the "I press multiply" "When" step should be offered to copy to clipboard
