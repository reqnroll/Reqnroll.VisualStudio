Feature: Automatic table formatting command

Rules:

* Autoformats DataTable
	* Autoformats DataTable when typing last pipe
	* Autoformats DataTable when typing middle pipe
	* Autoformats one-liner DataTable
* Autoformats Examples table

Scenario: Autoformats DataTable when typing last pipe
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the following numbers added
				|  number| reason |
				  |   1   | first number  |
			   | 2  |second number {caret}
		"""
	When I invoke the "Auto Format Table" command by typing "|"
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the following numbers added
				| number | reason        |
				| 1      | first number  |
				| 2      | second number |
		"""

Scenario: Autoformats DataTable when typing middle pipe
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the following numbers added
				|  number| reason |
				  |   1   {caret} first number  |
			   | 2  |second number |
		"""
	When I invoke the "Auto Format Table" command by typing "|"
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the following numbers added
				| number | reason        |
				| 1      | first number  |
				| 2      | second number |
		"""
		
Scenario: Autoformats one-liner DataTable
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the following numbers added
				|  number|    reason{caret}
		"""
	When I invoke the "Auto Format Table" command by typing "|"
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the following numbers added
				| number | reason |
		"""

Scenario: Autoformats Examples table
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario Outline: Add two numbers
			When I press add
		Examples: 
			|  number| reason |
				  |   1   {caret} first number  |
			   | 2  |second number |
		"""
	When I invoke the "Auto Format Table" command by typing "|"
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario Outline: Add two numbers
			When I press add
		Examples: 
			| number | reason        |
			| 1      | first number  |
			| 2      | second number |
		"""
		
