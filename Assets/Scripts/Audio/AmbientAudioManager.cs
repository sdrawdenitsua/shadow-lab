using System.Collections;
using UnityEngine;

namespace ShadowLab.Audio
{
    /// <summary>
    /// Shadow Lab ambient soundscape.
    ///
    ///  LAYER 1 — 528 Hz procedural sine wave (generated in code, no asset needed).
    ///            A pure Solfeggio tone fills the room at low volume.
    ///
    ///  LAYER 2 — Industrial loom rhythm track (AudioClip asset).
    ///            Volume crossfades based on player distance to nearest loom.
    ///
    ///  LAYER 3 — Sparse environmental one-shots:
    ///            metal groans, electrical hum, oil drips, steam.
    ///
    ///  DUCKING  — All layers duck 65% when Nova is speaking.
    /// </summary>
    public class AmbientAudioManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────
        [Header("528 Hz Base Tone")]
        [SerializeField] private AudioSource baseToneSource;
        [SerializeField] [Range(0f, 0.3f)] private float baseToneVolume = 0.10f;

        [Header("Loom Rhythm")]
        [SerializeField] private AudioSource loomRhythmSource;
        [SerializeField] private AudioClip   loomRhythmClip;
        [SerializeField] [Range(0f, 0.5f)] private float loomMaxVolume = 0.32f;
        [SerializeField] private Transform[] loomPositions;
        [SerializeField] private float       loomFadeInDist  = 6f;
        [SerializeField] private float       loomFullVolDist  = 1.8f;

        [Header("Environmental SFX")]
        [SerializeField] private AudioSource envSource;
        [SerializeField] private AudioClip[] metalGroanClips;
        [SerializeField] private AudioClip[] electricalHumClips;
        [SerializeField] private AudioClip   oilDripClip;
        [SerializeField] private float       envMinInterval = 18f;
        [SerializeField] private float       envMaxInterval = 40f;

        [Header("Ducking")]
        [SerializeField] [Range(0f, 1f)] private float duckTo     = 0.3f;
        [SerializeField]                 private float duckTime    = 0.45f;
        [SerializeField]                 private float unduckTime  = 1.2f;

        // ── Runtime ──────────────────────────────────────────────────
        private AudioClip _toneClip;
        private Transform _playerHead;
        private float     _targetBaseVol;
        private float     _targetLoomVol;
        private bool      _ducked;
        private Coroutine _duckCoroutine;

        // ── 528 Hz generation params ─────────────────────────────────
        private const float FREQ       = 528f;
        private const float TONE_SECS  = 4f;   // loop length
        private const int   SAMPLE_RATE= 44100;

        private void Start()
        {
            _playerHead      = Camera.main?.transform;
            _targetBaseVol   = baseToneVolume;

            // Generate the 528Hz sine wave AudioClip in code
            _toneClip = GenerateSineWave(FREQ, TONE_SECS);
            if (baseToneSource)
            {
                baseToneSource.clip        = _toneClip;
                baseToneSource.loop        = true;
                baseToneSource.spatialBlend= 0f;    // 2D — fills entire space
                baseToneSource.volume      = 0f;
                baseToneSource.Play();
                StartCoroutine(FadeVolume(baseToneSource, baseToneVolume, 3f));
            }

            // Loom rhythm
            if (loomRhythmSource && loomRhythmClip)
            {
                loomRhythmSource.clip   = loomRhythmClip;
                loomRhythmSource.loop   = true;
                loomRhythmSource.volume = 0f;
                loomRhythmSource.Play();
            }

            StartCoroutine(LoomProximityLoop());
            StartCoroutine(EnvironmentalEventLoop());
        }

        private void Update()
        {
            // Smooth volume each frame
            if (baseToneSource)
                baseToneSource.volume = Mathf.MoveTowards(
                    baseToneSource.volume, _targetBaseVol, Time.deltaTime * 0.5f);

            if (loomRhythmSource)
                loomRhythmSource.volume = Mathf.MoveTowards(
                    loomRhythmSource.volume, _targetLoomVol, Time.deltaTime * 0.8f);
        }

        // ── Public: Nova state ───────────────────────────────────────

        public void SetNovaSpeaking(bool speaking)
        {
            if (_ducked == speaking) return;
            _ducked = speaking;

            if (_duckCoroutine != null) StopCoroutine(_duckCoroutine);
            _duckCoroutine = StartCoroutine(speaking ? DuckDown() : DuckUp());
        }

        // ── 528 Hz Procedural Generator ──────────────────────────────

        private static AudioClip GenerateSineWave(float freq, float durationSecs)
        {
            int    samples = Mathf.RoundToInt(SAMPLE_RATE * durationSecs);
            float[] data   = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // Pure Solfeggio tone
                float sine = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Subtle 2nd harmonic (1056Hz) for warmth — very low level
                float harm2 = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.06f;

                // Sub-harmonic (264Hz) adds body without muddying the frequency
                float sub   = Mathf.Sin(2f * Mathf.PI * freq * 0.5f * t) * 0.04f;

                data[i] = (sine + harm2 + sub) * 0.85f;
            }

            // Crossfade loop points — 10ms fade at start/end
            int fade = SAMPLE_RATE / 100;
            for (int i = 0; i < fade; i++)
            {
                float f = (float)i / fade;
                data[i]             *= f;
                data[samples - 1 - i] *= f;
            }

            var clip = AudioClip.Create("528Hz_SolfeggioTone", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ── Loom proximity crossfade ─────────────────────────────────

        private IEnumerator LoomProximityLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.25f);
                if (_playerHead == null || loomPositions == null) continue;

                float minDist = float.MaxValue;
                foreach (var lp in loomPositions)
                    if (lp) minDist = Mathf.Min(minDist, Vector3.Distance(_playerHead.position, lp.position));

                float raw = Mathf.InverseLerp(loomFadeInDist, loomFullVolDist, minDist) * loomMaxVolume;
                _targetLoomVol = _ducked ? raw * duckTo : raw;
            }
        }

        // ── Sparse environmental events ──────────────────────────────

        private IEnumerator EnvironmentalEventLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(envMinInterval, envMaxInterval));
                if (envSource == null) continue;

                int roll = Random.Range(0, 4);
                AudioClip pick = null;
                float vol = 0f;

                switch (roll)
                {
                    case 0 when metalGroanClips?.Length > 0:
                        pick = metalGroanClips[Random.Range(0, metalGroanClips.Length)];
                        vol  = 0.38f; break;
                    case 1 when electricalHumClips?.Length > 0:
                        pick = electricalHumClips[Random.Range(0, electricalHumClips.Length)];
                        vol  = 0.28f; break;
                    case 2 when oilDripClip:
                        pick = oilDripClip;
                        vol  = 0.50f; break;
                }

                if (pick) envSource.PlayOneShot(pick, _ducked ? vol * duckTo : vol);
            }
        }

        // ── Duck helpers ─────────────────────────────────────────────

        private IEnumerator DuckDown()
        {
            float start = _targetBaseVol;
            float end   = baseToneVolume * duckTo;
            for (float t = 0; t < duckTime; t += Time.deltaTime)
            {
                _targetBaseVol = Mathf.Lerp(start, end, t / duckTime);
                yield return null;
            }
            _targetBaseVol = end;
        }

        private IEnumerator DuckUp()
        {
            float start = _targetBaseVol;
            float end   = baseToneVolume;
            for (float t = 0; t < unduckTime; t += Time.deltaTime)
            {
                _targetBaseVol = Mathf.Lerp(start, end, t / unduckTime);
                yield return null;
            }
            _targetBaseVol = end;
        }

        private static IEnumerator FadeVolume(AudioSource src, float to, float time)
        {
            for (float t = 0; t < time; t += Time.deltaTime)
            {
                src.volume = Mathf.Lerp(0f, to, t / time);
                yield return null;
            }
            src.volume = to;
        }

        private void OnDestroy()
        {
            if (_toneClip) Destroy(_toneClip);
        }
    }

    // ── Companion: spatial one-shot helper ───────────────────────────
    [RequireComponent(typeof(AudioSource))]
    public class LabAudioSource : MonoBehaviour
    {
        private AudioSource _src;
        private void Awake() => _src = GetComponent<AudioSource>();
        public void PlayOneShot(AudioClip clip, float vol = 1f) => _src?.PlayOneShot(clip, vol);
    }
}
