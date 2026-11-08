# 02: Use one Ticket Status vocabulary across the agent docs, and drop the CI mention from the test comment

**What to build:** The scaffolding left the Ticket `Status:` line with two vocabularies. docs/agents/triage-labels.md and CLAUDE.md ('Default five roles ... recorded as a `Status:` line') list only needs-triage, needs-info, ready-for-agent, ready-for-human and wontfix. docs/agents/issue-tracker.md (changed under D-1) also uses `claimed` and `done` as Status values, and Sleeper marks Tickets `done`. Make the docs agree: add the lifecycle states `claimed` and `done` to docs/agents/triage-labels.md as Status values separate from the five triage roles, and mention them in the CLAUDE.md 'Triage labels' paragraph. Also, WindowsKeyDisabler.Linux.Tests/KeyboardGrabberTests.cs says the tests 'run in CI', but the repo has no CI; the gates are .sleeper/checks.json. Reword that comment to match docs/architecture.md's Testing section (sandboxes with no /dev/input, /dev/uinput, display or session). Don't edit any Ticket file under .scratch/.

**Blocked by:** None (can start immediately)

**Status:** done

**Severity:** minor

**Paths:** `docs/agents/triage-labels.md`, `docs/agents/issue-tracker.md`, `CLAUDE.md`, `WindowsKeyDisabler.Linux.Tests/KeyboardGrabberTests.cs`

- [ ] docs/agents/triage-labels.md lists every Status value used in docs/agents/issue-tracker.md (`claimed` and `done` as well as the five triage roles) and says which are triage roles and which are lifecycle states.
- [ ] The CLAUDE.md 'Triage labels' paragraph agrees with docs/agents/triage-labels.md and no longer implies that the five roles are the only Status values.
- [ ] The comment in WindowsKeyDisabler.Linux.Tests/KeyboardGrabberTests.cs no longer mentions CI and still says the tests must not need /dev/input or /dev/uinput.
- [ ] No files under .scratch/ are changed.
- [ ] `dotnet build WindowsKeyDisabler.sln` and `dotnet test WindowsKeyDisabler.sln` still pass with no new warnings.
