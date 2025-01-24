Feature: Auto format document command	

Rules:

* Feature file can be formatted with useful defaults
* Auto format should not add or remove empty lines
	* Should not change descriptions, comments
* Selected text of a feature file can be formatted
* Formatting rules can be configured with editorconfig
* Caret should stay in the same line where it was

Rule: Feature file can be formatted with useful defaults

Scenario: Misformatted feature file is cleaned up
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
					@featureTag    @US1
		 Feature: Addition
		     Rule: Calculator functions

		  Background:
		Given I have entered 50 into the calculator

		  @focus
		  @WIP              @US1.1
		  Scenario: Add two numbers
		Given the following numbers added
				|  number| reason |
				  |   1   | first number  |
			   | 2  |second number |
		And foo
			When   bar
		
			Scenario Outline: Add multiple
				 Given I have entered <number1> to the calculator
				    And I have entered <number2> to the calculator
					When I check the output
					```
					1+2
					  3

 close
					```

			  @optimal
			Examples:
				| number1 | number2 |
				| 1 | 2|
			Examples: negative numbers
				| number1 | number2 |
				| -101 | -59              |
		"""
	When I invoke the "Auto Format Document" command
	Then the editor should be updated to
		"""
		@featureTag @US1
		Feature: Addition
		Rule: Calculator functions

		Background:
		    Given I have entered 50 into the calculator

		@focus
		@WIP @US1.1
		Scenario: Add two numbers
		    Given the following numbers added
		        | number | reason        |
		        | 1      | first number  |
		        | 2      | second number |
		    And foo
		    When bar

		Scenario Outline: Add multiple
		    Given I have entered <number1> to the calculator
		    And I have entered <number2> to the calculator
		    When I check the output
		        ```
		        1+2
		          3
		        
		        close
		        ```

		@optimal
		Examples:
		    | number1 | number2 |
		    | 1       | 2       |
		Examples: negative numbers
		    | number1 | number2 |
		    | -101    | -59     |
		"""

Rule: Formatting rules can be customized in the configuration file or from .editorconfig files

The settings in .editorconfig file override the setting from the config file. 

Used .editorconfig settings with the defaults:

 * indent_style = (inherited from VS, suggested: space)
 * indent_size = (inherited from VS, suggested: 2)
 * gherkin_indent_feature_children = false
 * gherkin_indent_rule_children = false
 * gherkin_indent_steps = true
 * gherkin_indent_and_steps = false
 * gherkin_indent_datatable = true
 * gherkin_indent_docstring = true
 * gherkin_indent_examples = false
 * gherkin_indent_examples_table = true
 * gherkin_table_cell_padding_size = 1

Scenario: The formatting rules are customized from configuration file
	In this scenario all rules are specified and all of them is the opposite as the default.
	The related editorconfig settings can be seen in the JSON comments.
	Given there is a Reqnroll project scope
	And the reqnroll.json configuration file contains
		"""
		{ 
			"ide": {
				"editor": {
					"gherkinFormat": {
						"indentFeatureChildren": true, // editorconfig: gherkin_indent_feature_children = true
						"indentRuleChildren": true, // editorconfig: gherkin_indent_rule_children = true
						"indentSteps": false, // editorconfig: gherkin_indent_steps = false
						"indentAndSteps": true, // editorconfig: gherkin_indent_and_steps = true
						"indentDataTable": false, // editorconfig: gherkin_indent_datatable = false
						"indentDocString": false, // editorconfig: gherkin_indent_docstring = false
						"indentExamples": true, // editorconfig: gherkin_indent_examples = true
						"indentExamplesTable": false, // editorconfig: gherkin_indent_examples_table = false
						"tableCellPaddingSize": 0 // editorconfig: gherkin_table_cell_padding_size = 0
					}
				}
			}
		}
		"""
	And the following feature file in the editor
		"""
					@featureTag    @US1
		 Feature: Addition
		     Rule: Calculator functions

		  Background:
		Given I have entered 50 into the calculator

		  @focus
		  @WIP              @US1.1
		  Scenario: Add two numbers
		Given the following numbers added
				|  number| reason |
				  |   1   | first number  |
			   | 2  |second number |
		And foo
			When   bar
		
			Scenario Outline: Add multiple
				 Given I have entered <number1> to the calculator
				    And I have entered <number2> to the calculator
					When I check the output
					```
					1+2
					  3

 close
					```

			  @optimal
			Examples:
				| number1 | number2 |
				| 1 | 2|
			Examples: negative numbers
				| number1 | number2 |
				| -101 | -59              |
		"""
	When I invoke the "Auto Format Document" command
	Then the editor should be updated to
		"""
		@featureTag @US1
		Feature: Addition
		    Rule: Calculator functions
		
		        Background:
		        Given I have entered 50 into the calculator
		
		        @focus
		        @WIP @US1.1
		        Scenario: Add two numbers
		        Given the following numbers added
		        |number|reason       |
		        |1     |first number |
		        |2     |second number|
		            And foo
		        When bar
		
		        Scenario Outline: Add multiple
		        Given I have entered <number1> to the calculator
		            And I have entered <number2> to the calculator
		        When I check the output
		        ```
		        1+2
		          3
		        
		        close
		        ```
		
		            @optimal
		            Examples:
		            |number1|number2|
		            |1      |2      |
		            Examples: negative numbers
		            |number1|number2|
		            |-101   |-59    |
		"""


Rule: Caret should stay in the same line where it was

Scenario: Caret is moved to the end of the line
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
			Feature: {caret}Addition
		Scenario: Add two numbers
		"""
	When I invoke the "Auto Format Document" command
	Then the editor should be updated to
		"""
		Feature: Addition{caret}
		Scenario: Add two numbers
		"""

Rule: Selected text of a feature file can be formatted

Scenario: Selected part of feature file is formatted
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		Given t{sel}he following numbers added
				|  number| reason |
				  |   1   | first number  |
			   | 2  |second number |
		And {/sel}foo
		   When   bar
		"""
	When I invoke the "Auto Format Selection" command
	Then the editor should be updated to
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		    Given the following numbers added
		        | number | reason        |
		        | 1      | first number  |
		        | 2      | second number |
		    And foo
		   When   bar
		"""

Scenario: Caret line of feature file is formatted
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		Given t{caret}he following numbers added
		   When   bar
		"""
	When I invoke the "Auto Format Selection" command
	Then the editor should be updated to
		"""
		 Feature: Addition
		  Scenario: Add two numbers
		    Given the following numbers added
		   When   bar
		"""

Rule: Auto format should not add or remove empty lines

Scenario: Formatting of Descriptions and Comments are not changed

	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
			this is an indented description

		#not working since 2000
		Scenario: Add two numbers
		  Todo: handle negative numbers

		    Given I have entered 50 into the calculator
		"""
	When I invoke the "Auto Format Document" command without waiting for the tag changes
	Then the editor should be updated to
		"""
		Feature: Addition
			this is an indented description

		#not working since 2000
		Scenario: Add two numbers
		  Todo: handle negative numbers

		    Given I have entered 50 into the calculator
		"""

Rule: Auto format should replace repeated keywords with "And"

Scenario: Repeating keywords are replaced with "And"

	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
		    Given I have entered 50 into the calculator
		    Given I have entered 70 into the calculator
			When I add them
			When I check the result
			Then there should be no error
			Then the result should be 120
		"""
	When I invoke the "Auto Format Document" command without waiting for the tag changes
	Then the editor should be updated to
		"""
		Feature: Addition

		Scenario: Add two numbers
		    Given I have entered 50 into the calculator
		    And I have entered 70 into the calculator
		    When I add them
		    And I check the result
		    Then there should be no error
		    And the result should be 120
		"""