using System.Runtime.InteropServices;

namespace WindowsKeyDisabler.Tests
{
	public class KeySignalsTests
	{
		[Fact]
		public void Sigusr1DisablesTheWindowsKey()
		{
			Assert.False(KeySignals.WindowsKeyEnabledFor((PosixSignal)10));
		}

		[Fact]
		public void Sigusr2EnablesTheWindowsKey()
		{
			Assert.True(KeySignals.WindowsKeyEnabledFor((PosixSignal)12));
		}

		[Theory]
		[InlineData(PosixSignal.SIGTERM)]
		[InlineData(PosixSignal.SIGHUP)]
		[InlineData(PosixSignal.SIGINT)]
		public void OtherSignalsLeaveTheKeyAlone(PosixSignal signal)
		{
			Assert.Null(KeySignals.WindowsKeyEnabledFor(signal));
		}

		[Fact]
		public void AllListsExactlyTheKeySignals()
		{
			Assert.Equal([KeySignals.Disable, KeySignals.Enable], KeySignals.All);
		}
	}
}
