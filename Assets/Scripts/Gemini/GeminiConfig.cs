using UnityEngine;

namespace ShadowLab.Gemini
{
    /// <summary>
    /// Stores the Gemini 2.0 Flash API key.
    /// Create via: Assets → Create → ShadowLab → Gemini Config
    ///
    /// IN UNITY EDITOR: Paste your key into the apiKey field.
    /// IN BUILD:        Set environment variable GEMINI_API_KEY before building,
    ///                  or inject via a secure config loader (see LoadFromEnv()).
    ///
    /// NEVER commit this asset with a real key — it's in .gitignore.
    /// </summary>
    [CreateAssetMenu(menuName = "ShadowLab/Gemini Config", fileName = "GeminiConfig")]
    public class GeminiConfig : ScriptableObject
    {
        [Header("Gemini 2.0 Flash")]
        [Tooltip("Paste key here in Editor. Leave blank to load from GEMINI_API_KEY env var.")]
        public string apiKey = "";

        [Header("Model")]
        public string modelId = "gemini-2.0-flash";

        /// <summary>
        /// Returns the API key — checks env var first, then the serialized field.
        /// Call this instead of accessing apiKey directly.
        /// </summary>
        public string GetKey()
        {
            // Try environment variable first (CI / secure build pipeline)
            string env = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrEmpty(env)) return env;

            // Fall back to inspector-assigned value
            if (!string.IsNullOrEmpty(apiKey)) return apiKey;

            Debug.LogError("[GeminiConfig] No API key found. Set GEMINI_API_KEY env var or assign in inspector.");
            return "";
        }
    }
}
