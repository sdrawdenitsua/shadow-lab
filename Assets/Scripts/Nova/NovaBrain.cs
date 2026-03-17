using System.Collections;
using UnityEngine;
using ShadowLab.Gemini;
using ShadowLab.Audio;

namespace ShadowLab.Nova
{
    // ── State machine ────────────────────────────────────────────────
    public enum NovaState { IDLE, LISTENING, THINKING, SPEAKING, FIXING }

    /// <summary>
    /// Nova's full state machine brain.
    /// States:  IDLE → LISTENING → THINKING → SPEAKING → IDLE
    ///          IDLE → FIXING (when she walks to a machine)
    ///
    /// All Animator parameters driven here:
    ///   bool  isIdle, isListening, isThinking, isSpeaking, isFixing
    ///   float awarenessBlend  (0=unaware, 1=full)
    ///   float speakIntensity  (driven by TTS amplitude)
    /// </summary>
    [RequireComponent(typeof(GeminiClient))]
    [RequireComponent(typeof(AetherLog))]
    public class NovaBrain : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────
        [Header("Components")]
        [SerializeField] private Animator            animator;
        [SerializeField] private NovaBody            body;
        [SerializeField] private VioletEyeEmitter    violetEye;
        [SerializeField] private NovaVoice           voice;
        [SerializeField] private NovaDialogueHUD     hud;
        [SerializeField] private AmbientAudioManager ambient;

        [Header("Proximity")]
        [SerializeField] private float awarenessRadius    = 5f;
        [SerializeField] private float talkRadius         = 2.2f;

        [Header("Idle Lines")]
        [TextArea(2, 4)]
        [SerializeField] private string[] idleQuips =
        {
            "Tension's drifting on that rapier head, Chief. Worth checking before the next pass.",
            "I've been running the 528 numbers. Something's locking in around sector 3.",
            "That mineral bath is reading 61 degrees. Not critical — but I'm watching it.",
            "You ever notice how the loom changes its sound right before it goes down?",
            "The Z71 is in the lot. Just saying.",
            "3-6-9. Every pattern on this floor follows it if you know where to look.",
            "Dobby cam on 22 is wearing uneven. Five thousand more picks, maybe.",
        };

        [Header("Timing")]
        [SerializeField] private float idleQuipInterval = 50f;

        // ── Animator hashes ──────────────────────────────────────────
        private static readonly int H_IsIdle       = Animator.StringToHash("isIdle");
        private static readonly int H_IsListening  = Animator.StringToHash("isListening");
        private static readonly int H_IsThinking   = Animator.StringToHash("isThinking");
        private static readonly int H_IsSpeaking   = Animator.StringToHash("isSpeaking");
        private static readonly int H_IsFixing     = Animator.StringToHash("isFixing");
        private static readonly int H_AwarenessBlend = Animator.StringToHash("awarenessBlend");
        private static readonly int H_SpeakIntensity = Animator.StringToHash("speakIntensity");

        // ── Runtime ──────────────────────────────────────────────────
        private GeminiClient _gemini;
        private AetherLog    _aether;
        private Transform    _playerHead;
        private NovaState    _state = NovaState.IDLE;
        private float        _idleTimer;
        private string       _streamBuffer;
        private string       _lastChiefMsg;

        // ── Properties ───────────────────────────────────────────────
        public NovaState State => _state;

        // ── Unity lifecycle ──────────────────────────────────────────
        private void Awake()
        {
            _gemini = GetComponent<GeminiClient>();
            _aether = GetComponent<AetherLog>();

            _gemini.OnStreamStart    += HandleStreamStart;
            _gemini.OnStreamChunk    += HandleStreamChunk;
            _gemini.OnStreamComplete += HandleStreamComplete;
            _gemini.OnError          += HandleError;
        }

        private void Start()
        {
            _playerHead = Camera.main?.transform;
            _idleTimer  = Random.Range(idleQuipInterval * 0.5f, idleQuipInterval);
            TransitionTo(NovaState.IDLE);
            StartCoroutine(ProximityLoop());
        }

        private void Update()
        {
            TickIdleQuip();
        }

        // ── Public entry points ──────────────────────────────────────

        /// <summary>Chief pressed the Call Nova button.</summary>
        public void ChiefSays(string message)
        {
            if (_state == NovaState.THINKING || _state == NovaState.SPEAKING) return;
            _lastChiefMsg = message;
            hud?.ShowChief(message);
            TransitionTo(NovaState.LISTENING);

            // Short beat so Nova "turns" before thinking
            StartCoroutine(DelayedThink(0.6f, message));
        }

        /// <summary>Trigger Nova to walk to a machine and work.</summary>
        public void StartFixing()  => TransitionTo(NovaState.FIXING);
        public void StopFixing()   => TransitionTo(NovaState.IDLE);

        // ── State machine ────────────────────────────────────────────

        private void TransitionTo(NovaState next)
        {
            if (_state == next) return;
            _state = next;

            // Clear all bool params first
            if (animator)
            {
                animator.SetBool(H_IsIdle,      false);
                animator.SetBool(H_IsListening, false);
                animator.SetBool(H_IsThinking,  false);
                animator.SetBool(H_IsSpeaking,  false);
                animator.SetBool(H_IsFixing,    false);
            }

            switch (next)
            {
                case NovaState.IDLE:
                    animator?.SetBool(H_IsIdle, true);
                    violetEye?.SetState(VioletEyeState.Idle);
                    body?.SetPosture(NovaBodyState.Idle);
                    ambient?.SetNovaSpeaking(false);
                    break;

                case NovaState.LISTENING:
                    animator?.SetBool(H_IsListening, true);
                    violetEye?.SetState(VioletEyeState.Idle);
                    body?.SetPosture(NovaBodyState.Listening);
                    break;

                case NovaState.THINKING:
                    animator?.SetBool(H_IsThinking, true);
                    violetEye?.SetState(VioletEyeState.Thinking);
                    body?.SetPosture(NovaBodyState.Listening);
                    hud?.ShowThinking();
                    break;

                case NovaState.SPEAKING:
                    animator?.SetBool(H_IsSpeaking, true);
                    violetEye?.SetState(VioletEyeState.Speaking);
                    body?.SetPosture(NovaBodyState.Speaking);
                    ambient?.SetNovaSpeaking(true);
                    break;

                case NovaState.FIXING:
                    animator?.SetBool(H_IsFixing, true);
                    violetEye?.SetState(VioletEyeState.Alert);
                    body?.SetPosture(NovaBodyState.Working);
                    break;
            }
        }

        // ── Gemini event handlers ────────────────────────────────────

        private void HandleStreamStart()
        {
            _streamBuffer = "";
            TransitionTo(NovaState.SPEAKING);
            hud?.ClearNova();
        }

        private void HandleStreamChunk(string chunk)
        {
            _streamBuffer += chunk;
            hud?.AppendNova(chunk);                     // typewriter fed by stream
            animator?.SetFloat(H_SpeakIntensity,
                Mathf.Lerp(animator.GetFloat(H_SpeakIntensity),
                           Random.Range(0.4f, 1f), 0.35f));
        }

        private void HandleStreamComplete(string fullText)
        {
            _aether.RecordExchange(_lastChiefMsg, fullText);
            voice?.Speak(fullText, OnVoiceComplete);
        }

        private void HandleError(string err)
        {
            hud?.ShowNova(err);
            TransitionTo(NovaState.IDLE);
        }

        private void OnVoiceComplete()
        {
            animator?.SetFloat(H_SpeakIntensity, 0f);
            TransitionTo(NovaState.IDLE);
        }

        // ── Helpers ──────────────────────────────────────────────────

        private IEnumerator DelayedThink(float delay, string message)
        {
            yield return new WaitForSeconds(delay);
            TransitionTo(NovaState.THINKING);
            _gemini.Send(message, _aether.GetRecentMemory(12));
        }

        private void TickIdleQuip()
        {
            if (_state != NovaState.IDLE) return;
            _idleTimer -= Time.deltaTime;
            if (_idleTimer > 0f) return;
            _idleTimer = idleQuipInterval + Random.Range(-12f, 12f);

            if (_playerHead != null &&
                Vector3.Distance(transform.position, _playerHead.position) > awarenessRadius) return;

            string quip = idleQuips[Random.Range(0, idleQuips.Length)];
            hud?.ShowNova(quip);
            voice?.Speak(quip, null);
            TransitionTo(NovaState.SPEAKING);
            StartCoroutine(ReturnToIdleAfter(quip.Split(' ').Length * 0.4f + 1f));
        }

        private IEnumerator ReturnToIdleAfter(float sec)
        {
            yield return new WaitForSeconds(sec);
            if (_state == NovaState.SPEAKING) TransitionTo(NovaState.IDLE);
        }

        private IEnumerator ProximityLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.4f);
                if (_playerHead == null) continue;
                float dist  = Vector3.Distance(transform.position, _playerHead.position);
                float blend = dist < talkRadius ? 1f : dist < awarenessRadius ? 0.5f : 0f;
                body?.SetAwareness(blend);
                animator?.SetFloat(H_AwarenessBlend,
                    Mathf.Lerp(animator.GetFloat(H_AwarenessBlend), blend, 0.15f));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, awarenessRadius);
            Gizmos.color = Color.cyan;    Gizmos.DrawWireSphere(transform.position, talkRadius);
        }
    }
}
