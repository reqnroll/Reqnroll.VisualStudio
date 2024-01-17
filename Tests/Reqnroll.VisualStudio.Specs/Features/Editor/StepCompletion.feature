Feature: Step Completion

Scenario: Offers step definitions of the scenario block at the caret
	Given there is a Reqnroll project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		  When {caret}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Complete" command
	Then a completion list should pop up with the following items
		| item        |
		| I press add |

Scenario: Offers step definitions when space pressed after a step keyword
	Given there is a Reqnroll project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		  When{caret}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Complete" command by typing " "	
	Then a completion list should pop up with the following items
		| item        |
		| I press add |

Scenario: Completes step at the caret position
	Given there is a Reqnroll project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  When I {caret}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Complete" command
	And commit the "I press add" completion item
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
		  When I press add
		"""

Scenario: Replaces step at the caret position
	Given there is a Reqnroll project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  When I {caret} press mulitply
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Complete" command
	And commit the "I press add" completion item
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
		  When I press add
		"""

Scenario: Offers simple step definitions with parameter placeholders
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type  | regex                                               | param types |
		| Given | ^I have entered (.*) into the "([^"]*)" calculator$ | i\|s        |
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given {caret}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Complete" command
	Then a completion list should pop up with the following items
		| item                                                |
		| I have entered [int] into the "[string]" calculator |

Scenario: Offers complex step definitions as regex
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type  | regex                                       | param types |
		| Given | ^I use a (?:[step]+) with (\\d+) parameter$ | i           |
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given {caret}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Complete" command
	Then a completion list should pop up with the following items
		| item                                      |
		| I use a (?:[step]+) with (\\d+) parameter |

Scenario Outline: Filters completion list
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type  | regex                   |
		| Given | ^there is a calculator$ |
		| Given | ^something else$        |
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given {caret}
		"""
	And the project is built and the initial binding discovery is performed
	And the "Complete" command has been invoked
	When I invoke the "Filter Completion" command by typing "<filter text>"
	Then a completion list should list the following items
		| item            |
		| <matching item> |
Examples: 
	| description                     | filter text      | matching item         |
	| prefix match                    | there is         | there is a calculator |
	| contains                        | calc             | there is a calculator |
	| multiple word contains          | is a calc        | there is a calculator |
	| multiple word any order         | calculator there | there is a calculator |
	| multiple word any order, prefix | calculator the   | there is a calculator |
	| multiple word any order, infix  | cul he           | there is a calculator |
