using SummerMemories.ADV;
using SummerMemories.Battle;
using SummerMemories.Core.Content;
using SummerMemories.Core.Localization;
using SummerMemories.Core.Save;
using SummerMemories.Core.UI;
using SummerMemories.Loop;
using UnityEngine;

namespace SummerMemories.App
{
    /// <summary>
    /// 全局流程导演：标题 → 序章 ADV → 首次回潮（归墟）教学 → 第一场战棋
    /// → 胜利进周目时间线 / 败北再次回潮重战。持有存档、本地化、异闻录与素材定位器。
    /// </summary>
    public class GameDirector : MonoBehaviour
    {
        private LocalizationService _l10n;
        private TipsSystem _tips;
        private ExternalContentLocator _content;
        private SaveSlot _slot;
        private Transform _screenRoot;

        private void Awake()
        {
            _screenRoot = new GameObject("Screens").transform;
            _screenRoot.SetParent(transform, false);

            _l10n = new LocalizationService();
            _l10n.Load("zh");

            _tips = new TipsSystem();
            _tips.LoadCatalog(Resources.Load<TextAsset>("Tips/tips_zh"));

            _content = new ExternalContentLocator();
            _content.EnsureRoot();

            ShowTitle();
        }

        // —— 屏幕切换 ——
        private void ClearScreens()
        {
            for (var i = _screenRoot.childCount - 1; i >= 0; i--)
                Destroy(_screenRoot.GetChild(i).gameObject);
        }

        private void ShowTitle()
        {
            ClearScreens();
            TitleScreen.Build(_screenRoot, SaveSystem.HasSave(), OnNewGame, OnContinue);
        }

        private void OnNewGame()
        {
            _slot = new SaveSlot { slotName = "auto", currentChapterId = "prologue" };
            _tips.RestoreFromSave(null);
            PlayAdv("prologue");
        }

        private void OnContinue()
        {
            _slot = SaveSystem.Load();
            if (_slot == null) { ShowTitle(); return; }
            _tips.RestoreFromSave(_slot.tipsUnlocked);
            if (_slot.battle01Cleared) ShowTimeline(true);
            else StartBattle();
        }

        // —— ADV ——
        private void PlayAdv(string scriptId)
        {
            ClearScreens();
            var ta = Resources.Load<TextAsset>("Story/" + scriptId);
            if (ta == null)
            {
                Core.Log.Error($"缺少剧情脚本 Resources/Story/{scriptId}");
                return;
            }
            var script = JsonUtility.FromJson<AdvScript>(ta.text);
            var go = new GameObject("ADV_" + scriptId);
            go.transform.SetParent(_screenRoot, false);
            var view = go.AddComponent<AdvView>();
            view.Play(script,
                onFinished: () =>
                {
                    if (scriptId == "prologue" || scriptId == "loop_teaching") StartBattle();
                },
                onLoopReset: HandleLoopReset,
                onTip: UnlockTip);
        }

        private void HandleLoopReset(string anchor, string tip)
        {
            UnlockTip(tip);
            _slot.loopCount++;
            _slot.anchorId = string.IsNullOrEmpty(anchor) ? _slot.anchorId : anchor;
            SyncAndSave();
            ClearScreens();
            TeachingDialog.Show(_screenRoot,
                "归墟 · 回潮",
                "黑水自四面八方涌来，你坠入无底之谷——归墟。\n" +
                "再睁眼，又是登岛那天清晨。\n" +
                "别人都忘了，唯有你记得一切；《山海异闻》中记下的情报，不会随潮水退去。",
                "溯流而上",
                () => PlayAdv("loop_teaching"));
        }

        private void UnlockTip(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_tips.Unlock(id))
            {
                Core.Log.Info($"解锁异闻：{_tips.TitleOf(id)}");
                if (_slot != null) SyncAndSave();
            }
        }

        private void SyncAndSave()
        {
            _tips.SyncToSave(_slot.tipsUnlocked);
            SaveSystem.Save(_slot);
        }

        // —— 战斗 ——
        private void StartBattle()
        {
            ClearScreens();
            _slot.currentChapterId = "battle_01";
            SaveSystem.Save(_slot);

            var ta = Resources.Load<TextAsset>("Battles/battle_01");
            var cfg = JsonUtility.FromJson<BattleConfig>(ta.text);
            var go = new GameObject("Battle_01");
            go.transform.SetParent(_screenRoot, false);
            var view = go.AddComponent<BattleView>();
            view.Play(cfg, OnBattleEnd);
        }

        private void OnBattleEnd(bool victory)
        {
            if (victory)
            {
                _slot.battle01Cleared = true;
                SyncAndSave();
                ShowTimeline(true);
                return;
            }
            // 败北 = 一次回潮，情报继承，战斗从头
            _slot.loopCount++;
            UnlockTip("tip_death_returns");
            SyncAndSave();
            ClearScreens();
            TeachingDialog.Show(_screenRoot,
                "回潮",
                "你又一次死去。\n归墟把你吐回战斗开始之前——这一次，夜叉的扑杀规律仍在你脑中。",
                "带着异闻再战",
                StartBattle);
        }

        private void ShowTimeline(bool cleared)
        {
            ClearScreens();
            LoopTimelineScreen.Build(_screenRoot, _slot.loopCount, _tips, cleared,
                ShowTitle, StartBattle);
        }
    }
}
