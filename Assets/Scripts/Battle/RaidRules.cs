using UnityEngine;
using ShiftingMetropolis.App;
using ShiftingMetropolis.Dungeon;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// レイドの数値。通常ボスを土台に、HPは参加人数で膨らむ共有プールにする。
    /// </summary>
    public static class RaidRules
    {
        public const int DailyTurns = 15;
        public const int ClearDiamonds = 50;
        const int MinPerHead = 400;
        const int MaxPerHead = 40000000;
        const int MaxPool = 1500000000;

        public static int PerHead(int week, int floor, int clears)
        {
            var grown = DungeonRunner.ScaleEnemyStats(
                new CharacterStats(1, 1, 1, 1, 1), floor, true, week);
            // その階まで勉強した人が、1日15手のうち攻撃を中心にすると3〜4日で1層。
            float clearMul = 1f + Mathf.Min(0.20f, Mathf.Max(0, clears) * 0.006f);
            float hp = grown.maxHp * 4.4f * clearMul;
            if (week == 1 && floor <= 6)
            {
                float open = (7 - floor) / 6f;
                hp /= 1f + open;
            }
            int value = Mathf.RoundToInt(hp);
            if (value < MinPerHead) value = MinPerHead;
            if (value > MaxPerHead) value = MaxPerHead;
            return value;
        }

        public static int PoolHp(int perHead, int participants)
        {
            int count = Mathf.Max(1, participants);
            long prod = (long)Mathf.Max(1, perHead) * count;
            if (prod > MaxPool) prod = MaxPool;
            if (prod < 1) prod = 1;
            return (int)prod;
        }

        public static int ScaleRemaining(int oldHp, int oldMax, int newMax)
        {
            if (newMax < 1) newMax = 1;
            if (oldMax < 1) return newMax;
            double ratio = (double)Mathf.Clamp(oldHp, 0, oldMax) / oldMax;
            int hp = (int)System.Math.Round(ratio * newMax);
            if (oldHp > 0 && hp < 1) hp = 1;
            if (hp > newMax) hp = newMax;
            if (hp < 0) hp = 0;
            return hp;
        }

        public static CharacterStats MakeStats(int week, int floor, int clears, EnemyKit.Kind kind, int sharedMaxHp)
        {
            var stats = DungeonRunner.ScaleEnemyStats(
                new CharacterStats(1, 1, 1, 12, 8), floor, true, week);
            float grow = 1f + Mathf.Min(0.25f, Mathf.Max(0, clears) * 0.008f);
            float atk = 1.12f * grow;
            float def = 1.08f * grow;
            float spd = 1.04f;
            float luck = 1.04f;
            if (kind == EnemyKit.Kind.Shogun)
            {
                atk *= 0.94f;
                def *= 1.14f;
            }
            else if (kind == EnemyKit.Kind.Monster)
            {
                atk *= 1.12f;
                def *= 0.90f;
                spd *= 1.04f;
            }
            stats.attack = Mathf.Max(1, Mathf.RoundToInt(stats.attack * atk));
            stats.defense = Mathf.Max(1, Mathf.RoundToInt(stats.defense * def));
            stats.speed = Mathf.Clamp(Mathf.RoundToInt(stats.speed * spd), 1, StudyStoreSpeedCap());
            stats.luck = Mathf.Clamp(Mathf.RoundToInt(stats.luck * luck), 1, StudyStoreSpeedCap());
            stats.maxHp = Mathf.Max(1, sharedMaxHp);
            return stats;
        }

        static int StudyStoreSpeedCap()
        {
            return StudyStore.BaseSpeed + StudyStore.MaxSpdLuckSteps + 40;
        }

        public static string FormatHp(int value)
        {
            if (value < 0) value = 0;
            return value.ToString("N0");
        }

        public static float BodyScale(int floor)
        {
            return floor % 10 == 0 ? 3.28f : 2.88f;
        }
    }
}
