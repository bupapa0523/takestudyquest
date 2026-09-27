using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// 敵の技セット。序盤はギア1の格闘、終盤は演出の重い技。敵種でも中身を変える。
    /// </summary>
    public static class EnemyKit
    {
        public enum Kind { Killer, Monster, Shogun }

        static readonly string[] KillerG1 =
        {
            "kick_1", "kick_2", "kick_3", "drop_kick", "uppercut_jab", "chapa_kick", "bicycle_kick", "jump_smash"
        };
        static readonly string[] KillerG2 =
        {
            "fist_fight", "hurricane_kick", "knee_combo", "shoulder_throw", "capoeira_kick", "butterfly_twirl"
        };
        static readonly string[] KillerG3 =
        {
            "choke_lift", "ryuken", "rasengan", "kamehameha"
        };
        static readonly string[] KillerBoss =
        {
            "hakai", "shinra_tensei", "ryuken"
        };

        static readonly string[] MonsterG1 =
        {
            "element_fire", "element_water", "gunplay_shot", "spirit_gun", "arrow_shot", "element_light_punch"
        };
        static readonly string[] MonsterG2 =
        {
            "cero", "arcane_bolt", "meteora", "element_fire_burn", "telekinesis_barrage", "hound"
        };
        static readonly string[] MonsterG3 =
        {
            "kurohitsugi", "wide_cast_bolt", "element_dark_swallow", "makankosappo", "element_nature_pierce"
        };
        static readonly string[] MonsterBoss =
        {
            "kyoshiki_murasaki", "field_nuke", "gran_rey_cero", "hakai"
        };

        static readonly string[] ShogunG1 =
        {
            "kick_3", "jump_smash", "drop_kick", "uppercut_jab"
        };
        static readonly string[] ShogunG2 =
        {
            "getsuga", "cero", "choke_lift", "hurricane_kick"
        };
        static readonly string[] ShogunG3 =
        {
            "kurohitsugi", "shinra_tensei", "kamehameha", "makankosappo"
        };
        static readonly string[] ShogunBoss =
        {
            "hakai", "gran_rey_cero", "kurohitsugi", "shinra_tensei"
        };

        public static Kind KindFrom(string label)
        {
            if (string.IsNullOrEmpty(label)) return Kind.Killer;
            if (label.IndexOf("怪人", StringComparison.Ordinal) >= 0) return Kind.Monster;
            if (label.IndexOf("将軍", StringComparison.Ordinal) >= 0
                || label.IndexOf("ドリズル", StringComparison.Ordinal) >= 0
                || label.IndexOf("月光", StringComparison.Ordinal) >= 0
                || label.IndexOf("傘", StringComparison.Ordinal) >= 0
                || label.IndexOf("紅", StringComparison.Ordinal) >= 0
                || label.IndexOf("サミデール", StringComparison.Ordinal) >= 0)
                return Kind.Shogun;
            return Kind.Killer;
        }

        public static List<Skill> Build(int floor, bool boss, Kind kind)
        {
            return Build(floor, boss, kind, (int)kind);
        }

        public static List<Skill> Build(int floor, bool boss, string label)
        {
            return Build(floor, boss, KindFrom(label), 0);
        }

        static List<Skill> Build(int floor, bool boss, Kind kind, int salt)
        {
            var rng = new System.Random(floor * 9187 + (int)kind * 29 + salt + (boss ? 7 : 0));
            string[] g1 = Pool(kind, 1);
            string[] g2 = Pool(kind, 2);
            string[] g3 = Pool(kind, 3);
            string[] smashPool = boss ? BossPool(kind) : g3;

            string id1 = Pick(rng, g1, null, null);
            string id2 = Pick(rng, g2, id1, null);
            string id3 = Pick(rng, g3, id1, id2);

            var list = new List<Skill>
            {
                new Skill("ためる", SkillCommandType.Charge, 0, 1f, 1f),
                FromId(id1, SkillCommandType.Skill1),
                FromId(id2, SkillCommandType.Skill2),
                FromId(id3, SkillCommandType.Skill3),
            };
            if (boss)
            {
                string smashId = floor >= 50
                    ? Pick(rng, smashPool, id1, id2)
                    : Pick(rng, g2.Length > 0 ? g2 : g3, id1, id2);
                var smash = FromId(smashId, SkillCommandType.BrainSmash);
                smash.requiredGear = 1;
                smash.requiredGauge = 0;
                smash.isUltimate = true;
                list.Add(smash);
            }
            return list;
        }

        public static EnemyJob JobFromKind(Kind kind)
        {
            if (kind == Kind.Monster) return EnemyJob.Mage;
            if (kind == Kind.Shogun) return EnemyJob.Knight;
            return EnemyJob.Bruiser;
        }

        static string[] Pool(Kind kind, int gear)
        {
            if (kind == Kind.Monster)
            {
                if (gear <= 1) return MonsterG1;
                if (gear == 2) return MonsterG2;
                return MonsterG3;
            }
            if (kind == Kind.Shogun)
            {
                if (gear <= 1) return ShogunG1;
                if (gear == 2) return ShogunG2;
                return ShogunG3;
            }
            if (gear <= 1) return KillerG1;
            if (gear == 2) return KillerG2;
            return KillerG3;
        }

        static string[] BossPool(Kind kind)
        {
            if (kind == Kind.Monster) return MonsterBoss;
            if (kind == Kind.Shogun) return ShogunBoss;
            return KillerBoss;
        }

        static string Pick(System.Random rng, string[] pool, string avoidA, string avoidB)
        {
            if (pool == null || pool.Length == 0) return "kick_1";
            for (int n = 0; n < 8; n++)
            {
                string id = pool[rng.Next(pool.Length)];
                if (id != avoidA && id != avoidB && UltimateCatalog.Get(id) != null) return id;
            }
            return pool[0];
        }

        static float EnemyPower(UltimateSkillDefinition def)
        {
            float raw = def.MultiplierAtLevel(1);
            float cap = 1.2f;
            if (def.gear == UltimateGear.Gear2) cap = 1.7f;
            else if (def.gear == UltimateGear.Gear3) cap = 2.8f;
            else if (def.gear == UltimateGear.Brainstorm) cap = 3.3f;
            if (raw <= 0.05f) return raw;
            return Mathf.Min(raw, cap);
        }

        static Skill FromId(string id, SkillCommandType type)
        {
            var def = UltimateCatalog.Get(id);
            if (def == null)
            {
                var fallback = new Skill("殴る", type, 0, 1.3f, 0.9f);
                fallback.requiredGear = 1;
                return fallback;
            }
            int need = UltimateCatalog.GearNeed(def.gear);
            if (need > 3) need = 3;
            if (need < 1) need = 1;
            float power = EnemyPower(def);
            var skill = new Skill(def.displayName, type, 0, power, 0.88f, true);
            skill.requiredGear = need;
            skill.ultimateId = def.id;
            return skill;
        }
    }
}
