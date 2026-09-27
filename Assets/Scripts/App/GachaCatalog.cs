using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ShiftingMetropolis.Battle;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    public enum GachaRarity
    {
        Common,
        Uncommon,
        Rare,
        Legend
    }

    public class GachaRateRow
    {
        public GachaRarity rarity;
        public string name;
        public float percent;
    }

    public class GachaDrop
    {
        public string name;
        public GachaRarity rarity;
        public bool skill;
        public bool skin;
        public bool duplicate;
        public int version;
        public bool maxed;
    }

    public class GachaPullResult
    {
        public bool ok;
        public string message;
        public int refund;
        public readonly List<GachaDrop> drops = new List<GachaDrop>();
    }

    /// <summary>
    /// ガチャの中身と割合。数値はここだけ変えれば排出が変わる。
    /// </summary>
    public static class GachaCatalog
    {
        public const int PullCost = 5;
        public const int PullTenCount = 10;
        public const int DuplicateRefund = 2;

        // レアリティごとの全体排出確率目標（合計100%）
        public const float RateCommon = 52.5f;     // コモン (Common)
        public const float RateUncommon = 32.5f;   // アンコモン (Uncommon)
        public const float RateRare = 10.0f;       // エピック (Epic / Rare)
        public const float RateLegend = 5.0f;      // レジェンド (Legend)

        class Entry
        {
            public string id;
            public string name;
            public bool skill;
            public bool skin;
            public int weight;
            public GachaRarity rarity;
        }

        static List<Entry> pool;

        public static int WeightSum()
        {
            return TotalWeight(Pool());
        }

        public static int WeightOf(GachaRarity rarity)
        {
            var items = Pool();
            int sum = 0;
            for (int i = 0; i < items.Count; i++)
                if (items[i].rarity == rarity) sum += items[i].weight;
            return sum;
        }

        public static string RarityName(GachaRarity rarity)
        {
            switch (rarity)
            {
                case GachaRarity.Common: return "コモン";
                case GachaRarity.Uncommon: return "アンコモン";
                case GachaRarity.Rare: return "エピック";
                default: return "レジェンド";
            }
        }

        public static string RarityLabel(GachaRarity rarity)
        {
            switch (rarity)
            {
                case GachaRarity.Common: return "コモン (52.5%)";
                case GachaRarity.Uncommon: return "アンコモン (32.5%)";
                case GachaRarity.Rare: return "エピック (10.0%)";
                default: return "レジェンド (5.0%)";
            }
        }

        public static float RarityTotalRate(GachaRarity rarity)
        {
            switch (rarity)
            {
                case GachaRarity.Common: return RateCommon;
                case GachaRarity.Uncommon: return RateUncommon;
                case GachaRarity.Rare: return RateRare;
                default: return RateLegend;
            }
        }

        public static List<GachaRateRow> RateRows()
        {
            var items = Pool();
            var rows = new List<GachaRateRow>();
            var order = new[] { GachaRarity.Common, GachaRarity.Uncommon, GachaRarity.Rare, GachaRarity.Legend };
            for (int r = 0; r < order.Length; r++)
            {
                var curRarity = order[r];
                int countInRarity = 0;
                for (int i = 0; i < items.Count; i++)
                    if (items[i].rarity == curRarity) countInRarity++;

                float eachPercent = countInRarity > 0 ? RarityTotalRate(curRarity) / countInRarity : 0f;

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].rarity != curRarity) continue;
                    rows.Add(new GachaRateRow
                    {
                        rarity = curRarity,
                        name = items[i].name,
                        percent = eachPercent
                    });
                }
            }
            return rows;
        }

        public static GachaPullResult Pull()
        {
            return PullMany(1);
        }

        public static GachaPullResult PullMany(int count)
        {
            var result = new GachaPullResult();
            if (count < 1) count = 1;
            if (StudyStore.GrowthLocked())
            {
                result.message = "バトル中は引けません";
                return result;
            }
            int cost = PullCost * count;
            if (!StudyStore.TrySpendDiamonds(cost))
            {
                result.message = "ダイヤが足りません";
                return result;
            }
            var lines = new StringBuilder();
            int got = 0;
            for (int n = 0; n < count; n++)
            {
                var item = Roll();
                if (item == null)
                {
                    StudyStore.AddDiamonds(PullCost);
                    continue;
                }
                var drop = new GachaDrop
                {
                    name = item.name,
                    rarity = item.rarity,
                    skill = item.skill,
                    skin = item.skin
                };
                if (item.skin)
                {
                    int before = StudyStore.BodyLevel(item.id);
                    if (before <= 0)
                    {
                        StudyStore.GrantBody(item.id);
                        drop.version = 1;
                    }
                    else if (before >= StudyStore.SkillVersionMax)
                    {
                        drop.duplicate = true;
                        drop.maxed = true;
                        drop.version = before;
                        StudyStore.AddDiamonds(PullCost);
                        result.refund += PullCost;
                    }
                    else
                    {
                        drop.duplicate = true;
                        drop.version = StudyStore.RaiseBodyVersion(item.id);
                    }
                }
                else if (item.skill)
                {
                    int before = StudyStore.UltimateLevel(item.id);
                    if (before <= 0)
                    {
                        StudyStore.GrantSkill(item.id);
                        drop.version = 1;
                    }
                    else if (before >= StudyStore.SkillVersionMax)
                    {
                        drop.duplicate = true;
                        drop.maxed = true;
                        drop.version = before;
                        StudyStore.AddDiamonds(PullCost);
                        result.refund += PullCost;
                    }
                    else
                    {
                        drop.duplicate = true;
                        drop.version = StudyStore.RaiseSkillVersion(item.id);
                    }
                }
                else
                {
                    int before = StudyStore.ArmorLevel(item.id);
                    if (before <= 0)
                    {
                        StudyStore.GrantArmor(item.id);
                        drop.version = 1;
                    }
                    else if (before >= StudyStore.SkillVersionMax)
                    {
                        drop.duplicate = true;
                        drop.maxed = true;
                        drop.version = before;
                        StudyStore.AddDiamonds(PullCost);
                        result.refund += PullCost;
                    }
                    else
                    {
                        drop.duplicate = true;
                        drop.version = StudyStore.RaiseArmorVersion(item.id);
                    }
                }
                result.drops.Add(drop);
                if (lines.Length > 0) lines.Append('\n');
                lines.Append(RarityLabel(item.rarity));
                lines.Append("  ");
                lines.Append(item.name);
                if (drop.version > 0) lines.Append("  " + StudyStore.VersionMark(drop.version));
                if (drop.maxed) lines.Append("  還元" + PullCost);
                got++;
            }
            if (got == 0)
            {
                result.message = "中身がありません";
                return result;
            }
            result.ok = true;
            result.message = lines.ToString();
            return result;
        }

        static int TotalWeight(List<Entry> items)
        {
            int sum = 0;
            for (int i = 0; i < items.Count; i++) sum += Mathf.Max(0, items[i].weight);
            return sum;
        }

        static GachaRarity RollRarity()
        {
            float roll = Random.Range(0f, 100f);
            if (roll < RateLegend) return GachaRarity.Legend;
            if (roll < RateLegend + RateRare) return GachaRarity.Rare;
            if (roll < RateLegend + RateRare + RateUncommon) return GachaRarity.Uncommon;
            return GachaRarity.Common;
        }

        static Entry Roll()
        {
            var items = Pool();
            GachaRarity targetRarity = RollRarity();

            var matching = new List<Entry>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].rarity == targetRarity) matching.Add(items[i]);
            }

            if (matching.Count == 0)
            {
                // 万が一該当レアリティが空ならプール全体からランダム
                return items[Random.Range(0, items.Count)];
            }

            return matching[Random.Range(0, matching.Count)];
        }

        static List<Entry> Pool()
        {
            if (pool != null) return pool;
            pool = new List<Entry>();
            foreach (var def in UltimateCatalog.AllDefs)
            {
                if (def == null || StudyStore.IsFreeSkill(def.id)) continue;
                pool.Add(new Entry
                {
                    id = def.id,
                    name = "技  " + def.displayName,
                    skill = true,
                    weight = SkillWeight(def.gear),
                    rarity = SkillRarity(def.gear)
                });
            }
            for (int extra = HeroAppearance.ArmorExtraFirst; extra <= HeroAppearance.ArmorExtraLast; extra++)
            {
                for (int option = 1; option <= HeroAppearance.ArmorTypeOptions; option++)
                {
                    pool.Add(new Entry
                    {
                        id = HeroAppearance.ArmorPieceId(extra, option),
                        name = "装備  " + HeroAppearance.ArmorPieceName(extra, option),
                        skill = false,
                        weight = ArmorWeight(option),
                        rarity = ArmorRarity(option)
                    });
                }
            }
            var bodies = new List<HeroAppearance.GachaBody>();
            HeroAppearance.CollectGachaBodies(bodies);
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                GachaRarity r = GachaRarity.Common;
                if (body.weight == 3) r = GachaRarity.Legend;
                else if (body.weight == 2) r = GachaRarity.Rare;
                else if (body.weight == 1) r = GachaRarity.Uncommon;
                else r = GachaRarity.Common;

                pool.Add(new Entry
                {
                    id = body.id,
                    name = "外見  " + body.label,
                    skill = false,
                    skin = true,
                    weight = 1,
                    rarity = r
                });
            }
            return pool;
        }

        static int SkillWeight(UltimateGear gear)
        {
            return 1;
        }

        static int ArmorWeight(int option)
        {
            return 1;
        }

        static GachaRarity SkillRarity(UltimateGear gear)
        {
            switch (gear)
            {
                case UltimateGear.Gear1: return GachaRarity.Common;
                case UltimateGear.Gear2: return GachaRarity.Uncommon;
                case UltimateGear.Gear3: return GachaRarity.Rare;
                default: return GachaRarity.Legend;
            }
        }

        static GachaRarity ArmorRarity(int option)
        {
            if (option <= 2) return GachaRarity.Common;
            if (option <= 4) return GachaRarity.Uncommon;
            if (option == 5) return GachaRarity.Rare;
            return GachaRarity.Legend;
        }

        public struct GiftRoll
        {
            public string id;
            public string name;
            public string kind;
        }

        public static GiftRoll RollGift(int seed)
        {
            var items = Pool();
            var roll = new GiftRoll();
            if (items.Count == 0) return roll;
            var rng = new System.Random(seed & 0x7fffffff);
            double ticket = rng.NextDouble() * 100.0;
            GachaRarity rarity = GachaRarity.Common;
            if (ticket < RateLegend) rarity = GachaRarity.Legend;
            else if (ticket < RateLegend + RateRare) rarity = GachaRarity.Rare;
            else if (ticket < RateLegend + RateRare + RateUncommon) rarity = GachaRarity.Uncommon;
            var matching = new List<Entry>();
            for (int i = 0; i < items.Count; i++)
                if (items[i].rarity == rarity) matching.Add(items[i]);
            if (matching.Count == 0) matching = items;
            var item = matching[rng.Next(matching.Count)];
            roll.id = item.id;
            roll.name = item.name;
            roll.kind = item.skill ? "skill" : item.skin ? "skin" : "armor";
            return roll;
        }

        public static string GrantGift(string id, string kind)
        {
            if (string.IsNullOrEmpty(id)) return "";
            bool skill = kind == "skill";
            bool skin = kind == "skin";
            if (skill)
            {
                int before = StudyStore.UltimateLevel(id);
                if (before <= 0)
                {
                    StudyStore.GrantSkill(id);
                    return "新しい技";
                }
                if (before >= StudyStore.SkillVersionMax)
                {
                    StudyStore.AddDiamonds(PullCost);
                    return "技は" + StudyStore.SkillVersionMax + "まで。ダイヤ +" + PullCost;
                }
                int version = StudyStore.RaiseSkillVersion(id);
                return "技が " + StudyStore.VersionMark(version) + " に";
            }
            if (skin)
            {
                int before = StudyStore.BodyLevel(id);
                if (before <= 0)
                {
                    StudyStore.GrantBody(id);
                    return "新しい外見";
                }
                if (before >= StudyStore.SkillVersionMax)
                {
                    StudyStore.AddDiamonds(PullCost);
                    return "外見は" + StudyStore.SkillVersionMax + "まで。ダイヤ +" + PullCost;
                }
                int version = StudyStore.RaiseBodyVersion(id);
                return "外見が " + StudyStore.VersionMark(version) + " に";
            }
            int armorBefore = StudyStore.ArmorLevel(id);
            if (armorBefore <= 0)
            {
                StudyStore.GrantArmor(id);
                return "新しい装備";
            }
            if (armorBefore >= StudyStore.SkillVersionMax)
            {
                StudyStore.AddDiamonds(PullCost);
                return "装備は" + StudyStore.SkillVersionMax + "まで。ダイヤ +" + PullCost;
            }
            int armorVersion = StudyStore.RaiseArmorVersion(id);
            return "装備が " + StudyStore.VersionMark(armorVersion) + " に";
        }
    }
}
