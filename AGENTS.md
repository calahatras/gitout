# Gitout

Gitout is a graphical tool to use git which is a version control system.
It features varios views from which a user can run git commands from the UI

- Workspaces - a list of git folders.
- Log - a version tree with a list of git commits each displaying a subject line, date, and author. A user can select commits and compare them, showing file versions and changes.
- Stage - show a list of changed files in the current workspace or index.
- Settings - Configuration for the application

## Folder structure

GitOut - Main WPF application project. Contains application entry points (`App.xaml` / `App.xaml.cs`), the main project file (`GitOut.csproj`), icons and compiled outputs in `bin/` and intermediate build files in `obj/`.

GitOut\Features - Root folder for feature-based organization. Each subfolder groups code for a single area of responsibility or feature set used by the application.

Feature folders (under `GitOut\\Features`):

- Collections - Lightweight, specialized collection types and helpers used across the app (observable lists, indexed collections, adapters).
- Diagnostics - Routines and helpers for running and reporting diagnostic tasks (process execution, health checks, logging of diagnostics, and troubleshooting helpers for git operations).
- Git - Core abstractions and models of Git concepts (repositories, commits, branches, diffs, plumbing wrappers and high-level operations used by the UI).
- Input - Input handling and listeners (keyboard, mouse, drag-and-drop, clipboard interactions and other UI input utilities).
- IO - File and stream helpers, safe file access, path utilities, and small wrappers around file-system operations used by the app.
- Logging - Application logging abstractions and configuration (log sinks, formatters, and integration with any logging framework used by the project).
- Material - UI resources and asset helpers for material-like styles and controls used by the application (resource dictionaries, control templates and shared styles).
- Memory - In-memory caches, memory management helpers and lightweight data stores used for temporary application state.
- Native - P/Invoke bindings and small native interop helpers (if the app needs OS-level hooks, native dialogs, or platform-specific features).
- Navigation - Navigation services and view management (view models, navigation stacks, and helpers for switching views within the WPF shell).
- Options - Option models and helpers for user-editable options; preference storage and validation helpers.
- Settings - App settings wrappers, persistence adapters (load/save settings), and strongly-typed settings models.
- Storage - Persistence-related helpers (local storage adapters, simple data stores, serialization helpers for saving user/state data).
- Text - Text utilities, formatting helpers, syntax helpers, and any text-processing utilities used by the UI.

Other project folders and files:

- GitOut\\Themes - Resource dictionaries and theme assets (colors, brushes, control styles) used to skin the application.
- GitOut\\Wpf - WPF-specific controls, custom user controls and control libraries used by the main app UI.
- GitOutTest - Unit and integration tests for the `GitOut` project. Contains test project files and test assets.
- GitProperties - A small source generator / tool that emits the repository information (for example the current git commit hash) into generated code so the application can display version info at runtime.

## 3rd party dependencies

`Microsoft.Extensions.Hosting` is used to set up dependency injection.
`FakeItEasy` is used to mock classes in test files with `nunit` used as the test runner.

## Testing (notes)

Use the `dotnet test` tool to run all tests.

## C# Development

### C# Instructions

- Always use the latest version C#.

### General Instructions

- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- For libraries or external dependencies, mention their usage and purpose in comments.

### Naming Conventions

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).

### Formatting

- Apply code-formatting style defined in `.editorconfig` and/or csharpier.
- Prefer file-scoped namespace declarations and single-line using directives.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.

### Nullable Reference Types

- Declare variables non-nullable, and check for `null` at entry points.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Validation and Error Handling

- Guide the implementation of model validation using data annotations.
- Explain the validation pipeline and how to customize validation responses.
- Demonstrate a global exception handling strategy using middleware.
- Show how to create consistent error responses across the API.
- Explain problem details (RFC 7807) implementation for standardized error responses.

### API Versioning and Documentation

- Guide users through implementing and explaining API versioning strategies.
- Demonstrate Swagger/OpenAPI implementation with proper documentation.
- Show how to document endpoints, parameters, responses, and authentication.
- Explain versioning in both controller-based and Minimal APIs.
- Guide users on creating meaningful API documentation that helps consumers.

### Logging and Monitoring

- Guide the implementation of structured logging using Microsoft logging or other providers.
- Explain the logging levels and when to use each.
- Demonstrate integration with Application Insights for telemetry collection.
- Show how to implement custom telemetry and correlation IDs for request tracking.
- Explain how to monitor API performance, errors, and usage patterns.

### Testing

- Always include test cases for critical paths of the application.
- Guide users through creating unit tests.
- Do not emit "Act", "Arrange" or "Assert" comments.
- Copy existing style in nearby files for test method names and capitalization.
- Explain integration testing approaches for API endpoints.
- Demonstrate how to mock dependencies for effective testing.
- Explain test-driven development principles as applied to API development.

### Performance Optimization

- Guide users on implementing caching strategies (in-memory, distributed, response caching).
- Explain asynchronous programming patterns and why they matter for API performance.
- Demonstrate pagination, filtering, and sorting for large data sets.
- Show how to implement compression and other performance optimizations.
- Explain how to measure and benchmark API performance.

### Deployment and DevOps

- Guide users through containerizing their API using .NET's built-in container support (`dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer`).
- Explain the differences between manual Dockerfile creation and .NET's container publishing features.
- Explain CI/CD pipelines for NET applications.
- Show how to implement health checks and readiness probes.
