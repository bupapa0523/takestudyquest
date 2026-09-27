using UnityEngine;

namespace ShiftingMetropolis.Battle
{
    /// <summary>属性。Unset は属性なしの既存技。</summary>
    public enum BattleElement
    {
        Unset,
        Fire,
        Water,
        Light,
        Nature,
        Dark,
        Mu
    }

    public static class BattleElementUtil
    {
        public static string Label(BattleElement e)
        {
            switch (e)
            {
                case BattleElement.Fire: return "炎";
                case BattleElement.Water: return "水";
                case BattleElement.Light: return "光";
                case BattleElement.Nature: return "木";
                case BattleElement.Dark: return "闇";
                case BattleElement.Mu: return "無";
                default: return "";
            }
        }

        public static Color Accent(BattleElement e)
        {
            switch (e)
            {
                case BattleElement.Fire: return new Color(1f, 0.42f, 0.12f);
                case BattleElement.Water: return new Color(0.28f, 0.72f, 1f);
                case BattleElement.Light: return new Color(1f, 0.95f, 0.55f);
                case BattleElement.Nature: return new Color(0.35f, 0.88f, 0.38f);
                case BattleElement.Dark: return new Color(0.42f, 0.08f, 0.55f);
                case BattleElement.Mu: return new Color(0.82f, 0.84f, 0.9f);
                default: return Color.white;
            }
        }

        public static bool IsElementSkill(BattleElement e) => e != BattleElement.Unset;
    }
}
