using System.Collections;
using UnityEngine;

namespace ShadowLab.Audio
{
    /// <summary>
    /// Manages the Shadow Lab ambient soundscape:
    ///   - 528Hz base tone (continuous)
    ///   - Loom rhythm track (zone-based, fades by proximity)
    ///   - Environmental one-shots (drips, metal groans, electricity)
    /// </summary>
    public class AmbientAudioManager : MonoBehaviour
    {
        [Header("528 Hz Base Tone")]
        [SerializeField] private AudioSource baseToneSource;
        [SerializeField] private float       baseToneVolume  = 0.12f;
        [SerializeField] private float       baseToneFreq    = 528f;

        [Header("Loom Rhythm")]
        [SerializeField] private AudioSource loomRhythmSource;
        [SerializeField] private AudioClip   loomRhythmClip;
        [SerializeField] private float       loomRhythmMaxVolume = 0.35f;
        [SerializeField] private Transform[] loomPositions;    // world positions of loom machines

        [Header("Environmental SFX")]
        [SerializeField] private AudioSource envSource;
        [SerializeField] private AudioClip[] metalGroanClips;
        [SerializeField] private AudioClip[] electricalHumClips;
        [SerializeField] private AudioClip   oilDripClip;
        [SerializeField] private float       envEventInterval = 25f;

        [Header("Nova Speaking Ducking")]
        [SerializeField] private float duckFactor  = 0.35f;   // reduce ambient to this when Nova speaks
        [SerializeField] private float duckFadeTime= 0.5f;

        private Transform _playerHead;
        private float     _currentBaseVol;
        private float     _currentLoomVol;
        private bool      _isDucked;
        private AudioClip _generatedTone;

        private void Start()
        {
            var cam = Camera.main;
            if (cam) _playerHead = cam.transform;

            _generatedTone = Generate528HzTone(4f);
            if (baseToneSource)
            {
                baseToneSource.clip   = _generatedTone;
                baseToneSource.loop   = true;
                baseToneSource.volume = baseToneVolume;
                baseToneSource.spatialBlend = 0f; // 2D — fills the whole room
                baseToneSource.Play();
            }

            if (loomRhythmSource && loomRhythmClip)
            {
                loomRhythmSource.clip  = loomRhythmClip;
                loomRhythmSource.loop  = true;
                loomRhythmSource.volume = 0f;
                loomRhythmSource.Play();
            }

            StartCoroutine(EnvironmentalEvents());
            StartCoroutine(LoomProximityLoop());
        }

        // ── Called by NovaBrain when she starts/stops speaking ──────
        public void SetNovaSpeaking(bool speaking)
        {
            if (_isDucked == speaking) return;
            _isDucked = speaking;
            StopAllCoroutines();
            StartCoroutine(DuckAmbient(speaking));
            StartCoroutine(LoomProximityLoop());
            StartCoroutine(EnvironmentalEvents());
        }

        // ── 528 Hz Procedural Tone ───────────────────────────────────
        private AudioClip Generate528HzTone(float durationSeconds)
        {
            int sampleRate = 44100;
            int samples    = Mathf.RoundToInt(sampleRate * durationSeconds);
            float[] data   = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Pure 528Hz sine + subtle 3rd harmonic for warmth
                data[i] = Mathf.Sin(2 * Mathf.PI * baseToneFreq * t) * 0.7f
                         + Mathf.Sin(2 * Mathf.PI * baseToneFreq * 3f * t) * 0.08f;
            }

            // Fade in/out to avoid clicks when looping
            int fadeLen = sampleRate / 10;
            for (int i = 0; i < fadeLen; i++)
            {
                float fade = (float)i / fadeLen;
                data[i] *= fade;
                data[samples - 1 - i] *= fade;
            }

            AudioClip clip = AudioClip.Create("528Hz", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ── Loom rhythm fades based on distance to nearest loom ──────
        private IEnumerator LoomProximityLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.3f);
                if (_playerHead == null || loomRhythmSource == null || loomPositions == null) continue;

                float minDist = float.MaxValue;
                foreach (var lp in loomPositions)
                {
                    if (lp == null) continue;
                    float d = Vector3.Distance(_playerHead.position, lp.position);
                    if (d < minDist) minDist = d;
                }

                float targetVol = Mathf.InverseLerp(8f, 2f, minDist) * loomRhythmMaxVolume;
                if (_isDucked) targetVol *= duckFactor;
                loomRhythmSource.volume = Mathf.Lerp(loomRhythmSource.volume, targetVol, 0.1f);
            }
        }

        private IEnumerator DuckAmbient(bool duck)
        {
            float startVol = baseToneSource ? baseToneSource.volume : baseToneVolume;
            float endVol   = duck ? baseToneVolume * duckFactor : baseToneVolume;
            float t = 0f;
            while (t < duckFadeTime)
            {
                t += Time.deltaTime;
                if (baseToneSource) baseToneSource.volume = Mathf.Lerp(startVol, endVol, t / duckFadeTime);
                yield return null;
            }
        }

        private IEnumerator EnvironmentalEvents()
        {
            while (true)
            {
                yield return new WaitForSeconds(envEventInterval + Random.Range(-8f, 8f));

                int roll = Random.Range(0, 4);
                switch (roll)
                {
                    case 0 when metalGroanClips?.Length > 0:
                        envSource?.PlayOneShot(metalGroanClips[Random.Range(0, metalGroanClips.Length)], 0.4f);
                        break;
                    case 1 when electricalHumClips?.Length > 0:
                        envSource?.PlayOneShot(electricalHumClips[Random.Range(0, electricalHumClips.Length)], 0.3f);
                        break;
                    case 2 when oilDripClip:
                        envSource?.PlayOneShot(oilDripClip, 0.5f);
                        break;
                }
            }
        }

        private void OnDestroy()
        {
            if (_generatedTone) Destroy(_generatedTone);
        }
    }

    /// <summary>Simple wrapper for one-shot spatial audio at a world position.</summary>
    public class LabAudioSource : MonoBehaviour
    {
        private AudioSource _source;
        private void Awake() => _source = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        public void PlayOneShot(AudioClip clip, float vol = 1f) => _source?.PlayOneShot(clip, vol);
    }
}
