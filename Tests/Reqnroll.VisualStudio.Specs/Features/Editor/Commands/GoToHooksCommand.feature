Feature: Go to hooks command

Rule: Jumps to hooks related to the scenario

Scenario: Lists hooks executed for the scenario
	e.g. multiple [BeforeScenario], or [BeforeScenario] and [AfterScenario]
	Given there is a Reqnroll project scope
	And the following hooks in the project:
		| type                | method          | tag scope        | hook order |
		| BeforeScenario      | ResetDatabase   |                  | 1          |
		| BeforeScenario      | LoginUser       | @login           | 2          |
		| BeforeScenario      | NotInScope      | @otherTag        |            |
		| AfterScenario       | SaveLogs        |                  |            |
		| BeforeTestRun       | StartBrowser    |                  |            |
		| BeforeFeature       | ClearCache      |                  | 20         |
		| BeforeFeature       | ClearBasicCache | @basic           | 10         |
		| BeforeFeature       | ClearOtherCache | @otherFeatureTag |            |
		| AfterTestRun        | StopBrowser     |                  |            |
		| BeforeScenarioBlock | PrepareData     |                  |            |
		| AfterStep           | LogStep         |                  |            |
	When the following feature file is opened in the editor
		"""
		@basic
		Feature: Addition

		@login
		Scenario: Add two numbers
			When I {caret}press add
		"""
	And the project is built and the initial binding discovery is performed
	When I invoke the "Go To Hooks" command
	Then a jump list "Go to hooks" is opened with the following items
		| hook            | hook type           | hook scope |
		| StartBrowser    | BeforeTestRun       |            |
		| ClearBasicCache | BeforeFeature       | @basic     |
		| ClearCache      | BeforeFeature       |            |
		| ResetDatabase   | BeforeScenario      |            |
		| LoginUser       | BeforeScenario      | @login     |
		| PrepareData     | BeforeScenarioBlock |            |
		| LogStep         | AfterStep           |            |       
		| SaveLogs        | AfterScenario       |            |
		| StopBrowser     | AfterTestRun        |            |
	And invoking the first item from the jump list navigates to the "StartBrowser" hook
