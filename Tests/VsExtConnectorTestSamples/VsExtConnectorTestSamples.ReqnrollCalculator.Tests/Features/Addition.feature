Feature: Addition

The calculator functions related to addition, 
that is denoted with the '+' operator.

@core
Rule: Positive numbers can be added

Scenario: Add two numbers
    This is the most common use-case:
    people often sum two numbers.

    Given the first number is 50
    And the second number is 70
    When the two numbers are added
    Then the result should be 120

@commutativity
Scenario: Addition is commutative
    Given the first number is 70
    And the second number is 50
    When the two numbers are added
    Then the result should be 120

Rule: Non-positive numbers can be added

@edgeCase
Scenario Outline: Add zeros
    Given the first number is <first>
    And the second number is <second>
    When the two numbers are added
    Then the result should be <result>
Examples: 
    | first | second | result |
    | 0     | 0      | 0      |
    | 0     | 42     | 42     |
@testOnly
Examples: 
    | first | second | result |
    | 42    | 0      | 42     |

Scenario: Add negatives
    Given the entered numbers are
        | number |
        | -5     |
        | -7     |
    When the two numbers are added
    Then the result should be -12
    And the text message should be
        """
		The result is -12.
		"""

