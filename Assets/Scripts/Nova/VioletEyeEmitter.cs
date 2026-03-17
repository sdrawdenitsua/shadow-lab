using System.Collections;
using UnityEngine;

namespace ShadowLab.Nova
{
    public enum VioletEyeState { Idle, Thinking, Speaking, Alert }

    /// <summary>
    /// Controls the _EmissionColor property of the VioletEye URP shader at runtime.
    ///
    /// IDLE     → slow sinusoidal pulse  (soft violet, 1.5s period)
    /// THINKING → rapid erratic flicker  (dim violet, mimics processing)
    /// SPEAKING → live amplitude flicker (bright violet, driven by voice)
    /// ALERT    → sharp red pulse        (danger / lockout state)
    ///
    /// Assign both eye Material instances (so we don't mutate the shared asset).
    /// </summary>
    public class VioletEyeEmitter : MonoBehaviour
    {
        [Header("Eye Materials (instanced)")]
        [SerializeField] private Renderer leftEyeRenderer;
        [SerializeField] private Renderer rightEyeRenderer;
        [SerializeField] private int       matIndex = 0;

        // ── Emission colours ─────────────────────────────────────────
        [Header("State Colours")]
        [SerializeField] private Color colIdle     = new Color(0.54f, 0.00f, 1.00f); // #8b00ff
        [SerializeField] private Color colThinking = new Color(0.28f, 0.00f, 0.55f); // dim
        [SerializeField] private Color colSpeaking = new Color(0.75f, 0.30f, 1.00f); // bright
        [SerializeField] private Color colAlert    = new Color(1.00f, 0.12f, 0.12f); // red

        // ── Intensity ranges per state ────────────────────────────────
        [Header("Intensity Ranges")]
        [SerializeField] private Vector2 idleRange     = new Vector2(0.8f,  1.6f);
        [SerializeField] private Vector2 thinkingRange = new Vector2(0.2f,  1.0f);
        [SerializeField] private Vector2 speakingRange = new Vector2(1.5f,  3.2f);
        [SerializeField] private Vector2 alertRange    = new Vector2(1.8f,  3.5f);

        // ── Pulse speeds ──────────────────────────────────────────────
        [Header("Speeds")]
        [SerializeField] private float idlePulseSpeed     = 1.1f;
        [SerializeField] private float thinkFlickerMin    = 0.03f;
        [SerializeField] private float thinkFlickerMax    = 0.09f;
        [SerializeField] private float speakFlickerMin    = 0.03f;
        [SerializeField] private float speakFlickerMax    = 0.11f;
        [SerializeField] private float alertPulseSpeed    = 4.0f;

        // ── Runtime ───────────────────────────────────────────────────
        private Material   _leftMat;
        private Material   _rightMat;
        private Coroutine  _activeRoutine;
        private static readonly int EMISSION_ID = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            // Always instance — never mutate the shared project asset
            _leftMat  = leftEyeRenderer  ? new Material(leftEyeRenderer.sharedMaterials[matIndex])  : null;
            _rightMat = rightEyeRenderer ? new Material(rightEyeRenderer.sharedMaterials[matIndex]) : null;

            if (_leftMat  && leftEyeRenderer)  SetMat(leftEyeRenderer,  _leftMat);
            if (_rightMat && rightEyeRenderer) SetMat(rightEyeRenderer, _rightMat);
        }

        private void Start() => SetState(VioletEyeState.Idle);

        // ── Public API ────────────────────────────────────────────────

        public void SetState(VioletEyeState state)
        {
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);

            _activeRoutine = state switch
            {
                VioletEyeState.Idle     => StartCoroutine(SinePulse   (colIdle,     idleRange,     idlePulseSpeed)),
                VioletEyeState.Thinking => StartCoroutine(ErraticFlicker(colThinking, thinkingRange, thinkFlickerMin, thinkFlickerMax)),
                VioletEyeState.Speaking => StartCoroutine(ErraticFlicker(colSpeaking, speakingRange, speakFlickerMin, speakFlickerMax)),
                VioletEyeState.Alert    => StartCoroutine(SinePulse   (colAlert,    alertRange,    alertPulseSpeed)),
                _                       => null,
            };
        }

        // ── Coroutines ────────────────────────────────────────────────

        /// Smooth sinusoidal pulse — used for IDLE and ALERT.
        private IEnumerator SinePulse(Color baseColor, Vector2 intensityRange, float speed)
        {
            float phase = Random.Range(0f, Mathf.PI * 2f);
            while (true)
            {
                phase += Time.deltaTime * speed * Mathf.PI * 2f;
                float t         = (Mathf.Sin(phase) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(intensityRange.x, intensityRange.y, t);
                Apply(baseColor * intensity);
                yield return null;
            }
        }

        /// Rapid random flicker — used for THINKING and SPEAKING.
        private IEnumerator ErraticFlicker(Color baseColor, Vector2 intensityRange,
                                           float minInterval, float maxInterval)
        {
            while (true)
            {
                float intensity = Random.Range(intensityRange.x, intensityRange.y);

                // Occasionally spike very bright (neural burst)
                if (Random.value < 0.08f) intensity = intensityRange.y * 1.4f;

                Apply(baseColor * intensity);
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            }
        }

        // ── Helpers ───────────────────────────────────────────────────

        private void Apply(Color emission)
        {
            _leftMat?.SetColor(EMISSION_ID,  emission);
            _rightMat?.SetColor(EMISSION_ID, emission);
        }

        private static void SetMat(Renderer r, Material m)
        {
            var mats = r.sharedMaterials;
            mats[0]  = m;
            r.sharedMaterials = mats;
        }

        private void OnDestroy()
        {
            if (_leftMat)  Destroy(_leftMat);
            if (_rightMat) Destroy(_rightMat);
        }
    }
}
