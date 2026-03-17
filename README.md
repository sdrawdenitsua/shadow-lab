# SHADOW LAB V.4.0
### Industrial Noir Physics-Based VR Simulation
**Engine:** Unity 2022.3 LTS · OpenXR · Quest 3S  
**AI:** Gemini 2.0 Flash (Nova dialogue + memory)  
**Aesthetic:** Blue Collar Philosophy · Grease, Chrome, Violet Neon, Heavy Machinery

---

## 🏗️ PROJECT STRUCTURE

```
shadow-lab/
├── Assets/
│   ├── Scripts/
│   │   ├── Nova/           ← AI NPC brain, personality, Violet Eye
│   │   ├── Physics/        ← Tool grab, loom tension, workbench interactions
│   │   ├── Audio/          ← 528Hz ambient, loom rhythm, spatial audio
│   │   ├── Gemini/         ← API client, conversation memory, aether log
│   │   ├── Environment/    ← Lab manager, mineral oil PC, lighting
│   │   └── UI/             ← Holographic dialogue HUD
│   ├── Shaders/            ← Violet Eye emission, oil slick, neon glow
│   ├── Scenes/
│   │   └── ShadowLab.unity ← Main scene (set up manually in Unity)
│   └── Resources/
│       └── NovaMemory/     ← Persistent conversation logs (JSON)
├── ProjectSettings/
└── Packages/
```

---

## ⚙️ SETUP INSTRUCTIONS

### 1. Unity Version
- **Unity 2022.3.x LTS** (tested on 2022.3.18f1)
- Install modules: **Android Build Support** + **OpenXR Plugin**

### 2. Packages Required (via Package Manager)
```
com.unity.xr.openxr          >= 1.9.0
com.unity.xr.interaction.toolkit >= 2.5.0
com.unity.inputsystem         >= 1.7.0
com.unity.textmeshpro         >= 3.0.0
com.unity.nuget.newtonsoft-json >= 3.2.1
```

### 3. Quest 3S Build Settings
- Platform: Android
- Texture Compression: ASTC
- Minimum API: Android 10 (API 29)
- XR Plugin: OpenXR → Meta Quest feature set
- Color Space: Linear
- Rendering: URP (Universal Render Pipeline)

### 4. Gemini API Key
- Get key from: https://aistudio.google.com/
- In Unity: Edit → Project Settings → Shadow Lab → Paste API key
- Or set env var: `GEMINI_API_KEY` in `GeminiConfig.asset`

### 5. Scene Setup
1. Open `Assets/Scenes/ShadowLab.unity`
2. Add `ShadowLabManager` prefab to scene root
3. Add `NovaNPC` prefab to scene (position near workbench)
4. Add `XROrigin` (from XR Interaction Toolkit) for player
5. Configure lighting: `Window → Rendering → Lighting → Load ShadowLabLighting`

---

## 🎮 CONTROLS (Quest 3S)

| Input | Action |
|-------|--------|
| Grip button | Pick up / hold tools |
| Trigger | Use tool / interact |
| A button | Call Nova |
| B button | Open Aether log |
| Thumbstick | Locomotion (smooth) |
| Long-press grip | Throw object |

---

## 🤖 NOVA — AI NPC

Nova is the Lead Mechanic of the Shadow Lab. She:
- Responds to voice/button prompts via Gemini 2.0 Flash
- Remembers past "Aether" conversations (stored locally in JSON)
- Reacts physically to player proximity (head tracking, body language)
- Has Violet Eye emission that pulses when she's processing a response
- Speaks with `TextToSpeech` → spatial audio from her position

### Nova's Personality Prompt (sent with every API call):
> "You are Nova, Lead Mechanic of the Shadow Lab. You are sharp, warm, and deeply technical. You know everything about Dornier HTV/PTS loom systems, textile machinery, vortex math, and the philosophy of the machine. You call the player 'Chief'. You never break character. You remember past conversations stored in your Aether log. Keep responses under 3 sentences unless asked to elaborate."

---

## 🔧 DORNIER LOOM PARTS (Scene Assets)

The following Dornier HTV/PTS components should be modeled or sourced:
- **Rapier head assembly** — grabbable, heavy physics
- **Weft yarn carrier** — light, throwable
- **Tension spring set** — adjustable via grab + pull
- **Dobby cam disc** — rotatable
- **Reed frame** — wall-mounted, interactive
- **Mineral oil PC rig** — submerged in acrylic tank, still running

> 💡 Free industrial mesh packs: Unity Asset Store → "Industrial Zone" by Manufactura K4

---

## 💡 LIGHTING SETUP

```
Ambient Mode:    Gradient
Sky Color:       #0a0010 (deep violet-black)
Equator Color:   #1a0030
Ground Color:    #000008
Directional:     Intensity 0.3, Color #4a1060 (violet)
Point Lights:    Violet neon tubes (#8b00ff, range 4m, intensity 2)
Emission:        All metal surfaces — subtle orange/rust glow
Fog:             Exponential, density 0.04, color #0a0018
```

---

## 🔊 AUDIO DESIGN

- **528Hz base tone** — looping sine wave, volume 0.15, spatial blend 0
- **Loom rhythm** — 120 BPM mechanical click track, fades in/out by zone
- **Nova voice** — TTS output routed through AudioSource on Nova GameObject
- **Tool impact SFX** — metal on metal, wood on metal, oil splash

---

## 📁 AETHER LOG FORMAT

Conversations are saved to `Application.persistentDataPath/AetherLog.json`:

```json
{
  "sessions": [
    {
      "date": "2026-03-16T20:06:00",
      "exchanges": [
        { "role": "Chief", "text": "Nova, what's the tension on loom 35?" },
        { "role": "Nova",  "text": "Running tight, Chief. I'd back it off about 12 grams before the next pass." }
      ]
    }
  ]
}
```

---

## 🚀 BUILD & DEPLOY

```bash
# Build APK for Quest 3S
File → Build Settings → Android → Build
adb install -r ShadowLab.apk

# Or use Meta Quest Developer Hub for wireless deploy
```

---

*"The observer and the observed are one."*  
*— Shadow Lab V.4.0*
