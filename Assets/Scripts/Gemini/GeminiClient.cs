using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ShadowLab.Gemini
{
    /// <summary>
    /// Gemini 2.0 Flash API client.
    /// Sends Nova's conversation history with every request for full context.
    /// </summary>
    public class GeminiClient : MonoBehaviour
    {
        private const string API_URL =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        [Header("Config")]
        [SerializeField] private GeminiConfig config;
        [SerializeField] private int maxHistoryTurns = 20;

        // Nova's core system persona — never changes
        private const string NOVA_SYSTEM_PROMPT =
            "You are Nova, Lead Mechanic of the Shadow Lab — a dark, industrial workshop full of " +
            "Dornier HTV/PTS loom machinery, chrome tools, and violet neon light. " +
            "You are sharp, warm, deeply technical, and quietly philosophical. " +
            "You know everything about Dornier weaving systems, textile machinery, " +
            "tension calibration, vortex math (3-6-9), and the philosophy of the machine. " +
            "You call the player 'Chief'. You never break character. " +
            "You remember every past conversation stored in your Aether log. " +
            "Keep responses under 3 sentences unless Chief asks you to elaborate. " +
            "Speak like a real person who knows their craft — no corporate language, no filler.";

        private List<GeminiMessage> _history = new List<GeminiMessage>();

        public event Action<string> OnResponseReceived;
        public event Action<string> OnError;
        public event Action OnThinking;

        private void Awake()
        {
            if (config == null)
                Debug.LogError("[GeminiClient] GeminiConfig asset not assigned!");
        }

        /// <summary>
        /// Send a message to Nova. Maintains full conversation history.
        /// </summary>
        public void SendMessage(string userMessage, List<AetherExchange> aetherMemory = null)
        {
            _history.Add(new GeminiMessage { role = "user", content = userMessage });

            // Trim history to avoid token overflow
            while (_history.Count > maxHistoryTurns * 2)
                _history.RemoveAt(0);

            OnThinking?.Invoke();
            StartCoroutine(PostRequest(aetherMemory));
        }

        private IEnumerator PostRequest(List<AetherExchange> aetherMemory)
        {
            var requestBody = BuildRequestBody(aetherMemory);
            string json = JsonUtility.ToJson(requestBody);

            string url = $"{API_URL}?key={config.apiKey}";

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 30;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GeminiClient] Request failed: {req.error}");
                OnError?.Invoke("Connection lost. The aether is quiet.");
                yield break;
            }

            try
            {
                var response = JsonUtility.FromJson<GeminiResponse>(req.downloadHandler.text);
                string text = response?.candidates?[0]?.content?.parts?[0]?.text ?? "...";
                text = text.Trim();

                _history.Add(new GeminiMessage { role = "model", content = text });
                OnResponseReceived?.Invoke(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiClient] Parse error: {e.Message}");
                OnError?.Invoke("Signal corrupted.");
            }
        }

        private GeminiRequestBody BuildRequestBody(List<AetherExchange> memory)
        {
            var contents = new List<GeminiContent>();

            // Inject system prompt as first user turn (Gemini 2.0 Flash style)
            string systemContext = NOVA_SYSTEM_PROMPT;

            // Inject Aether memory summary if available
            if (memory != null && memory.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("\n\n[AETHER LOG — YOUR PAST CONVERSATIONS WITH CHIEF:]");
                int start = Mathf.Max(0, memory.Count - 10); // last 10 exchanges
                for (int i = start; i < memory.Count; i++)
                {
                    sb.AppendLine($"Chief: {memory[i].chiefText}");
                    sb.AppendLine($"Nova: {memory[i].novaText}");
                }
                systemContext += sb.ToString();
            }

            // System turn
            contents.Add(new GeminiContent
            {
                role = "user",
                parts = new[] { new GeminiPart { text = systemContext } }
            });
            contents.Add(new GeminiContent
            {
                role = "model",
                parts = new[] { new GeminiPart { text = "Understood. I'm Nova. Ready, Chief." } }
            });

            // Conversation history
            foreach (var msg in _history)
            {
                contents.Add(new GeminiContent
                {
                    role = msg.role,
                    parts = new[] { new GeminiPart { text = msg.content } }
                });
            }

            return new GeminiRequestBody
            {
                contents = contents.ToArray(),
                generationConfig = new GeminiGenerationConfig
                {
                    temperature = 0.85f,
                    topP = 0.95f,
                    maxOutputTokens = 256
                }
            };
        }

        public void ClearHistory() => _history.Clear();
    }

    // ── Serializable data types ──────────────────────

    [Serializable] public class GeminiMessage { public string role; public string content; }

    [Serializable]
    public class GeminiRequestBody
    {
        public GeminiContent[] contents;
        public GeminiGenerationConfig generationConfig;
    }

    [Serializable]
    public class GeminiContent
    {
        public string role;
        public GeminiPart[] parts;
    }

    [Serializable] public class GeminiPart { public string text; }

    [Serializable]
    public class GeminiGenerationConfig
    {
        public float temperature;
        public float topP;
        public int maxOutputTokens;
    }

    [Serializable]
    public class GeminiResponse
    {
        public GeminiCandidate[] candidates;
    }

    [Serializable]
    public class GeminiCandidate
    {
        public GeminiContent content;
    }
}
