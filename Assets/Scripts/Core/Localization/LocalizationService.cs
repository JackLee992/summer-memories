using System;
using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Core.Localization
{
    [Serializable]
    public class StringEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class StringTable
    {
        public string language = "zh";
        public List<StringEntry> entries = new List<StringEntry>();
    }

    /// <summary>
    /// 极简本地化服务：从 Resources/Localization/strings_{lang}.json 加载。
    /// MVP 仅提供简中；结构上为简/英/日/韩预留（沿用 frost-story 的多语设计）。
    /// 字幕语言与配音语言分离在内容数据层体现。
    /// </summary>
    public class LocalizationService
    {
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>();
        public string Language { get; private set; } = "zh";

        public void Load(string lang = "zh")
        {
            Language = lang;
            _map.Clear();
            var ta = Resources.Load<TextAsset>($"Localization/strings_{lang}");
            if (ta == null)
            {
                Log.Warn($"缺少语言表 Localization/strings_{lang}");
                return;
            }
            try
            {
                var table = JsonUtility.FromJson<StringTable>(ta.text);
                foreach (var e in table.entries) _map[e.key] = e.value;
            }
            catch (Exception e)
            {
                Log.Error($"语言表解析失败：{e.Message}");
            }
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            return _map.TryGetValue(key, out var v) ? v : key;
        }
    }
}
