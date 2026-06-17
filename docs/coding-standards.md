# Coding Standards

This document defines the coding and architecture standards for MyCancerTeam. The overarching principle is **KISS — Keep It Simple, Stupid.** Every rule below serves that goal.

---

## 1. Code formatting — Microsoft .NET conventions

We follow the [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and [framework design guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/).

### Naming

| Element | Style | Example |
|---|---|---|
| Namespace, class, struct, enum, method, property, event | PascalCase | `PatientProfile`, `GetNotes()` |
| Interface | `I` + PascalCase | `INoteStore` |
| Local variable, parameter | camelCase | `noteCount`, `patientId` |
| Private field | `_camelCase` | `_noteStore` |
| Constant | PascalCase | `MaxRetryCount` |
| Async method | Suffix `Async` | `LoadNotesAsync()` |

### Layout

- One class per file. File name matches the class name.
- Use file-scoped namespaces (`namespace X;`).
- `using` directives at the top, sorted, with `System` namespaces first.
- Braces on their own line (Allman style).
- Four-space indentation, no tabs.
- Keep methods short. If a method doesn't fit on one screen (~30 lines), consider splitting it.

### Language usage

- Use `var` when the type is obvious from the right-hand side.
- Prefer pattern matching and switch expressions where they simplify logic.
- Use `string.IsNullOrWhiteSpace()` over manual null/empty checks.
- Prefer collection expressions (`[1, 2, 3]`) when targeting .NET 8+.
- Avoid `#region` blocks — they hide complexity rather than removing it.

---

## 2. Clean Code principles

- **Meaningful names.** Classes, methods, and variables should reveal intent. No abbreviations unless universally understood (e.g., `Id`, `Url`).
- **Small functions.** Each method does one thing. If you need a comment to explain a block, extract it to a well-named method instead.
- **No magic values.** Use named constants or enums.
- **Fail fast.** Validate inputs at the boundary (public methods, API entry points) with guard clauses. Throw early, catch late.
- **Don't repeat yourself (DRY) — but don't over-abstract.** Extract duplication only when the duplicated pieces genuinely represent the same concept. Two things that look alike today may diverge tomorrow.
- **Minimise dependencies.** Every `using` and every NuGet package is a maintenance cost. Add one only when it earns its keep.
- **Boy Scout Rule.** Leave the code cleaner than you found it — but within the scope of your change. Don't refactor the world in a bug-fix PR.

---

## 3. Layered architecture

### Dependency direction

```
App → Infrastructure → Core
```

Nothing flows the other way. Core is the innermost layer and has **zero** external dependencies.

### Layer responsibilities

| Layer | Contains | Does NOT contain |
|---|---|---|
| **Core** | Domain models, enums, interfaces (`INoteStore`, `IDraftWriter`, etc.), workflow abstractions, agent definitions | HTTP clients, file I/O, Azure SDKs, DI registration |
| **Infrastructure** | Implementations of Core interfaces, Azure clients, file readers, configuration loading, external API calls | Business rules, workflow orchestration |
| **App** | `Program.cs`, DI wiring, `appsettings` loading, interactive host | Business logic, direct Azure SDK usage outside DI setup |

### Practical rules

- **Define interfaces in Core, implement in Infrastructure.** This keeps Core testable without real services.
- **Register dependencies in App.** Infrastructure may provide extension methods like `AddInfrastructure(IServiceCollection)` for convenience, but the call site is in App.
- **No cross-cutting shortcuts.** Don't reference `Infrastructure` from `Core` to "save time". If you need something in Core, define an interface there and implement it in Infrastructure.

---

## 4. Twelve-Factor App principles

We follow the [Twelve-Factor App](https://12factor.net/) methodology where applicable to a desktop/console host:

| Factor | How we apply it |
|---|---|
| **I. Codebase** | One repo, tracked in git. |
| **II. Dependencies** | Explicitly declared in `.csproj` files. No GAC or machine-global assumptions. `dotnet restore` is all you need. |
| **III. Config** | All environment-specific values come from environment variables (`MYCANCERTEAM_*`) or `.env` files — never hard-coded. See `config/environments/`. |
| **IV. Backing services** | Azure OpenAI, Bing Search, SerpAPI are treated as attached resources configured via URLs/keys in env vars. Swapping a provider means changing config, not code. |
| **V. Build, release, run** | `dotnet build` produces the artifact. Environment config is applied at run-time, not baked in. |
| **VI. Processes** | The app runs as a single stateless process. Persistent state lives in local files, not in-memory singletons that assume process lifetime. |
| **VII. Port binding** | Not directly applicable (console app), but any future HTTP surface should self-host. |
| **VIII. Concurrency** | Scale by running separate instances or background workers, not by threading shared mutable state. |
| **IX. Disposability** | Fast startup, graceful shutdown. Use `CancellationToken` throughout async work. |
| **X. Dev/prod parity** | `config/environments/` keeps dev, test, and prod configs structurally identical, differing only in values. |
| **XI. Logs** | Log to stdout/stderr. The host environment decides where logs go. Use `ILogger<T>`, not `Console.WriteLine` for diagnostics. |
| **XII. Admin processes** | One-off tasks (migrations, data fixes) are run as separate commands or scripts, not embedded in the main app path. |

---

## 5. Testing

- **Test new behaviour.** Every new public method or workflow should have at least one happy-path and one failure-path test.
- **Keep tests fast.** Mock external dependencies (Azure, file I/O) using the interfaces defined in Core.
- **Name tests clearly.** Use the pattern `MethodName_Scenario_ExpectedResult` or a descriptive sentence.
- **No test interdependence.** Each test sets up its own state and tears it down.
- **Run the full suite before pushing:** `dotnet test MyCancerTeam.slnx`

---

## 6. KISS reminders

These are the most common KISS violations we want to avoid:

- **Don't add abstractions "for future flexibility" that no current feature needs.** YAGNI (You Aren't Gonna Need It).
- **Don't introduce a design pattern just because it exists.** Use a pattern when it solves a concrete problem you have today.
- **Prefer straightforward imperative code over clever one-liners.** Readability beats conciseness.
- **Avoid deep inheritance hierarchies.** Prefer composition and small interfaces.
- **When in doubt, write less code.** The best code is the code you didn't have to write.
