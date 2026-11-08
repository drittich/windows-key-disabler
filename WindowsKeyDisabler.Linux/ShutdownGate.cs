namespace WindowsKeyDisabler
{
	/// <summary>
	/// Lets exactly one caller start shutting down. Exit, the lifetime's events and POSIX signals can all
	/// arrive together during a session logout, and from different threads.
	/// </summary>
	internal sealed class ShutdownGate
	{
		private int _started;

		public bool TryBegin() => Interlocked.Exchange(ref _started, 1) == 0;
	}
}
