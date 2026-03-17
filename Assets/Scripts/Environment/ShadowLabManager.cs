using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ShadowLab.Audio;
using ShadowLab.Nova;

namespace ShadowLab.Environment
{
    /// <summary>
    /// Master scene manager for the Shadow Lab.
    /// Handles: startup sequence, lighting state, zone transitions, nova call button.
    /// Attach to the scene root "ShadowLabManager" GameObject.
    /// </summary>
    public class ShadowLabManager : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private NovaBrain          nova;
        [SerializeField] private AmbientAudioManager ambientAudio;
        [SerializeField] private Light              mainDirectionalLight;
        [SerializeField] private Light[]            violetPointLights;
        [SerializeField] private Light[]            workbenchLights;

        [Header("XR")]
        [SerializeField] private XROrigin           xrOrigin;
        [SerializeField] private ActionBasedController leftController;
        [SerializeField] private ActionBasedController rightController;

        [Header("Startup")]
        [SerializeField] private float              bootSequenceDuration = 3f;
        [SerializeField] private GameObject         bootFadeCanvas;

        [Header("Lighting Settings")]
        [SerializeField] private Color   ambientColor   = new Color(0.04f, 0f, 0.08f);
        [SerializeField] private float   violetIntensity = 1.8f;
        [SerializeField] private float   pulseAmplitude  = 0.3f;
        [SerializeField] private float   pulseSpeed      = 0.4f;

        [Header("Mineral Oil PC")]
        [SerializeField] private GameObject mineralOilPC;
        [SerializeField] private ParticleSystem oilBubbles;
        [SerializeField] private Light          pcGlowLight;

        private float _pulseTime;
        private bool  _booted;

        private void Start()
        {
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fogColor     = new Color(0.04f, 0f, 0.06f);
            RenderSettings.fogDensity   = 0.04f;
            RenderSettings.fog          = true;

            StartCoroutine(BootSequence());
        }

        private void Update()
        {
            if (!_booted) return;
            PulseVioletLights();
        }

        // ── Boot sequence ────────────────────────────────
        private System.Collections.IEnumerator BootSequence()
        {
            // Fade in from black
            if (bootFadeCanvas) bootFadeCanvas.SetActive(true);
            yield return new WaitForSeconds(bootSequenceDuration);
            if (bootFadeCanvas) bootFadeCanvas.SetActive(false);

            _booted = true;
            oilBubbles?.Play();

            // Nova's greeting on startup
            yield return new WaitForSeconds(1.5f);
            nova?.ChiefSays("Nova, I'm in the lab.");
        }

        // ── Violet light pulse ───────────────────────────
        private void PulseVioletLights()
        {
            _pulseTime += Time.deltaTime * pulseSpeed;
            float pulse = violetIntensity + Mathf.Sin(_pulseTime) * pulseAmplitude;

            foreach (var light in violetPointLights)
                if (light) light.intensity = pulse;

            // PC glow — slightly different phase
            if (pcGlowLight)
                pcGlowLight.intensity = (violetIntensity * 0.6f) + Mathf.Sin(_pulseTime * 1.3f) * (pulseAmplitude * 0.5f);
        }

        // ── XR Button bindings (called from XR Input Actions) ────────

        /// <summary>Called when Chief presses A button — summons Nova.</summary>
        public void OnCallNovaButton()
        {
            nova?.ChiefSays("Nova, you there?");
        }

        /// <summary>Called when Chief long-presses B — opens Aether log.</summary>
        public void OnAetherLogButton()
        {
            // TODO: wire to NovaDialogueHUD.ShowAetherLog()
            Debug.Log("[ShadowLab] Aether log requested.");
        }

        /// <summary>Toggle a workbench light on/off.</summary>
        public void ToggleWorkbenchLight(int index)
        {
            if (index >= 0 && index < workbenchLights.Length && workbenchLights[index])
                workbenchLights[index].enabled = !workbenchLights[index].enabled;
        }

        private void OnValidate()
        {
            // Auto-find if not assigned
            if (nova == null) nova = FindFirstObjectByType<NovaBrain>();
            if (ambientAudio == null) ambientAudio = FindFirstObjectByType<AmbientAudioManager>();
        }
    }
}
