Feature: Calculator

Simple calculator for adding two numbers

Scenario: Add two numbers
	Given the first number is 50
	And the second number is 70
	When the two numbers are added
	Then the result should be 120
	#When Datetime is requested
	#Then It is the same