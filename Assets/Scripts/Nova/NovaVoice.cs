using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ShadowLab.Nova
{
    /// <summary>
    /// Nova's voice system.
    /// Primary: Android TTS via plugin bridge.
    /// Fallback: subtitles-only mode (always works).
    /// Optional: Wire a cloud TTS (ElevenLabs/Google) by replacing the SynthesizeSpeech coroutine.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NovaVoice : MonoBehaviour
    {
        [Header("Voice Settings")]
        [SerializeField] private float pitchBase  = 0.92f;   // Slightly lower = more authoritative
        [SerializeField] private float rateBase   = 0.9f;    // Slightly slower = deliberate
        [SerializeField] private float volume     = 1f;

        [Header("Optional Cloud TTS")]
        [SerializeField] private bool  useCloudTTS   = false;
        [SerializeField] private string cloudTTSApiKey = "";  // Google Cloud TTS or ElevenLabs

        private AudioSource _audioSource;
        private Action      _onComplete;
        private bool        _speaking;

        private void Awake() => _audioSource = GetComponent<AudioSource>();

        public void Speak(string text, Action onComplete = null)
        {
            _onComplete = onComplete;
            if (_speaking) StopAllCoroutines();

            if (useCloudTTS && !string.IsNullOrEmpty(cloudTTSApiKey))
                StartCoroutine(CloudTTS(text));
            else
                StartCoroutine(AndroidTTS(text));
        }

        public void StopSpeaking()
        {
            StopAllCoroutines();
            _audioSource.Stop();
            _speaking = false;
        }

        // ── Android TTS (on-device, no API key needed) ──────────────
        private IEnumerator AndroidTTS(string text)
        {
            _speaking = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            using var tts = new AndroidJavaObject("android.speech.tts.TextToSpeech",
                AndroidJavaClass.Find("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"),
                null);

            // Give TTS a frame to initialize
            yield return new WaitForSeconds(0.3f);

            tts.Call<int>("setSpeechRate", rateBase);
            tts.Call<int>("setPitch", pitchBase);
            tts.Call<int>("speak", text, 0, null, "nova_utterance");

            // Wait for speech to complete (approximate by word count)
            float duration = Mathf.Max(1.5f, text.Split(' ').Length * 0.38f);
            yield return new WaitForSeconds(duration);
            tts.Call<int>("stop");
#else
            // Editor / non-Android: just wait (subtitles handle it)
            float duration = Mathf.Max(1.5f, text.Split(' ').Length * 0.38f);
            yield return new WaitForSeconds(duration);
#endif
            _speaking = false;
            _onComplete?.Invoke();
        }

        // ── Google Cloud TTS (optional, high-quality) ───────────────
        private IEnumerator CloudTTS(string text)
        {
            _speaking = true;
            string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={cloudTTSApiKey}";

            string body = JsonUtility.ToJson(new GoogleTTSRequest
            {
                input = new TTSInput { text = text },
                voice = new TTSVoice { languageCode = "en-US", name = "en-US-Neural2-F", ssmlGender = "FEMALE" },
                audioConfig = new TTSAudioConfig { audioEncoding = "LINEAR16", speakingRate = rateBase, pitch = -2.0f }
            });

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonUtility.FromJson<GoogleTTSResponse>(req.downloadHandler.text);
                byte[] audioBytes = Convert.FromBase64String(resp.audioContent);
                AudioClip clip = WavUtility.ToAudioClip(audioBytes, "nova_speech");
                if (clip != null)
                {
                    _audioSource.clip = clip;
                    _audioSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }
            }
            else
            {
                Debug.LogWarning("[NovaVoice] Cloud TTS failed, falling back to Android TTS");
                yield return StartCoroutine(AndroidTTS(text));
                yield break;
            }

            _speaking = false;
            _onComplete?.Invoke();
        }

        [Serializable] class GoogleTTSRequest  { public TTSInput input; public TTSVoice voice; public TTSAudioConfig audioConfig; }
        [Serializable] class TTSInput          { public string text; }
        [Serializable] class TTSVoice          { public string languageCode; public string name; public string ssmlGender; }
        [Serializable] class TTSAudioConfig    { public string audioEncoding; public float speakingRate; public float pitch; }
        [Serializable] class GoogleTTSResponse { public string audioContent; }
    }
}
