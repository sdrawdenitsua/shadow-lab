using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ShadowLab.Audio;

namespace ShadowLab.Physics
{
    /// <summary>
    /// Dornier warp tension knob / lever.
    ///
    /// MOVEMENT: Constrained on local Y-axis only (up = increase, down = decrease).
    /// OUTPUT:   NormalizedTension (0.0 – 1.0) + GramsTension (0g – 120g).
    /// FEEDBACK: Haptic click at every 5g step. Colour indicator (green/amber/red).
    /// ALERT:    Fires TensionOutOfSpec event — NovaBrain listens and comments.
    ///
    /// Attach to the knob/lever root. The XRGrabInteractable is on a child
    /// "GrabPoint" so the knob rotates while the handle position stays fixed.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class LoomTensionMechanism : MonoBehaviour, ILabInteractable
    {
        // ── Inspector ─────────────────────────────────────────────────
        [Header("Tension Range")]
        [SerializeField] private float minGrams     =   0f;
        [SerializeField] private float maxGrams     = 120f;
        [SerializeField] private float defaultGrams =  48f;

        [Header("Spec Window (green zone)")]
        [SerializeField] private float specLow  = 38f;
        [SerializeField] private float specHigh = 58f;

        [Header("Y-Axis Constraint")]
        [SerializeField] private float yTravelMeters = 0.12f;   // how far hand can move up/down
        [SerializeField] private float sensitivity    = 1.0f;   // multiplier

        [Header("Knob Visual")]
        [SerializeField] private Transform knobMesh;            // rotates as tension changes
        [SerializeField] private float     knobRotRange = 270f; // degrees full range

        [Header("Indicator")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private int       indicatorMatIdx = 0;

        [Header("SFX")]
        [SerializeField] private LabAudioSource audioSrc;
        [SerializeField] private AudioClip      clickClip;

        // ── Events ────────────────────────────────────────────────────
        public event System.Action<float> OnTensionChanged;    // grams
        public event System.Action        OnOutOfSpec;
        public event System.Action        OnBackInSpec;

        // ── Properties ────────────────────────────────────────────────
        public float NormalizedTension => (_currentGrams - minGrams) / (maxGrams - minGrams);
        public float GramsTension      => _currentGrams;
        public bool  InSpec            => _currentGrams >= specLow && _currentGrams <= specHigh;

        // ── Runtime ───────────────────────────────────────────────────
        private float  _currentGrams;
        private bool   _held;
        private float  _tensionAtGrab;
        private float  _handYAtGrab;
        private bool   _wasInSpec = true;
        private int    _lastClickStep;
        private PhysicsInteractionController _activeHand;
        private Material _indicatorMat;

        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly Color COL_GREEN   = new Color(0.15f, 0.85f, 0.20f);
        private static readonly Color COL_AMBER   = new Color(1.00f, 0.68f, 0.00f);
        private static readonly Color COL_RED     = new Color(1.00f, 0.10f, 0.10f);

        private void Awake()
        {
            _currentGrams = Mathf.Clamp(defaultGrams, minGrams, maxGrams);

            if (indicatorRenderer)
                _indicatorMat = new Material(indicatorRenderer.sharedMaterials[indicatorMatIdx]);

            RefreshVisuals();
        }

        // ── ILabInteractable ──────────────────────────────────────────

        public void OnGrabbed(PhysicsInteractionController hand)
        {
            _held         = true;
            _activeHand   = hand;
            _tensionAtGrab= _currentGrams;
            _handYAtGrab  = hand.transform.position.y;
        }

        public void OnReleased(PhysicsInteractionController hand)
        {
            _held       = false;
            _activeHand = null;
        }

        public void OnUsed(PhysicsInteractionController hand) { }

        // ── Update ────────────────────────────────────────────────────

        private void Update()
        {
            if (!_held || _activeHand == null) return;

            float deltaY    = _activeHand.transform.position.y - _handYAtGrab;
            float deltaGrams= (deltaY / yTravelMeters) * (maxGrams - minGrams) * sensitivity;
            float newGrams  = Mathf.Clamp(_tensionAtGrab + deltaGrams, minGrams, maxGrams);

            if (Mathf.Abs(newGrams - _currentGrams) < 0.01f) return;

            // Click haptic every 5g step
            int step = Mathf.FloorToInt(newGrams / 5f);
            if (step != _lastClickStep)
            {
                _lastClickStep = step;
                audioSrc?.PlayOneShot(clickClip);
                float pullForce = Mathf.Abs(deltaGrams) / (maxGrams - minGrams);
                _activeHand.ResistanceHaptic(pullForce);
            }

            bool wasSpec = InSpec;
            _currentGrams = newGrams;
            RefreshVisuals();
            OnTensionChanged?.Invoke(_currentGrams);

            // Spec change events
            if (wasSpec && !InSpec)  { _wasInSpec = false; OnOutOfSpec?.Invoke(); }
            if (!wasSpec && InSpec)  { _wasInSpec = true;  OnBackInSpec?.Invoke(); }
        }

        // ── Visuals ───────────────────────────────────────────────────

        private void RefreshVisuals()
        {
            // Knob rotation
            if (knobMesh)
            {
                float angle = Mathf.Lerp(-knobRotRange * 0.5f, knobRotRange * 0.5f, NormalizedTension);
                knobMesh.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            // Indicator colour
            if (_indicatorMat)
            {
                Color c = InSpec ? COL_GREEN :
                          (Mathf.Abs(_currentGrams - (specLow + specHigh) * 0.5f) < 20f ? COL_AMBER : COL_RED);
                _indicatorMat.SetColor(BASE_COLOR_ID, c);
            }
        }

        private void OnDestroy()
        {
            if (_indicatorMat) Destroy(_indicatorMat);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position,
                            transform.position + Vector3.up * yTravelMeters);
            Gizmos.DrawLine(transform.position,
                            transform.position - Vector3.up * yTravelMeters);
        }
    }
}
