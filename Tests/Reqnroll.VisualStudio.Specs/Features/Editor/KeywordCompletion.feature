@gherkin6
Feature: Keyword completion

Rules:

* Completion offers the keywords available at the cursor position
  * In the beginning of the file it offers Language, Tag and Feature
  * After a sceanrio it offers Step keywords, Scenario and Scenario Oultine
* Completion offers language-specific keywords
  * Offers the keywords of the configured language
  * Offers the keywords of the file language
* Completion offers step arguments
  * After a step offers data table and doc string markers
* Completion list is shown when the first letter is typed in a line
  * Completion list is shown when the first letter is typed in a line

Scenario: In the beginning of the file it offers Language, Tag and Feature
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		{caret}
		"""
	When I invoke the "Complete" command
	Then a completion list should pop up with the following items
		| item           |
		| simulated error|
		| #language:     |
		| @tag1          |
		| Feature:       |
		| Business Need: |
		| Ability:       |

Scenario: After a sceanrio it offers Step keywords, Scenario, Scenario Oultine and Examples
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		{caret}
		"""
	When I invoke the "Complete" command
	Then a completion list should pop up with the following keyword items
		| item               |
		| Given              |
		| When               |
		| Then               |
		| And                |
		| But                |
		| Scenario:          |
		| Example:           |
		| Scenario Outline:  |
		| Scenario Template: |
		| Examples:          |
		| Scenarios:         |
		| Rule:              |

Scenario: Completes keyword at the caret position
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		  A{caret}
		"""
	When I invoke the "Complete" command
	And commit the "And" completion item
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		  And 
		"""

Scenario: Replaces keyword at the caret position
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		  Gi{caret}ven I have entered 70 into the calculator
		"""
	When I invoke the "Complete" command
	And commit the "And" completion item
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		  And I have entered 70 into the calculator
		"""

Scenario: Offers the keywords of the configured language
	Given there is a Reqnroll project scope
	And the reqnroll.json configuration file contains
		"""
		{ 
			"language": {
				"feature": "hu-HU"
			}
		}
		"""
	And the following feature file in the editor
		"""
		{caret}
		"""
	When I invoke the "Complete" command
	Then a completion list should pop up with the following keyword items
		| item       |
		| Jellemző:  |

Scenario: Offers the keywords of the file language
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		#language: hu
		{caret}
		"""
	When I invoke the "Complete" command
	Then a completion list should pop up with the following keyword items
		| item       |
		| Jellemző:  |

Scenario: After a step offers data table and doc string markers
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  Given I have entered 50 into the calculator
		{caret}
		"""
	When I invoke the "Complete" command
	Then a completion list should pop up with the following markers
		| item |
		| \|   |
		| """  |
		| ```  |
		| *    |
	
Scenario: Completion list is shown when the first letter is typed in a line
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		  {caret}
		"""
	When I invoke the "Complete" command by typing "B"
	Then a completion list should pop up with the following keyword items
		| item               |
		| Background:        |
		| Scenario:          |
		| Example:           |
		| Scenario Outline:  |
		| Scenario Template: |
		| Rule:              |

Scenario: A short description is shown for each keyword
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
		  {caret}
		"""
	And the "Complete" command is being invoked
	When I invoke the "Filter Completion" command by typing "Giv"
	Then a completion list should pop up with the following keyword items
		| item  | description                             |
		| Given | Describes the context for the behaviour |
