using System.Runtime.InteropServices;

namespace WindowsKeyDisabler
{
	/// <summary>
	/// The POSIX signals that switch the Windows key from outside the app, e.g. from a Steam launch option:
	/// SIGUSR1 disables it, SIGUSR2 enables it.
	/// </summary>
	internal static class KeySignals
	{
		// PosixSignal has no USR1/USR2 members, but Create takes raw Linux signal numbers cast to it
		public static readonly PosixSignal Disable = (PosixSignal)10;
		public static readonly PosixSignal Enable = (PosixSignal)12;

		public static readonly PosixSignal[] All = [Disable, Enable];

		/// <summary>The Windows key state a signal asks for, or null if it isn't one of ours.</summary>
		public static bool? WindowsKeyEnabledFor(PosixSignal signal) =>
			signal == Disable ? false
			: signal == Enable ? true
			: null;
	}
}
