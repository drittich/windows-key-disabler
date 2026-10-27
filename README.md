# WindowsKeyDisabler
A windows tray utility for gamers that temporarily disables the Windows key.

## Description

This is a Windows tray application that lets you easily enable or disable the Windows key. Useful for when playing full screen games and you don't want to get kicked out of full screen mode due to hitting the Windows key by mistake.

## Usage

Launch the application and it will appear in your tray, with an icon that looks like <img src="https://github.com/drittich/windows-key-disabler/assets/1222810/9ac977b9-fd32-40cb-a082-f68516229c16" width="16">. 

You can click the tray icon to toggle the Windows key off and on, or right click it to use the menu.
To quit the application, right-click the tray icon and select Exit.

## Linux

There is also a Linux build (tested on Fedora KDE Plasma under Wayland) in `WindowsKeyDisabler.Linux`. It works the same way: click the tray icon to toggle the Windows (Super/Meta) key, or right-click it for the menu.

Wayland doesn't allow apps to hook the keyboard globally, so while the key is disabled the app takes exclusive control of each keyboard and passes every other key through a virtual keyboard. This needs access to `/dev/input` and `/dev/uinput`, which `install.sh` grants to the logged-in user through a udev rule.

Download `WindowsKeyDisabler-<version>-linux-x64.tar.gz` from the [latest release](https://github.com/drittich/windows-key-disabler/releases/latest), then:

```bash
tar -xzf WindowsKeyDisabler-*-linux-x64.tar.gz
cd WindowsKeyDisabler-*-linux-x64
./install.sh
```

To build from source instead (requires the [.NET 10 SDK](https://learn.microsoft.com/dotnet/core/install/linux)), run `./install.sh` from the `WindowsKeyDisabler.Linux` folder.

This installs the app to `~/.local/share/windows-key-disabler`, adds it to the application menu, and installs the udev rule (asks for sudo). To start it on login, add it under System Settings > Autostart.

Keyboards plugged in while the key is disabled aren't affected until you toggle it off and on again.
