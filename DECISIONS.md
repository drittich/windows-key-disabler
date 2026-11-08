# Decisions

The choices Sleeper Run 01a1b99d66aa7217af86a766ea070b45's Agents, and its Conductor, made that the spec, the ADRs and the Research
Notes didn't settle, P1 first. Sleeper rewrites this file after each merge.

## D-2: Added a separate WindowsKeyDisabler.Linux.Tests project (xUnit 2.9.3, Microsoft.NET.Test.Sdk, net10.0/linux-x64) referencing the Linux project. Added InternalsVisibleTo to the Linux csproj so tests can reach internal types. Did not include coverlet.

- **Severity:** P2
- **Status:** reviewed
- **Ticket:** scaffolding: Scaffold the Target Repo
- **Role:** architect
- **Context:** There was no test harness, and I had to choose a test framework and a place for the tests.
- **Decision:** Added a separate WindowsKeyDisabler.Linux.Tests project (xUnit 2.9.3, Microsoft.NET.Test.Sdk, net10.0/linux-x64) referencing the Linux project. Added InternalsVisibleTo to the Linux csproj so tests can reach internal types. Did not include coverlet.
- **Alternatives considered:** xunit.v3; MSTest or NUnit; No InternalsVisibleTo (test public types only)
- **Rationale:** This is the default `dotnet new xunit` template, and its packages were already in the local NuGet cache. Only the Linux build has open work. The Windows build can't run on Linux.
- **Reversibility:** Easy: test-only project plus one csproj item.
- **Spec reference:** None.

## D-1: Kept the existing triage labels block and file. Updated docs/agents/issue-tracker.md to match the current template, which uses Status `done` instead of `resolved`, to match Sleeper marking Tickets done.

- **Severity:** P3
- **Status:** reviewed
- **Ticket:** scaffolding: Scaffold the Target Repo
- **Role:** architect
- **Context:** The triage skill isn't installed, so setup-matt-pocock-skills says to leave out the Triage labels block. The repo already has that block and docs/agents/triage-labels.md.
- **Decision:** Kept the existing triage labels block and file. Updated docs/agents/issue-tracker.md to match the current template, which uses Status `done` instead of `resolved`, to match Sleeper marking Tickets done.
- **Alternatives considered:** Remove the triage labels block and file; Leave issue-tracker.md as it was
- **Rationale:** Build on what the repo already has. The Status: line convention is already in use in the Ticket files.
- **Reversibility:** Trivial: doc edits only.
- **Spec reference:** None.

## D-3: The build check builds the whole solution, including the Windows net8.0-windows project, through EnableWindowsTargeting.

- **Severity:** P3
- **Status:** reviewed
- **Ticket:** scaffolding: Scaffold the Target Repo
- **Role:** architect
- **Context:** I had to choose what the build check covers.
- **Decision:** The build check builds the whole solution, including the Windows net8.0-windows project, through EnableWindowsTargeting.
- **Alternatives considered:** Build only the Linux and test projects
- **Rationale:** It builds successfully on this machine, and it catches anyone breaking the Windows build by accident.
- **Reversibility:** Easy: edit .sleeper/checks.json.
- **Spec reference:** None.

## D-4: The notes recommend one idempotent cleanup method on App, called from Exit, the lifetime events and the signal handlers. If the run-once logic grows, it should move into a small internal class that can be tested without Avalonia. No port or interface unless a second adapter exists.

- **Severity:** P3
- **Status:** reviewed
- **Ticket:** scaffolding: Scaffold the Target Repo
- **Role:** architect
- **Context:** The architecture notes needed to say where the graceful-shutdown cleanup logic should go.
- **Decision:** The notes recommend one idempotent cleanup method on App, called from Exit, the lifetime events and the signal handlers. If the run-once logic grows, it should move into a small internal class that can be tested without Avalonia. No port or interface unless a second adapter exists.
- **Alternatives considered:** Prescribe a dedicated ShutdownCoordinator class with an interface up front
- **Rationale:** The Ticket suggests a single shared cleanup method. Per codebase-design, a seam with only one adapter is just indirection.
- **Reversibility:** Easy: guidance only.
- **Spec reference:** .scratch/graceful-shutdown/issues/01-crash-on-session-shutdown.md (Suggested approach)

## D-5: I used a throwaway shell script that greps the doc and comment text against the acceptance criteria as the red/green test. It failed before the edits and passed after. The gates were the build/test checks in .sleeper/checks.json. I didn't commit a doc-content test to the xUnit project.

- **Severity:** P3
- **Status:** reviewed
- **Ticket:** graceful-shutdown/02-use-one-ticket-status-vocabulary-across: 02: Use one Ticket Status vocabulary across the agent docs, and drop the CI mention from the test comment
- **Role:** implementer
- **Context:** The change is docs and a code comment only, with no runtime behaviour. The tdd skill asks for test-first at the nearest public interface.
- **Decision:** I used a throwaway shell script that greps the doc and comment text against the acceptance criteria as the red/green test. It failed before the edits and passed after. The gates were the build/test checks in .sleeper/checks.json. I didn't commit a doc-content test to the xUnit project.
- **Alternatives considered:** Add an xUnit test that reads the markdown files and asserts the Status vocabulary
- **Rationale:** A committed test asserting markdown text would tie the C# test project to doc wording and go beyond the Ticket's scope. The conservative option was to verify the docs without adding a lasting test.
- **Reversibility:** Easy: a doc-content test can be added later.
- **Spec reference:** None.

## D-6: I reworded the pointer to say triage-labels.md covers the triage roles and the lifecycle states `claimed` and `done`.

- **Severity:** P3
- **Status:** reviewed
- **Ticket:** graceful-shutdown/02-use-one-ticket-status-vocabulary-across: 02: Use one Ticket Status vocabulary across the agent docs, and drop the CI mention from the test comment
- **Role:** implementer
- **Context:** issue-tracker.md is in the Ticket's Paths, but no criterion requires changing it. Its Status line pointed to triage-labels.md only 'for the role strings'.
- **Decision:** I reworded the pointer to say triage-labels.md covers the triage roles and the lifecycle states `claimed` and `done`.
- **Alternatives considered:** Leave issue-tracker.md unchanged
- **Rationale:** Without this the docs would still imply the role strings are the only Status values, which is the mismatch the Ticket is about.
- **Reversibility:** Trivial one-line revert.
- **Spec reference:** Paths: docs/agents/issue-tracker.md
