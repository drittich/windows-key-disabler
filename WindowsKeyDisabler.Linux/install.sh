#!/usr/bin/env bash
# Installs Windows Key Disabler for the current user, from a release download or by building from source.
set -euo pipefail

cd "$(dirname "$0")"
INSTALL_DIR="$HOME/.local/share/windows-key-disabler"

if [ -f windows-key-disabler ]; then
	mkdir -p "$INSTALL_DIR"
	install -m 755 windows-key-disabler "$INSTALL_DIR/"
	install -m 644 ./*.so ./*.ico "$INSTALL_DIR/"
else
	dotnet publish -c Release -o "$INSTALL_DIR" --self-contained -p:PublishSingleFile=true
fi

mkdir -p "$HOME/.local/share/applications"
sed "s|@INSTALL_DIR@|$INSTALL_DIR|g" windows-key-disabler.desktop > "$HOME/.local/share/applications/windows-key-disabler.desktop"

echo "Installing udev rule (needs sudo) so the app can read keyboards and create a virtual one..."
sudo install -m 644 70-windows-key-disabler.rules /etc/udev/rules.d/
sudo modprobe uinput
sudo udevadm control --reload-rules
sudo udevadm trigger --subsystem-match=input --subsystem-match=misc

echo "Installed. Launch \"Windows Key Disabler\" from the application menu."
echo "To start it on login, add it in System Settings > Autostart."
