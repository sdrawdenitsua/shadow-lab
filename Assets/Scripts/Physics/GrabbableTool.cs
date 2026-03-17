using UnityEngine;

namespace ShadowLab.Physics
{
    /// <summary>
    /// Base class for all grabbable tools in the Shadow Lab.
    /// Extend this for: wrench, tension gauge, rapier head, yarn carrier, etc.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable))]
    public class GrabbableTool : MonoBehaviour, ILabInteractable
    {
        [Header("Tool Identity")]
        public string toolName = "Unknown Tool";
        public float  massKg   = 0.5f;

        [Header("Impact SFX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   metalImpactClip;
        [SerializeField] private AudioClip   woodImpactClip;
        [SerializeField] private float       impactThreshold = 1.5f;

        protected Rigidbody    _rb;
        protected bool         _isHeld;
        protected PhysicsInteractionController _activeHand;

        protected virtual void Awake()
        {
            _rb      = GetComponent<Rigidbody>();
            _rb.mass = massKg;
        }

        public virtual void OnGrabbed(PhysicsInteractionController hand)
        {
            _isHeld     = true;
            _activeHand = hand;
            _rb.isKinematic = false;
        }

        public virtual void OnReleased(PhysicsInteractionController hand)
        {
            _isHeld     = false;
            _activeHand = null;
        }

        public virtual void OnUsed(PhysicsInteractionController hand) { }

        private void OnCollisionEnter(Collision col)
        {
            if (_isHeld) return;
            float force = col.impulse.magnitude;
            if (force < impactThreshold) return;

            AudioClip clip = col.gameObject.CompareTag("Wood") ? woodImpactClip : metalImpactClip;
            if (clip && audioSource)
            {
                float volume = Mathf.Clamp01(force / 20f);
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }

    // ── Specific tools ──────────────────────────────────────

    public class RapierHead : GrabbableTool
    {
        [Header("Rapier Head")]
        public float tensionRating = 45f;

        protected override void Awake()
        {
            toolName = "Dornier Rapier Head (HTV)";
            massKg   = 1.8f;
            base.Awake();
        }
    }

    public class TensionGauge : GrabbableTool
    {
        [Header("Gauge")]
        [SerializeField] private LoomTensionMechanism targetLoom;
        [SerializeField] private Transform needleTransform;

        protected override void Awake()
        {
            toolName = "Tension Gauge";
            massKg   = 0.3f;
            base.Awake();
        }

        private void Update()
        {
            if (!_isHeld || targetLoom == null || needleTransform == null) return;
            float angle = Mathf.Lerp(-90f, 90f, targetLoom.CurrentTension / 100f);
            needleTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }

    public class WeftYarnCarrier : GrabbableTool
    {
        protected override void Awake()
        {
            toolName = "Weft Yarn Carrier";
            massKg   = 0.12f;
            base.Awake();
        }
    }
}
