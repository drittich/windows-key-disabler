# Architecture

Windows Key Disabler is a tray app that turns the Windows (Super/Meta) key off and on. There are two separate
builds that share nothing but the icons and the idea:

| Project                          | Platform              | UI                  | How the key is blocked                         |
| -------------------------------- | --------------------- | ------------------- | ---------------------------------------------- |
| `WindowsKeyDisabler/`            | Windows, `net10.0-windows` | WinForms `NotifyIcon` | `WH_KEYBOARD_LL` low-level hook (`KeyboardHook`) |
| `WindowsKeyDisabler.Linux/`      | Linux, `net10.0` / `linux-x64` | Avalonia 11.3 `TrayIcon` | `EVIOCGRAB` + uinput mirror (`KeyboardGrabber`) |
| `WindowsKeyDisabler.Linux.Tests/`| Linux, `net10.0`      | —                   | xUnit tests for the Linux build                |

Open work (see `.scratch/`) is on the Linux build. Leave the Windows build alone unless a Ticket names it.

## Linux build modules

### `Program` (`WindowsKeyDisabler.Linux/Program.cs`)

Entry point. Configures Avalonia (`AppBuilder.Configure<App>().UsePlatformDetect()`) and starts the classic desktop
lifetime with `ShutdownMode.OnExplicitShutdown`, since there is no window. Process-wide setup that must happen
**before Avalonia starts** belongs here: platform options (e.g. `X11PlatformOptions`), environment tweaks, and
POSIX signal registration.

- X session management is disabled (`X11PlatformOptions.EnableSessionManagement = false`). Avalonia's libSM
  teardown raced process exit at logout and segfaulted in `SmcCloseConnection`.
- `SIGTERM`/`SIGINT`/`SIGHUP` are handled by cancelling the default termination and posting `App.Quit()` to the
  UI thread. A repeated signal, or one before Avalonia is up, falls through to the default termination.

### `App` (`WindowsKeyDisabler.Linux/Program.cs`)

The tray UI and the enabled/disabled state.

- Interface (to Avalonia): `OnFrameworkInitializationCompleted()` builds the tray icon and menu
  (Enable / Disable / Exit) and starts with the key enabled.
- `EnableWindowsKey(bool)` is the single place that changes state: it calls `KeyboardGrabber.Grab()`/`Release()`,
  falls back to enabled and shows a `notify-send` error if the grab fails, then syncs the icon and the menu checks.
- `Quit()` (from **Exit** and the signal handlers) releases the grab, disposes the tray icon and shuts down the
  lifetime. The lifetime's `Exit` event also runs the release, so every way out gives the keyboard back. Both are
  guarded by `ShutdownGate` (`ShutdownGate.cs`), a thread-safe run-once flag.

### `KeyboardGrabber` (`WindowsKeyDisabler.Linux/KeyboardGrabber.cs`)

A deep module: three members in front of all the evdev/uinput work.

- `Grab()`: opens every physical keyboard under `/dev/input`, waits (≤2s) for held keys to be released, creates a
  uinput mirror per keyboard named `<name> (Windows Key Disabler)`, takes `EVIOCGRAB` on each, then starts the
  background **Keyboard forwarder** thread. Throws `InvalidOperationException` with a user-facing message on
  failure (permissions, no keyboards). No-op if already grabbed.
- `Release()`: signals the forwarder through an eventfd, joins it, ungrabs and destroys every device. Safe to call
  repeatedly and when nothing is grabbed. Must not be called from the forwarder thread (it joins it).
- `IsGrabbed`: true while the forwarder runs.

Internals: `ForwardEvents` polls the stop eventfd plus a (physical, virtual) fd pair per device, drops
`KEY_LEFTMETA`/`KEY_RIGHTMETA`, writes the rest to the mirror, and copies `EV_LED` back from the mirror to the
physical keyboard. If either fd of a pair reports `POLLERR/POLLHUP/POLLNVAL` (`IsDeviceGone`: unplugged keyboard,
or access revoked at logout), the whole device is closed so the loop can't spin. All native
calls are private `DllImport("libc")` declarations at the bottom of the file.

### Install assets

`install.sh` (publish + desktop entry + udev rule), `70-windows-key-disabler.rules` (grants `uaccess` on keyboards
and `/dev/uinput`), `windows-key-disabler.desktop` (template, `@INSTALL_DIR@` substituted by `install.sh`).

## Where new code belongs

- **Session-manager / X11 platform options, signal registration** → `Program.Main`, before `StartWithClassicDesktopLifetime`.
- **One idempotent cleanup path** (release grab, dispose tray icon, shut down the lifetime) → a single method on
  `App`, called from Exit, from the lifetime's `ShutdownRequested`/`Exit` events and from signal handlers
  (marshalled to the UI thread with `Dispatcher.UIThread.Post`). If the "run once, from any thread" logic grows
  beyond a few lines, extract it into its own small `internal` class in the Linux project so it can be unit-tested
  without Avalonia; don't add an interface/port for it unless a second adapter actually exists.
- **Device-level robustness** (e.g. how the forwarder handles a revoked or errored virtual fd) → inside
  `KeyboardGrabber`. Keep its interface at `Grab`/`Release`/`IsGrabbed`; add to it only if a caller truly needs it.
- **New files** go in `WindowsKeyDisabler.Linux/`, namespace `WindowsKeyDisabler`, one top-level type per file.

## Testing

`WindowsKeyDisabler.Linux.Tests` references the Linux project; `InternalsVisibleTo` exposes `internal` types to it,
so prefer `internal` over `public` for new testable classes.

Tests run in sandboxes with **no access to `/dev/input`, `/dev/uinput`, a display, or a session bus**. So:

- Unit-test logic that doesn't touch devices or Avalonia (state, cleanup ordering, idempotence, event filtering if
  it's pulled out into a pure function).
- Don't call `KeyboardGrabber.Grab()` or start Avalonia in tests.
- Behaviour that needs real hardware or a Plasma session (logout, reboot, `coredumpctl`) is verified manually;
  note the manual steps in your Outcome rather than writing a test that can't run here.
