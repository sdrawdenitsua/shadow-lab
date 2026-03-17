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

echo "aW1wb3J0IG9zCmhvbWUgPSBvcy5wYXRoLmV4cGFuZHVzZXIoIn4iKQpwYXRoID0gb3MucGF0aC5qb2luKGhvbWUsICIuY29uZmlnIiwgImdvZG90IiwgImVkaXRvcl9zZXR0aW5ncy00LnRyZXMiKQpjb250ZW50ID0gJ1tnZF9yZXNvdXJjZSB0eXBlPSJFZGl0b3JTZXR0aW5ncyIgZm9ybWF0PTNdXG5cbltyZXNvdXJjZV1cbmV4cG9ydC9hbmRyb2lkL2FuZHJvaWRfc2RrX3BhdGggPSAiL3Vzci9sb2NhbC9saWIvYW5kcm9pZC9zZGsiXG5leHBvcnQvYW5kcm9pZC9kZWJ1Z19rZXlzdG9yZSA9ICIvdG1wL2RlYnVnLmtleXN0b3JlIlxuZXhwb3J0L2FuZHJvaWQvZGVidWdfa2V5c3RvcmVfdXNlciA9ICJhbmRyb2lkZGVidWdrZXkiXG5leHBvcnQvYW5kcm9pZC9kZWJ1Z19rZXlzdG9yZV9wYXNzID0gImFuZHJvaWQiXG4nCndpdGggb3BlbihwYXRoLCAndycpIGFzIGY6CiAgICBmLndyaXRlKGNvbnRlbnQpCnByaW50KCJXcm90ZToiLCBwYXRoKQpwcmludCgiQ29udGVudCByZXByOiIsIHJlcHIoY29udGVudCkpCg==" | base64 -d > /tmp/write_settings.py
          python3 /tmp/write_settings.py

echo "Editor settings content:"
cat ~/.config/godot/editor_settings-4.tres | cat -A
