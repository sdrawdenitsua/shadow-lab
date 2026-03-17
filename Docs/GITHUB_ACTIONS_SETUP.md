# Shadow Lab — GitHub Actions Build Setup

Free APK builds via GitHub Actions. No PC, no paid tier.

---

## One-Time Setup: Get Your Unity License

GitHub Actions needs your Unity license to activate during the build.
This is a one-time thing.

### Step 1 — Get Unity License File
1. Install Unity Hub on any computer (a library PC works)
2. Activate your Unity license
3. Go to: Help → Manage License → Return License
4. The `.ulf` file is at:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
5. Open that file in a text editor, copy ALL the contents

### Step 2 — Add Secrets to GitHub Repo
1. Go to `github.com/sdrawdenitsua/shadow-lab`
2. **Settings** → **Secrets and variables** → **Actions**
3. Add these 3 secrets:

| Secret Name | Value |
|-------------|-------|
| `UNITY_LICENSE` | (paste the .ulf file contents) |
| `UNITY_EMAIL` | your Unity account email |
| `UNITY_PASSWORD` | your Unity account password |

---

## Running a Build

### Auto (on every push)
Every time code is pushed to `main`, a build starts automatically.

### Manual trigger
1. Go to `github.com/sdrawdenitsua/shadow-lab`
2. **Actions** tab
3. **Build Shadow Lab APK** → **Run workflow** → **Run**

---

## Downloading the APK
1. Go to **Actions** tab in the repo
2. Click the latest successful build (green checkmark)
3. Scroll down to **Artifacts**
4. Tap **ShadowLab-Android** → downloads the APK
5. Sideload via SideQuest mobile

---

## Free Tier Limits
- 2,000 minutes/month free (each build ~20 min = ~100 free builds/month)
- Artifacts stored 14 days
- Unlimited repos
