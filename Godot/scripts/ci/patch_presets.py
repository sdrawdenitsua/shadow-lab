#!/usr/bin/env python3
"""Verify export_presets.cfg has keystore before Godot export."""
content = open("Godot/project/export_presets.cfg").read()
assert "keystore/release=" in content, "keystore not found in presets!"
print("export_presets.cfg OK")
print(content)
