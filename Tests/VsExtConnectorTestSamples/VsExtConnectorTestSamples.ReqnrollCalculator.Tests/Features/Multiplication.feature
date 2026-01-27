Feature: Multiplication

The calculator functions related to multiplication

@core
Rule: Numbers can be multiplied

Scenario: Add two numbers
    Given the first number is 5
    And the second number is 7
    When the two numbers are multiplied
    Then the result should be 35

Rule: Can multiply with zero

Scenario: Multily with zero (wrong)
    Given the first number is 5
    And the second number is 0
    When the two numbers are multiplied
    Then the result should be 1
