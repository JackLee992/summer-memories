using System.Collections.Generic;
using System.Linq;

namespace SummerMemories.Battle
{
    /// <summary>战斗内"时之沙"快照（内层回溯），每场战斗限定次数。</summary>
    public class BattleSnapshot
    {
        public int TurnNumber;
        public int NailsLeft;
        public List<BattleUnit> Units = new List<BattleUnit>();
        public string SelectedUnitId;
    }

    public class RewindSystem
    {
        private readonly Stack<BattleSnapshot> _stack = new Stack<BattleSnapshot>();
        public int ChargesLeft { get; private set; }
        public int MaxCharges { get; private set; }

        public RewindSystem(int charges)
        {
            MaxCharges = charges;
            ChargesLeft = charges;
        }

        public void Push(BattleSnapshot snapshot)
        {
            _stack.Push(snapshot);
        }

        public bool CanRewind => ChargesLeft > 0 && _stack.Count > 0;

        public BattleSnapshot Rewind()
        {
            if (!CanRewind) return null;
            ChargesLeft--;
            return _stack.Pop();
        }

        /// <summary>新回合不清空栈，允许跨回合回退到更早的决策点。</summary>
        public void Clear() { _stack.Clear(); }
    }
}
