using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShadowLab.Gemini
{
    /// <summary>
    /// Persistent conversation memory — "The Aether Log".
    /// Saves/loads JSON to device storage so Nova remembers across sessions.
    /// </summary>
    public class AetherLog : MonoBehaviour
    {
        private const string FILENAME = "AetherLog.json";
        private string _filePath;

        private AetherLogData _data = new AetherLogData();

        public List<AetherExchange> CurrentSession => _data.currentSession;
        public List<AetherSession> AllSessions => _data.sessions;

        private void Awake()
        {
            _filePath = Path.Combine(Application.persistentDataPath, FILENAME);
            Load();
            Debug.Log($"[AetherLog] Loaded {_data.sessions.Count} past sessions from {_filePath}");
        }

        public void RecordExchange(string chiefText, string novaText)
        {
            var exchange = new AetherExchange
            {
                timestamp = DateTime.Now.ToString("o"),
                chiefText = chiefText,
                novaText  = novaText
            };
            _data.currentSession.Add(exchange);
            Save();
        }

        public void CloseSession()
        {
            if (_data.currentSession.Count == 0) return;

            _data.sessions.Add(new AetherSession
            {
                date      = DateTime.Now.ToString("o"),
                exchanges = new List<AetherExchange>(_data.currentSession)
            });

            // Keep last 50 sessions
            while (_data.sessions.Count > 50)
                _data.sessions.RemoveAt(0);

            _data.currentSession.Clear();
            Save();
        }

        /// <summary>
        /// Returns the last N exchanges across all sessions (for Gemini context).
        /// </summary>
        public List<AetherExchange> GetRecentMemory(int count = 20)
        {
            var all = new List<AetherExchange>();
            foreach (var session in _data.sessions)
                all.AddRange(session.exchanges);
            all.AddRange(_data.currentSession);

            int start = Mathf.Max(0, all.Count - count);
            return all.GetRange(start, all.Count - start);
        }

        private void Save()
        {
            try { File.WriteAllText(_filePath, JsonUtility.ToJson(_data, true)); }
            catch (Exception e) { Debug.LogError($"[AetherLog] Save failed: {e.Message}"); }
        }

        private void Load()
        {
            if (!File.Exists(_filePath)) return;
            try { _data = JsonUtility.FromJson<AetherLogData>(File.ReadAllText(_filePath)) ?? new AetherLogData(); }
            catch (Exception e) { Debug.LogError($"[AetherLog] Load failed: {e.Message}"); }
        }

        private void OnApplicationPause(bool pause) { if (pause) CloseSession(); }
        private void OnApplicationQuit() => CloseSession();
    }

    [Serializable] public class AetherLogData
    {
        public List<AetherSession>  sessions       = new();
        public List<AetherExchange> currentSession = new();
    }

    [Serializable] public class AetherSession
    {
        public string date;
        public List<AetherExchange> exchanges = new();
    }

    [Serializable] public class AetherExchange
    {
        public string timestamp;
        public string chiefText;
        public string novaText;
    }
}
