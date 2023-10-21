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
			// we don't need a form, so we can just run the ApplicationContext directly
			Application.Run(new MyCustomApplicationContext());
		}
	}

	public class MyCustomApplicationContext : ApplicationContext
	{
		private NotifyIcon notifyIcon;
		private readonly KeyboardHook _hook;
		private readonly Icon enabledIcon;
		private readonly Icon disabledIcon;
		private bool isEnabled = true;
		private readonly ToolStripMenuItem enableWindowsKeyMenuItem;
		private readonly ToolStripMenuItem disableWindowsKeyMenuItem;

		public MyCustomApplicationContext()
		{
			_hook = new KeyboardHook();
			enabledIcon = new Icon("enabled.ico");
			disabledIcon = new Icon("disabled.ico");

			SetupNotifyIcon();

			enableWindowsKeyMenuItem = GetAllMenuItems(notifyIcon!.ContextMenuStrip!.Items).Single(i => i.Text == "Enable Windows Key");
			disableWindowsKeyMenuItem = GetAllMenuItems(notifyIcon.ContextMenuStrip!.Items).Single(i => i.Text == "Disable Windows Key");

			EnableWindowsKey_Click(null, null);

			notifyIcon.Click += (s, e) =>
			{
				if (e is MouseEventArgs me && me.Button == MouseButtons.Right)
				{
					return;
				}

				EnableWindowsKey(!isEnabled);
				enableWindowsKeyMenuItem.Checked = isEnabled;
				disableWindowsKeyMenuItem.Checked = !isEnabled;
			};
		}

		private void SetupNotifyIcon()
		{
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
		}

		private void ExitApplication_Click(object? sender, EventArgs e)
		{
			Environment.Exit(0);
		}

		void EnableWindowsKey_Click(object? sender, EventArgs? e)
		{
			enableWindowsKeyMenuItem.Checked = true;
			disableWindowsKeyMenuItem.Checked = false;

			EnableWindowsKey(true);
		}

		void DisableWindowsKey_Click(object? sender, EventArgs e)
		{
			enableWindowsKeyMenuItem.Checked = false;
			disableWindowsKeyMenuItem.Checked = true;

			EnableWindowsKey(false);
		}

		void EnableWindowsKey(bool enabled)
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

		private IEnumerable<ToolStripMenuItem> GetAllMenuItems(ToolStripItemCollection items)
		{
			foreach (ToolStripItem item in items)
			{
				if (item is ToolStripMenuItem menuItem)
				{
					yield return menuItem;

					if (menuItem.HasDropDownItems)
					{
						foreach (ToolStripMenuItem subItem in GetAllMenuItems(menuItem.DropDownItems))
						{
							yield return subItem;
						}
					}
				}
			}
		}
	}
}