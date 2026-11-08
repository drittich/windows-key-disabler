namespace WindowsKeyDisabler.Tests
{
	public class ShutdownGateTests
	{
		[Fact]
		public void OnlyTheFirstCallerGetsToShutDown()
		{
			var gate = new ShutdownGate();

			Assert.True(gate.TryBegin());
			Assert.False(gate.TryBegin());
			Assert.False(gate.TryBegin());
		}
	}
}
