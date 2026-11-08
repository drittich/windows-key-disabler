# Windows Key Disabler

Tray utility that temporarily disables the Windows (Super/Meta) key, for full-screen gaming. Two independent C#
builds: `WindowsKeyDisabler/` (Windows, WinForms) and `WindowsKeyDisabler.Linux/` (Linux/Wayland, Avalonia). See
`docs/architecture.md` for the modules, their interfaces and where new code goes.

## Build and test

Requires the .NET 10 SDK (it also builds the `net8.0-windows` project on Linux via `EnableWindowsTargeting`).

```bash
dotnet build WindowsKeyDisabler.sln   # builds all three projects
dotnet test WindowsKeyDisabler.sln    # xUnit tests in WindowsKeyDisabler.Linux.Tests
```

These two commands are the gates in `.sleeper/checks.json`; every change must pass both. Keep the build free of
new warnings.

Running the Linux app needs a desktop session and the udev rule from `WindowsKeyDisabler.Linux/install.sh`; agent
sandboxes have neither, so don't try to launch it or grab keyboards in tests.

## Conventions

- C# with `Nullable` and `ImplicitUsings` enabled; **tabs** for indentation in `.cs` files, two spaces in `.csproj`.
- Block-scoped `namespace WindowsKeyDisabler { ... }` in app code, `WindowsKeyDisabler.Tests` in tests.
- Private fields: existing code mixes `_camelCase` (`_grabber`) and `camelCase` (`trayIcon`); follow the file you're in.
- Comments are short, lowercase, and explain *why* (see `KeyboardGrabber.cs`). Don't narrate obvious code.
- Native interop is private `DllImport("libc", SetLastError = true)` declarations inside the class that uses them;
  report failures through `Check(...)`-style `InvalidOperationException`s with a user-facing message.
- New testable types: `internal`, in the Linux project (tests see internals via `InternalsVisibleTo`).
- Add tests to `WindowsKeyDisabler.Linux.Tests/`, one test class per production type (`<Type>Tests.cs`), xUnit
  `[Fact]`/`[Theory]`, test through the type's interface. Tests must not need devices, a display or a session.
- No new NuGet dependencies without a clear need; keep Avalonia on the version pinned in the csproj unless a Ticket
  says otherwise.
- Don't touch the Windows build unless a Ticket names it.
- Update `README.md` when user-visible behaviour or install steps change.

## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`. Don't edit
Ticket files or their `Status:` lines while implementing; the Sleeper conductor owns them.

### Triage labels

Default five roles (needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix), recorded as a `Status:` line in each issue file. The same line also takes the lifecycle states `claimed` and `done`, which are not triage roles. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
