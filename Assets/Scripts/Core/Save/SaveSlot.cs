using System;
using System.Collections.Generic;

namespace SummerMemories.Core.Save
{
    /// <summary>
    /// 单个存档槽位的全部持久化数据。JsonUtility 不支持 Dictionary，
    /// 因此键值对使用有序列表存储。
    /// </summary>
    [Serializable]
    public class SaveSlot
    {
        public string slotName = "auto";
        public string savedAt = "";
        public int version = 1;

        // —— 周目（外层回溯）——
        public int loopCount = 0;
        public string anchorId = "jul01_dock";
        public string currentChapterId = "prologue";

        // —— 跨周目继承的 TIPS 情报 ——
        public List<string> tipsUnlocked = new List<string>();

        // —— 剧情标记（角色生死、已触发事件等）——
        public List<FlagEntry> flags = new List<FlagEntry>();

        // —— ADV 进度（脚本 id + 指令下标）——
        public string advScriptId = "";
        public int advCommandIndex = 0;

        // —— 战斗教学：救命毫毛剩余次数 ——
        public int battleRewindLeft = 3;
        public bool battle01Cleared = false;

        public string GetFlag(string key, string def = "")
        {
            foreach (var f in flags) if (f.key == key) return f.value;
            return def;
        }

        public void SetFlag(string key, string value)
        {
            foreach (var f in flags)
            {
                if (f.key == key) { f.value = value; return; }
            }
            flags.Add(new FlagEntry { key = key, value = value });
        }
    }

    [Serializable]
    public class FlagEntry
    {
        public string key;
        public string value;
    }
}
