using System;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// キャラクターの基礎ステータス(HP/攻撃/防御/素早さ/運)。
    /// プロトタイプではハードコードで用意し、将来的にステータス振り分け画面から編集される想定。
    /// </summary>
    [Serializable]
    public class CharacterStats
    {
        public int maxHp = 100;
        public int attack = 20;
        public int defense = 10;
        public int speed = 10;
        public int luck = 10;

        public CharacterStats() { }

        public CharacterStats(int maxHp, int attack, int defense, int speed, int luck)
        {
            this.maxHp = maxHp;
            this.attack = attack;
            this.defense = defense;
            this.speed = speed;
            this.luck = luck;
        }
    }
}
