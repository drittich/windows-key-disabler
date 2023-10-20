namespace WindowsKeyDisabler
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MyCustomApplicationContext());
		}
	}

	public class MyCustomApplicationContext : ApplicationContext
	{
		private NotifyIcon notifyIcon;
		private KeyboardHook _hook;
		private Icon enabledIcon;
		private Icon disabledIcon;
		private bool isEnabled = true;

		public MyCustomApplicationContext()
		{
			_hook = new KeyboardHook();
			enabledIcon = new Icon("enabled.ico");
			disabledIcon = new Icon("disabled.ico");

			notifyIcon = new NotifyIcon()
			{
				Icon = enabledIcon,
				ContextMenuStrip = new ContextMenuStrip(),
				Visible = true,

			};

			notifyIcon.ContextMenuStrip.Items.Add("Enable Windows Key", null, EnableWindowsKey_Click);
			notifyIcon.ContextMenuStrip.Items.Add("Disable Windows Key", null, DisableWindowsKey_Click);
			notifyIcon.ContextMenuStrip.Items.Add("-");
			notifyIcon.ContextMenuStrip.Items.Add("Exit", null, ExitApplication_Click);
			notifyIcon.Text = "Windows Key Disabler";

			// toggle the notify icon when clicked
			notifyIcon.Click += (s, e) =>
			{
				if (e is MouseEventArgs me && me.Button == MouseButtons.Right)
				{
					return;
				}

				EnabledWindowsKey(!isEnabled);
			};
		}

		private void ExitApplication_Click(object? sender, EventArgs e)
		{
			Environment.Exit(0);
		}

		void EnableWindowsKey_Click(object? sender, EventArgs e)
		{
			EnabledWindowsKey(true);
		}

		void DisableWindowsKey_Click(object? sender, EventArgs e)
		{
			EnabledWindowsKey(false);
		}

		void EnabledWindowsKey(bool enabled)
		{
			if (enabled)
			{
				_hook.UnhookKeyboard();
				notifyIcon.Icon = enabledIcon;
				isEnabled = true;
			}
			else
			{
				_hook.HookKeyboard();
				notifyIcon.Icon = disabledIcon;
				isEnabled = false;
			}
		}
	}
}