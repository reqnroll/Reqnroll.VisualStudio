# Assets and Events

## IdeScope (IProjectSystem)

Represents the IDE environment. It remains alive even if solution is cloded or reopened,
(therefore not named SolutionScope), but it clears up pretty much everything on solution cloded. 

### IdeScope Events

* ProjectsBuilt
  * Fired after a full or partial build has been finished
  * Fired on UI thread
* ProjectOutputsUpdated
  * Fired after a full or partial build has been finished and the event subscriptions
    for the ProjectsBuilt event have been executed. 
  * Fired on UI thread

## ProjectScope (IProjectScope)

Represents a phisycal project within a solution (solution folders or temporary solutions do not 
have project scope).

From Deveroom's prespective the project can be of the following kinds.

* Reqnroll Test Project -- a project that is configured to use Reqnroll and contains feature 
  files (ie. there are tests to be run from the project).
* Reqnroll Lib Project -- a project that is configured to use Reqnroll, but does not contain
  feature files, but used to share step definitions or other bindigns for other Reqnroll Test 
  Projects.
* Feature File Container Project -- a project that contains feature files, but it is not 
  configured to use Reqnroll. It might use another BDD tool or Deveroom was not able to detect 
  the Reqnroll version used by the project.
* Other Project -- everything else
* Uninitialized -- For some project types, the installed NuGet packages cannot be queried right 
  after opening but only after the project dependencies are loaded by VS. Until this happens,
  we cannot decide what is the real kind of the project. 

The project kind can be changed over time (e.g. a feature file is added to a Reqnroll Lib Project).

The project kind of the project can be queried from the project settings.

## DeveroomConfiguration

A set configuration settings that are needed for Deveroom for the proper handling of the project
and the feature files. The most important setting is the default project feature language.

The configuration is by default project-scope dependent (different projects can have different 
configuration), but there might be also a solution and a machine level configuration as well (not 
implemented yet).

The configuration currently gathered from the Reqnroll configuration files (`App.config`, 
`reqnroll.json`), but later it will be possible to change/extend this with a Deveroom-specific 
config file as well (`deveroom.json`).

The configuration can be gathered any time as it does not depend on any Visual Studio infrastructure.

The configuration is provided by the `ProjectScopeDeveroomConfigurationProvider` and the 
`ProjectSystemDeveroomConfigurationProvider` classes.

### Detect Configuration Changes

The configuration is checked and loaded

* Initially, when the project scope is created
* After build (IdeScope.ProjectsBuilt)

The gathering (loading) of the configuration must not depend on the project settings to avoid 
circular dependencies and updates.

### Handling Load Errors

* Initially & on change: the config sources that cause issues are ignored. When there are 
  no valid config sources, a default configuration is used

### Configuration Events

* ConfigurationChanged -- fired when the configuration has been changed, but not when it is 
  initially gatehered.
  * Fired on UI thread

## ProjectSettings

A set of settings that represent the project's platform and the installed packages. Only those
settigns are gathered that are relevant for Deveroom.

The project settings currently contain:

* Kind
* TargetFrameworkMoniker
* OutputAssemblyPath
* DefaultNamespace
* ReqnrollPackage (with version and install path)
* ReqnrollConfigFilePath
* ReqnrollProjectTraits
  * MsBuildGeneration
  * XUnitAdapter
  * DesignTimeFeatureFileGeneration

The project settings are managed by the `ProjectSettingsProvider` attached to the project scope.

### Detect Project Settings Changes

The project settings is checked and loaded

* Initially, when the project scope is created (it can happen that the settigns are not 
  available yet at that time).
* A few seconds after the project scope is created (if it was not available). This repeats 
  a few times if necessary.
* After project configuration changed 
  (ProjectScopeDeveroomConfigurationProvider.ConfigurationChanged)
* After build (IdeScope.ProjectsBuilt)
* In case when the first feature file of a project is opened in the editor. In this case 
  the DeveroomTagger manually refreshes the project settings (kind changes to 
  Reqnroll Test Project from Reqnroll Lib Project), so that the users should not need 
  to make a build. (The binding registry is also checked.)

### Handling Load Errors

* Initially: the settings are Uninitialized (the project kind becomes Uninitialized)
* On change: the previous settings remains active

### Project Settings Events

* SettingsInitialized -- fired when the project settings have been loaded successfuly for the 
  first time.
  * Fired on UI thread

## ProjectBindingRegistry

The currently known step definitions of a particular Reqnroll Test Project. For all other project
kinds a "not available" value is returned (DiscoveryStatus.NonReqnrollTestProject).

After successful binding discovery, the binding status is Discovered (DiscoveryStatus.Discovered)

The ProjectBindingRegistry is managed my the `DiscoveryService` attached to the project scope.

### Detect Binding Registry Changes

The binding registry is loaded on a background thread. The changes are checked and the loading 
is triggered 

* When project settings initialized (ProjectSettingsProvider.SettingsInitialized)
* After project outputs updated (built) (IdeScope.ProjectOutputsUpdated)
* In case when the first feature file of a project is opened in the editor. See project settings 
  change detection for details.

It can happen that the two events are triggered by the same build: when the project settings could 
not be initialized normally, after the first build, the project settings will be initialized and 
also the ProjectOutputsUpdated event will be fired. To avoid duplicated binding discovery, the 
DiscoveryService will ignore bindign discovery requests when there is an ongoing discovery process 
active.

### Handling Load Errors

* Initially
  * When the binding registry is requested before the first discovery: the binding registry is 
    uninitialized (DiscoveryStatus.Uninitialized)
  * When project settings uninitialized: the binding registry is uninitialized 
    (DiscoveryStatus.UninitializedProjectSettings)
  * When project is not a Reqnroll Test Project: the binding registry is not available 
    (DiscoveryStatus.NonReqnrollTestProject)
  * When test assembly not found: the binding registry is uninitialized 
    (DiscoveryStatus.TestAssemblyNotFound)
  * When there was an error during binding discovery: the binding registry is invalid 
    (DiscoveryStatus.Error)
* On change: the previous settings remains active

### Binding Registry Events

* BindingRegistryChanged -- fired when there is a new version of binding registry available
  * May be fired from a background thread, but also from the UI thread!
