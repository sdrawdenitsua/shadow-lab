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

# Find Java SDK path (JAVA_HOME is set by setup-java action)
JAVA_SDK="${JAVA_HOME}"
echo "Java SDK: ${JAVA_SDK}"
ls "${JAVA_SDK}/bin/java"

mkdir -p ~/.config/godot

# Write EditorSettings using tee with exact newlines
{
  echo '[gd_resource type="EditorSettings" format=3]'
  echo ''
  echo '[resource]'
  echo "export/android/java_sdk_path = \"${JAVA_SDK}\""
  echo 'export/android/android_sdk_path = "/usr/local/lib/android/sdk"'
  echo 'export/android/debug_keystore = "/tmp/debug.keystore"'
  echo 'export/android/debug_keystore_user = "androiddebugkey"'
  echo 'export/android/debug_keystore_pass = "android"'
} > ~/.config/godot/editor_settings-4.tres

echo "Editor settings written:"
cat ~/.config/godot/editor_settings-4.tres
