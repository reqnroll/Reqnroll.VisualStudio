Feature: Find step definition usages command

Rules:
* List usages of a step definition and allows jumping to the usage
	* Finds usages of a step definition with a few usage
	* Finds usage of a step definition with a single usage
* Detects and report unused step definition
	* The step definition is not used
* Manual: Offers to show usages in the search window

Scenario: Finds usage of a step definition with a single usage
	Given there is a Reqnroll project scope
	And the following feature file "Addition.feature"
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
		"""
	And the following C# step definition class in the editor
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When("I press add")]
			public void WhenIPressAdd()
			{{caret} 
			}
		}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Find Step Definition Usages" command
	Then a jump list "Step definition usages" is opened with the following steps
		| step                                    |
		| Addition.feature(4,2): When I press add |
	And invoking the first item from the jump list navigates to the "I press add" step in "Addition.feature" line 4

Scenario: Finds usage of a step definition with a few usage
	Given there is a Reqnroll project scope
	And the following feature file "Addition.feature"
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			When I press add
		Scenario: Other scenario
			When I press add
		"""
	And the following C# step definition class in the editor
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When("I press add")]
			public void WhenIPressAdd()
			{{caret} 
			}
		}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Find Step Definition Usages" command
	Then a jump list "Step definition usages" is opened with the following steps
		| step                                    |
		| Addition.feature(4,2): When I press add |
		| Addition.feature(5,2): When I press add |
		| Addition.feature(7,2): When I press add |
	And invoking the first item from the jump list navigates to the "I press add" step in "Addition.feature" line 4

Scenario: The step definition is not used
	Given there is a Reqnroll project scope
	And the following feature file "Addition.feature"
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press multiply
		"""
	And the following C# step definition class in the editor
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When("I press add")]
			public void WhenIPressAdd()
			{{caret} 
			}
		}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Find Step Definition Usages" command
	Then a jump list "Step definition usages" is opened with the following steps
		| step                     |
		| Could not find any usage |
