using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftingMetropolis.Battle
{
    public class BattleCharacter
    {
        public string characterName;
        public CharacterStats baseStats;
        public int currentHp;
        public Action<int> onHpLoss;
        public int currentGauge;
        public int maxGauge = 100;
        public int gaugePerCharge = 30;
        public int currentGear = 1;
        public SkillCommandType pendingGearSkill;
        public SkillCommandType lastCommand;
        public const int MaxGear = 3;
        public int brainSmashChargesLeft = 3;

        public bool guarding;
        public bool focused;
        public int burnTurns;
        public int burnDamage;
        public int soakTurns;
        public int soakDamage;
        public int lightMarkTurns;
        public int stunTurns;
        public int lastBurnDamage;
        public int lastSoakDamage;
        public bool skippedFromStun;
        public int atkBoostTurns;
        public float atkBoostMul = 1f;
        public int defBoostTurns;
        public float defBoostMul = 1f;
        public int evadeTurns;
        public float evadeBonus;
        public int senseInvertTurns;
        public bool senseInvertEnded;
        public int shogunFormTurns;
        public bool shogunFormEnded;
        public int voidRealmTurns;
        public bool holdBuffTick;

        public List<Skill> skills;
        public bool isPlayer;
        public Transform visual;

        public BattleCharacter(string characterName, CharacterStats baseStats, List<Skill> skills, bool isPlayer, Transform visual)
        {
            this.characterName = characterName;
            this.baseStats = baseStats;
            this.currentHp = baseStats.maxHp;
            this.currentGauge = isPlayer ? 20 : 15;
            this.currentGear = 1;
            this.skills = skills;
            this.isPlayer = isPlayer;
            this.visual = visual;
        }

        public bool IsAlive => currentHp > 0;

        public float HpRatio => baseStats.maxHp <= 0 ? 0f : Mathf.Clamp01((float)currentHp / baseStats.maxHp);

        public Skill GetSkill(SkillCommandType type)
        {
            if (skills == null) return null;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].type == type) return skills[i];
            }
            return null;
        }

        public bool CanUse(Skill skill)
        {
            if (skill == null) return false;
            if (isPlayer)
            {
                if (senseInvertTurns > 0 || shogunFormTurns > 0)
                    return true;
                if (skill.type == SkillCommandType.BrainSmash)
                    return ShiftingMetropolis.Progress.StudyStore.CanUseBrainSmash();
                if (followUpStrike && skill.type != SkillCommandType.Charge)
                    return true;
                return currentGear >= Mathf.Max(1, skill.requiredGear);
            }
            if (skill.type == SkillCommandType.Charge) return currentGear < MaxGear;
            if (skill.type == SkillCommandType.BrainSmash)
                return brainSmashChargesLeft > 0;
            if (followUpStrike)
                return true;
            return currentGear >= Mathf.Max(1, skill.requiredGear);
        }

        public bool BeginTurn()
        {
            lastBurnDamage = 0;
            lastSoakDamage = 0;
            skippedFromStun = false;
            guarding = false;

            if (burnTurns > 0 && IsAlive)
            {
                lastBurnDamage = Mathf.Max(1, burnDamage);
                TakeDamage(lastBurnDamage);
                burnTurns--;
            }

            if (soakTurns > 0 && soakDamage > 0 && IsAlive)
            {
                lastSoakDamage = Mathf.Max(1, soakDamage);
                TakeDamage(lastSoakDamage);
            }

            if (!IsAlive) return false;

            if (stunTurns > 0)
            {
                stunTurns--;
                skippedFromStun = true;
                return false;
            }

            bool hold = holdBuffTick;
            holdBuffTick = false;
            if (!hold)
            {
                if (atkBoostTurns > 0)
                {
                    atkBoostTurns--;
                    if (atkBoostTurns <= 0) atkBoostMul = 1f;
                }
                if (defBoostTurns > 0)
                {
                    defBoostTurns--;
                    if (defBoostTurns <= 0) defBoostMul = 1f;
                }
                if (evadeTurns > 0)
                {
                    evadeTurns--;
                    if (evadeTurns <= 0) evadeBonus = 0f;
                }
                if (senseInvertTurns > 0)
                {
                    senseInvertTurns--;
                    if (senseInvertTurns <= 0) senseInvertEnded = true;
                }
                if (shogunFormTurns > 0)
                {
                    shogunFormTurns--;
                    if (shogunFormTurns <= 0) shogunFormEnded = true;
                }
                if (voidRealmTurns > 0) voidRealmTurns--;
            }
            if (soakTurns > 0)
            {
                soakTurns--;
                if (soakTurns <= 0) soakDamage = 0;
            }
            if (lightMarkTurns > 0) lightMarkTurns--;

            return true;
        }

        public void ApplyCharge()
        {
            currentGauge = Mathf.Min(maxGauge, currentGauge + gaugePerCharge);
            guarding = true;
            focused = true;
            RaiseGear();
        }

        public bool followUpStrike;

        public bool RaiseGear()
        {
            if (currentGear >= MaxGear) return false;
            int gain = 1;
            if (baseStats != null && UnityEngine.Random.value < LuckGearJumpChance(baseStats.luck))
                gain = 2;
            currentGear = Mathf.Min(MaxGear, currentGear + gain);
            guarding = true;
            focused = true;
            return true;
        }

        public static float LuckGearJumpChance(int luck)
        {
            float u = Mathf.Clamp01((luck - ShiftingMetropolis.Progress.StudyStore.BaseLuck) / (float)ShiftingMetropolis.Progress.StudyStore.MaxSpdLuckSteps);
            return u * 0.36f;
        }

        public bool CanRaiseGear()
        {
            return currentGear < MaxGear;
        }

        public void SpendForSkill(Skill skill)
        {
            if (skill == null) return;
            if (followUpStrike) return;
            if (isPlayer)
            {
                if (skill.type == SkillCommandType.BrainSmash) return;
                if (senseInvertTurns > 0 || shogunFormTurns > 0) return;
                currentGear = GearAfterCast(skill);
                return;
            }
            if (skill.type == SkillCommandType.BrainSmash)
                brainSmashChargesLeft = Mathf.Max(0, brainSmashChargesLeft - 1);
            else if (skill.type != SkillCommandType.Charge)
                currentGear = GearAfterCast(skill);
        }

        int GearAfterCast(Skill skill)
        {
            int need = Mathf.Clamp(skill.requiredGear, 1, MaxGear);
            if (need <= 1) return currentGear;
            if (need >= currentGear) return 1;
            return need;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            currentHp = Mathf.Max(0, currentHp - amount);
            if (onHpLoss != null) onHpLoss(amount);
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            currentHp = Mathf.Min(baseStats.maxHp, currentHp + amount);
        }

        public void OnDealHit(Skill skill)
        {
            if (skill != null && skill.type == SkillCommandType.Skill1)
                currentGauge = Mathf.Min(maxGauge, currentGauge + 8);
        }

        public static float DamageAilmentChance(int luck)
        {
            float u = Mathf.Clamp01((luck - ShiftingMetropolis.Progress.StudyStore.BaseLuck) / (float)ShiftingMetropolis.Progress.StudyStore.MaxSpdLuckSteps);
            return 0.50f + u * 0.20f;
        }

        public static float CrowdControlChance(int luck)
        {
            float u = Mathf.Clamp01((luck - ShiftingMetropolis.Progress.StudyStore.BaseLuck) / (float)ShiftingMetropolis.Progress.StudyStore.MaxSpdLuckSteps);
            return 0.10f + u * 0.20f;
        }

        public static float AilmentChance(int luck)
        {
            return CrowdControlChance(luck);
        }

        public void OnReceiveHit(Skill skill, int attackStat, int attackerLuck = 0)
        {
            if (skill == null) return;
            float ccChance = CrowdControlChance(attackerLuck);
            float dmgChance = DamageAilmentChance(attackerLuck);
            if (skill.type == SkillCommandType.Skill2)
            {
                if (UnityEngine.Random.value < ccChance) stunTurns = Mathf.Max(stunTurns, 1);
            }
            else if (skill.type == SkillCommandType.Skill3)
            {
                if (UnityEngine.Random.value < dmgChance)
                {
                    burnTurns = Mathf.Max(burnTurns, 1);
                    burnDamage = Mathf.Max(4, attackStat / 8);
                }
            }
            else if (skill.type == SkillCommandType.BrainSmash)
            {
                if (UnityEngine.Random.value < ccChance) stunTurns = Mathf.Max(stunTurns, 1);
            }
        }

        public void ApplyUltimateBuff(string kind)
        {
            if (kind == "kaioken")
            {
                atkBoostTurns = 2;
                atkBoostMul = 1.22f;
                int recoil = Mathf.Max(1, currentHp / 18);
                TakeDamage(recoil);
            }
            else if (kind == "reiatsu")
            {
                atkBoostTurns = 2;
                atkBoostMul = 1.15f;
                focused = true;
            }
            else if (kind == "sakanade")
            {
                senseInvertTurns = 2;
                evadeTurns = 2;
                evadeBonus = 0.04f;
            }
            else if (kind == "shogun_henshin")
            {
                atkBoostTurns = 2;
                atkBoostMul = 1.15f;
                defBoostTurns = 2;
                defBoostMul = 1.15f;
                shogunFormTurns = 2;
            }
            else if (kind == "element_mu")
            {
                voidRealmTurns = 2;
            }
            holdBuffTick = true;
        }

        public string StatusShort()
        {
            var parts = new List<string>();
            if (guarding) parts.Add("防御");
            if (focused) parts.Add("集中");
            if (atkBoostTurns > 0) parts.Add("攻撃↑" + atkBoostTurns);
            if (defBoostTurns > 0) parts.Add("防御↑" + defBoostTurns);
            if (evadeTurns > 0) parts.Add("回避↑" + evadeTurns);
            if (burnTurns > 0) parts.Add("やけど" + burnTurns);
            if (soakTurns > 0) parts.Add("浸水" + soakTurns);
            if (lightMarkTurns > 0) parts.Add("光印" + lightMarkTurns);
            if (voidRealmTurns > 0) parts.Add("白虚" + voidRealmTurns);
            if (stunTurns > 0) parts.Add("スタン");
            return parts.Count == 0 ? "" : string.Join(" ", parts);
        }

        public void SetSkill(Skill skill)
        {
            if (skill == null || skills == null) return;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].type == skill.type)
                {
                    skills[i] = skill;
                    return;
                }
            }
            skills.Add(skill);
        }
    }
}
