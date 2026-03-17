using System.Collections;
using UnityEngine;

namespace ShadowLab.Environment
{
    /// <summary>
    /// The mineral oil submerged PC rig.
    /// Runs particle bubbles, screen flicker, temperature simulation.
    /// Grabbable acrylic tank lid can be removed.
    /// </summary>
    public class MineralOilPC : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private ParticleSystem bubbleParticles;
        [SerializeField] private Renderer       screenRenderer;
        [SerializeField] private Light          screenGlow;
        [SerializeField] private Material       screenMaterial;

        [Header("Temperature")]
        [SerializeField] private float minTemp = 38f;
        [SerializeField] private float maxTemp = 72f;
        private float _currentTemp;

        [Header("Screen Content")]
        [SerializeField] private string[] screenLines = {
            "SHADOW_LAB::KERNEL 4.0.528",
            "VIOLET_EYE_DRIVER: LOADED",
            "NOVA_CORE: ONLINE",
            "AETHER_SYNC: 528Hz LOCKED",
            "DORNIER_INTERFACE: 37 UNITS",
            "GEMINI_API: CONNECTED",
            "CHIEF_BIOMETRICS: VERIFIED",
            "SOUL_BOOLEAN: TRUE",
            "ATMOSPHERIC_OPACITY: 0%",
            "> _"
        };

        private static readonly int EmissID = Shader.PropertyToID("_EmissionColor");
        private float _flickerTime;

        private void Start()
        {
            _currentTemp = Random.Range(minTemp, minTemp + 10f);
            bubbleParticles?.Play();
            StartCoroutine(TempSimulation());
            StartCoroutine(ScreenFlicker());
        }

        private IEnumerator TempSimulation()
        {
            while (true)
            {
                yield return new WaitForSeconds(5f);
                _currentTemp = Mathf.Clamp(
                    _currentTemp + Random.Range(-2f, 3f),
                    minTemp, maxTemp
                );
                // Bubbles speed up with heat
                if (bubbleParticles != null)
                {
                    var main = bubbleParticles.main;
                    main.simulationSpeed = Mathf.Lerp(0.5f, 2.5f, (_currentTemp - minTemp) / (maxTemp - minTemp));
                }
            }
        }

        private IEnumerator ScreenFlicker()
        {
            while (true)
            {
                // Mostly stable, occasional flicker
                float stability = Random.value;
                if (stability < 0.95f)
                {
                    yield return new WaitForSeconds(Random.Range(2f, 8f));
                    continue;
                }

                // Quick flicker
                if (screenGlow) screenGlow.intensity *= 0.3f;
                yield return new WaitForSeconds(Random.Range(0.03f, 0.1f));
                if (screenGlow) screenGlow.intensity /= 0.3f;
                yield return new WaitForSeconds(Random.Range(0.02f, 0.05f));
                if (screenGlow) screenGlow.intensity *= 0.5f;
                yield return new WaitForSeconds(0.05f);
                if (screenGlow) screenGlow.intensity /= 0.5f;
            }
        }

        public float GetTemperature() => _currentTemp;
    }
}
