using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ShadowLab.Physics
{
    /// <summary>
    /// Hand physics controller for Quest 3S.
    ///
    /// Wraps XRDirectInteractor with:
    ///   • Weight-based haptic feedback on grab (OnSelectEntered)
    ///   • Continuous hold rumble proportional to Rigidbody.mass
    ///   • Throw velocity calculated from a rolling average of hand velocities
    ///   • Surface resistance haptic on slow-drag (for knobs, levers)
    ///   • Impact haptic on collision (routed from grabbed object)
    /// </summary>
    [RequireComponent(typeof(XRController))]
    [RequireComponent(typeof(XRDirectInteractor))]
    public class PhysicsInteractionController : MonoBehaviour
    {
        [Header("Hand")]
        [SerializeField] private bool isRightHand = true;

        [Header("Haptics")]
        [SerializeField] private float grabHapticBase    = 0.25f;  // minimum on any grab
        [SerializeField] private float grabHapticPerKg   = 0.07f;  // added per kg of mass
        [SerializeField] private float holdHapticPerKg   = 0.018f; // continuous hold rumble per kg
        [SerializeField] private float maxHapticAmplitude= 1.0f;

        [Header("Throw")]
        [SerializeField] private int   velocitySamples   = 6;
        [SerializeField] private float throwMultiplier   = 1.35f;
        [SerializeField] private float angularThrowMult  = 0.9f;

        // ── Runtime ──────────────────────────────────────────────────
        private XRController       _controller;
        private XRDirectInteractor _interactor;

        private IXRSelectInteractable _held;
        private Rigidbody             _heldRb;
        private float                 _heldMass;

        private readonly Queue<Vector3> _velSamples  = new();
        private readonly Queue<Vector3> _angSamples  = new();
        private Vector3 _prevPos;
        private Vector3 _prevRot;

        // ── Lifecycle ─────────────────────────────────────────────────

        private void Awake()
        {
            _controller = GetComponent<XRController>();
            _interactor = GetComponent<XRDirectInteractor>();
        }

        private void OnEnable()
        {
            _interactor.selectEntered.AddListener(OnSelectEntered);
            _interactor.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            _interactor.selectEntered.RemoveListener(OnSelectEntered);
            _interactor.selectExited.RemoveListener(OnSelectExited);
        }

        private void Update()
        {
            SampleVelocity();
            HoldHaptics();
        }

        // ── Grab / Release ────────────────────────────────────────────

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            _held  = args.interactableObject;
            _heldRb = (_held as MonoBehaviour)?.GetComponent<Rigidbody>();
            _heldMass = _heldRb ? _heldRb.mass : 0.5f;

            // Haptic: base + mass-scaled impulse
            float amp = Mathf.Clamp(grabHapticBase + _heldMass * grabHapticPerKg, 0f, maxHapticAmplitude);
            Haptic(amp, 0.14f);

            // Notify ILabInteractable
            (_held as ILabInteractable)?.OnGrabbed(this);

            _velSamples.Clear();
            _angSamples.Clear();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            // Apply throw velocity
            if (_heldRb != null)
            {
                _heldRb.linearVelocity        = AverageVelocity(_velSamples) * throwMultiplier;
                _heldRb.angularVelocity = AverageVelocity(_angSamples) * angularThrowMult;
            }

            (_held as ILabInteractable)?.OnReleased(this);
            Haptic(0.08f, 0.05f);

            _held     = null;
            _heldRb   = null;
            _heldMass = 0f;
        }

        // ── Haptics ───────────────────────────────────────────────────

        public void Haptic(float amplitude, float duration)
        {
            _controller?.SendHapticImpulse(
                Mathf.Clamp(amplitude, 0f, maxHapticAmplitude), duration);
        }

        /// <summary>
        /// Call this from LoomTensionMechanism when the knob moves.
        /// Gives resistance feel proportional to how hard the player is pulling.
        /// </summary>
        public void ResistanceHaptic(float normalizedForce)
        {
            float amp = Mathf.Clamp01(normalizedForce) * 0.55f;
            Haptic(amp, 0.03f);
        }

        /// <summary>
        /// Call from collision callbacks on held objects.
        /// </summary>
        public void ImpactHaptic(float impulseMagnitude)
        {
            float amp = Mathf.Clamp01(impulseMagnitude / 15f) * maxHapticAmplitude;
            Haptic(amp, 0.08f);
        }

        // ── Velocity tracking ─────────────────────────────────────────

        private void SampleVelocity()
        {
            Vector3 vel = (transform.position    - _prevPos) / Time.deltaTime;
            Vector3 ang = (transform.eulerAngles - _prevRot) / Time.deltaTime;

            _velSamples.Enqueue(vel);
            _angSamples.Enqueue(ang);

            while (_velSamples.Count > velocitySamples) _velSamples.Dequeue();
            while (_angSamples.Count > velocitySamples) _angSamples.Dequeue();

            _prevPos = transform.position;
            _prevRot = transform.eulerAngles;
        }

        private void HoldHaptics()
        {
            if (_held == null || _heldMass < 2f) return;
            // Subtle continuous rumble for heavy objects
            float amp = Mathf.Clamp(_heldMass * holdHapticPerKg, 0f, 0.22f);
            if (Random.value < Time.deltaTime * 8f) // ~8 impulses per second
                Haptic(amp, 0.06f);
        }

        private static Vector3 AverageVelocity(Queue<Vector3> samples)
        {
            Vector3 sum = Vector3.zero;
            foreach (var v in samples) sum += v;
            return samples.Count > 0 ? sum / samples.Count : Vector3.zero;
        }
    }

    // ── Interface all grabbable lab objects implement ─────────────────
    public interface ILabInteractable
    {
        void OnGrabbed(PhysicsInteractionController hand);
        void OnReleased(PhysicsInteractionController hand);
        void OnUsed(PhysicsInteractionController hand);
    }
}
