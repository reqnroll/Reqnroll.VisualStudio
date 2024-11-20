Feature: Syntax coloring

Rules:

* Highlight definition line keywords
	* Highlights definition line keywords
* Highlight tags
	* Highlights tags
* Highlight definition descriptions
	* Highlights definition descriptions
* Highlight step keywords
	* Highlights step keywords
	* Highlights non-English step keywords
	* TODO: Highlights non-English step keywords when language is defined in Reqnroll config
* Highlight comments
	* Highlights comments
* Highlight step arguments
	* Highlights doc strings
	* Highlights data tables
* Highlight Scenario Outline placeholders
	* Highlights Scenario Outline placeholders


Scenario: Highlights definition line keywords
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		
		Background:
			Given I have entered 50 into the calculator
		
		Scenario: Add two numbers
			When I press add
		
		Scenario Outline: Add two numbers outline
			When I press <op>
		Examples:
			| op  |
			| add |
		"""
	Then all DefinitionLineKeyword section should be highlighted as
		"""
		{DefinitionLineKeyword}Feature:{/DefinitionLineKeyword} Addition
		
		{DefinitionLineKeyword}Background:{/DefinitionLineKeyword}
			Given I have entered 50 into the calculator
		
		{DefinitionLineKeyword}Scenario:{/DefinitionLineKeyword} Add two numbers
			When I press add
		
		{DefinitionLineKeyword}Scenario Outline:{/DefinitionLineKeyword} Add two numbers outline
			When I press <op>
		{DefinitionLineKeyword}Examples:{/DefinitionLineKeyword}
			| op  |
			| add |
		"""

@gherkin6
Scenario: Highlights rule line keywords
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		
		Scenario: General feature scenario
			When I press add
		
		Rule: Empty rule
		
		Rule: This is a rule
		
		Scenario: Scenario illustrating a rule
			When I press add
		
		Rule: This is a rule with a background
		
		Background:
			Given I have entered 50 into the calculator			
		
		Scenario: Scenario illustrating a rule with a background
			When I press add
		"""
	Then all DefinitionLineKeyword section should be highlighted as
		"""
		{DefinitionLineKeyword}Feature:{/DefinitionLineKeyword} Addition
		
		{DefinitionLineKeyword}Scenario:{/DefinitionLineKeyword} General feature scenario
			When I press add
		
		{DefinitionLineKeyword}Rule:{/DefinitionLineKeyword} Empty rule
		
		{DefinitionLineKeyword}Rule:{/DefinitionLineKeyword} This is a rule
		
		{DefinitionLineKeyword}Scenario:{/DefinitionLineKeyword} Scenario illustrating a rule
			When I press add
		
		{DefinitionLineKeyword}Rule:{/DefinitionLineKeyword} This is a rule with a background
		
		{DefinitionLineKeyword}Background:{/DefinitionLineKeyword}
			Given I have entered 50 into the calculator			
		
		{DefinitionLineKeyword}Scenario:{/DefinitionLineKeyword} Scenario illustrating a rule with a background
			When I press add
		"""

Scenario: Highlights tags
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		@foo @bar
		@baz
		Feature: Addition
		
		@qux
		Scenario: Add two numbers
			When I press add
		
		@rqux
		Rule: Commutativity
		
		@quux
		Scenario Outline: Add two numbers outline
			When I press <op>
		@corge
		Examples:
			| op  |
			| add |
		"""
	Then all Tag section should be highlighted as
		"""
		{Tag}@foo{/Tag} {Tag}@bar{/Tag}
		{Tag}@baz{/Tag}
		Feature: Addition
		
		{Tag}@qux{/Tag}
		Scenario: Add two numbers
			When I press add
		
		{Tag}@rqux{/Tag}
		Rule: Commutativity
		
		{Tag}@quux{/Tag}
		Scenario Outline: Add two numbers outline
			When I press <op>
		{Tag}@corge{/Tag}
		Examples:
			| op  |
			| add |
		"""

Scenario: Highlights definition descriptions
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		
		This is a description of a feature
			with indented line
		
			with empty line
		
		Background:
			This is a description of a background
			Given I have entered 50 into the calculator
		
		Scenario: Add two numbers
			This is a description of a scenario
			When I press add
		
		Scenario Outline: Add two numbers outline
			This is a description of a outline
			When I press <op>
		Examples:
			This is a description of an examples block
			| op  |
			| add |
		"""
	Then all Description section should be highlighted as
		"""
		Feature: Addition
		
		{Description}This is a description of a feature
			with indented line
		
			with empty line{/Description}
		
		Background:
		{Description}	This is a description of a background{/Description}
			Given I have entered 50 into the calculator
		
		Scenario: Add two numbers
		{Description}	This is a description of a scenario{/Description}
			When I press add
		
		Scenario Outline: Add two numbers outline
		{Description}	This is a description of a outline{/Description}
			When I press <op>
		Examples:
		{Description}	This is a description of an examples block{/Description}
			| op  |
			| add |
		"""


Scenario: Highlights step keywords
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		
		Scenario: Add two numbers
			Given I have entered 50 into the calculator
			And I have entered 70 into the calculator
			When I press add
			Then the result should be 120 on the screen
			But there should be no error indicated
		"""
	Then all StepKeyword section should be highlighted as
		"""
		Feature: Addition
		
		Scenario: Add two numbers
			{StepKeyword}Given {/StepKeyword}I have entered 50 into the calculator
			{StepKeyword}And {/StepKeyword}I have entered 70 into the calculator
			{StepKeyword}When {/StepKeyword}I press add
			{StepKeyword}Then {/StepKeyword}the result should be 120 on the screen
			{StepKeyword}But {/StepKeyword}there should be no error indicated
		"""

Scenario: Highlights non-English step keywords
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		#language: hu-HU
		Jellemző: Összeadás
		
		Forgatókönyv: Két számot összeadok
			Amennyiben összadom a számokat
		"""
	Then all section of types StepKeyword, DefinitionLineKeyword should be highlighted as
		"""
		#language: hu-HU
		{DefinitionLineKeyword}Jellemző:{/DefinitionLineKeyword} Összeadás
		
		{DefinitionLineKeyword}Forgatókönyv:{/DefinitionLineKeyword} Két számot összeadok
			{StepKeyword}Amennyiben {/StepKeyword}összadom a számokat
		"""

Scenario: Highlights non-English step keywords using default feature language
	Given there is a Reqnroll project scope
	And the reqnroll.json configuration file contains
		"""
		{ 
			"language": {
				"feature": "hu-HU"
			}
		}
		"""
	When the following feature file is opened in the editor
		"""
		Jellemző: Összeadás
		
		Forgatókönyv: Két számot összeadok
			Amennyiben összadom a számokat
		"""
	Then all section of types StepKeyword, DefinitionLineKeyword should be highlighted as
		"""
		{DefinitionLineKeyword}Jellemző:{/DefinitionLineKeyword} Összeadás
		
		{DefinitionLineKeyword}Forgatókönyv:{/DefinitionLineKeyword} Két számot összeadok
			{StepKeyword}Amennyiben {/StepKeyword}összadom a számokat
		"""

Scenario: Highlights comments
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		#language: en-US
		Feature: Addition
		
		# this is a comment
			# this is also a comment
		"""
	Then all Comment section should be highlighted as
		"""
		{Comment}#language: en-US{/Comment}
		Feature: Addition
		
		{Comment}# this is a comment{/Comment}
		{Comment}	# this is also a comment{/Comment}
		"""

Scenario: Highlights doc strings
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the following formulas
				```
				2 + 3
					4 * 8
		
				6 / 2
				```
			When I calculate the results
		"""
	Then all DocString section should be highlighted as
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the following formulas
		{DocString}		```
				2 + 3
					4 * 8
		
				6 / 2
				```{/DocString}
			When I calculate the results
		"""

Scenario: Highlights data table
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the operands entered
				| operand | type           |
				| 42      | integer        |
				| 7.1     | floating point |
			When I calculate the results
		"""
	Then all section of types DataTable, DataTableHeader should be highlighted as
		"""
		Feature: Addition
		Scenario: Add two numbers
			Given the operands entered
		{DataTable}		| {DataTableHeader}operand{/DataTableHeader} | {DataTableHeader}type{/DataTableHeader}           |
				| 42      | integer        |
				| 7.1     | floating point |{/DataTable}
			When I calculate the results
		"""

Scenario: Highlights Scenario Outline placeholders
	Given there is a Reqnroll project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		Scenario Outline: Add two numbers
			Given there is a number <a> and <b>
			When I press <op>
		Examples:
			| a | b | op  |
			| 1 | 2 | add |
		"""
	Then all ScenarioOutlinePlaceholder section should be highlighted as
		"""
		Feature: Addition
		Scenario Outline: Add two numbers
			Given there is a number {ScenarioOutlinePlaceholder}<a>{/ScenarioOutlinePlaceholder} and {ScenarioOutlinePlaceholder}<b>{/ScenarioOutlinePlaceholder}
			When I press {ScenarioOutlinePlaceholder}<op>{/ScenarioOutlinePlaceholder}
		Examples:
			| {ScenarioOutlinePlaceholder}a{/ScenarioOutlinePlaceholder} | {ScenarioOutlinePlaceholder}b{/ScenarioOutlinePlaceholder} | {ScenarioOutlinePlaceholder}op{/ScenarioOutlinePlaceholder}  |
			| 1 | 2 | add |
		"""

Rule: Language configuration changes are applied to open feature file editors

# this scenario is flaky on CI
@quarantine
Scenario: Default feature file language was changed
	Given there is a non-Reqnroll project scope
	And the reqnroll.json configuration file contains
		"""
		{ 
			"language": {
				"feature": "en-US"
			}
		}
		"""
	And the following feature file in the editor
		"""
		Jellemző: Összeadás
		
		Forgatókönyv: Két számot összeadok
			Amennyiben összadom a számokat
		"""
	When the reqnroll.json configuration file is updated to
		"""
		{ 
			"language": {
				"feature": "hu-HU"
			}
		}
		"""
	And the project is built
	Then all section of types StepKeyword, DefinitionLineKeyword should be highlighted as
		"""
		{DefinitionLineKeyword}Jellemző:{/DefinitionLineKeyword} Összeadás
		
		{DefinitionLineKeyword}Forgatókönyv:{/DefinitionLineKeyword} Két számot összeadok
			{StepKeyword}Amennyiben {/StepKeyword}összadom a számokat
		"""

Scenario Outline: Do not highlight undefined step keywords
	Given there is a project scope which is <projectKind>
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		
		Scenario: Add two numbers
			Given I have entered 50 into the calculator
			And I have entered 70 into the calculator
			When I press add
			Then the result should be 120 on the screen
			But there should be no error indicated
		"""
	Then there are no sections of type UndefinedStep

Examples:
	| projectKind                 |
	| FeatureFileContainerProject |
	| OtherProject                |
	| Uninitialized               |
