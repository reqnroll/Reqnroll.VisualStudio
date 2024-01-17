Feature: Define steps command

Rules:

* Lists all undefined steps
  * There are undefined steps
  * Two undefined step has the same step definition skeleton
* Shows error when all steps defined
  * All steps are defined
* Selected step definition skeletons can be copied to clipboard
  * Selected step definition skeletons are copied to clipboard
* Selected step definitions skeletons can be saved to a new file
  * Selected step definition skeletons are saved to a new file
  * Selected step definition skeletons are saved to a new file in StepDefinitions folder

Scenario: There are undefined steps
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type  | regex                          |
		| Given | the operands have been entered |
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			Given the operands have been entered
			When I press multiply
			Then the result is calculated
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Define Steps" command
	Then the define steps dialog should be opened with the following step definition skeletons
		| type | expression               |
		| When | I press multiply         |
		| Then | the result is calculated |

Scenario: Two undefined step has the same step definition skeleton
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			Given the operand 4 has been entered
			And the operand 2 has been entered
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Define Steps" command
	Then the define steps dialog should be opened with the following step definition skeletons
		| type  | expression                        |
		| Given | the operand (.*) has been entered |

Scenario: All steps are defined
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type  | regex                     |
		| Given | the operands have been entered |
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			Given the operands have been entered
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Define Steps" command
	Then a ShowProblem dialog should be opened with "All steps have been defined in this file already."

Scenario: Selected step definition skeletons are copied to clipboard
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			Given the operands have been entered
			When I press multiply
			Then the result is calculated
		"""
	And the project is built and the initial binding discovery is performed
	And the "Define Steps" command is being invoked
	When I select the step definition snippets 0,1
	And close the define steps dialog with "Copy to clipboard"
	Then the following step definition snippets should be copied to the clipboard
		| type  | expression                     |
		| Given | the operands have been entered |
		| When  | I press multiply               |

Scenario: Selected step definition skeletons are saved to a new file
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			Given the operands have been entered
			When I press multiply
			Then the result is calculated
		"""
	And the project is built and the initial binding discovery is performed
	And the "Define Steps" command is being invoked
	When I select the step definition snippets 0,1
	And close the define steps dialog with "Create"
	Then the following step definition snippets should be in file "AdditionStepDefinitions.cs"
		| type  | expression                     |
		| Given | the operands have been entered |
		| When  | I press multiply               |

