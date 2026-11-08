# Linux build crashes (SIGSEGV) when Fedora shuts down or reboots

Status: needs-triage
Type: bug

## Problem

When you shut down, reboot or log out of a Fedora KDE Plasma session, the Linux build (`WindowsKeyDisabler.Linux`) crashes with SIGSEGV instead of exiting cleanly. KDE's crash handler (drkonqi) and ABRT record a core dump each time.

## Evidence

The system has three core dumps, and all three have the same signature (`coredumpctl list | grep windows-key-disabler`):

| When                  | PID     | Crashing frame                       |
| --------------------- | ------- | ------------------------------------ |
| 2026-11-07 08:57      | 1304110 | `SmcCloseConnection (libSM.so.6)`    |
| 2026-11-07 09:08      | 3457    | `SmcCloseConnection (libSM.so.6)`    |
| 2026-11-08 00:14      | 16578   | `SmcCloseConnection (libSM.so.6)`    |

The most recent crash (PID 16578), in timeline order:

- `00:14:35` Plasma starts `org.kde.Shutdown`, so the session is ending.
- The crash happens on a `.NET TP Worker` thread:
  `general protection fault ... in libSM.so.6`, frame `#0 SmcCloseConnection`.
- At the same moment, the main thread is in process teardown: `_int_free_*` ← `__glXAPITeardown (libGLX.so.0)`.

## Likely cause

Avalonia runs on X11 here (through XWayland) and registers with the X session manager through libSM. The session manager tells the client the session is ending (a "die" or shutdown request). Two things then happen at the same time:

- A thread-pool worker closes the session-manager connection (`SmcCloseConnection`).
- The main thread has already started process exit and is tearing down native libraries such as GLX.

The worker then touches freed memory and segfaults. The cause sits in Avalonia's X11 session-management integration, not in our keyboard code. Our code makes it worse because nothing in it handles a session ending:

- `App` in `WindowsKeyDisabler.Linux/Program.cs` releases the grab and disposes the tray icon only from the tray's **Exit** menu item.
- No code handles `ShutdownRequested`, `SIGTERM` or session end, so `KeyboardGrabber` (physical keyboard `EVIOCGRAB` grabs plus uinput mirror devices) is never released on that path.

The Avalonia version in use is 11.3.20 (`WindowsKeyDisabler.Linux.csproj`).

## Desired behaviour

When the session ends (shutdown, reboot, logout, or `systemctl --user stop` of the autostart unit), the app:

1. Releases the keyboard grab and destroys its virtual devices (`KeyboardGrabber.Release()`).
2. Disposes the tray icon.
3. Exits with code 0, leaving no core dump, no drkonqi crash notification and no ABRT entry.
4. Does not delay the shutdown noticeably (well under systemd's default 90s stop timeout; target < 1s).

## Suggested approach (verify before relying on it)

- **Remove the crash path:** opt out of X11 session management, since this tray app has no session state to save. Check whether Avalonia 11.3's `X11PlatformOptions` has a switch for this (e.g. `EnableSessionManagement = false` via `.With(new X11PlatformOptions { ... })` in `Program.Main`). If it doesn't, try unsetting `SESSION_MANAGER` for the process before `AppBuilder` starts, or upgrade Avalonia if a newer release fixes the race.
- **Shut down cleanly on signals:** register `PosixSignalRegistration.Create(PosixSignal.SIGTERM / SIGINT / SIGHUP, ...)`. In the handler, cancel the default termination and post a shutdown to the UI thread. That shutdown should run the same cleanup as **Exit**.
- **Share one cleanup method:** move the cleanup into a single method that is safe to call more than once. Call it from **Exit**, from the lifetime's `ShutdownRequested`/`Exit` events, and from the signal handlers. `KeyboardGrabber.Release()` is already safe to call repeatedly, but `trayIcon.Dispose()` should be guarded.
- **Check `ForwardEvents`:** if logind revokes device access during logout before we are signalled, the virtual (uinput) fd's `POLLERR/POLLHUP/POLLNVAL` are never checked. Make sure the loop can't spin or throw in that case.

## Acceptance criteria

- [ ] Running `systemctl --user stop 'app-windows\x2dkey\x2ddisabler@autostart.service'` exits cleanly with the key either enabled or disabled, and adds nothing to `coredumpctl list`.
- [ ] Logging out of Plasma with the app running (key disabled) produces no core dump and no drkonqi notification.
- [ ] Rebooting and shutting down from Plasma add no new `windows-key-disabler` entries to `coredumpctl list` from the previous boot (check with `journalctl -b -1 | grep -i 'windows-key'`).
- [ ] After a stop or logout while the key was disabled, no leftover "(Windows Key Disabler)" virtual devices remain and the physical keyboard is not left grabbed (check `/proc/bus/input/devices`).
- [ ] The tray **Exit** item and the enable/disable toggles still work as before.

## Comments
