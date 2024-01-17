Feature: Go to step definition command

Rules:

* Jumps to step definition if defined
	* Jumps to the step definition
	* Lists step definitions if multiple step definitions matching (e.g. scenario outline)
* Do not do anything if cursor is not standing on a step
	* Cursor stands in a scenario header line
* Offers copying step definition skeleton to clipboard if undefined
	* Navigate from an undefined step

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
