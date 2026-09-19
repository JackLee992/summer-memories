using System;
using System.Collections.Generic;

namespace SummerMemories.Loop
{
    [Serializable]
    public class TipEntry
    {
        public string id;
        public string title;
        public string text;
    }

    [Serializable]
    public class TipTable
    {
        public List<TipEntry> entries = new List<TipEntry>();
    }

    /// <summary>
    /// TIPS 情报系统：跨周目继承的核心元层。玩家在某周目获得的情报，
    /// 回潮后仍然保留（石头与精卫跨循环携带记忆）。
    /// 数据持久化到 SaveSlot.tipsUnlocked。
    /// </summary>
    public class TipsSystem
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        private readonly Dictionary<string, TipEntry> _catalog = new Dictionary<string, TipEntry>();

        public event Action<string> OnUnlocked;
        public IEnumerable<string> UnlockedIds => _unlocked;

        public void LoadCatalog(UnityEngine.TextAsset json)
        {
            _catalog.Clear();
            if (json == null) return;
            try
            {
                var table = UnityEngine.JsonUtility.FromJson<TipTable>(json.text);
                foreach (var e in table.entries) _catalog[e.id] = e;
            }
            catch (Exception e)
            {
                Core.Log.Error($"TIPS 目录解析失败：{e.Message}");
            }
        }

        public void RestoreFromSave(IEnumerable<string> ids)
        {
            _unlocked.Clear();
            if (ids == null) return;
            foreach (var id in ids) _unlocked.Add(id);
        }

        /// <summary>解锁情报；返回 true 表示新获得。</summary>
        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || _unlocked.Contains(id)) return false;
            _unlocked.Add(id);
            OnUnlocked?.Invoke(id);
            return true;
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        public List<string> SyncToSave(List<string> saveList)
        {
            saveList.Clear();
            saveList.AddRange(_unlocked);
            return saveList;
        }

        public TipEntry GetEntry(string id)
            => _catalog.TryGetValue(id, out var e) ? e : null;

        public string TitleOf(string id) => _catalog.TryGetValue(id, out var e) ? e.title : id;
    }
}
