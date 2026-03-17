using UnityEngine;

namespace ShadowLab.Gemini
{
    /// <summary>
    /// ScriptableObject — stores Gemini API key.
    /// Create via: Assets → Create → ShadowLab → Gemini Config
    /// NEVER commit this file with a real key. Add to .gitignore.
    /// </summary>
    [CreateAssetMenu(menuName = "ShadowLab/Gemini Config", fileName = "GeminiConfig")]
    public class GeminiConfig : ScriptableObject
    {
        [Header("Gemini 2.0 Flash")]
        [Tooltip("Get from https://aistudio.google.com/")]
        public string apiKey = "YOUR_GEMINI_API_KEY_HERE";

        [Header("Model")]
        public string modelId = "gemini-2.0-flash";
    }
}
