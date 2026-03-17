using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowLab.Debug
{
    /// <summary>
    /// Unified logging wrapper for all Shadow Lab scripts.
    /// Every log is tagged [ShadowLab][SUBSYSTEM] for clean Bug Jaeger filtering.
    ///
    /// USAGE:
    ///   ShadowLabLogger.Log(this, "Nova entered THINKING state");
    ///   ShadowLabLogger.Warn(this, "Haptic amplitude clamped");
    ///   ShadowLabLogger.Error(this, "GeminiClient stream failed");
    ///   ShadowLabLogger.State(this, "IDLE", "THINKING");
    ///   ShadowLabLogger.Stream(this, "token chunk received");
    ///
    /// BUG JAEGER FILTER:
    ///   Tag filter: "ShadowLab"
    ///   Or filter by subsystem: "ShadowLab][Nova" / "ShadowLab][Gemini" etc.
    ///
    /// LOGCAT COMMAND (adb):
    ///   adb logcat -s Unity:V | grep ShadowLab
    /// </summary>
    public static class ShadowLabLogger
    {
        // ── Config ────────────────────────────────────────────────────
        public static bool Verbose  = true;   // flip to false in release builds
        public static bool LogStates= true;
        public static bool LogStream= false;  // high frequency — off by default

        // ── Subsystem name cache ──────────────────────────────────────
        private static readonly Dictionary<Type, string> _nameCache = new();

        // ── Public API ────────────────────────────────────────────────

        public static void Log(object sender, string msg)
        {
            if (!Verbose) return;
            UnityEngine.Debug.Log(Format(sender, "INFO", msg));
        }

        public static void Warn(object sender, string msg)
        {
            UnityEngine.Debug.LogWarning(Format(sender, "WARN", msg));
        }

        public static void Error(object sender, string msg)
        {
            UnityEngine.Debug.LogError(Format(sender, "ERR", msg));
        }

        /// <summary>Log a state machine transition.</summary>
        public static void State(object sender, string from, string to)
        {
            if (!LogStates) return;
            UnityEngine.Debug.Log(Format(sender, "STATE", $"{from} → {to}"));
        }

        /// <summary>Log a Gemini streaming event (high frequency — gated by LogStream).</summary>
        public static void Stream(object sender, string msg)
        {
            if (!LogStream) return;
            UnityEngine.Debug.Log(Format(sender, "STREAM", msg));
        }

        /// <summary>Log a haptic event with amplitude.</summary>
        public static void Haptic(object sender, float amplitude, float duration)
        {
            if (!Verbose) return;
            UnityEngine.Debug.Log(Format(sender, "HAPTIC", $"amp={amplitude:F2} dur={duration:F3}s"));
        }

        /// <summary>Log audio event.</summary>
        public static void Audio(object sender, string msg)
        {
            if (!Verbose) return;
            UnityEngine.Debug.Log(Format(sender, "AUDIO", msg));
        }

        /// <summary>Log physics/interaction event.</summary>
        public static void Physics(object sender, string msg)
        {
            if (!Verbose) return;
            UnityEngine.Debug.Log(Format(sender, "PHYSICS", msg));
        }

        // ── Formatter ─────────────────────────────────────────────────

        private static string Format(object sender, string level, string msg)
        {
            string subsystem = GetSubsystem(sender);
            // Bug Jaeger will pick up "[ShadowLab]" as the tag prefix
            return $"[ShadowLab][{subsystem}][{level}] {msg}";
        }

        private static string GetSubsystem(object sender)
        {
            if (sender == null) return "Core";
            var type = sender is Type t ? t : sender.GetType();
            if (_nameCache.TryGetValue(type, out var cached)) return cached;

            // Strip namespace — just use the class name
            string name = type.Name
                .Replace("Manager",   "")
                .Replace("Controller","")
                .Replace("Mechanism", "")
                .Replace("Emitter",   "")
                .Trim();

            _nameCache[type] = name;
            return name;
        }
    }

    // ── In-headset log overlay (optional) ────────────────────────────

    /// <summary>
    /// Attach to a world-space Canvas in the Shadow Lab.
    /// Shows last N log lines directly in VR — useful when Bug Jaeger
    /// isn't open and you want to see logs while in the headset.
    /// </summary>
    public class ShadowLabLogOverlay : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Text textComponent;
        [SerializeField] private int                 maxLines = 12;
        [SerializeField] private bool                errorsOnly = false;

        private readonly Queue<string> _lines = new();

        private void OnEnable()  => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (!condition.Contains("[ShadowLab]")) return;
            if (errorsOnly && type != LogType.Error && type != LogType.Warning) return;

            // Colour code by level
            string col = type switch
            {
                LogType.Error   => "#ff4444",
                LogType.Warning => "#ffaa00",
                _               => "#aaffcc",
            };

            _lines.Enqueue($"<color={col}>{condition}</color>");
            while (_lines.Count > maxLines) _lines.Dequeue();

            if (textComponent)
                textComponent.text = string.Join("\n", _lines);
        }
    }
}
