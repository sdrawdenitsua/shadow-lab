#!/bin/bash
set -e

keytool -genkeypair -v \
  -keystore /tmp/debug.keystore \
  -alias androiddebugkey \
  -keyalg RSA -keysize 2048 -validity 10000 \
  -storepass android -keypass android \
  -dname "CN=Android Debug,O=Android,C=US"

echo "Keystore generated:"
ls -lh /tmp/debug.keystore

mkdir -p ~/.config/godot
SETTINGS_FILE=~/.config/godot/editor_settings-4.tres

printf '[gd_resource type="EditorSettings" format=3]\n' > "$SETTINGS_FILE"
printf '\n' >> "$SETTINGS_FILE"
printf '[resource]\n' >> "$SETTINGS_FILE"
printf 'export/android/android_sdk_path = "/usr/local/lib/android/sdk"\n' >> "$SETTINGS_FILE"
printf 'export/android/debug_keystore = "/tmp/debug.keystore"\n' >> "$SETTINGS_FILE"
printf 'export/android/debug_keystore_user = "androiddebugkey"\n' >> "$SETTINGS_FILE"
printf 'export/android/debug_keystore_pass = "android"\n' >> "$SETTINGS_FILE"

echo "Editor settings written:"
cat "$SETTINGS_FILE"
