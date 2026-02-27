## Addendum #2 — VS Extension Best Practices Review

A review of the plan against Visual Studio extension capabilities, conventions, and best practices. This addendum documents gaps, anti-patterns, and recommendations identified during design review.

### Finding 1: Plan reinvents VS Brokered Services (ServiceHub) patterns

The plan's custom control plane + named pipe negotiation + registration handshake replicates what [VS Brokered Services](https://learn.microsoft.com/visualstudio/extensibility/internals/brokered-service-essentials) (`IServiceBroker`, `ServiceHub`) already provide:

- `IServiceBroker` handles service discovery, proxy creation, and lifetime management
- `ServiceJsonRpcDescriptor` uses StreamJsonRpc with proper serialization, versioning via `ServiceMoniker`, and formatter choice (JSON vs MessagePack)
- VS manages auxiliary process lifecycle (launch, crash recovery, shutdown)
- The service interface is declared in a `netstandard2.0` assembly — exactly what the Protocol project does

**However**, there is a legitimate reason the plan cannot use ServiceHub directly: the connector must run under a **project-specific .NET runtime** (`dotnet exec` with a TFM-matched DLL like `Reqnroll-Generic-net8.0\reqnroll-vs.dll`). ServiceHub hosts run on the VS runtime, not on an arbitrary user-project runtime. The custom `dotnet exec` launch is therefore unavoidable.

**Resolution:** Acknowledged as a justified tradeoff. The plan adopts ServiceHub's `ServiceMoniker`-style versioning pattern for `ServiceRegistration` (see updated Section 3.1) rather than using an ad-hoc `ApiVersion` string.

### Finding 2: Sync-over-async deadlock risk in `ConnectorServiceManager`

The plan's `ConnectorServiceManager.RunDiscovery()` uses `.GetAwaiter().GetResult()`:

```csharp
return _serviceRpc
    .InvokeAsync<DiscoveryResult>(...)
    .GetAwaiter().GetResult();
```

In VS extensions, this can deadlock if called on the UI thread. The current call chain runs via `FireAndForgetOnBackgroundThread` (`TaskCreationOptions.LongRunning`), but `IDiscoveryResultProvider.RunDiscovery()` is a synchronous interface — nothing prevents a future caller from invoking it on the main thread.

**Resolution:** The `ConnectorServiceManager` code (Section 3.3.2) has been updated to:
1. Add `ThreadHelper.ThrowIfOnUIThread()` as a guard
2. Use `InvokeWithCancellationAsync` with the per-request timeout (Layer 3 resilience)

Long-term, `IDiscoveryResultProvider` should be made async (`Task<DiscoveryResult> RunDiscoveryAsync(...)`), but that is a broader refactor outside the scope of this plan.

### Finding 3: No integration with VS shutdown cancellation lifecycle

`VsIdeScope` manages a `_backgroundTaskTokenSource` that is cancelled on solution close / VS shutdown. The plan's `ConnectorServiceManager` and `ControlPlaneServer` create independent `CancellationTokenSource` instances with no connection to this existing lifecycle.

**Resolution:** The `ControlPlaneServer` code (Section 3.3.1) has been updated to accept an external `CancellationToken`. `ConnectorServiceManager` links its internal cancellation to the token provided by the existing `IIdeScope.FireAndForgetOnBackgroundThread` infrastructure. Phase 1 tasks (Section 9) now include explicit VS lifecycle integration.

### Finding 4: Plan does not specify wiring to existing VS solution/project events

The extension already has a well-defined event infrastructure in `VsIdeScope`:

```csharp
// VsIdeScope.cs
_updateSolutionEventsListener.BuildCompleted += ...;
_solutionEventListener.Closed += ...;
_solutionEventListener.BeforeCloseProject += ...;
```

And in `DiscoveryService`:
```csharp
_projectSettingsProvider.WeakSettingsInitialized += ProjectSystemOnProjectsBuilt;
_projectScope.IdeScope.WeakProjectOutputsUpdated += ProjectSystemOnProjectsBuilt;
```

The plan's Section 5 said "Project built / config changed → Send reload" without specifying which events.

**Resolution:** Section 5 lifecycle events table has been updated to reference the specific VS events that trigger each action. `ConnectorServiceManager` subscribes to `WeakProjectOutputsUpdated` for reload and solution close events for shutdown.

### Finding 5: `ControlPlaneServer` async loop not tracked by `JoinableTaskFactory`

In VS extensions, long-running async work should be tracked by `JoinableTaskFactory` to prevent shutdown hangs. Untracked tasks can keep VS alive after the user tries to close it.

**Resolution:** The plan now specifies that the control plane is launched via `IIdeScope.FireAndForgetOnBackgroundThread()` which provides both token-based cancellation and exception handling. See updated Phase 1 tasks in Section 9.

### Finding 6: Service-side errors should surface in the VS Error List

The extension already has `IDeveroomErrorListServices` with category-based error management. Service-side errors (failed reload, ALC crash, etc.) should be reported through this existing infrastructure, not just logged.

**Resolution:** Phase 1 tasks updated to include error list integration. Service-side errors returned in `DiscoveryResult.ErrorMessage` already flow through the existing `DiscoveryInvoker` error handling, which reports to the Error List. Additional service lifecycle errors (crash, timeout) should be reported via `IDeveroomErrorListServices` in `ConnectorServiceManager`.

### Finding 7: Log transport should use existing `IDeveroomLogger` pipeline

The extension has `VsDeveroomOutputPaneServices` integrated with `IDeveroomLogger`. Rather than designing a new log transport, service-side log messages should be forwarded via the existing RPC connection (as part of `DiscoveryResult.LogMessages` or a new RPC notification) and piped into the existing `IDeveroomLogger`.

**Resolution:** Phase 2 logging task updated to specify integration with existing `IDeveroomLogger` infrastructure rather than a separate log channel.

### Finding 8: Addendum #1 hosting framework tradeoffs (corrected)

~~Original concern: assembly version conflicts between the host's `M.E.Hosting`/DI/Logging and the test project's copies.~~

**Corrected assessment:** Because the `TestAssemblyLoadContext` resolves dependencies from the test project's `.deps.json` and NuGet cache *before* falling back to the Default ALC, and because no `Microsoft.Extensions.*` types cross the ALC boundary (discovery results are simple POCOs), version conflicts are not a realistic risk. The ALC provides full isolation.

The actual concerns are deployment size (~15-20 additional DLLs), startup latency (`Host.CreateDefaultBuilder()` initialization), and `.deps.json` probe-path complexity. See corrected caution in Addendum #1.

### Summary of findings

| # | Issue | Severity | Effort | Status |
|---|---|---|---|---|
| 1 | Reinvents ServiceHub patterns | Medium | Medium | Justified; adopted `ServiceMoniker` versioning |
| 2 | Sync-over-async deadlock risk | High | Low | Fixed in Section 3.3.2 |
| 3 | No VS shutdown cancellation integration | High | Low | Fixed in Sections 3.3.1, 3.3.2, 9 |
| 4 | No specific VS event wiring | Medium | Low | Fixed in Section 5 |
| 5 | Untracked async loop | Medium | Low | Fixed in Section 9 |
| 6 | Error List not used for service errors | Medium | Low | Added to Phase 1 |
| 7 | Log transport design | Low | Low | Updated Phase 2 |
| 8 | Hosting framework tradeoffs (deployment size, startup latency) | Low | Low | Caution corrected in Addendum #1; ALC isolates version conflicts |


