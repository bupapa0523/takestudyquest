using UnityEngine;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.Dungeon
{
    /// <summary>
    /// 100階の塔。10階ごとにステージが変わり、10 / 20 / ... / 100 がボス。
    /// </summary>
    public static class DungeonRunner
    {
        public struct ThemeInfo
        {
            public string label;
            public string shortName;
            public Color fogColor;
            public Color lightTint;
            public float fogDensity;
            public bool horror;
        }

        public const int StageCount = 10;

        static readonly ThemeInfo[] Themes =
        {
            T("石造りの塔層", "石塔", new Color(0.52f, 0.55f, 0.62f), new Color(0.92f, 0.94f, 1f), 0.018f),
            T("ネオン街区", "街区", new Color(0.62f, 0.72f, 0.86f), new Color(1f, 0.98f, 0.92f), 0.008f),
            T("原生林", "原生林", new Color(0.18f, 0.42f, 0.16f), new Color(0.62f, 1f, 0.55f), 0.026f),
            T("墓苑", "墓苑", new Color(0.22f, 0.10f, 0.04f), new Color(1f, 0.48f, 0.12f), 0.030f),
            T("玩具回廊", "玩具", new Color(0.22f, 0.48f, 0.92f), new Color(0.45f, 0.95f, 1f), 0.012f),
            T("城塞村", "城塞", new Color(0.72f, 0.58f, 0.32f), new Color(1f, 0.88f, 0.55f), 0.012f),
            T("地下牢", "地下牢", new Color(0.12f, 0.10f, 0.08f), new Color(1f, 0.62f, 0.28f), 0.034f),
            T("廃トレーラー", "廃園", new Color(0.48f, 0.44f, 0.22f), new Color(1f, 0.86f, 0.42f), 0.022f),
            T("霓虹の穴", "霓虹", new Color(0.10f, 0.02f, 0.22f), new Color(1f, 0.25f, 0.95f), 0.020f),
            T("塔頂の環", "塔頂", new Color(0.42f, 0.16f, 0.22f), new Color(1f, 0.48f, 0.32f), 0.016f),
        };

        static readonly ThemeInfo[] BossThemes =
        {
            B("塔の番人", new Color(0.75f, 0.08f, 0.08f), new Color(1f, 0.35f, 0.28f), 0.022f),
            B("交差点の主", new Color(0.70f, 0.06f, 0.12f), new Color(1f, 0.38f, 0.28f), 0.020f),
            B("森の王", new Color(0.68f, 0.06f, 0.06f), new Color(1f, 0.40f, 0.25f), 0.022f),
            B("墓守", new Color(0.72f, 0.07f, 0.10f), new Color(1f, 0.36f, 0.24f), 0.022f),
            B("玩具の暴君", new Color(0.74f, 0.09f, 0.14f), new Color(1f, 0.42f, 0.30f), 0.020f),
            B("城塞の騎士長", new Color(0.78f, 0.07f, 0.07f), new Color(1f, 0.34f, 0.22f), 0.022f),
            B("獄長", new Color(0.70f, 0.04f, 0.06f), new Color(1f, 0.30f, 0.20f), 0.024f),
            B("廃園の影", new Color(0.74f, 0.08f, 0.10f), new Color(1f, 0.38f, 0.26f), 0.022f),
            B("霓虹の王", new Color(0.80f, 0.08f, 0.18f), new Color(1f, 0.32f, 0.40f), 0.022f),
            B("塔頂の神", new Color(0.88f, 0.04f, 0.08f), new Color(1f, 0.38f, 0.25f), 0.025f),
        };

        static ThemeInfo T(string label, string shortName, Color fog, Color light, float density)
        {
            return new ThemeInfo
            {
                label = label,
                shortName = shortName,
                fogColor = fog,
                lightTint = light,
                fogDensity = density,
                horror = false
            };
        }

        static ThemeInfo B(string label, Color fog, Color light, float density)
        {
            return new ThemeInfo
            {
                label = label,
                shortName = label,
                fogColor = fog,
                lightTint = light,
                fogDensity = density,
                horror = true
            };
        }

        public static int CurrentFloor => StudyStore.DungeonFloor;
        public static int MaxFloor => StudyStore.DungeonMaxFloor;
        public static bool IsBossFloor(int floor) => StudyStore.IsBossFloor(floor);
        public static bool CurrentIsBoss => IsBossFloor(CurrentFloor);

        public static int StageIndex(int floor)
        {
            return Mathf.Clamp((floor - 1) / 10, 0, StageCount - 1);
        }

        public static int ZoneIndex(int floor) => StageIndex(floor);

        public static int StageStartFloor(int stage)
        {
            stage = Mathf.Clamp(stage, 0, StageCount - 1);
            return stage * 10 + 1;
        }

        public static int StageBossFloor(int stage)
        {
            stage = Mathf.Clamp(stage, 0, StageCount - 1);
            return (stage + 1) * 10;
        }

        public static string StageShortName(int stage)
        {
            stage = Mathf.Clamp(stage, 0, StageCount - 1);
            return Themes[stage].shortName;
        }

        public static string StageRangeLabel(int stage)
        {
            return StageStartFloor(stage) + "–" + (StageBossFloor(stage) - 1) + "F / ボス" + StageBossFloor(stage) + "F";
        }

        public static ThemeInfo GetTheme(int floor, bool boss)
        {
            int s = StageIndex(floor);
            return boss ? BossThemes[s] : Themes[s];
        }

        public static float StatMultiplier(int floor, bool boss)
        {
            int level = Mathf.Clamp(floor, 1, StudyStore.MaxLevel);
            int points = StudyStore.BpRequiredForLevel(level) / Mathf.Max(1, StudyStore.GrowthStatCost);
            int each = points / 3;
            float grown = 1f + each / 40f;
            if (boss) grown *= 1.2f;
            return grown;
        }

        public static ShiftingMetropolis.Battle.CharacterStats ScaleEnemyStats(ShiftingMetropolis.Battle.CharacterStats baseStats, int floor, bool boss)
        {
            return ScaleEnemyStats(baseStats, floor, boss, StudyStore.DungeonWeek);
        }

        public static ShiftingMetropolis.Battle.CharacterStats ScaleEnemyStats(ShiftingMetropolis.Battle.CharacterStats baseStats, int floor, bool boss, int week)
        {
            week = Mathf.Clamp(week, 1, StudyStore.MaxDungeonWeek);
            floor = Mathf.Clamp(floor, 1, StudyStore.DungeonMaxFloor);
            int minutes = StudyStore.MinutesForTower(week, floor);
            int points = minutes / Mathf.Max(1, StudyStore.GrowthStatCost);
            int each = points / 5;
            int playerHp = StudyStore.BaseHp + each * 5;
            int playerAtk = StudyStore.BaseAttack + each;
            int playerDef = StudyStore.BaseDefense + each;

            // プレイヤーの防具装備（序盤でも防具数箇所で+6〜+10、進行で+20〜30）を想定
            int assumedArmorDef = Mathf.Min(28, 6 + (week - 1) * 3 + floor / 15);
            int effectivePlayerDef = playerDef + assumedArmorDef;

            const float assumedSkill = 1.2f;
            const float hits = 8f;
            int gap = Mathf.Max(8, Mathf.RoundToInt(playerHp / (hits * assumedSkill)));
            int hp = Mathf.RoundToInt(playerHp * (boss ? 1.16f : 1.02f));
            int atk = effectivePlayerDef + gap;
            int def = Mathf.Max(1, playerAtk - gap);
            if (boss)
            {
                atk = Mathf.RoundToInt(atk * 1.03f);
                def = Mathf.RoundToInt(def * 1.03f);
            }
            // 1周目の序盤だけ底上げする。1時間(必殺2回)では1階を落とせず、3時間で勝てる。
            if (week == 1 && floor <= 6)
            {
                float open = (7 - floor) / 6f;
                hp = Mathf.RoundToInt(hp * (1f + 1.00f * open));
                atk = Mathf.RoundToInt(atk * (1f + 0.12f * open));
            }
            hp = Mathf.RoundToInt(hp * 1.35f);
            atk = Mathf.RoundToInt(atk * 1.35f);
            int speed = StudyStore.BaseSpeed + Mathf.Min(StudyStore.MaxSpdLuckSteps, Mathf.RoundToInt(each * 0.55f));
            int luck = StudyStore.BaseLuck + Mathf.Min(StudyStore.MaxSpdLuckSteps, Mathf.RoundToInt(each * 0.55f));
            return new ShiftingMetropolis.Battle.CharacterStats(
                maxHp: Mathf.Max(1, hp),
                attack: Mathf.Max(1, atk),
                defense: def,
                speed: speed,
                luck: luck);
        }

        public static string FloorLabel(int floor, bool boss)
        {
            int week = Mathf.Clamp(StudyStore.DungeonWeek, 1, StudyStore.MaxDungeonWeek);
            string text = week + "周目  " + Mathf.Clamp(floor, 1, StudyStore.DungeonMaxFloor) + "F";
            if (boss) text += " ボス";
            return text;
        }
    }
}
