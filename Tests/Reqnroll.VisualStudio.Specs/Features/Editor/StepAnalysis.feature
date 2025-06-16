Feature: Step analysis

Rules:

* Highlight defined/undefined steps
	* Highlights defined/undefined steps
	* Highlights step parameters
	* Analyses all examples of scenario outline
* Show step definition errors
	* The step definition has invalid parameter count
	* Ambiguous step definitions
* Handle scopes
	* Matches scoped step definitions
	* Analyses all scopes of background steps
* Refresh analysis after build
	* Step is just defined and the project is built

Result Variations:
* Match
	* Matching step => jump
		* Unscoped
        * DataTable&DocString => select overload based on param type
		* Scoped
		* Unscoped&Scoped => use scoped or unscoped depending on contex (no ambiguity)
        * SO, but mathces to the same stepdef
	* Binding errors (e.g. parameter count) => err,jump
	* OUT: Parameter error (e.g. invalid conversion)
	* OUT: Same method matches multiple ways
* Undefined
	* No candidating step definitions => undef
	* OUT: Some match, but with different type => error (now: undef)
	* OUT: Some match, but with different scope => error (now: undef)
    * SO, but all (incl. empty) undefined => undef
* Ambiguous
	* Multiple matching step => err,list
	* Multiple step, some with binding/parameter errors => err,list
    * SO, but all ambiguous => err,list (of merged candidates)
* Multi-match (SO, Background)
	* All match => list
	* Some with binding/parameter errors => err,list
	* Some undefined => undef,list incl. generate step def
	* Some ambiguous => err,list

Scenario: Highlights defined/undefined steps
	Given there is a Reqnroll project scope with calculator step definitions
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			Then there is an undefined step
		"""
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep,UndefinedStep should be highlighted as
		"""
		Feature: Addition

		Scenario: Add two numbers
			When {DefinedStep}I press add{/DefinedStep}
			Then {UndefinedStep}there is an undefined step{/UndefinedStep}
		"""

Scenario: Highlights step parameters
	Given there is a Reqnroll project scope with calculator step definitions
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			Then the result should be 120 on the screen
		"""
	And the project is built and the initial binding discovery is performed
	Then all StepParameter section should be highlighted as
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			Then the result should be {StepParameter}120{/StepParameter} on the screen
		"""

Scenario: Analyses all examples of scenario outline
	Given there is a Reqnroll project scope with calculator step definitions
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario Outline: Add two numbers
			# this step is both defined (1st ex) and undefined (2nd ex)
			When I press <what>
		Examples: 
			| what         |
			| add          |
			| undefined op |
		"""
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep,UndefinedStep should be highlighted as
		"""
		Feature: Addition

		Scenario Outline: Add two numbers
			# this step is both defined (1st ex) and undefined (2nd ex)
			When {DefinedStep}{UndefinedStep}I press <what>{/UndefinedStep}{/DefinedStep}
		Examples: 
			| what         |
			| add          |
			| undefined op |
		"""

Scenario: The step definition has invalid parameter count
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type | regex                             | param types |
		| When | I use a step with (\d+) parameter |             |
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario: My scenario
			When I use a step with 1 parameter
		"""
	And the project is built and the initial binding discovery is performed
	Then all BindingError section should be highlighted as
		"""
		Feature: Addition

		Scenario: My scenario
			When {BindingError}I use a step with 1 parameter{/BindingError}
		"""

Scenario: Ambiguous step definitions
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type | regex       | method |
		| When | I use a .*  | M1     |
		| When | I .* a step | M2     |
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario: My scenario
			When I use a step
		"""
	And the project is built and the initial binding discovery is performed
	Then all BindingError section should be highlighted as
		"""
		Feature: Addition

		Scenario: My scenario
			When {BindingError}I use a step{/BindingError}
		"""

Scenario: Matches tag scoped step definitions
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type | regex                        | tag scope   |
		| When | I use mytag scoped step      | @mytag      |
		| When | I use featuretag scoped step | @featuretag |
	When the following feature file is opened in the editor
		"""
		@featuretag
		Feature: Addition

		@mytag
		Scenario: Tagged scenario
			When I use mytag scoped step
			When I use featuretag scoped step

		Scenario: Untagged scenario
			When I use mytag scoped step
			When I use featuretag scoped step
		"""
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep,UndefinedStep should be highlighted as
		"""
		@featuretag
		Feature: Addition

		@mytag
		Scenario: Tagged scenario
			When {DefinedStep}I use mytag scoped step{/DefinedStep}
			When {DefinedStep}I use featuretag scoped step{/DefinedStep}

		Scenario: Untagged scenario
			When {UndefinedStep}I use mytag scoped step{/UndefinedStep}
			When {DefinedStep}I use featuretag scoped step{/DefinedStep}
		"""

Scenario: Matches feature scoped step definitions
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type | regex                       | feature scope |
		| When | I use a feature scoped step | Addition      |
		| When | I use a feature scoped step | Substraction  |
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario: Random scenario
			When I use a feature scoped step
		"""
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep should be highlighted as
		"""
		Feature: Addition

		Scenario: Random scenario
			When {DefinedStep}I use a feature scoped step{/DefinedStep}
		"""
	And no binding error should be highlighted

Scenario: Matches scenario scoped step definitions
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type | regex                        | scenario scope  |
		| When | I use a scenario scoped step | Random scenario |
		| When | I use a scenario scoped step |                 |
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario: Random scenario
			When I use a scenario scoped step
		"""
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep should be highlighted as
		"""
		Feature: Addition

		Scenario: Random scenario
			When {DefinedStep}I use a scenario scoped step{/DefinedStep}
		"""
	And no binding error should be highlighted

Scenario: Matches combination scoped step definitions
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type | regex                               | scenario scope  | feature scope | tag scope |
		| When | I use a combination scoped step     | Random scenario | Addition      | @mytag    |
		| When | I use a feature and tag scoped step |                 | Addition      | @mytag    |
		| When | I use a feature scoped step         |                 | Addition      | @mytag    |
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		@mytag
		Scenario: Random scenario
			When I use a combination scoped step

		@mytag
		Scenario: Another scenario
			When I use a feature and tag scoped step

		Scenario: Yet another scenario
			When I use a feature scoped step
		"""
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep,UndefinedStep should be highlighted as
		"""
		Feature: Addition
		
		@mytag
		Scenario: Random scenario
			When {DefinedStep}I use a combination scoped step{/DefinedStep}
		
		@mytag
		Scenario: Another scenario
			When {DefinedStep}I use a feature and tag scoped step{/DefinedStep}

		Scenario: Yet another scenario
			When {UndefinedStep}I use a feature scoped step{/UndefinedStep}
		"""
	And no binding error should be highlighted

Scenario: Matches on multiple tags (without improperly highlighting as Ambiguous)
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type | regex                               | tag scope          |
		| When | I use a step with multiple tags     | @mytag1,@mytag2    |
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		@mytag1
		@mytag2
		Scenario: Random scenario
			When I use a step with multiple tags

		"""
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep should be highlighted as
		"""
		Feature: Addition
		
		@mytag1
		@mytag2
		Scenario: Random scenario
			When {DefinedStep}I use a step with multiple tags{/DefinedStep}
		"""
	And no binding error should be highlighted

Scenario: Analyses all scopes of background steps
	Given there is a Reqnroll project scope
	And the following step definitions in the project:
		| type  | regex                         | tag scope    |
		| Given | I use scenariotag scoped step | @scenariotag |
		| Given | I use ruletag scoped step     | @ruletag     |
		| Given | I use othertag scoped step    | @othertag    |
		| When  | I use a normal step           |              |
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Background: 
			Given I use scenariotag scoped step
			Given I use othertag scoped step

		@scenariotag
		Scenario: Tagged scenario
			When I use a normal step

		@ruletag
		Rule: Sample rule

		Background: 
			Given I use ruletag scoped step
			Given I use othertag scoped step

		@scenariotag
		Scenario: Scenario in tagged rule
			When I use a normal step
		"""
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep,UndefinedStep should be highlighted as
		"""
		Feature: Addition

		Background: 
			Given {DefinedStep}I use scenariotag scoped step{/DefinedStep}
			Given {UndefinedStep}I use othertag scoped step{/UndefinedStep}

		@scenariotag
		Scenario: Tagged scenario
			When {DefinedStep}I use a normal step{/DefinedStep}

		@ruletag
		Rule: Sample rule

		Background: 
			Given {DefinedStep}I use ruletag scoped step{/DefinedStep}
			Given {UndefinedStep}I use othertag scoped step{/UndefinedStep}

		@scenariotag
		Scenario: Scenario in tagged rule
			When {DefinedStep}I use a normal step{/DefinedStep}
		"""


Scenario: Step is just defined and the project is built
	Given there is a Reqnroll project scope with calculator step definitions
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			Then there is an undefined step
		"""
	When a new step definition is added to the project as:
		| method       | type | regex                      |
		| ThenNewStep1 | Then | there is an undefined step |
	And the project is built and the initial binding discovery is performed
	Then all section of types DefinedStep,UndefinedStep should be highlighted as
		"""
		Feature: Addition

		Scenario: Add two numbers
			When {DefinedStep}I press add{/DefinedStep}
			Then {DefinedStep}there is an undefined step{/DefinedStep}
		"""
