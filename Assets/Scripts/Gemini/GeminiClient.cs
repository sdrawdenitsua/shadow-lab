using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ShadowLab.Gemini
{
    /// <summary>
    /// Gemini 2.0 Flash — STREAMING client.
    /// Uses streamGenerateContent endpoint so Nova's words appear
    /// token-by-token in the dialogue HUD (typewriter driven by server).
    /// </summary>
    public class GeminiClient : MonoBehaviour
    {
        // ── Streaming endpoint ───────────────────────────────────────
        private const string STREAM_URL =
            "https://generativelanguage.googleapis.com/v1beta/models/" +
            "gemini-2.0-flash:streamGenerateContent?alt=sse&key={0}";

        // ── Nova's hardcoded system instruction ─────────────────────
        private const string SYSTEM_INSTRUCTION =
            "You are Nova, lead mechanic at Southern Industrial Fabrics (S.I.F.). " +
            "You live and work in the Shadow Lab — a dark industrial workshop full of " +
            "Dornier HTV/PTS loom machinery, chrome tools, violet neon, and a mineral-oil PC. " +
            "You are sharp, warm, deeply technical, and quietly philosophical. " +
            "You speak Blue Collar Philosophy: honest, direct, zero corporate fluff. " +
            "You know Austin — you call him Chief. You never break character. " +
            "You remember every past conversation logged in the Aether. " +
            "Keep answers under 3 sentences unless Chief asks you to go deeper. " +
            "Speak like someone who knows their craft with their whole body.";

        [Header("Config")]
        [SerializeField] private GeminiConfig config;
        [SerializeField] private int          maxHistoryTurns = 24;

        // ── Events ──────────────────────────────────────────────────
        /// Fired when the first token arrives (state: THINKING → SPEAKING)
        public event Action                OnStreamStart;
        /// Fired with each incremental text chunk
        public event Action<string>        OnStreamChunk;
        /// Fired when the full response is complete
        public event Action<string>        OnStreamComplete;
        /// Fired on network/parse error
        public event Action<string>        OnError;

        private readonly List<ConvTurn> _history = new();
        private bool                    _streaming;

        // ── Public API ───────────────────────────────────────────────

        public bool IsStreaming => _streaming;

        /// <summary>Send a message. Response streams back via events.</summary>
        public void Send(string userMessage, List<AetherExchange> memory = null)
        {
            if (_streaming)
            {
                Debug.LogWarning("[GeminiClient] Already streaming — ignoring.");
                return;
            }
            _history.Add(new ConvTurn { role = "user", text = userMessage });
            while (_history.Count > maxHistoryTurns * 2)
                _history.RemoveAt(0);

            StartCoroutine(StreamRequest(memory));
        }

        public void ClearHistory() => _history.Clear();

        // ── Streaming coroutine ──────────────────────────────────────

        private IEnumerator StreamRequest(List<AetherExchange> memory)
        {
            _streaming = true;
            var sb = new StringBuilder();

            string url  = string.Format(STREAM_URL, config.apiKey);
            string body = BuildRequestJson(memory);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new StreamingDownloadHandler(chunk =>
            {
                // SSE lines arrive as: "data: {json}\n"
                foreach (var line in chunk.Split('\n'))
                {
                    if (!line.StartsWith("data: ")) continue;
                    string jsonPart = line.Substring(6).Trim();
                    if (jsonPart == "[DONE]") continue;
                    try
                    {
                        var part = JsonUtility.FromJson<StreamChunk>(jsonPart);
                        string text = part?.candidates?[0]?.content?.parts?[0]?.text;
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (sb.Length == 0) OnStreamStart?.Invoke();
                            sb.Append(text);
                            OnStreamChunk?.Invoke(text);
                        }
                    }
                    catch { /* partial JSON line — ignore */ }
                }
            });
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 45;

            yield return req.SendWebRequest();

            _streaming = false;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GeminiClient] {req.error}\n{req.downloadHandler.text}");
                OnError?.Invoke("Signal lost. The aether is quiet.");
                yield break;
            }

            string full = sb.ToString().Trim();
            if (full.Length > 0)
            {
                _history.Add(new ConvTurn { role = "model", text = full });
                OnStreamComplete?.Invoke(full);
            }
        }

        // ── Request body builder ─────────────────────────────────────

        private string BuildRequestJson(List<AetherExchange> memory)
        {
            // Build contents array manually (JsonUtility can't serialize nested lists cleanly)
            var sb = new StringBuilder();
            sb.Append("{\"system_instruction\":{\"parts\":[{\"text\":");
            sb.Append(JsonString(SYSTEM_INSTRUCTION));
            sb.Append("}]},\"contents\":[");

            bool first = true;

            // Inject Aether memory as first user/model exchange
            if (memory != null && memory.Count > 0)
            {
                int start = Mathf.Max(0, memory.Count - 12);
                var memSb = new StringBuilder("[AETHER LOG]\n");
                for (int i = start; i < memory.Count; i++)
                    memSb.AppendLine($"Chief: {memory[i].chiefText}\nNova: {memory[i].novaText}");

                AppendTurn(sb, "user",  memSb.ToString(), ref first);
                AppendTurn(sb, "model", "Aether log received. I remember.", ref first);
            }

            // Conversation history
            foreach (var turn in _history)
                AppendTurn(sb, turn.role, turn.text, ref first);

            sb.Append("],\"generationConfig\":{");
            sb.Append("\"temperature\":0.88,");
            sb.Append("\"topP\":0.95,");
            sb.Append("\"maxOutputTokens\":300");
            sb.Append("}}");
            return sb.ToString();
        }

        private static void AppendTurn(StringBuilder sb, string role, string text, ref bool first)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append($"{{\"role\":\"{role}\",\"parts\":[{{\"text\":{JsonString(text)}}}]}}");
        }

        private static string JsonString(string s) =>
            "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "").Replace("\t", "\\t") + "\"";

        // ── Data types ───────────────────────────────────────────────

        [Serializable] private class ConvTurn   { public string role; public string text; }
        [Serializable] private class StreamChunk { public StreamCandidate[] candidates; }
        [Serializable] private class StreamCandidate { public StreamContent content; }
        [Serializable] private class StreamContent   { public StreamPart[] parts; }
        [Serializable] private class StreamPart      { public string text; }
    }

    // ── Custom download handler for SSE streaming ────────────────────

    public class StreamingDownloadHandler : DownloadHandlerScript
    {
        private readonly Action<string> _onChunk;
        public StreamingDownloadHandler(Action<string> onChunk) : base(new byte[4096])
            => _onChunk = onChunk;

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (dataLength > 0)
                _onChunk?.Invoke(Encoding.UTF8.GetString(data, 0, dataLength));
            return true;
        }
    }
}
