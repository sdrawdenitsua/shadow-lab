using System.Collections;
using UnityEngine;

namespace ShadowLab.Nova
{
    public enum VioletEyeState { Idle, Thinking, Speaking, Alert }

    /// <summary>
    /// Controls Nova's Violet Eye emission shader.
    /// Requires a material with "_EmissionColor" and "_EmissionIntensity" properties.
    /// </summary>
    public class VioletEyeEmitter : MonoBehaviour
    {
        [Header("Eye Renderers")]
        [SerializeField] private Renderer leftEyeRenderer;
        [SerializeField] private Renderer rightEyeRenderer;
        [SerializeField] private int       materialIndex = 0;

        [Header("Colors by State")]
        [SerializeField] private Color idleColor     = new Color(0.54f, 0f,   1f);    // #8b00ff
        [SerializeField] private Color thinkingColor = new Color(0.35f, 0f,   0.65f); // dim violet
        [SerializeField] private Color speakingColor = new Color(0.75f, 0.3f, 1f);    // bright violet
        [SerializeField] private Color alertColor    = new Color(1f,    0.2f, 0.2f);  // red

        [Header("Intensity")]
        [SerializeField] private float idleIntensity     = 1.2f;
        [SerializeField] private float thinkingIntensity = 0.6f;
        [SerializeField] private float speakingIntensity = 2.5f;
        [SerializeField] private float alertIntensity    = 3f;

        [Header("Pulse")]
        [SerializeField] private float pulseSpeed = 2f;

        private Material _leftMat;
        private Material _rightMat;
        private VioletEyeState _currentState = VioletEyeState.Idle;
        private Coroutine _pulseCoroutine;

        private static readonly int EmissionColorID    = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionIntensityID = Shader.PropertyToID("_EmissionIntensity");

        private void Awake()
        {
            // Instance materials so we don't affect shared assets
            if (leftEyeRenderer)
                _leftMat = leftEyeRenderer.materials[materialIndex];
            if (rightEyeRenderer)
                _rightMat = rightEyeRenderer.materials[materialIndex];
        }

        private void Start() => SetState(VioletEyeState.Idle);

        public void SetState(VioletEyeState state)
        {
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _currentState = state;

            switch (state)
            {
                case VioletEyeState.Idle:
                    _pulseCoroutine = StartCoroutine(PulseLoop(idleColor, idleIntensity * 0.7f, idleIntensity * 1.3f, 1.5f));
                    break;
                case VioletEyeState.Thinking:
                    _pulseCoroutine = StartCoroutine(PulseLoop(thinkingColor, thinkingIntensity * 0.3f, thinkingIntensity, pulseSpeed * 2f));
                    break;
                case VioletEyeState.Speaking:
                    _pulseCoroutine = StartCoroutine(SpeakingFlicker(speakingColor, speakingIntensity));
                    break;
                case VioletEyeState.Alert:
                    _pulseCoroutine = StartCoroutine(PulseLoop(alertColor, alertIntensity * 0.5f, alertIntensity, pulseSpeed * 3f));
                    break;
            }
        }

        private IEnumerator PulseLoop(Color color, float minIntensity, float maxIntensity, float speed)
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime * speed;
                float intensity = Mathf.Lerp(minIntensity, maxIntensity, (Mathf.Sin(t) + 1f) * 0.5f);
                ApplyEmission(color * intensity);
                yield return null;
            }
        }

        private IEnumerator SpeakingFlicker(Color color, float intensity)
        {
            while (true)
            {
                float flicker = intensity * (0.85f + Random.Range(0f, 0.3f));
                ApplyEmission(color * flicker);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
            }
        }

        private void ApplyEmission(Color emissionColor)
        {
            _leftMat?.SetColor(EmissionColorID, emissionColor);
            _rightMat?.SetColor(EmissionColorID, emissionColor);
        }

        private void OnDestroy()
        {
            // Clean up instanced materials
            if (_leftMat)  Destroy(_leftMat);
            if (_rightMat) Destroy(_rightMat);
        }
    }
}
