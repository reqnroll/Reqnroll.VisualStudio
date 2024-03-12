Feature: Find unused step definitions command

List unused step definitions and allows jumping to the step definition method

Rule: Finds step definitions that are not matching any feature steps

Scenario: Find unused step definition with a single step definition attribute
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

Scenario: Find unused step definition with a single step definition attribute across multiple feature files
	Given there is a Reqnroll project scope
	And the following feature file "Addition.feature"
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
		"""
	And the following feature file "Subrtraction.feature"
		"""
		Feature: Subtraction

		Scenario: Sub two numbers
			When I press Sub
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

Scenario: Reports if there were no unused step definitions
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
		| step                                 |
		| There are no unused step definitions |

Rule: Finds step definitions with multiple binding attributes, listing only those that are unused

Scenario: Only finds unused step definition attributes when the method had multiple attributes
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

