using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ShadowLab.Gemini;
using ShadowLab.UI;
using ShadowLab.Audio;

namespace ShadowLab.Nova
{
    /// <summary>
    /// Nova's central brain. Connects Gemini, memory, voice, Violet Eye, and body language.
    /// Attach to the NovaNPC root GameObject.
    /// </summary>
    [RequireComponent(typeof(GeminiClient))]
    [RequireComponent(typeof(AetherLog))]
    [RequireComponent(typeof(AudioSource))]
    public class NovaBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NovaBody         body;
        [SerializeField] private VioletEyeEmitter violetEye;
        [SerializeField] private NovaDialogueHUD  dialogueHUD;
        [SerializeField] private NovaVoice        voice;
        [SerializeField] private AmbientAudioManager ambientAudio;

        [Header("Proximity")]
        [SerializeField] private float awarenessRadius   = 4f;
        [SerializeField] private float conversationRadius= 2f;

        [Header("Idle lines — Nova says these unprompted")]
        [TextArea(2,4)]
        [SerializeField] private string[] idleQuips = {
            "Tension's been drifting on that rapier head, Chief. Worth a look.",
            "I've been running numbers on the 528 resonance. Something's locking in.",
            "That oil bath is reading hot. Not critical, but I'm watching it.",
            "You ever notice how the loom sounds different right before it goes down? Almost like it's asking for help.",
            "The Z71's in the lot. Just saying.",
        };

        private GeminiClient _gemini;
        private AetherLog    _aether;
        private Transform    _playerHead;
        private bool         _isThinking;
        private bool         _isSpeaking;
        private float        _idleTimer;
        private float        _idleInterval = 45f;

        private void Awake()
        {
            _gemini = GetComponent<GeminiClient>();
            _aether = GetComponent<AetherLog>();

            _gemini.OnResponseReceived += HandleResponse;
            _gemini.OnThinking         += HandleThinking;
            _gemini.OnError            += HandleError;
        }

        private void Start()
        {
            // Find player head (XR camera)
            var cam = Camera.main;
            if (cam) _playerHead = cam.transform;

            _idleTimer = Random.Range(20f, _idleInterval);
            StartCoroutine(ProximityLoop());
        }

        private void Update()
        {
            if (_playerHead == null) return;

            // Idle quips
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f && !_isThinking && !_isSpeaking)
            {
                _idleTimer = _idleInterval + Random.Range(-10f, 10f);
                float dist = Vector3.Distance(transform.position, _playerHead.position);
                if (dist < awarenessRadius)
                    SayIdle();
            }
        }

        // ── Public API ───────────────────────────────

        /// <summary>Called when Chief presses the Call Nova button.</summary>
        public void ChiefSays(string message)
        {
            if (_isThinking) return;
            dialogueHUD?.ShowChiefMessage(message);
            _gemini.SendMessage(message, _aether.GetRecentMemory(15));
        }

        /// <summary>Called by voice recognition system.</summary>
        public void ChiefSaysVoice(string transcription) => ChiefSays(transcription);

        // ── Private handlers ─────────────────────────

        private void HandleThinking()
        {
            _isThinking = true;
            violetEye?.SetState(VioletEyeState.Thinking);
            body?.SetBodyLanguage(NovaBodyState.Listening);
            dialogueHUD?.ShowThinking();
        }

        private void HandleResponse(string text)
        {
            _isThinking = false;
            _isSpeaking = true;

            // Save to Aether log
            string lastChief = dialogueHUD?.LastChiefMessage ?? "...";
            _aether.RecordExchange(lastChief, text);

            violetEye?.SetState(VioletEyeState.Speaking);
            body?.SetBodyLanguage(NovaBodyState.Speaking);
            dialogueHUD?.ShowNovaMessage(text);
            voice?.Speak(text, OnSpeechComplete);
        }

        private void HandleError(string error)
        {
            _isThinking = false;
            dialogueHUD?.ShowNovaMessage(error);
            violetEye?.SetState(VioletEyeState.Idle);
            body?.SetBodyLanguage(NovaBodyState.Idle);
        }

        private void OnSpeechComplete()
        {
            _isSpeaking = false;
            violetEye?.SetState(VioletEyeState.Idle);
            body?.SetBodyLanguage(NovaBodyState.Idle);
        }

        private void SayIdle()
        {
            string quip = idleQuips[Random.Range(0, idleQuips.Length)];
            _isSpeaking = true;
            dialogueHUD?.ShowNovaMessage(quip);
            voice?.Speak(quip, OnSpeechComplete);
            violetEye?.SetState(VioletEyeState.Speaking);
            body?.SetBodyLanguage(NovaBodyState.Speaking);
        }

        private IEnumerator ProximityLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (_playerHead == null) continue;
                float dist = Vector3.Distance(transform.position, _playerHead.position);
                body?.SetAwarenessLevel(dist < conversationRadius ? 1f : dist < awarenessRadius ? 0.5f : 0f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, awarenessRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, conversationRadius);
        }
    }
}
