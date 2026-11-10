using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowsKeyDisabler
{
	internal static class Program
	{
		private static readonly ShutdownGate signalShutdown = new();

		// kept referenced for the life of the process, a collected registration stops handling its signal
		private static PosixSignalRegistration[] signalRegistrations = [];

		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			// systemd stops the autostart unit with SIGTERM at logout, shutdown and reboot
			signalRegistrations = [.. new[] { PosixSignal.SIGTERM, PosixSignal.SIGINT, PosixSignal.SIGHUP }
				.Select(signal => PosixSignalRegistration.Create(signal, OnTerminationSignal))];

			// X session management must be off: Avalonia's libSM client reads ICE on a thread-pool loop that keeps
			// running while the session manager tears the connection down at logout, and segfaults. a tray app has
			// no session state to save anyway. EnableSessionManagement alone doesn't stop the client connecting,
			// so hide the session manager from libSM. Environment.SetEnvironmentVariable doesn't reach the native
			// environment on Unix, hence unsetenv
			unsetenv("SESSION_MANAGER");

			// we don't need a window, the app lives in the tray until Exit is chosen
			AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.With(new X11PlatformOptions { EnableSessionManagement = false })
				.StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
		}

		private static void OnTerminationSignal(PosixSignalContext context)
		{
			// before Avalonia is up nothing is grabbed yet, so the default termination is fine; a repeated
			// signal also falls through, so a hung UI thread can't hold up the shutdown
			if (Application.Current is not App app || !signalShutdown.TryBegin())
			{
				return;
			}

			context.Cancel = true;
			Dispatcher.UIThread.Post(app.Quit);
		}

		[DllImport("libc", SetLastError = true)]
		private static extern int unsetenv(string name);
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
		private readonly ShutdownGate quitting = new();
		private readonly ShutdownGate resourcesReleased = new();

		public override void OnFrameworkInitializationCompleted()
		{
			enabledIcon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "enabled.ico"));
			disabledIcon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "disabled.ico"));

			SetupTrayIcon();
			EnableWindowsKey(true);

			trayIcon.Clicked += (s, e) => EnableWindowsKey(!isEnabled);

			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.Exit += (s, e) => ReleaseResources();
			}

			base.OnFrameworkInitializationCompleted();
		}

		private void SetupTrayIcon()
		{
			enableWindowsKeyMenuItem = new NativeMenuItem("Enable Windows Key") { ToggleType = NativeMenuItemToggleType.CheckBox };
			enableWindowsKeyMenuItem.Click += (s, e) => EnableWindowsKey(true);

			disableWindowsKeyMenuItem = new NativeMenuItem("Disable Windows Key") { ToggleType = NativeMenuItemToggleType.CheckBox };
			disableWindowsKeyMenuItem.Click += (s, e) => EnableWindowsKey(false);

			var exitMenuItem = new NativeMenuItem("Exit");
			exitMenuItem.Click += (s, e) => Quit();

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

		// the one way out, whether from Exit or a termination signal; must run on the UI thread
		internal void Quit()
		{
			if (!quitting.TryBegin())
			{
				return;
			}

			ReleaseResources();
			((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Shutdown();
		}

		// also runs from the lifetime's Exit event, so every way the app ends gives the keyboard back
		private void ReleaseResources()
		{
			if (!resourcesReleased.TryBegin())
			{
				return;
			}

			_grabber.Release();
			trayIcon.Dispose();
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
