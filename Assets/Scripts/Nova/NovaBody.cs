using System.Collections;
using UnityEngine;

namespace ShadowLab.Nova
{
    public enum NovaBodyState { Idle, Listening, Speaking, Working, Startled }

    /// <summary>
    /// Controls Nova's physical body — head tracking, breathing, hand gestures.
    /// Uses Animation Rigging or manual bone transforms.
    /// </summary>
    public class NovaBody : MonoBehaviour
    {
        [Header("Head Look-At")]
        [SerializeField] private Transform headBone;
        [SerializeField] private float headTrackSpeed  = 3f;
        [SerializeField] private float maxHeadAngle    = 60f;

        [Header("Breathing")]
        [SerializeField] private Transform chestBone;
        [SerializeField] private float breathDepth     = 0.008f;
        [SerializeField] private float breathRate      = 0.3f;

        [Header("Awareness")]
        [SerializeField] private Animator animator;

        private Transform _playerHead;
        private float     _awarenessLevel; // 0=unaware, 1=full attention
        private float     _breathTime;

        private static readonly int AwarenessParam  = Animator.StringToHash("Awareness");
        private static readonly int BodyStateParam  = Animator.StringToHash("BodyState");

        private void Start()
        {
            var cam = Camera.main;
            if (cam) _playerHead = cam.transform;
        }

        private void LateUpdate()
        {
            HandleHeadTracking();
            HandleBreathing();
        }

        public void SetAwarenessLevel(float level)
        {
            _awarenessLevel = Mathf.Lerp(_awarenessLevel, level, Time.deltaTime * 2f);
            animator?.SetFloat(AwarenessParam, _awarenessLevel);
        }

        public void SetBodyLanguage(NovaBodyState state)
        {
            animator?.SetInteger(BodyStateParam, (int)state);

            if (state == NovaBodyState.Startled)
                StartCoroutine(ResetStartle());
        }

        private void HandleHeadTracking()
        {
            if (headBone == null || _playerHead == null) return;
            if (_awarenessLevel < 0.1f) return;

            Vector3 targetDir = (_playerHead.position - headBone.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(targetDir);

            // Clamp to max angle
            float angle = Quaternion.Angle(transform.rotation, targetRot);
            if (angle > maxHeadAngle)
            {
                targetRot = Quaternion.RotateTowards(transform.rotation, targetRot, maxHeadAngle);
            }

            headBone.rotation = Quaternion.Slerp(
                headBone.rotation, targetRot,
                Time.deltaTime * headTrackSpeed * _awarenessLevel
            );
        }

        private void HandleBreathing()
        {
            if (chestBone == null) return;
            _breathTime += Time.deltaTime * breathRate * Mathf.PI * 2f;
            float offset = Mathf.Sin(_breathTime) * breathDepth;
            chestBone.localScale = new Vector3(1f, 1f + offset, 1f);
        }

        private IEnumerator ResetStartle()
        {
            yield return new WaitForSeconds(0.5f);
            SetBodyLanguage(NovaBodyState.Idle);
        }
    }
}
