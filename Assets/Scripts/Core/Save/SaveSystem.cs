using System.IO;
using UnityEngine;

namespace SummerMemories.Core.Save
{
    /// <summary>
    /// JSON 本地存档。路径：Application.persistentDataPath/saves/{slot}.json。
    /// 不依赖任何第三方库；损坏存档回退为 null，由上层决定处理方式。
    /// </summary>
    public static class SaveSystem
    {
        private static string Dir => Path.Combine(Application.persistentDataPath, "saves");

        private static string PathFor(string slot)
        {
            var safe = string.IsNullOrEmpty(slot) ? "auto" : slot;
            return Path.Combine(Dir, safe + ".json");
        }

        public static bool HasSave(string slot = "auto")
        {
            return File.Exists(PathFor(slot));
        }

        public static void Save(SaveSlot data)
        {
            Directory.CreateDirectory(Dir);
            data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(PathFor(data.slotName), json);
            Log.Info($"存档已写入 {PathFor(data.slotName)}");
        }

        public static SaveSlot Load(string slot = "auto")
        {
            var path = PathFor(slot);
            if (!File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveSlot>(json);
                if (data == null) throw new InvalidDataException("空存档");
                return data;
            }
            catch (System.Exception e)
            {
                Log.Error($"存档损坏，读取失败：{e.Message}");
                var broken = path + ".broken-" + System.DateTime.Now.Ticks;
                try { File.Move(path, broken); Log.Warn($"损坏存档已隔离到 {broken}"); } catch { }
                return null;
            }
        }

        public static void Delete(string slot = "auto")
        {
            var path = PathFor(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
