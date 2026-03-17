using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace ShadowLab.Physics
{
    /// <summary>
    /// Master controller for all physics-based hand interactions.
    /// Wraps XR Interaction Toolkit with Shadow Lab-specific behavior:
    /// weight simulation, two-handed grabs, surface resistance.
    /// Attach one per hand controller.
    /// </summary>
    public class PhysicsInteractionController : MonoBehaviour
    {
        [Header("Hand")]
        [SerializeField] private XRController    xrController;
        [SerializeField] private XRDirectInteractor interactor;
        [SerializeField] private Transform       handTransform;
        [SerializeField] private bool            isRightHand = true;

        [Header("Haptics")]
        [SerializeField] private float baseHapticAmplitude = 0.3f;
        [SerializeField] private float maxHapticAmplitude  = 1.0f;

        [Header("Throw")]
        [SerializeField] private float throwMultiplier = 1.4f;
        [SerializeField] private int   velocitySampleCount = 5;

        private Queue<Vector3>   _velocitySamples = new Queue<Vector3>();
        private Vector3          _prevHandPos;
        private IXRGrabInteractable _heldObject;
        private float            _heldObjectMass = 1f;

        private void OnEnable()
        {
            interactor.selectEntered.AddListener(OnGrab);
            interactor.selectExited.AddListener(OnRelease);
        }

        private void OnDisable()
        {
            interactor.selectEntered.RemoveListener(OnGrab);
            interactor.selectExited.RemoveListener(OnRelease);
        }

        private void Update()
        {
            // Track hand velocity for throw calculation
            Vector3 vel = (handTransform.position - _prevHandPos) / Time.deltaTime;
            _velocitySamples.Enqueue(vel);
            if (_velocitySamples.Count > velocitySampleCount)
                _velocitySamples.Dequeue();
            _prevHandPos = handTransform.position;

            // Weight-based haptic feedback when holding heavy objects
            if (_heldObject != null)
            {
                float weightFeedback = Mathf.Clamp01(_heldObjectMass / 10f) * 0.15f;
                if (weightFeedback > 0.05f)
                    SendHaptic(weightFeedback, 0.05f);
            }
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            _heldObject = args.interactableObject;

            // Get mass if the object has a Rigidbody
            if (args.interactableObject is MonoBehaviour mb)
            {
                var rb = mb.GetComponent<Rigidbody>();
                _heldObjectMass = rb ? rb.mass : 1f;
            }

            // Haptic pulse on pickup — stronger for heavier objects
            float hapticStrength = Mathf.Lerp(0.2f, 0.8f, Mathf.Clamp01(_heldObjectMass / 8f));
            SendHaptic(hapticStrength, 0.12f);

            // Notify the interactable about being grabbed
            if (args.interactableObject is ILabInteractable labItem)
                labItem.OnGrabbed(this);
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            // Calculate throw velocity from recent samples
            Vector3 avgVelocity = Vector3.zero;
            foreach (var v in _velocitySamples) avgVelocity += v;
            if (_velocitySamples.Count > 0)
                avgVelocity /= _velocitySamples.Count;

            if (args.interactableObject is MonoBehaviour mb)
            {
                var rb = mb.GetComponent<Rigidbody>();
                if (rb) rb.linearVelocity = avgVelocity * throwMultiplier;
            }

            if (args.interactableObject is ILabInteractable labItem)
                labItem.OnReleased(this);

            SendHaptic(0.1f, 0.05f);
            _heldObject = null;
            _heldObjectMass = 1f;
        }

        public void SendHaptic(float amplitude, float duration)
        {
            xrController?.SendHapticImpulse(
                Mathf.Clamp(amplitude, 0f, maxHapticAmplitude), duration);
        }

        public void SendHapticImpact(float mass, float impactForce)
        {
            float strength = Mathf.Clamp01((mass * impactForce) / 20f) * maxHapticAmplitude;
            SendHaptic(strength, 0.08f);
        }
    }

    /// <summary>Interface for all interactive Shadow Lab objects.</summary>
    public interface ILabInteractable
    {
        void OnGrabbed(PhysicsInteractionController hand);
        void OnReleased(PhysicsInteractionController hand);
        void OnUsed(PhysicsInteractionController hand);
    }
}
