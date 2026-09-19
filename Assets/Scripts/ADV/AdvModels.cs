using System;
using System.Collections.Generic;

namespace SummerMemories.ADV
{
    /// <summary>
    /// 一段章节脚本。所有指令使用同一个扁平类，便于 JsonUtility 反序列化；
    /// 按 type 取用不同字段。剧情、演出、选项数据与代码分离（可外置为 JSON）。
    /// </summary>
    [Serializable]
    public class AdvScript
    {
        public string id;
        public string title;
        public List<AdvCommand> commands = new List<AdvCommand>();
    }

    [Serializable]
    public class AdvCommand
    {
        // bg / portrait / line / narration / choice / tip / loopreset / end
        public string type;

        // 演出
        public string color;      // bg：占位背景色（hex）
        public string art;        // bg：原创背景图 key（Resources/Art/Bg），缺失则用 color
        public string note;       // bg：场景/时间提示文字
        public string portrait;   // 立绘 key（外置素材名，缺失则占位）
        public string side;       // left / right
        public string bgm;        // bgm key（MVP 仅记录，不播放版权音乐）
        public string sfx;

        // 文本
        public string speaker;
        public string text;

        // 选项
        public List<AdvChoice> options;

        // 回溯
        public string anchor;     // loopreset：回到的锚点 id
        public string tip;        // loopreset/tip：解锁的 TIPS id
    }

    [Serializable]
    public class AdvChoice
    {
        public string text;
        public string setFlag;    // key=value
        public int jumpIndex;     // 选择后跳转的指令下标
    }
}
