using System.Runtime.InteropServices;
using System.Text;

namespace WindowsKeyDisabler
{
	/// <summary>
	/// Wayland has no global keyboard hooks, so instead we take exclusive control of each physical
	/// keyboard (EVIOCGRAB) and replay everything except the Windows/Super/Meta keys through a
	/// mirrored virtual device created with uinput.
	/// </summary>
	public unsafe class KeyboardGrabber
	{
		private const string VirtualDeviceSuffix = " (Windows Key Disabler)";
		private const string PermissionHint = "Run install.sh once to grant access to keyboards and /dev/uinput.";

		private const ushort EV_SYN = 0x00;
		private const ushort EV_KEY = 0x01;
		private const ushort EV_REL = 0x02;
		private const ushort EV_ABS = 0x03;
		private const ushort EV_MSC = 0x04;
		private const ushort EV_SW = 0x05;
		private const ushort EV_LED = 0x11;
		private const ushort SYN_REPORT = 0;
		private const ushort KEY_A = 30;
		private const ushort KEY_LEFTMETA = 125;
		private const ushort KEY_RIGHTMETA = 126;

		// event types we mirror, with the uinput request that enables each code for that type
		private static readonly (ushort Type, uint SetBitNr)[] MirroredTypes =
		[
			(EV_KEY, 101), (EV_REL, 102), (EV_ABS, 103), (EV_MSC, 104), (EV_LED, 105), (EV_SW, 109),
		];

		private const int O_RDWR = 0x2;
		private const int O_NONBLOCK = 0x800;
		private const int O_CLOEXEC = 0x80000;
		private const short POLLIN = 0x1;
		private const short POLLERR = 0x8;
		private const short POLLHUP = 0x10;
		private const short POLLNVAL = 0x20;
		private const int EACCES = 13;
		private const int EPERM = 1;

		private readonly List<GrabbedDevice> _devices = [];
		private Thread? _thread;
		private int _stopFd = -1;

		public bool IsGrabbed => _thread != null;

		public void Grab()
		{
			if (IsGrabbed)
			{
				return;
			}

			var keyboards = OpenKeyboards();
			try
			{
				WaitForKeysReleased(keyboards);

				while (keyboards.Count > 0)
				{
					_devices.Add(new GrabbedDevice(keyboards[0], CreateMirror(keyboards[0])));
					keyboards.RemoveAt(0);
				}

				foreach (var device in _devices)
				{
					Check(ioctl(device.PhysicalFd, EVIOCGRAB, 1), "grab keyboard");
				}
			}
			finally
			{
				keyboards.ForEach(fd => close(fd));
			}

			_stopFd = eventfd(0, O_CLOEXEC);
			Check(_stopFd, "create eventfd");
			_thread = new Thread(ForwardEvents) { IsBackground = true, Name = "Keyboard forwarder" };
			_thread.Start();
		}

		public void Release()
		{
			if (_thread != null)
			{
				ulong one = 1;
				write(_stopFd, &one, sizeof(ulong));
				_thread.Join();
				_thread = null;
			}

			if (_stopFd >= 0)
			{
				close(_stopFd);
				_stopFd = -1;
			}

			foreach (var device in _devices)
			{
				device.Close();
			}
			_devices.Clear();
		}

		private static List<int> OpenKeyboards()
		{
			var keyboards = new List<int>();
			bool accessDenied = false;

			foreach (string path in Directory.GetFiles("/dev/input", "event*").OrderBy(p => p.Length).ThenBy(p => p))
			{
				int fd = open(path, O_RDWR | O_NONBLOCK | O_CLOEXEC);
				if (fd < 0)
				{
					accessDenied |= Marshal.GetLastPInvokeError() is EACCES or EPERM;
					continue;
				}

				var keyBits = GetBits(fd, EV_KEY);
				bool isKeyboard = TestBit(keyBits, KEY_A) && (TestBit(keyBits, KEY_LEFTMETA) || TestBit(keyBits, KEY_RIGHTMETA));
				if (isKeyboard && !GetName(fd).EndsWith(VirtualDeviceSuffix))
				{
					keyboards.Add(fd);
				}
				else
				{
					close(fd);
				}
			}

			if (keyboards.Count == 0)
			{
				throw new InvalidOperationException(accessDenied
					? $"Permission denied reading keyboards. {PermissionHint}"
					: "No keyboards were found.");
			}

			return keyboards;
		}

		// grabbing while a key is held would leave it stuck down, since the release would never reach the desktop
		private static void WaitForKeysReleased(List<int> keyboards)
		{
			var deadline = DateTime.UtcNow.AddSeconds(2);
			byte* state = stackalloc byte[BitsBufferSize];

			while (DateTime.UtcNow < deadline)
			{
				bool anyDown = false;
				foreach (int fd in keyboards)
				{
					new Span<byte>(state, BitsBufferSize).Clear();
					ioctl(fd, Ioc(IOC_READ, 'E', 0x18, BitsBufferSize), state);
					anyDown |= new Span<byte>(state, BitsBufferSize).ContainsAnyExcept((byte)0);
				}

				if (!anyDown)
				{
					return;
				}
				Thread.Sleep(20);
			}
		}

		private static int CreateMirror(int physicalFd)
		{
			int fd = open("/dev/uinput", O_RDWR | O_NONBLOCK | O_CLOEXEC);
			if (fd < 0)
			{
				throw new InvalidOperationException($"Permission denied opening /dev/uinput. {PermissionHint}");
			}

			try
			{
				foreach (var (type, setBitNr) in MirroredTypes)
				{
					var bits = GetBits(physicalFd, type);
					if (!bits.AsSpan().ContainsAnyExcept((byte)0))
					{
						continue;
					}

					Check(ioctl(fd, Ioc(IOC_WRITE, 'U', 100, sizeof(int)), type), "set event type");
					for (int code = 0; code < bits.Length * 8; code++)
					{
						if (TestBit(bits, code))
						{
							Check(ioctl(fd, Ioc(IOC_WRITE, 'U', setBitNr, sizeof(int)), code), "set event code");
						}
					}
				}

				var props = new byte[BitsBufferSize];
				fixed (byte* p = props)
				{
					ioctl(physicalFd, Ioc(IOC_READ, 'E', 0x09, props.Length), p);
				}
				for (int prop = 0; prop < props.Length * 8; prop++)
				{
					if (TestBit(props, prop))
					{
						ioctl(fd, Ioc(IOC_WRITE, 'U', 110, sizeof(int)), prop);
					}
				}

				var setup = new UinputSetup();
				ioctl(physicalFd, Ioc(IOC_READ, 'E', 0x02, sizeof(InputId)), &setup.Id);
				var name = Encoding.UTF8.GetBytes(GetName(physicalFd) + VirtualDeviceSuffix);
				new ReadOnlySpan<byte>(name, 0, Math.Min(name.Length, 79)).CopyTo(new Span<byte>(setup.Name, 80));
				Check(ioctl(fd, Ioc(IOC_WRITE, 'U', 3, sizeof(UinputSetup)), &setup), "set up virtual keyboard");

				var absBits = GetBits(physicalFd, EV_ABS);
				for (int code = 0; code < absBits.Length * 8; code++)
				{
					if (TestBit(absBits, code))
					{
						var absSetup = new UinputAbsSetup { Code = (ushort)code };
						ioctl(physicalFd, Ioc(IOC_READ, 'E', 0x40 + (uint)code, sizeof(InputAbsInfo)), &absSetup.AbsInfo);
						Check(ioctl(fd, Ioc(IOC_WRITE, 'U', 4, sizeof(UinputAbsSetup)), &absSetup), "set up virtual axis");
					}
				}

				Check(ioctl(fd, Ioc(0, 'U', 1, 0), 0), "create virtual keyboard");
				return fd;
			}
			catch
			{
				close(fd);
				throw;
			}
		}

		private void ForwardEvents()
		{
			// poll slot 0 is the stop signal, then a (physical, virtual) pair per device
			var fds = new PollFd[1 + _devices.Count * 2];
			var buffer = new InputEvent[64];
			fds[0] = new PollFd { Fd = _stopFd, Events = POLLIN };

			while (true)
			{
				for (int i = 0; i < _devices.Count; i++)
				{
					fds[1 + i * 2] = new PollFd { Fd = _devices[i].PhysicalFd, Events = POLLIN };
					fds[2 + i * 2] = new PollFd { Fd = _devices[i].VirtualFd, Events = POLLIN };
				}

				fixed (PollFd* p = fds)
				{
					if (poll(p, (ulong)fds.Length, -1) < 0)
					{
						continue;
					}
				}

				if (fds[0].Revents != 0)
				{
					return;
				}

				for (int i = 0; i < _devices.Count; i++)
				{
					var device = _devices[i];

					// the keyboard was unplugged, stop listening to it (a negative fd is ignored by poll)
					if ((fds[1 + i * 2].Revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
					{
						device.Close();
						continue;
					}

					if ((fds[1 + i * 2].Revents & POLLIN) != 0)
					{
						int count = ReadEvents(device.PhysicalFd, buffer);
						int kept = 0;
						for (int e = 0; e < count; e++)
						{
							if (buffer[e].Type == EV_KEY && buffer[e].Code is KEY_LEFTMETA or KEY_RIGHTMETA)
							{
								continue; // suppress key press
							}
							buffer[kept++] = buffer[e];
						}
						WriteEvents(device.VirtualFd, buffer, kept);
					}

					// the desktop sets Caps Lock etc. on the virtual keyboard, pass that on to the real one
					if ((fds[2 + i * 2].Revents & POLLIN) != 0)
					{
						int count = ReadEvents(device.VirtualFd, buffer);
						int kept = 0;
						for (int e = 0; e < count; e++)
						{
							if (buffer[e].Type == EV_LED)
							{
								buffer[kept++] = buffer[e];
							}
						}
						if (kept > 0)
						{
							buffer[kept++] = new InputEvent { Type = EV_SYN, Code = SYN_REPORT };
							WriteEvents(device.PhysicalFd, buffer, kept);
						}
					}
				}
			}
		}

		private static int ReadEvents(int fd, InputEvent[] buffer)
		{
			fixed (InputEvent* p = buffer)
			{
				nint bytes = read(fd, p, buffer.Length * sizeof(InputEvent));
				return bytes > 0 ? (int)(bytes / sizeof(InputEvent)) : 0;
			}
		}

		private static void WriteEvents(int fd, InputEvent[] buffer, int count)
		{
			if (count == 0 || fd < 0)
			{
				return;
			}

			fixed (InputEvent* p = buffer)
			{
				write(fd, p, count * sizeof(InputEvent));
			}
		}

		private const int BitsBufferSize = 128; // enough for KEY_MAX (0x2ff) bits
		private const uint IOC_WRITE = 1;
		private const uint IOC_READ = 2;
		private static readonly ulong EVIOCGRAB = Ioc(IOC_WRITE, 'E', 0x90, sizeof(int));

		private static ulong Ioc(uint dir, char type, uint nr, int size) => (dir << 30) | ((uint)size << 16) | ((uint)type << 8) | nr;

		private static byte[] GetBits(int fd, ushort type)
		{
			var bits = new byte[BitsBufferSize];
			fixed (byte* p = bits)
			{
				ioctl(fd, Ioc(IOC_READ, 'E', 0x20 + (uint)type, bits.Length), p);
			}
			return bits;
		}

		private static bool TestBit(ReadOnlySpan<byte> bits, int bit) => (bits[bit / 8] & (1 << (bit % 8))) != 0;

		private static string GetName(int fd)
		{
			byte* name = stackalloc byte[256];
			int length = ioctl(fd, Ioc(IOC_READ, 'E', 0x06, 256), name);
			return length > 0 ? Encoding.UTF8.GetString(name, length).TrimEnd('\0') : "";
		}

		private static void Check(int result, string action)
		{
			if (result < 0)
			{
				throw new InvalidOperationException($"Failed to {action} (errno {Marshal.GetLastPInvokeError()}).");
			}
		}

		private sealed class GrabbedDevice(int physicalFd, int virtualFd)
		{
			public int PhysicalFd { get; private set; } = physicalFd;
			public int VirtualFd { get; private set; } = virtualFd;

			public void Close()
			{
				if (PhysicalFd >= 0)
				{
					ioctl(PhysicalFd, EVIOCGRAB, 0);
					close(PhysicalFd);
					PhysicalFd = -1;
				}

				if (VirtualFd >= 0)
				{
					ioctl(VirtualFd, Ioc(0, 'U', 2, 0), 0); // UI_DEV_DESTROY
					close(VirtualFd);
					VirtualFd = -1;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct InputEvent
		{
			public long Sec;
			public long Usec;
			public ushort Type;
			public ushort Code;
			public int Value;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct InputId
		{
			public ushort BusType;
			public ushort Vendor;
			public ushort Product;
			public ushort Version;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct InputAbsInfo
		{
			public int Value;
			public int Minimum;
			public int Maximum;
			public int Fuzz;
			public int Flat;
			public int Resolution;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct UinputSetup
		{
			public InputId Id;
			public fixed byte Name[80];
			public uint FfEffectsMax;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct UinputAbsSetup
		{
			public ushort Code;
			public InputAbsInfo AbsInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct PollFd
		{
			public int Fd;
			public short Events;
			public short Revents;
		}

		[DllImport("libc", SetLastError = true)]
		private static extern int open(string path, int flags);

		[DllImport("libc", SetLastError = true)]
		private static extern int close(int fd);

		[DllImport("libc", SetLastError = true)]
		private static extern nint read(int fd, void* buf, nint count);

		[DllImport("libc", SetLastError = true)]
		private static extern nint write(int fd, void* buf, nint count);

		[DllImport("libc", SetLastError = true)]
		private static extern int ioctl(int fd, ulong request, void* arg);

		[DllImport("libc", SetLastError = true)]
		private static extern int ioctl(int fd, ulong request, nint arg);

		[DllImport("libc", SetLastError = true)]
		private static extern int poll(PollFd* fds, ulong nfds, int timeout);

		[DllImport("libc", SetLastError = true)]
		private static extern int eventfd(uint initval, int flags);
	}
}
