using UnityEngine;

namespace ShiftingMetropolis.Battle
{
    public static class ElementCombat
    {
        public static float ActorDamageMultiplier(BattleCharacter actor)
        {
            if (actor != null && actor.voidRealmTurns > 0) return 1.12f;
            return 1f;
        }

        public static void ApplyOnHit(BattleCharacter actor, BattleCharacter target, BattleElement element, int attackStat, int damageDealt, string skillId = null)
        {
            if (actor == null || target == null || !BattleElementUtil.IsElementSkill(element)) return;

            bool smash = skillId == "element_fire_core"
                || skillId == "element_water_spear"
                || skillId == "element_light_cross"
                || skillId == "element_nature_jaw"
                || skillId == "element_dark_hex"
                || skillId == "element_mu_press";
            switch (element)
            {
                case BattleElement.Fire:
                    target.burnTurns = Mathf.Max(target.burnTurns, 2);
                    target.burnDamage = Mathf.Max(target.burnDamage, smash
                        ? Mathf.Max(7, attackStat / 6)
                        : Mathf.Max(5, attackStat / 7));
                    break;
                case BattleElement.Water:
                    target.soakTurns = Mathf.Max(target.soakTurns, 2);
                    if (skillId == "element_water_drown" || skillId == "element_water_spear")
                        target.soakDamage = Mathf.Max(target.soakDamage, smash
                            ? Mathf.Max(6, attackStat / 6)
                            : Mathf.Max(4, attackStat / 8));
                    break;
                case BattleElement.Light:
                    target.lightMarkTurns = Mathf.Max(target.lightMarkTurns, 2);
                    break;
                case BattleElement.Nature:
                case BattleElement.Dark:
                case BattleElement.Mu:
                    target.stunTurns = Mathf.Max(target.stunTurns, 1);
                    break;
            }
        }

        public static float SoakDamageFactor(BattleCharacter target)
        {
            if (target != null && target.soakTurns > 0) return 1.12f;
            return 1f;
        }

        public static float LightMarkEvasionPenalty(BattleCharacter target)
        {
            if (target != null && target.lightMarkTurns > 0) return 0.04f;
            return 0f;
        }
    }
}
