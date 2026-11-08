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

		// poll(2) revents bits from <poll.h>: POLLIN 0x1, POLLERR 0x8, POLLHUP 0x10, POLLNVAL 0x20
		[Theory]
		[InlineData((short)0x08)]
		[InlineData((short)0x10)]
		[InlineData((short)0x20)]
		[InlineData((short)(0x01 | 0x10))]
		public void TreatsAnErroredOrRevokedDeviceAsGone(short revents)
		{
			Assert.True(KeyboardGrabber.IsDeviceGone(revents));
		}

		[Theory]
		[InlineData((short)0x00)]
		[InlineData((short)0x01)]
		public void KeepsAHealthyDevice(short revents)
		{
			Assert.False(KeyboardGrabber.IsDeviceGone(revents));
		}
	}
}
