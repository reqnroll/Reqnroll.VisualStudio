Feature: Binding Discovery

Scenario: Discover bindings from a simple latest Reqnroll project
	Given there is a simple Reqnroll project for the latest version
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions

Scenario: Discover bindings from Reqnroll project with plugin
	Given there is a simple Reqnroll project with plugin for the latest version
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And there is a "Then" step with regex "there should be a step from a plugin"

Scenario: Discover bindings from Reqnroll project with external bindings
	Given there is a simple Reqnroll project with external bindings for the latest version
	And the project is built
	When the binding discovery performed
	Then the discovery succeeds with several step definitions
	And there is a "Then" step with regex "there should be a step from an external assembly"
