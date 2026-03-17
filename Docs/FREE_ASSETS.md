# Shadow Lab — Free Asset Sources
> Curated for: Quest 3S / Unity 2022.3 LTS / Industrial + Sci-fi aesthetic

---

## 🏭 ENVIRONMENT — Industrial / Workshop

### Poly Haven (CC0 — free forever, commercial ok)
https://polyhaven.com

| Asset | Type | Use in Shadow Lab |
|-------|------|-------------------|
| `factory_floor_02` | Texture (PBR) | Main floor concrete |
| `metal_grate_02` | Texture (PBR) | Grate panels, drain covers |
| `rusty_metal_02` | Texture (PBR) | Loom frame weathering |
| `brushed_metal` | Texture (PBR) | Workbench top, tools |
| `oil_drum` | HDRI | Ambient lighting reference |

Download as 1K or 2K for Quest — 4K is too heavy.

---

### Sketchfab — Free Industrial
https://sketchfab.com/search?q=industrial&features=downloadable&price=0

| Asset | Creator | License | Use |
|-------|---------|---------|-----|
| "Industrial Machinery" | Various | CC-BY | Loom details |
| "Metal Workbench" | Various | CC-BY | Bench replacement |
| "Oil Tank" | Various | CC-BY | Immersion tank ref |
| "Cable Tray" | Various | CC0 | Overhead cables |
| "Pipe Fittings" | Various | CC-BY | Wall details |

**Workflow:** Download → Blender (fix scale) → Export FBX → Unity

---

### Unity Asset Store — Free Packs
https://assetstore.unity.com/?price=0-0

| Pack | Use |
|------|-----|
| **POLYGON - Industrial Pack (Demo)** | Loom environment props |
| **Free Industrial Zone** | Floor tiles, wall panels |
| **Modular Industrial Kit (Free)** | Structural pieces |
| **Grunge Decal Set** | Floor oil stains, wear marks |
| **Simple Metal Textures** | Budget PBR metals |

---

## 🤖 CHARACTER (Nova)

### Sketchfab — Female Base Mesh
Search: `female character base mesh free download`

| Asset | Notes |
|-------|-------|
| "Female Basemesh" by theApe | Clean topology, rigged |
| "Sci-fi Female" various | Search filtered by downloadable |

**Retargeting workflow:**
1. Import to Blender
2. Apply Rigify rig (Add → Armature → Human (Meta-Rig))
3. Weight paint to new rig
4. Export FBX with embedded armature
5. Unity: Humanoid rig → Animator Controller → NovaBrain.cs

---

### ReadyPlayerMe (Free)
https://readyplayer.me

Create a custom Nova avatar:
- Pick female body
- Industrial/mechanic outfit
- Violet eyes (matches VioletEyeEmitter.cs)
- Export as `.glb` → import to Unity directly

---

## 🔧 TOOLS / PROPS

### Sketchfab Free
| Asset | Use |
|-------|-----|
| "Wrench Set" | Workbench pegboard |
| "Torque Wrench" | Interactive grab prop |
| "Digital Multimeter" | Control panel detail |
| "Oil Can Vintage" | Floor prop |
| "Neon Sign" | Wall decoration |

---

## 🎵 AUDIO (Free)

### Freesound.org (CC0 / CC-BY)
https://freesound.org

| Search Term | Use in AmbientAudioManager.cs |
|------------|-------------------------------|
| `rapier loom weaving` | loomRhythmClip |
| `industrial machinery loop` | ambient background |
| `metal clank` | metalGroanClips[] |
| `electrical hum` | electricalHumClips[] |
| `oil drip` | oilDripClip |
| `hydraulic click` | LoomTensionMechanism clickClip |

### Zapsplat (Free tier)
https://www.zapsplat.com
Search: "industrial", "factory", "machine"

---

## 🖼️ TEXTURES PIPELINE

### For all downloaded textures:
1. Run through **Materialize** (free) to generate full PBR maps
   https://boundingboxsoftware.com/materialize/
2. Resize to 512x512 or 1024x1024 for Quest budget
3. Import to Unity → set Texture Type = Default, Compression = ASTC 6x6

---

## 📦 ASSET PRIORITY ORDER

If you only grab a few things, grab these first:

1. **Poly Haven metal textures** — immediate visual upgrade, no rigging needed
2. **ReadyPlayerMe Nova avatar** — fastest path to a real Nova character
3. **Freesound loom rhythm** — wire directly into AmbientAudioManager.cs
4. **Unity Asset Store "Modular Industrial Kit"** — fills the room fast

---

*All CC0 assets require no attribution. CC-BY assets require crediting the creator.*
*For personal/non-commercial use (your Quest 3S), everything here is fair game.*
