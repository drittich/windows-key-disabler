using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Diagnostics;

namespace WindowsKeyDisabler
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			// we don't need a window, the app lives in the tray until Exit is chosen
			AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
		}
	}

	public class App : Application
	{
		private TrayIcon trayIcon = null!;
		private readonly KeyboardGrabber _grabber = new();
		private WindowIcon enabledIcon = null!;
		private WindowIcon disabledIcon = null!;
		private bool isEnabled = true;
		private NativeMenuItem enableWindowsKeyMenuItem = null!;
		private NativeMenuItem disableWindowsKeyMenuItem = null!;

		public override void OnFrameworkInitializationCompleted()
		{
			enabledIcon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "enabled.ico"));
			disabledIcon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "disabled.ico"));

			SetupTrayIcon();
			EnableWindowsKey(true);

			trayIcon.Clicked += (s, e) => EnableWindowsKey(!isEnabled);

			base.OnFrameworkInitializationCompleted();
		}

		private void SetupTrayIcon()
		{
			enableWindowsKeyMenuItem = new NativeMenuItem("Enable Windows Key") { ToggleType = NativeMenuItemToggleType.CheckBox };
			enableWindowsKeyMenuItem.Click += (s, e) => EnableWindowsKey(true);

			disableWindowsKeyMenuItem = new NativeMenuItem("Disable Windows Key") { ToggleType = NativeMenuItemToggleType.CheckBox };
			disableWindowsKeyMenuItem.Click += (s, e) => EnableWindowsKey(false);

			var exitMenuItem = new NativeMenuItem("Exit");
			exitMenuItem.Click += ExitApplication_Click;

			trayIcon = new TrayIcon
			{
				Icon = enabledIcon,
				ToolTipText = "Windows Key Disabler",
				Menu = new NativeMenu
				{
					enableWindowsKeyMenuItem,
					disableWindowsKeyMenuItem,
					new NativeMenuItemSeparator(),
					exitMenuItem,
				},
				IsVisible = true,
			};

			TrayIcon.SetIcons(this, new TrayIcons { trayIcon });
		}

		private void ExitApplication_Click(object? sender, EventArgs e)
		{
			_grabber.Release();
			trayIcon.Dispose();
			((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Shutdown();
		}

		void EnableWindowsKey(bool enabled)
		{
			if (enabled)
			{
				_grabber.Release();
			}
			else
			{
				try
				{
					_grabber.Grab();
				}
				catch (Exception ex)
				{
					_grabber.Release();
					ShowError(ex.Message);
					enabled = true;
				}
			}

			isEnabled = enabled;
			trayIcon.Icon = enabled ? enabledIcon : disabledIcon;
			enableWindowsKeyMenuItem.IsChecked = enabled;
			disableWindowsKeyMenuItem.IsChecked = !enabled;
		}

		private static void ShowError(string message)
		{
			Console.Error.WriteLine(message);
			try
			{
				Process.Start("notify-send", ["--app-name=Windows Key Disabler", "--icon=dialog-error", "Could not disable the Windows key", message]);
			}
			catch
			{
				// notify-send isn't installed, stderr will have to do
			}
		}
	}
}
