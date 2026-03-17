using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ShadowLab.Audio;

namespace ShadowLab.Physics
{
    /// <summary>
    /// Dornier loom tension spring assembly.
    /// Player can grab the tension knob and pull/rotate to adjust warp tension.
    /// Provides haptic feedback proportional to resistance.
    /// Notifies NovaBrain when tension goes out of spec.
    /// </summary>
    public class LoomTensionMechanism : MonoBehaviour, ILabInteractable
    {
        [Header("Tension Settings")]
        [SerializeField] private float minTension      = 0f;
        [SerializeField] private float maxTension      = 100f;
        [SerializeField] private float nominalMin      = 35f;  // green zone
        [SerializeField] private float nominalMax      = 55f;  // green zone
        [SerializeField] private float currentTension  = 45f;

        [Header("Knob")]
        [SerializeField] private Transform knobTransform;
        [SerializeField] private Transform knobPivot;
        [SerializeField] private float     rotationSensitivity = 0.4f;

        [Header("Visual Indicator")]
        [SerializeField] private Renderer tensionIndicatorRenderer;
        [SerializeField] private int       indicatorMaterialIndex = 0;

        [Header("References")]
        [SerializeField] private LabAudioSource audioSource;
        [SerializeField] private AudioClip      tensionClickClip;

        private Material  _indicatorMat;
        private bool      _isHeld;
        private Vector3   _grabStartPos;
        private float     _tensionAtGrab;
        private PhysicsInteractionController _activeHand;

        private static readonly int ColorID = Shader.PropertyToID("_BaseColor");

        // Colors
        private static readonly Color GreenOK   = new Color(0.2f, 0.8f, 0.2f);
        private static readonly Color AmberWarn = new Color(1f,   0.7f, 0f);
        private static readonly Color RedDanger = new Color(1f,   0.1f, 0.1f);

        private void Awake()
        {
            if (tensionIndicatorRenderer)
                _indicatorMat = tensionIndicatorRenderer.materials[indicatorMaterialIndex];
            UpdateVisuals();
        }

        public float CurrentTension => currentTension;
        public bool  InSpec         => currentTension >= nominalMin && currentTension <= nominalMax;

        // ── ILabInteractable ─────────────────────────────
        public void OnGrabbed(PhysicsInteractionController hand)
        {
            _isHeld       = true;
            _activeHand   = hand;
            _grabStartPos = hand.transform.position;
            _tensionAtGrab= currentTension;
        }

        public void OnReleased(PhysicsInteractionController hand)
        {
            _isHeld     = false;
            _activeHand = null;

            if (!InSpec)
                Debug.Log($"[LoomTension] Out of spec: {currentTension:F1}g — Nova should comment.");
        }

        public void OnUsed(PhysicsInteractionController hand) { }

        // ── Update ──────────────────────────────────────
        private void Update()
        {
            if (!_isHeld || _activeHand == null) return;

            // Map vertical hand movement to tension change
            float deltaY   = _activeHand.transform.position.y - _grabStartPos.y;
            float newTension = Mathf.Clamp(
                _tensionAtGrab + deltaY * 100f * rotationSensitivity,
                minTension, maxTension
            );

            if (Mathf.Abs(newTension - currentTension) > 0.5f)
            {
                // Click sound at every 5g increment
                if (Mathf.FloorToInt(newTension / 5f) != Mathf.FloorToInt(currentTension / 5f))
                {
                    audioSource?.PlayOneShot(tensionClickClip);
                    float hapticStr = Mathf.Lerp(0.1f, 0.5f, Mathf.Abs(newTension - currentTension) / 20f);
                    _activeHand.SendHaptic(hapticStr, 0.04f);
                }

                currentTension = newTension;
                UpdateVisuals();
                RotateKnob(deltaY);
            }
        }

        private void UpdateVisuals()
        {
            if (_indicatorMat == null) return;

            Color c;
            if (InSpec)
                c = GreenOK;
            else if (currentTension < nominalMin - 15f || currentTension > nominalMax + 15f)
                c = RedDanger;
            else
                c = AmberWarn;

            _indicatorMat.SetColor(ColorID, c);
        }

        private void RotateKnob(float delta)
        {
            if (knobTransform == null) return;
            float angle = Mathf.Clamp(delta * 180f, -270f, 270f);
            knobTransform.localRotation = Quaternion.Euler(0, angle, 0);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = InSpec ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
        }
    }
}
