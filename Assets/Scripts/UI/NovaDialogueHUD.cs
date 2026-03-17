using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowLab.UI
{
    /// <summary>
    /// Holographic dialogue HUD that floats in the player's peripheral view.
    /// Uses a World-Space Canvas anchored to the XR Camera Offset.
    /// Shows: Nova's responses, Chief's messages, thinking animation, Aether log button.
    /// </summary>
    public class NovaDialogueHUD : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Canvas       worldCanvas;
        [SerializeField] private CanvasGroup  canvasGroup;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI novaText;
        [SerializeField] private TextMeshProUGUI chiefText;
        [SerializeField] private TextMeshProUGUI thinkingText;
        [SerializeField] private TextMeshProUGUI aetherLogText;

        [Header("Panels")]
        [SerializeField] private GameObject novaPanel;
        [SerializeField] private GameObject chiefPanel;
        [SerializeField] private GameObject thinkingPanel;
        [SerializeField] private GameObject aetherLogPanel;

        [Header("Nova Name Tag")]
        [SerializeField] private TextMeshProUGUI nameTagText;
        [SerializeField] private Image           nameTagAccent; // the violet line

        [Header("Positioning")]
        [SerializeField] private Transform playerCamera;
        [SerializeField] private Vector3   hudOffset    = new Vector3(0.3f, -0.15f, 0.8f);
        [SerializeField] private float     followSpeed  = 3f;
        [SerializeField] private float     autoHideTime = 8f;

        [Header("Typewriter")]
        [SerializeField] private float typewriterSpeed = 0.025f; // seconds per character

        private float   _hideTimer;
        private bool    _visible;
        private string  _lastChiefMessage;

        // Colors
        private static readonly Color VioletColor = new Color(0.75f, 0.3f, 1f);
        private static readonly Color AmberColor  = new Color(1f,    0.7f, 0f);

        public string LastChiefMessage => _lastChiefMessage;

        private void Start()
        {
            if (playerCamera == null) playerCamera = Camera.main?.transform;
            SetAllPanelsActive(false);
            if (canvasGroup) canvasGroup.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (!_visible || playerCamera == null) return;

            // Smooth follow — stays in peripheral vision
            Vector3 targetPos = playerCamera.TransformPoint(hudOffset);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(transform.position - playerCamera.position), Time.deltaTime * followSpeed);

            // Auto-hide
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f) StartCoroutine(FadeOut());
        }

        // ── Public API ───────────────────────────────────

        public void ShowNovaMessage(string text)
        {
            SetAllPanelsActive(false);
            novaPanel?.SetActive(true);
            StartCoroutine(TypewriterEffect(novaText, text, VioletColor));
            Show(autoHideTime);
        }

        public void ShowChiefMessage(string text)
        {
            _lastChiefMessage = text;
            chiefPanel?.SetActive(true);
            if (chiefText) { chiefText.text = text; chiefText.color = AmberColor; }
            Show(4f);
        }

        public void ShowThinking()
        {
            SetAllPanelsActive(false);
            thinkingPanel?.SetActive(true);
            StartCoroutine(ThinkingAnimation());
            Show(30f); // will be replaced by response
        }

        public void ShowAetherLog(string logText)
        {
            aetherLogPanel?.SetActive(true);
            if (aetherLogText) aetherLogText.text = logText;
            Show(15f);
        }

        public void Hide() => StartCoroutine(FadeOut());

        // ── Private ──────────────────────────────────────

        private void Show(float duration)
        {
            _visible   = true;
            _hideTimer = duration;
            StopCoroutine(nameof(FadeOut));
            StartCoroutine(FadeIn());
        }

        private void SetAllPanelsActive(bool active)
        {
            novaPanel?.SetActive(active);
            chiefPanel?.SetActive(active);
            thinkingPanel?.SetActive(active);
            aetherLogPanel?.SetActive(active);
        }

        private IEnumerator TypewriterEffect(TextMeshProUGUI tmp, string fullText, Color color)
        {
            if (tmp == null) yield break;
            tmp.color = color;
            tmp.text  = "";
            foreach (char c in fullText)
            {
                tmp.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        private IEnumerator ThinkingAnimation()
        {
            if (thinkingText == null) yield break;
            string[] frames = { "processing.  ", "processing.. ", "processing..." };
            int i = 0;
            while (true)
            {
                thinkingText.text = frames[i % frames.Length];
                i++;
                yield return new WaitForSeconds(0.4f);
            }
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.deltaTime * 4f;
                yield return null;
            }
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null) { _visible = false; yield break; }
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime * 2f;
                yield return null;
            }
            _visible = false;
            SetAllPanelsActive(false);
        }
    }
}
