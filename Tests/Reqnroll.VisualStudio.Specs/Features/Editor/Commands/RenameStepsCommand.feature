Feature: RenameStepsCommand

Rule: Simple parameterless step definition can be renamed

Scenario: A simple step with a sigle usage is renamed from step definition (code side)
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
	And the "Rename Step" command is being invoked
	When I specify "I choose add" as renamed step
	Then the file "Addition.feature" should be updated to
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I choose add
		"""
    And the editor should be updated to contain
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When("I choose add")]
			public void WhenIPressAdd()
			{
			}
		}
		"""

Rule: The rename step command can be invoked from the feature file

Scenario: A step is renamed from feature file
	Given there is a Reqnroll project scope
	And the following C# step definition class
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When("I press add")]
			public void WhenIPressAdd()
			{
			}
		}
		"""
	And the following feature file in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add{caret}
		"""
	And the project is built and the initial binding discovery is performed
	And the "Rename Step" command is being invoked
	When I specify "I choose add" as renamed step
	Then the file "Feature1.feature" should be updated to
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I choose add
		"""
    And the editor should be updated to contain
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When("I choose add")]
			public void WhenIPressAdd()
			{
			}
		}
		"""


Rule: User should choose the step definition to rename in case there are multiple step definitions on the method


Scenario: Multiple step definitions declared for the method
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
			[Given("I press add")]
			[When("I press add")]
			[When("I invoke add")]
			public void WhenIPressAdd()
			{{caret} 
			}
		}
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Rename Step" command without waiting for the tag changes
	Then a jump list "Choose step definition to rename" is opened with the following items
		| step type | step definition |
		| Given     | I press add     |
		| When      | I press add     |
		| When      | I invoke add    |
	And invoking the first item from the jump list renames the "I press add" "Given" step definition

Rule: Parametrized step definitions can be renamed (without changing the order or the regex of the parameters)

* Out of scope: 
  * reorder/remove/add parameters
  * change parameter expressions
  * rename steps where the non-parameter part of the expression contains variable parts (regex, cucumber expression alternates, etc.)

Scenario: A parametrized step definition is renamed from code side
	Given there is a Reqnroll project scope
	And the following feature file "Addition.feature"
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I add the numbers 5 and 3 together
		"""
	And the following C# step definition class in the editor
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When(@"I add the numbers (.*) and (\d+) together")]
			public void WhenIAddTheNumbersTogether(int op1, int op2)
			{{caret} 
			}
		}
		"""
	And the project is built and the initial binding discovery is performed
	And the "Rename Step" command is being invoked
	When I specify "I sum the numbers (.*) and (\d+) up" as renamed step
	Then the file "Addition.feature" should be updated to
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I sum the numbers 5 and 3 up
		"""
    And the editor should be updated to contain
		"""
		[Binding]
		public class CalculatorSteps
		{
			[When(@"I sum the numbers (.*) and (\d+) up")]
			public void WhenIAddTheNumbersTogether(int op1, int op2)
			{ 
			}
		}
		"""

