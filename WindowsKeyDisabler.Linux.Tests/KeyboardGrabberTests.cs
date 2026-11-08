namespace WindowsKeyDisabler.Tests
{
	// These tests must not need /dev/input or /dev/uinput access: they run in sandboxes with neither,
	// nor a display or session bus, so no keyboard can be grabbed. Exercise only what works without devices.
	public class KeyboardGrabberTests
	{
		[Fact]
		public void IsNotGrabbedWhenCreated()
		{
			var grabber = new KeyboardGrabber();

			Assert.False(grabber.IsGrabbed);
		}

		[Fact]
		public void ReleaseIsSafeToCallRepeatedlyWithoutAGrab()
		{
			var grabber = new KeyboardGrabber();

			grabber.Release();
			grabber.Release();

			Assert.False(grabber.IsGrabbed);
		}
	}
}
