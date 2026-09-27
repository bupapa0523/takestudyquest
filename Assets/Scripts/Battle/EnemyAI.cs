using System.Collections.Generic;
using UnityEngine;

namespace ShiftingMetropolis.Battle
{
    public enum EnemyJob
    {
        Bruiser,
        Mage,
        Knight
    }

    public static class EnemyAI
    {
        public static SkillCommandType ChooseCommand(BattleCharacter enemy, BattleCharacter player, EnemyJob job)
        {
            if (enemy == null) return SkillCommandType.Charge;

            if (enemy.pendingGearSkill != SkillCommandType.Charge)
            {
                var pending = enemy.GetSkill(enemy.pendingGearSkill);
                if (pending != null && pending.requiredGear >= 3 && enemy.currentGear < pending.requiredGear && enemy.CanRaiseGear())
                    return SkillCommandType.Charge;
                enemy.pendingGearSkill = SkillCommandType.Charge;
                if (pending != null && enemy.CanUse(pending))
                {
                    enemy.lastCommand = pending.type;
                    return pending.type;
                }
            }

            var moves = new List<Skill>();
            if (enemy.skills != null)
            {
                for (int i = 0; i < enemy.skills.Count; i++)
                {
                    var s = enemy.skills[i];
                    if (s == null || s.type == SkillCommandType.Charge) continue;
                    moves.Add(s);
                }
            }
            if (moves.Count == 0) return SkillCommandType.Charge;

            if (enemy.CanRaiseGear())
            {
                float charge = enemy.currentGear <= 1 ? 0.62f : 0.45f;
                if (Random.value < charge) return SkillCommandType.Charge;
            }

            var varied = new List<Skill>();
            for (int i = 0; i < moves.Count; i++)
                if (moves[i].type != enemy.lastCommand) varied.Add(moves[i]);
            if (varied.Count == 0) varied = moves;

            var pick = varied[Random.Range(0, varied.Count)];
            if (pick.requiredGear >= 3 && enemy.currentGear < pick.requiredGear)
            {
                enemy.pendingGearSkill = pick.type;
                if (enemy.CanRaiseGear()) return SkillCommandType.Charge;
            }

            if (!enemy.CanUse(pick))
            {
                var ready = new List<Skill>();
                for (int i = 0; i < moves.Count; i++)
                    if (enemy.CanUse(moves[i]) && moves[i].type != enemy.lastCommand) ready.Add(moves[i]);
                if (ready.Count == 0)
                {
                    for (int i = 0; i < moves.Count; i++)
                        if (enemy.CanUse(moves[i])) ready.Add(moves[i]);
                }
                if (ready.Count == 0) return SkillCommandType.Charge;
                pick = ready[Random.Range(0, ready.Count)];
            }

            enemy.lastCommand = pick.type;
            return pick.type;
        }

        public static EnemyJob JobFromLabel(string label)
        {
            return EnemyKit.JobFromKind(EnemyKit.KindFrom(label));
        }
    }
}
