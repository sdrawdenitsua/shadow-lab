#!/usr/bin/env python3
"""Patch export_presets.cfg with keystore info before Godot export."""
import sys

preset_path = "Godot/project/export_presets.cfg"
content = open(preset_path).read()

lines = [
    'keystore/release="/tmp/release.keystore"',
    'keystore/release_password="shadowlab123"',
    'keystore/release_user="shadowlab"',
]

for line in lines:
    key = line.split("=")[0]
    if key not in content:
        content = content.rstrip() + "\n" + line + "\n"

open(preset_path, "w").write(content)
print("export_presets.cfg patched successfully.")
print(content)
