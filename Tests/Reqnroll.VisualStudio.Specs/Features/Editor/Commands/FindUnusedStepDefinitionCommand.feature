Feature: Find unused step definitions command

Rules:
* List unused step definitions and allows jumping to the step definition method
	* Finds unused step definitions
	* Does not find step definitions that are bound to feature steps
	* Finds unused step definitions with multiple binding attributes, listing only those that are unused

Scenario: Find unused step definition with a single step kind attribute
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
			[When("I press multiply")]
			public void WhenIPressMultiply()
			{{caret} 
			}
		}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Find Unused Step Definitions" command
	Then a jump list "Unused Step Definitions" is opened with the following steps
		| step                                    |
		| Steps.cs(11,1): [When("I press multiply")] MyProject.CalculatorSteps.WhenIPressMultiply |
	And invoking the first item from the jump list navigates to the "I press multiply" "When" step definition

Scenario: Find unused step definition with multiple step kind attributes, ignoring bound steps
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
			[When("I press multiply")]
			[Then("I press equals")]
			[When("I press add")]
			public void WhenIPressMultiply()
			{{caret} 
			}
		}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Find Unused Step Definitions" command
	Then a jump list "Unused Step Definitions" is opened with the following steps
		| step                                    |
		| Steps.cs(13,1): [When("I press multiply")] MyProject.CalculatorSteps.WhenIPressMultiply |
		| Steps.cs(13,1): [Then("I press equals")] MyProject.CalculatorSteps.WhenIPressMultiply |
	And invoking the first item from the jump list navigates to the "I press multiply" "When" step definition

Scenario: Find unused step definition Command reports properly when No Unused Binding Methods exist
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
	When I invoke the "Find Unused Step Definitions" command
	Then a jump list "Unused Step Definitions" is opened with the following steps
		| step                                                               |
		| Could not find any unused step definitions at the current position |
