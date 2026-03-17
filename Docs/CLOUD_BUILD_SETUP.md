# Shadow Lab — Unity Cloud Build Setup
> Build your Quest APK with zero PC required

---

## What This Does
Unity Cloud Build connects to your GitHub repo, builds the APK in the cloud,
and gives you a download link. You download it to your Android phone,
then sideload to Quest 3S via SideQuest mobile.

---

## Step 1 — Create Unity Account (if you don't have one)
1. Go to **id.unity.com** on your phone browser
2. Sign up free
3. Verify email

---

## Step 2 — Create a Unity Project in the Dashboard
1. Go to **dashboard.unity3d.com**
2. Click **Create Project**
3. Name it: `ShadowLab`
4. Organization: create one called `ShadowLab` (or your name)

---

## Step 3 — Connect Your GitHub Repo
1. In the project dashboard → **Cloud Build** (left sidebar)
2. Click **Set up Cloud Build**
3. Source Control → **GitHub**
4. Authorize Unity to access your GitHub
5. Select repo: `sdrawdenitsua/shadow-lab`
6. Branch: `main`

---

## Step 4 — Create a Build Target
1. Click **Add new build target**
2. Platform: **Android**
3. Unity version: **2022.3.20f1 LTS**
4. Build name: `Android-Quest3S`
5. **Save**

---

## Step 5 — Configure Android Settings
In the build target settings:
- Bundle ID: `com.shadowlab.vr`
- Min SDK: **29** (Android 10 / Quest requirement)
- Target SDK: **32**
- Scripting Backend: **IL2CPP**
- Target Architecture: **ARM64** (Quest 3S is ARM64 only)
- Texture Compression: **ASTC**

---

## Step 6 — Build
1. Click **Start Build**
2. Wait ~10-20 minutes
3. You'll get an email when it's done
4. Download the `.apk` directly to your Android phone

---

## Step 7 — Sideload to Quest 3S
1. Open **SideQuest** on your Android phone
2. Connect Quest 3S via USB-C to your phone (needs OTG cable/adapter)
   OR use SideQuest wireless if you set it up before
3. Tap the APK → **Install**
4. On Quest: Library → **Unknown Sources** → **Shadow Lab**

---

## Troubleshooting
- **Build fails — missing packages**: Unity Cloud Build needs a `manifest.json`
  in `Packages/` folder. Nova will add this to the repo.
- **ARM64 error**: Make sure IL2CPP is selected, not Mono
- **AndroidManifest conflicts**: The one in `Assets/Plugins/Android/` takes priority

---

## Free Tier Limits
Unity Cloud Build free tier gives you:
- 25 build minutes/month
- 1 concurrent build
- Unlimited builds (just queued)

That's enough for Shadow Lab at this stage.
