using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SummerMemories.Core.Save
{
    /// <summary>Atomic slot writes; unreadable or unquarantined data is never overwritten.</summary>
    public static class SaveSystem
    {
        private static string Dir => Path.Combine(Application.persistentDataPath, "saves");
        private static readonly HashSet<string> Blocked = new HashSet<string>();
        private static string PathFor(string slot)
        {
            var safe = string.IsNullOrEmpty(slot) ? "auto" : slot;
            foreach (var c in safe)
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    throw new ArgumentException("Invalid save slot", nameof(slot));
            return Path.Combine(Dir, safe + ".json");
        }
        public static bool HasSave(string slot = "auto") => File.Exists(PathFor(slot));
        public static void Save(SaveSlot data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var path = PathFor(data.slotName);
            if (Blocked.Contains(path)) throw new IOException("Save slot requires recovery: " + path);
            // Validate an existing file before replacing it, even if the caller chose New Game.
            if (File.Exists(path)) Load(data.slotName);
            Directory.CreateDirectory(Dir);
            data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public static SaveSlot Load(string slot = "auto")
        {
            var path = PathFor(slot);
            if (!File.Exists(path)) return null;
            string json;
            try { json = File.ReadAllText(path); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Blocked.Add(path); throw; }
            try
            {
                var data = JsonUtility.FromJson<SaveSlot>(json);
                if (data == null || data.version != 1 || data.slotName != (string.IsNullOrEmpty(slot) ? "auto" : slot)
                    || !json.Contains("\"version\"") || !json.Contains("\"slotName\"")
                    || data.flags == null || data.tipsUnlocked == null || data.loopCount < 0)
                    throw new InvalidDataException("Invalid save data");
                return data;
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidDataException)
            {
                var broken = path + ".broken-" + DateTime.UtcNow.Ticks;
                try { File.Move(path, broken); }
                catch (Exception moveError)
                { Blocked.Add(path); throw new IOException("Cannot quarantine save: " + path, moveError); }
                Log.Warn("Corrupt save quarantined: " + broken);
                return null;
            }
        }
        public static void Delete(string slot = "auto")
        {
            var path = PathFor(slot);
            if (Blocked.Contains(path)) throw new IOException("Save slot requires recovery: " + path);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
