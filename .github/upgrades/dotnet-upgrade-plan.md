# .NET 10 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade CompilationLib\CompilationLib.csproj
4. Upgrade AspireApp1.ServiceDefaults\AspireApp1.ServiceDefaults.csproj
5. Upgrade AspireApp1.ApiService\AspireApp1.ApiService.csproj
6. Upgrade AspireApp1.Web\AspireApp1.Web.csproj
7. Upgrade AspireApp1.AppHost\AspireApp1.AppHost.csproj
8. Upgrade GuiGenericBuilderDesktop\GuiGenericBuilderDesktop.csproj
9. Upgrade ConsoleApp1\ConsoleApp1.csproj
10. Upgrade AspireApp1.Tests\AspireApp1.Tests.csproj


## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|



### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                 | Current Version | New Version | Description                                                                 |
|:---------------------------------------------|:---------------:|:-----------:|:----------------------------------------------------------------------------|
| Aspire.Hosting.AppHost                        |     9.0.0       |  13.0.1     | Deprecated; upgrade to 13.0.1 recommended for compatibility with .NET 10     |
| Aspire.Hosting.Testing                        |     9.0.0       |  13.0.1     | Deprecated; upgrade to 13.0.1 for test framework compatibility              |
| Microsoft.Extensions.Http.Resilience          |     9.0.0       |  10.0.0     | Update to package version targeting .NET 10                                |
| Microsoft.Extensions.ServiceDiscovery         |     9.0.0       |  10.0.0     | Deprecated in v9; replace with supported 10.0.0 version                     |
| OpenTelemetry.Instrumentation.AspNetCore      |     1.9.0       |  1.14.0     | Update OpenTelemetry instrumentation to supported 1.14.0                    |
| OpenTelemetry.Instrumentation.Http            |     1.9.0       |  1.14.0     | Update OpenTelemetry HTTP instrumentation to supported 1.14.0              |


### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### CompilationLib modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - None detected for this project.

Feature upgrades:
  - None.

Other changes:
  - None.


#### AspireApp1.ServiceDefaults modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - `Microsoft.Extensions.Http.Resilience` update from `9.0.0` to `10.0.0` (*recommended replacement for .NET 10*)
  - `Microsoft.Extensions.ServiceDiscovery` update from `9.0.0` to `10.0.0` (*deprecated v9, upgrade to v10*)
  - `OpenTelemetry.Instrumentation.AspNetCore` update from `1.9.0` to `1.14.0` (*recommended for .NET 10*)
  - `OpenTelemetry.Instrumentation.Http` update from `1.9.0` to `1.14.0` (*recommended for .NET 10*)

Feature upgrades:
  - Verify OpenTelemetry configuration compatibility with 1.14.0.

Other changes:
  - None.


#### AspireApp1.ApiService modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - None reported by analysis.

Feature upgrades:
  - None.

Other changes:
  - None.


#### AspireApp1.Web modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - None reported by analysis.

Feature upgrades:
  - None.

Other changes:
  - None.


#### AspireApp1.AppHost modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - `Aspire.Hosting.AppHost` update from `9.0.0` to `13.0.1` (*deprecated v9 - upgrade required*)

Feature upgrades:
  - Verify host APIs compatibility with Aspire.Hosting.AppHost 13.0.1.

Other changes:
  - None.


#### GuiGenericBuilderDesktop modifications

Project properties changes:
  - Target framework should be changed from `net8.0-windows` to `net10.0-windows`

NuGet packages changes:
  - None reported by analysis.

Feature upgrades:
  - For Blazor projects (if any inside solution) ensure any Blazor-specific package or code compatibility is verified. (Workspace contains a Blazor project; prioritize verifying Blazor components and project templates.)

Other changes:
  - None.


#### ConsoleApp1 modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - None reported by analysis.

Feature upgrades:
  - None.

Other changes:
  - None.


#### AspireApp1.Tests modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - `Aspire.Hosting.Testing` update from `9.0.0` to `13.0.1` (*deprecated v9 - upgrade required for test runtime*)

Feature upgrades:
  - Verify test hosting APIs for changes in Aspire.Hosting.Testing 13.0.1.

Other changes:
  - None.


