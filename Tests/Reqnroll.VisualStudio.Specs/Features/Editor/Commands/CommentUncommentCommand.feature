Feature: Comment and Uncomment commands

Rules:

* Comments out selected lines
	* Comments out caret line
	* Comments out selection lines with the smallest indent
* Uncomments selected lines
	* Uncomments selection lines
	* Uncomment ignores non-comment lines

Scenario: Comments out caret line
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
			When I press{caret} add
		"""
	When I invoke the "Comment" command
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
			#When I press add
		"""

Scenario: Comments out selection lines with the smallest indent
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		Scenario: Add two numbers
			When I press{sel} add
				And I press multiply
					And I press{/sel} subtract
		"""
	When I invoke the "Comment" command
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
			#When I press{caret} add
			#	And I press multiply
			#		And I press subtract
		"""

Scenario: Uncomments selection lines
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		#Scenario: Add two numbers
		#	When I press{sel} add
		#		And I press {/sel}multiply
		#			And I press subtract
		"""
	When I invoke the "Uncomment" command
	Then the editor should be updated to
		"""
		Feature: Addition
		#Scenario: Add two numbers
			When I press add
				And I press multiply
		#			And I press subtract
		"""

Scenario: Uncomment ignores non-comment lines
	Given there is a Reqnroll project scope
	And the following feature file in the editor
		"""
		Feature: Addition
		#Scenario: Add {sel}two numbers
			When I press add
		#		And I press {/sel}multiply
		#			And I press subtract
		"""
	When I invoke the "Uncomment" command
	Then the editor should be updated to
		"""
		Feature: Addition
		Scenario: Add two numbers
			When I press add
				And I press multiply
		#			And I press subtract
		"""
