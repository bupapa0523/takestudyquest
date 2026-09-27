using System.Collections.Generic;
using UnityEngine;

namespace ShiftingMetropolis.Battle
{
    public enum BasicMotion
    {
        Punch,
        Kick,
        Bolt
    }

    public enum BasicHitEffect
    {
        None,
        GaugeOnHit,
        Stun,
        Burn
    }

    public class BasicSkillDefinition
    {
        public string id;
        public string displayName;
        public SkillCommandType slot;
        public BasicMotion motion;
        public Color vfxColor;
        public int requiredGauge;
        public float powerMultiplier;
        public float accuracy;
        public BasicHitEffect hitEffect;
        public string hint;
    }

    public static class SkillCatalog
    {
        static readonly Dictionary<string, BasicSkillDefinition> All = new Dictionary<string, BasicSkillDefinition>();

        static SkillCatalog()
        {
            Add(new BasicSkillDefinition
            {
                id = "jab", displayName = "ジャブ", slot = SkillCommandType.Skill1,
                motion = BasicMotion.Punch, vfxColor = new Color(1f, 0.9f, 0.45f),
                requiredGauge = 0, powerMultiplier = 1.25f, accuracy = 0.94f,
                hitEffect = BasicHitEffect.GaugeOnHit, hint = "当たるとゲージ+8"
            });
            Add(new BasicSkillDefinition
            {
                id = "straight", displayName = "ストレート", slot = SkillCommandType.Skill1,
                motion = BasicMotion.Punch, vfxColor = new Color(1f, 0.78f, 0.35f),
                requiredGauge = 0, powerMultiplier = 1.55f, accuracy = 0.9f,
                hitEffect = BasicHitEffect.None, hint = "重いパンチ"
            });
            Add(new BasicSkillDefinition
            {
                id = "body", displayName = "ボディ", slot = SkillCommandType.Skill1,
                motion = BasicMotion.Punch, vfxColor = new Color(1f, 0.55f, 0.25f),
                requiredGauge = 0, powerMultiplier = 1.35f, accuracy = 0.92f,
                hitEffect = BasicHitEffect.Stun, hint = "たまにスタン"
            });

            Add(new BasicSkillDefinition
            {
                id = "roundhouse", displayName = "回し蹴り", slot = SkillCommandType.Skill2,
                motion = BasicMotion.Kick, vfxColor = new Color(1f, 0.5f, 0.15f),
                requiredGauge = 0, powerMultiplier = 1.9f, accuracy = 0.88f,
                hitEffect = BasicHitEffect.Stun, hint = "スタンしやすい"
            });
            Add(new BasicSkillDefinition
            {
                id = "dropkick", displayName = "ドロップキック", slot = SkillCommandType.Skill2,
                motion = BasicMotion.Kick, vfxColor = new Color(1f, 0.35f, 0.35f),
                requiredGauge = 0, powerMultiplier = 2.15f, accuracy = 0.82f,
                hitEffect = BasicHitEffect.None, hint = "高威力・当たりにくい"
            });
            Add(new BasicSkillDefinition
            {
                id = "sweep", displayName = "足払い", slot = SkillCommandType.Skill2,
                motion = BasicMotion.Kick, vfxColor = new Color(0.7f, 0.85f, 0.4f),
                requiredGauge = 0, powerMultiplier = 1.4f, accuracy = 0.93f,
                hitEffect = BasicHitEffect.Stun, hint = "安い蹴り"
            });

            Add(new BasicSkillDefinition
            {
                id = "bolt", displayName = "魔弾", slot = SkillCommandType.Skill3,
                motion = BasicMotion.Bolt, vfxColor = new Color(0.35f, 0.8f, 1f),
                requiredGauge = 0, powerMultiplier = 2.45f, accuracy = 0.9f,
                hitEffect = BasicHitEffect.Burn, hint = "やけど"
            });
            Add(new BasicSkillDefinition
            {
                id = "fireball", displayName = "火弾", slot = SkillCommandType.Skill3,
                motion = BasicMotion.Bolt, vfxColor = new Color(1f, 0.4f, 0.15f),
                requiredGauge = 0, powerMultiplier = 2.2f, accuracy = 0.88f,
                hitEffect = BasicHitEffect.Burn, hint = "強いやけど"
            });
            Add(new BasicSkillDefinition
            {
                id = "icebolt", displayName = "氷弾", slot = SkillCommandType.Skill3,
                motion = BasicMotion.Bolt, vfxColor = new Color(0.55f, 0.85f, 1f),
                requiredGauge = 0, powerMultiplier = 2.05f, accuracy = 0.92f,
                hitEffect = BasicHitEffect.Stun, hint = "凍らせてスタン"
            });
        }

        static void Add(BasicSkillDefinition def) => All[def.id] = def;

        public static BasicSkillDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return All.TryGetValue(id, out var d) ? d : null;
        }

        public static IEnumerable<BasicSkillDefinition> AllDefs => All.Values;

        public static List<BasicSkillDefinition> ForSlot(SkillCommandType slot)
        {
            var list = new List<BasicSkillDefinition>();
            foreach (var d in All.Values)
                if (d.slot == slot) list.Add(d);
            return list;
        }

        public static string DefaultId(SkillCommandType slot)
        {
            switch (slot)
            {
                case SkillCommandType.Skill1: return "jab";
                case SkillCommandType.Skill2: return "roundhouse";
                default: return "bolt";
            }
        }
    }

    public static class ShogunKit
    {
        public static readonly BasicSkillDefinition Bolt = new BasicSkillDefinition
        {
            id = "shogun_bolt", displayName = "黒弾", slot = SkillCommandType.Skill3,
            motion = BasicMotion.Bolt, vfxColor = new Color(0.04f, 0.01f, 0.05f),
            requiredGauge = 0, powerMultiplier = 1.7f, accuracy = 0.88f,
            hitEffect = BasicHitEffect.None, hint = "黒い球で殺す"
        };

        public static BasicSkillDefinition ForSlot(SkillCommandType slot)
        {
            if (slot == SkillCommandType.Charge) return null;
            return Bolt;
        }
    }
}
