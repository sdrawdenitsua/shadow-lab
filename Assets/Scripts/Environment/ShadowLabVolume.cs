using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShadowLab.Environment
{
    /// <summary>
    /// Configures the Global Volume for the Shadow Lab at runtime.
    ///
    /// Profile settings applied:
    ///   • Bloom          — violet-tinted, high intensity for neon lights
    ///   • Vignette       — heavy, dark corners pull focus inward
    ///   • Color Adjustments — deep contrast, violet hue shift, desaturated mid-tones
    ///   • Shadows/Midtones/Highlights — dark industrial colour grading
    ///   • Film Grain     — subtle 8mm grain for texture
    ///   • Chromatic Aberration — light edge fringing on fast head movement
    ///   • Depth of Field — subtle near blur when close to machines
    ///
    /// Attach to the Global Volume GameObject.
    /// Assign a blank Volume Profile in the inspector — this script fills it.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class ShadowLabVolume : MonoBehaviour
    {
        [Header("Override Values")]
        [SerializeField] private Color  bloomTint         = new Color(0.55f, 0.10f, 1.00f); // violet
        [SerializeField] private float  bloomIntensity    = 1.4f;
        [SerializeField] private float  bloomThreshold    = 0.85f;
        [SerializeField] private float  bloomScatter      = 0.72f;

        [SerializeField] private float  vignetteIntensity = 0.52f;
        [SerializeField] private float  vignetteSmoothness= 0.38f;
        [SerializeField] private Color  vignetteColor     = Color.black;

        [SerializeField] private float  postExposure      = -0.3f;   // slightly darker overall
        [SerializeField] private float  contrast          =  22f;    // punchy contrast
        [SerializeField] private float  saturation        = -18f;    // desaturate mid-tones
        [SerializeField] private float  hueShift          =   8f;    // shift toward violet

        [SerializeField] private Color  shadowsColor      = new Color(0.05f, 0.00f, 0.08f, 0f);
        [SerializeField] private Color  midtonesColor     = new Color(0.97f, 0.95f, 1.02f, 0f);
        [SerializeField] private Color  highlightsColor   = new Color(0.80f, 0.70f, 1.10f, 0f);

        [SerializeField] private float  grainIntensity    = 0.18f;
        [SerializeField] private float  grainSize         = 0.50f;

        [SerializeField] private float  chromaticAberation= 0.12f;

        private Volume _volume;

        private void Awake()
        {
            _volume = GetComponent<Volume>();
            if (_volume.profile == null)
                _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            ApplyProfile();
        }

        private void ApplyProfile()
        {
            var profile = _volume.profile;

            // ── Bloom ─────────────────────────────────────────────────
            if (profile.TryGet<Bloom>(out var bloom) || profile.Add<Bloom>(out bloom))
            {
                bloom.active           = true;
                bloom.threshold.value  = bloomThreshold;
                bloom.intensity.value  = bloomIntensity;
                bloom.scatter.value    = bloomScatter;
                bloom.tint.value       = bloomTint;
                bloom.highQualityFiltering.value = true;
            }

            // ── Vignette ──────────────────────────────────────────────
            if (profile.TryGet<Vignette>(out var vig) || profile.Add<Vignette>(out vig))
            {
                vig.active           = true;
                vig.color.value      = vignetteColor;
                vig.intensity.value  = vignetteIntensity;
                vig.smoothness.value = vignetteSmoothness;
                vig.rounded.value    = true;
            }

            // ── Color Adjustments ─────────────────────────────────────
            if (profile.TryGet<ColorAdjustments>(out var ca) || profile.Add<ColorAdjustments>(out ca))
            {
                ca.active                  = true;
                ca.postExposure.value      = postExposure;
                ca.contrast.value          = contrast;
                ca.colorFilter.value       = new Color(0.95f, 0.92f, 1.05f); // cool white
                ca.hueShift.value          = hueShift;
                ca.saturation.value        = saturation;
            }

            // ── Shadows Midtones Highlights ───────────────────────────
            if (profile.TryGet<ShadowsMidtonesHighlights>(out var smh) ||
                profile.Add<ShadowsMidtonesHighlights>(out smh))
            {
                smh.active              = true;
                smh.shadows.value       = shadowsColor;
                smh.midtones.value      = midtonesColor;
                smh.highlights.value    = highlightsColor;
            }

            // ── Film Grain ────────────────────────────────────────────
            if (profile.TryGet<FilmGrain>(out var grain) || profile.Add<FilmGrain>(out grain))
            {
                grain.active          = true;
                grain.type.value      = FilmGrainLookup.Medium1;
                grain.intensity.value = grainIntensity;
                grain.response.value  = grainSize;
            }

            // ── Chromatic Aberration ──────────────────────────────────
            if (profile.TryGet<ChromaticAberration>(out var ca2) ||
                profile.Add<ChromaticAberration>(out ca2))
            {
                ca2.active           = true;
                ca2.intensity.value  = chromaticAberation;
            }

            Debug.Log("[ShadowLabVolume] Post-processing profile applied.");
        }
    }
}
