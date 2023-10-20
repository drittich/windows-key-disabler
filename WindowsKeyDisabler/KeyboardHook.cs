using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowsKeyDisabler
{
	public class KeyboardHook
	{
		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x0100;
		private const int WM_SYSKEYDOWN = 0x0104;

		public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		public event EventHandler KeyPressed;

		private LowLevelKeyboardProc _proc;
		private IntPtr _hookID = IntPtr.Zero;

		public KeyboardHook()
		{
			_proc = HookCallback;
		}

		public void HookKeyboard()
		{
			_hookID = SetHook(_proc);
		}

		public void UnhookKeyboard()
		{
			UnhookWindowsHookEx(_hookID);
		}

		private IntPtr SetHook(LowLevelKeyboardProc proc)
		{
			using (Process process = Process.GetCurrentProcess())
			using (ProcessModule module = process.MainModule)
			{
				return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
			}
		}

		private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
			{
				int vkCode = Marshal.ReadInt32(lParam);
				int leftWindowsKey = 91;
				if (vkCode == leftWindowsKey)
				{
					KeyPressed?.Invoke(this, EventArgs.Empty);
					return (IntPtr)1; // suppress key press
				}
			}

			return CallNextHookEx(_hookID, nCode, wParam, lParam);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}
