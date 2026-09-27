using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    public static class HeroAppearance
    {
        public const int SlotSkin = 0;
        public const string PumpkinBodyId = "special:pumpkin";
        const string PumpkinMeshPath = "Assets/hotondo/ステージ/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/pumpkin_orange_jackolantern.fbx";
        public const int ArmorExtraFirst = 6;
        public const int ArmorExtraLast = 11;
        public const int ArmorTypeOptions = 6;

        public enum SkinKind { GanzSe, Synty, PolytopeMale, PolytopeFemale, Hotondo, HotondoWarrior, HotondoShogun }

        class SkinOption
        {
            public string label;
            public string path;
            public SkinKind kind;
            public SkinOption(string label, string path, SkinKind kind)
            {
                this.label = label;
                this.path = path;
                this.kind = kind;
            }
        }

        static readonly SkinOption[] AllSkins =
        {
            new SkinOption("常闇の量産兵", "Assets/URP GanzSe Free Modular Character Pack/Prefabs/Modular Character/GanzSe Free Modular Character Update 1_1.prefab", SkinKind.GanzSe),
            new SkinOption("疾風の剣士", "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_01/Starter_01.prefab", SkinKind.Synty),
            new SkinOption("蒼炎の剣士", "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_02/Starter_02.prefab", SkinKind.Synty),
            new SkinOption("黒鉄の剣士", "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_03/Starter_03.prefab", SkinKind.Synty),
            new SkinOption("銀翼の剣士", "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_04/Starter_04.prefab", SkinKind.Synty),
            new SkinOption("聖騎士・裂空", "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_Armors/PT_Lowpoly_Armors_Male_Moduar_Free.prefab", SkinKind.PolytopeMale),
            new SkinOption("聖騎士・月影", "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_Armors/PT_Lowpoly_Armors_Female_Moduar_Free.prefab", SkinKind.PolytopeFemale),
        };

        static SkinOption[] catalog;
        static List<int> availableIdx;

        static SkinOption[] Catalog()
        {
            if (catalog != null) return catalog;
            var list = new List<SkinOption>(AllSkins.Length + 160);
            list.AddRange(AllSkins);
#if UNITY_EDITOR
            DiscoverHotondo(list);
#else
            var table = PlayerAssetTable.Load();
            if (table != null && table.extraSkinPaths != null)
            {
                int n = table.extraSkinPaths.Length;
                for (int i = 0; i < n; i++)
                {
                    string path = table.extraSkinPaths[i];
                    if (string.IsNullOrEmpty(path)) continue;
                    string file = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (!PlayableHumanSkin(file)) continue;
                    string label = HotondoLabel(file, path);
                    if (string.IsNullOrEmpty(label) && table.extraSkinLabels != null && i < table.extraSkinLabels.Length)
                        label = table.extraSkinLabels[i];
                    int kind = table.extraSkinKinds != null && i < table.extraSkinKinds.Length
                        ? table.extraSkinKinds[i] : (int)SkinKind.Hotondo;
                    list.Add(new SkinOption(label, path, (SkinKind)kind));
                }
            }
#endif
            bool hasPumpkin = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].path == PumpkinBodyId) { hasPumpkin = true; break; }
            }
            if (!hasPumpkin)
                list.Add(new SkinOption("頭パンプキン", PumpkinBodyId, SkinKind.Hotondo));
            catalog = list.ToArray();
            return catalog;
        }

#if UNITY_EDITOR
        static void DiscoverHotondo(List<SkinOption> list)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var found = AssetDatabase.FindAssets("t:Model", new[] { "Assets/hotondo" });
            for (int i = 0; i < found.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(found[i]);
                if (!WantedHotondoSkin(path)) continue;
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!KeepHotondoFile(file)) continue;
                AddIfHumanoid(list, seen, path, IsShogunFile(file) ? SkinKind.HotondoShogun : SkinKind.Hotondo);
            }
        }

        static readonly string[] HotondoKeep =
        {
            "SHOGUN_Lowpoly", "DRIZZLE_Lowpoly", "GEKKOU_lowpoly", "KASA_Lowpoly", "KURENAI_lowpoly", "SAMIDALE_lowpoly",
            "Character_01", "Character_06", "Character_11", "Character_16", "Character_32",
            "Character_Female_02", "Character_Female_07", "Character_Female_12", "Character_Female_16", "Character_33_Female",
        };

        static bool IsWeirdFile(string file)
        {
            if (string.IsNullOrEmpty(file)) return false;
            if (Contains(file, "Police") || Contains(file, "Firefighter") || Contains(file, "Doctor") || Contains(file, "Sheriff"))
                return false;
            return LastNumber(file) >= 33;
        }

        static bool KeepHotondoFile(string file)
        {
            if (string.IsNullOrEmpty(file)) return false;
            if (IsBrokenSkinFile(file)) return false;
            if (IsKillerFile(file) || IsWeirdFile(file)) return true;
            for (int i = 0; i < HotondoKeep.Length; i++)
            {
                if (string.Equals(file, HotondoKeep[i], StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static bool IsShogunFile(string file)
        {
            return Contains(file, "SHOGUN") || Contains(file, "DRIZZLE") || Contains(file, "GEKKOU")
                || Contains(file, "KASA") || Contains(file, "KURENAI") || Contains(file, "SAMIDALE");
        }

        static bool WantedHotondoSkin(string path)
        {
            if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return false;
            if (path.IndexOf("/No-Rig/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf("/Extras/", StringComparison.OrdinalIgnoreCase) >= 0
                && path.IndexOf("Extras_Rig", StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (path.IndexOf("/Modular Parts/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf("/knight/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf("weaponsassetspack", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return path.IndexOf("Extras_Rig", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/Models/Rig/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/Trailer_Park/Characters/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/Outfits/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("_asset/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.EndsWith("Char_Ronin_01.fbx", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("Modular_Warrior.fbx", StringComparison.OrdinalIgnoreCase);
        }

        static void AddIfHumanoid(List<SkinOption> list, HashSet<string> seen, string path, SkinKind kind)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!seen.Add(path)) return;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) return;
            if (!HasHumanAvatar(path)) return;
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (kind == SkinKind.Hotondo && string.Equals(file, "Modular_Warrior", StringComparison.OrdinalIgnoreCase))
                kind = SkinKind.HotondoWarrior;
            if (kind != SkinKind.HotondoShogun && IsShogunFile(file))
                kind = SkinKind.HotondoShogun;
            list.Add(new SkinOption(HotondoLabel(file, path), path, kind));
        }

        static bool HasHumanAvatar(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                var av = assets[i] as Avatar;
                if (av != null && av.isHuman && av.isValid) return true;
            }
            return false;
        }
#endif

        static string HotondoLabel(string file, string path)
        {
            if (Contains(file, "SHOGUN")) return "覇将";
            if (Contains(file, "DRIZZLE")) return "小雨の刃";
            if (Contains(file, "GEKKOU")) return "月光の剣士";
            if (Contains(file, "KASA")) return "笠影の旅人";
            if (Contains(file, "KURENAI")) return "紅蓮の武人";
            if (Contains(file, "SAMIDALE")) return "五月雨の使い手";
            if (Contains(file, "Ronin")) return "孤高の浪人";
            if (Contains(file, "Modular_Warrior")) return "鋼装の戦士";

            switch (file)
            {
                case "Character_01": return "暁の拳士";
                case "Character_06": return "蒼嵐の格闘家";
                case "Character_11": return "剛腕の武闘家";
                case "Character_16": return "白銀の求道者";
                case "Character_32": return "風塵の拳鬼";
                case "Character_Female_02": return "紅蓮の格闘乙女";
                case "Character_Female_07": return "舞姫の武者";
                case "Character_Female_12": return "翡翠の拳姫";
                case "Character_Female_16": return "紫苑の拳聖";
                case "Character_33_Female": return "白華の女傑";

                case "Character_Killer": return "深淵の刃・影";
                case "Character_Killer_01": return "血煙の処刑人";
                case "Character_Killer_02": return "亡霊の暗殺者";
                case "Character_Killer_03": return "漆黒の屠殺人";
                case "Character_Killer_04": return "冷酷の凶刃";
                case "Character_Killer_05": return "狂気の狩人";
                case "Character_Killer_06": return "断罪の処刑刃";
                case "Character_Killer_07": return "黄泉の刈り手";
                case "Character_Killer_08": return "骸骨の死神";
                case "Character_Killer_09": return "怨嗟の刺客";
                case "Character_Killer_10": return "終焉の裁定者";

                case "Character_Monster": return "異形の王・邪骸";
                case "Character_Monster_01": return "蠢く肉塊の獣";
                case "Character_Monster_02": return "冥府の巨魁";
                case "Character_Monster_03": return "深怪の這い手";
                case "Character_Monster_04": return "腐蝕の怪物";
                case "Character_Monster_05": return "餓鬼の異形";
                case "Character_Monster_06": return "怨霊の狂獣";
                case "Character_Monster_07": return "奈落の捕食者";
                case "Character_Monster_08": return "混沌の魔王";
            }

            if (Contains(file, "Female_Ranger")) return "月夜の狩人・凛";
            if (Contains(file, "Male_Ranger")) return "森影の狩人";
            if (Contains(file, "Female_Peasant")) return "大地の乙女";
            if (Contains(file, "Male_Peasant")) return "大地の民";

            bool trailer = path.IndexOf("Trailer_Park", StringComparison.OrdinalIgnoreCase) >= 0;
            bool female = Contains(file, "Female");
            int num = LastNumber(file);

            if (trailer)
            {
                if (female) return "荒野の漂泊姫";
                return "荒野の無宿者";
            }

            string job = null;
            if (Contains(file, "Police")) job = female ? "追跡者・凛" : "秩序の追跡者";
            else if (Contains(file, "Firefighter")) job = female ? "業火の鎮火姫" : "業火の守り手";
            else if (Contains(file, "Doctor")) job = female ? "白衣の聖賢" : "白衣の賢者";
            else if (Contains(file, "_HM") || file.EndsWith("HM", StringComparison.Ordinal)) job = female ? "鍛冶の匠乙女" : "鋼の職人";
            else if (Contains(file, "Sheriff")) job = "掟の執行者";
            else if (Contains(file, "Killer")) job = "深淵の刃";
            else if (Contains(file, "Monster")) job = "異形の王";

            if (job != null) return job;
            if (num >= 33)
            {
                if (female)
                {
                    switch (num)
                    {
                        case 33: return "白華の女傑";
                        case 34: return "影舞のくノ一";
                        case 35: return "紫煙の妖姫";
                        case 36: return "冥花のアサシン";
                        case 37: return "宵闇の夜叉姫";
                        case 38: return "朧月の艶舞";
                        case 39: return "深淵の黒百合";
                        case 40: return "狂咲の死霊使い";
                        case 41: return "黄泉路の導き手";
                        case 42: return "幻惑の影魔女";
                        case 43: return "虚空の織姫";
                        case 44: return "冥華の氷姫";
                        case 45: return "血華の羅刹女";
                        default: return "幽玄の怪女・第" + num + "座";
                    }
                }
                else
                {
                    switch (num)
                    {
                        case 33: return "幽冥の拳法家";
                        case 34: return "影縫いの忍び";
                        case 35: return "黒嵐の狂戦士";
                        case 36: return "冥府の門番";
                        case 37: return "夜叉の武人";
                        case 38: return "宵闇の断罪者";
                        case 39: return "朧月の影武者";
                        case 40: return "幻影の奇術師";
                        case 41: return "漆黒の暗殺者";
                        case 42: return "骸哭の拳聖";
                        case 43: return "虚無の彷徨人";
                        case 44: return "冥雷の修羅";
                        case 45: return "黄泉返りの闘士";
                        default: return "宵闇の影武者・第" + num + "席";
                    }
                }
            }
            if (female)
                return num > 0 ? "流浪の女剣士・" + Letter(num) : "流浪の女剣士";
            return num > 0 ? "流浪の武芸者・" + Letter(num) : "流浪の武芸者";
        }

        static bool Contains(string s, string token)
        {
            return s.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static int LastNumber(string s)
        {
            int end = -1;
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] >= '0' && s[i] <= '9')
                {
                    if (end < 0) end = i;
                }
                else if (end >= 0)
                {
                    int n = 0;
                    for (int k = i + 1; k <= end; k++) n = n * 10 + (s[k] - '0');
                    return n;
                }
            }
            if (end < 0) return 0;
            int v = 0;
            for (int k = 0; k <= end; k++)
            {
                if (s[k] >= '0' && s[k] <= '9') v = v * 10 + (s[k] - '0');
            }
            return v;
        }

        static string Letter(int n)
        {
            if (n <= 0) return "";
            if (n <= 26) return ((char)('A' + n - 1)).ToString();
            return n.ToString();
        }

        static List<int> Available()
        {
            if (availableIdx != null) return availableIdx;
            availableIdx = new List<int>();
            var skins = Catalog();
            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i].path == PumpkinBodyId || PlayerAssets.Load<GameObject>(skins[i].path) != null)
                    availableIdx.Add(i);
            }
            return availableIdx;
        }

        static readonly string[] ArmorKeys = { "cloth_00", "01_A", "05_C" };
        static readonly string[] ArmorLabels = { "布の服", "騎士", "重装" };

        enum WeaponGrip { None, Synty, Polytope }

        class WeaponItem
        {
            public string label;
            public string path;
            public bool leftHand;
            public WeaponGrip grip;
            public Vector3 syntyPos;
            public Vector3 syntyEuler;
            public Color fallbackColor;
            public bool isGun;
            public WeaponItem(string label, string path, bool leftHand, WeaponGrip grip, Vector3 syntyPos, Vector3 syntyEuler, Color fallbackColor, bool isGun = false)
            {
                this.label = label;
                this.path = path;
                this.leftHand = leftHand;
                this.grip = grip;
                this.syntyPos = syntyPos;
                this.syntyEuler = syntyEuler;
                this.fallbackColor = fallbackColor;
                this.isGun = isGun;
            }
        }

        // Local pose on Synty prop_r / cloned GanzSe socket (blade +Y for Synty, -Y for Polytope).
        static readonly Vector3 SyntyMeleeEuler = new Vector3(-90f, 0f, 90f);
        static readonly Vector3 SyntyGunEuler = new Vector3(90f, 0f, 0f);
        static readonly Vector3 PolytopeOnPropEuler = new Vector3(90f, 0f, 90f);

        static readonly WeaponItem[] Weapons =
        {
            new WeaponItem("なし", null, false, WeaponGrip.None, Vector3.zero, Vector3.zero, Color.white),
            new WeaponItem("海賊の剣", "Assets/Synty/SidekickCharacters/_Demos/Meshes/Weapons/Pirate_Sword/SK_Sword.fbx", false, WeaponGrip.Synty, Vector3.zero, SyntyMeleeEuler, new Color(0.83f, 0.79f, 0.51f)),
            new WeaponItem("ゴブリンの斧", "Assets/Synty/SidekickCharacters/_Demos/Meshes/Weapons/Goblin_Axe/SK_Axe.fbx", false, WeaponGrip.Synty, Vector3.zero, SyntyMeleeEuler, new Color(0.55f, 0.38f, 0.22f)),
            new WeaponItem("バット", "Assets/Synty/SidekickCharacters/_Demos/Meshes/Weapons/Apocalypse_Bat/SK_Bat.fbx", false, WeaponGrip.Synty, Vector3.zero, SyntyMeleeEuler, new Color(0.42f, 0.26f, 0.12f)),
            new WeaponItem("銃", "Assets/Synty/SidekickCharacters/_Demos/Meshes/Weapons/Soldier_Gun/SK_Gun.fbx", false, WeaponGrip.Synty, Vector3.zero, SyntyGunEuler, new Color(0.25f, 0.27f, 0.22f), true),
            new WeaponItem("剣", "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/PT_Sword_01_a.prefab", false, WeaponGrip.Polytope, Vector3.zero, PolytopeOnPropEuler, Color.white),
            new WeaponItem("長剣", "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/PT_Longsword_01_a.prefab", false, WeaponGrip.Polytope, Vector3.zero, PolytopeOnPropEuler, Color.white),
            new WeaponItem("斧", "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/PT_ShortWaraxe_01_a.prefab", false, WeaponGrip.Polytope, Vector3.zero, PolytopeOnPropEuler, Color.white),
            new WeaponItem("メイス", "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/PT_Mace_01_a.prefab", false, WeaponGrip.Polytope, Vector3.zero, PolytopeOnPropEuler, Color.white),
            new WeaponItem("槍", "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/PT_Pike_01_a.prefab", false, WeaponGrip.Polytope, Vector3.zero, PolytopeOnPropEuler, Color.white),
            new WeaponItem("盾", "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/PT_Shield_01_a.prefab", true, WeaponGrip.Polytope, Vector3.zero, Vector3.zero, Color.white),
        };

        static readonly string[] GanzSeCategories =
        {
            "Hair Type", "Face Hair Type", "Eyebrow Type", "Eyes Type", "Nose Type", "Ears Type",
            "Head Armor Type", "Chest Armor Type", "Arm Armor Type", "Legs Armor Type",
            "Feet Armor Type", "Belt Armor Type"
        };
        static readonly string[] GanzSeCategoryLabels =
        {
            "髪", "髭", "眉", "目", "鼻", "耳",
            "兜", "胸", "腕", "脚", "靴", "ベルト"
        };

        public static bool IsArmorExtra(int extraIndex)
        {
            return extraIndex >= ArmorExtraFirst && extraIndex <= ArmorExtraLast;
        }

        public static string ArmorPieceId(int extraIndex, int optionIndex)
        {
            return "armor:" + extraIndex + ":" + optionIndex;
        }

        public static string ArmorPieceName(int extraIndex, int optionIndex)
        {
            return ExtraSlotLabel(SkinKind.GanzSe, extraIndex) + " タイプ" + optionIndex;
        }

        public static int GanzSeLookIndex()
        {
            var avail = Available();
            var skins = Catalog();
            for (int a = 0; a < avail.Count; a++)
            {
                int catalogIndex = avail[a];
                if (catalogIndex >= 0 && catalogIndex < skins.Length && skins[catalogIndex].kind == SkinKind.GanzSe)
                    return a;
            }
            return 0;
        }

        public static bool HasAnySkin()
        {
            return Available().Count > 0;
        }

        public struct GachaBody
        {
            public string id;
            public string label;
            public int weight;
        }

        public static void CollectGachaBodies(List<GachaBody> into)
        {
            if (into == null) return;
            var avail = Available();
            var skins = Catalog();
            for (int a = 0; a < avail.Count; a++)
            {
                int catalogIndex = avail[a];
                if (catalogIndex < 0 || catalogIndex >= skins.Length) continue;
                var opt = skins[catalogIndex];
                if (opt.kind == SkinKind.GanzSe) continue;
                string file = System.IO.Path.GetFileNameWithoutExtension(opt.path);
                if (IsBrokenSkinFile(file)) continue;
                int band = BodyBand(opt, file);
                into.Add(new GachaBody
                {
                    id = opt.path,
                    label = opt.label,
                    weight = band
                });
            }
        }

        static int BodyBand(SkinOption opt, string file)
        {
            if (opt.kind == SkinKind.HotondoShogun) return 3; // Legend (まれ 5%)
            if (IsKillerFile(file) || IsMonsterFile(file)) return 2; // Epic / Rare (少なめ 10%)
            if (!string.IsNullOrEmpty(opt.label) && (opt.label.StartsWith("変な姿", StringComparison.Ordinal) || opt.label == "頭パンプキン"))
                return 2; // Epic / Rare
            if (opt.kind == SkinKind.Synty || IsPolytope(opt.kind) || opt.kind == SkinKind.HotondoWarrior)
                return 1; // Uncommon (ふつう 32.5%)
            if (!string.IsNullOrEmpty(opt.label)
                && (opt.label.IndexOf("浪人", StringComparison.Ordinal) >= 0
                    || opt.label.IndexOf("レンジャー", StringComparison.Ordinal) >= 0))
                return 1; // Uncommon
            return 0; // Common (よく出る 52.5%)
        }

        public static int PlayerBodyCount()
        {
            return 1 + OwnedBodyLooks();
        }

        public static int PlayerBodyLook(int ordinal)
        {
            int ganz = GanzSeLookIndex();
            if (ordinal <= 0) return ganz;
            int seen = 0;
            var avail = Available();
            var skins = Catalog();
            for (int a = 0; a < avail.Count; a++)
            {
                int catalogIndex = avail[a];
                if (catalogIndex < 0 || catalogIndex >= skins.Length) continue;
                var opt = skins[catalogIndex];
                if (opt.kind == SkinKind.GanzSe) continue;
                if (!StudyStore.OwnsBody(opt.path)) continue;
                seen++;
                if (seen == ordinal) return a;
            }
            return ganz;
        }

        public static int PlayerBodyOrdinal(int lookIndex)
        {
            int ganz = GanzSeLookIndex();
            int wrapped = Available().Count == 0 ? 0 : Wrap(lookIndex, Available().Count);
            if (wrapped == ganz) return 0;
            int seen = 0;
            var avail = Available();
            var skins = Catalog();
            for (int a = 0; a < avail.Count; a++)
            {
                int catalogIndex = avail[a];
                if (catalogIndex < 0 || catalogIndex >= skins.Length) continue;
                var opt = skins[catalogIndex];
                if (opt.kind == SkinKind.GanzSe) continue;
                if (!StudyStore.OwnsBody(opt.path)) continue;
                seen++;
                if (a == wrapped) return seen;
            }
            return 0;
        }

        static int OwnedBodyLooks()
        {
            int n = 0;
            var avail = Available();
            var skins = Catalog();
            for (int a = 0; a < avail.Count; a++)
            {
                int catalogIndex = avail[a];
                if (catalogIndex < 0 || catalogIndex >= skins.Length) continue;
                var opt = skins[catalogIndex];
                if (opt.kind == SkinKind.GanzSe) continue;
                if (StudyStore.OwnsBody(opt.path)) n++;
            }
            return n;
        }

        public static int AvailableSkinCount()
        {
            return Available().Count;
        }

        public struct RaidOpponent
        {
            public int look;
            public string path;
            public string label;
            public SkinKind kind;
            public bool milestone;
        }

        public static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                if (string.IsNullOrEmpty(value)) return hash;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash & 0x7fffffff;
            }
        }

        public static int LookIndexForPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return -1;
            var avail = Available();
            var skins = Catalog();
            for (int a = 0; a < avail.Count; a++)
            {
                int catalogIndex = avail[a];
                if (catalogIndex < 0 || catalogIndex >= skins.Length) continue;
                if (skins[catalogIndex].path == path) return a;
            }
            return -1;
        }

        /// <summary>
        /// レイドの相手。ガチャ収録スキンだけ。1〜9層は通常スキン、10の倍数はボスと剣士。
        /// 同じ部屋・層・世代なら全員同じ一体になる。
        /// </summary>
        public static RaidOpponent PickRaidOpponent(int week, int floor, int generation, string room)
        {
            bool milestone = floor % 10 == 0;
            var bodies = new List<GachaBody>();
            CollectGachaBodies(bodies);
            var paths = new HashSet<string>();
            for (int i = 0; i < bodies.Count; i++)
            {
                if (!string.IsNullOrEmpty(bodies[i].id)) paths.Add(bodies[i].id);
            }

            var normal = new List<int>();
            var marked = new List<int>();
            var any = new List<int>();
            var avail = Available();
            var skins = Catalog();
            for (int a = 0; a < avail.Count; a++)
            {
                int catalogIndex = avail[a];
                if (catalogIndex < 0 || catalogIndex >= skins.Length) continue;
                var opt = skins[catalogIndex];
                if (opt == null || string.IsNullOrEmpty(opt.path) || !paths.Contains(opt.path)) continue;
                any.Add(a);
                bool sword = !string.IsNullOrEmpty(opt.label)
                    && opt.label.IndexOf("剣士", StringComparison.Ordinal) >= 0;
                bool bossSkin = opt.kind == SkinKind.HotondoShogun;
                if (bossSkin || sword) marked.Add(a);
                else normal.Add(a);
            }

            List<int> pool = milestone
                ? (marked.Count > 0 ? marked : any)
                : (normal.Count > 0 ? normal : any);
            var pick = new RaidOpponent { milestone = milestone, label = "巨影", kind = SkinKind.GanzSe };
            if (pool.Count == 0) return pick;
            int seed = StableHash((room ?? "") + ":" + week + ":" + floor + ":" + generation);
            int look = pool[seed % pool.Count];
            int chosenIndex = avail[look];
            var chosen = skins[chosenIndex];
            pick.look = look;
            pick.path = chosen.path ?? "";
            pick.label = string.IsNullOrEmpty(chosen.label) ? "巨影" : chosen.label;
            pick.kind = chosen.kind;
            return pick;
        }

        public static int PickBattleOpponent(int floor, bool boss, int avoidLook, int retry = 0)
        {
            var matches = new List<int>();
            var avail = Available();
            var skins = Catalog();
            for (int a = 0; a < avail.Count; a++)
            {
                int catalogIndex = avail[a];
                if (catalogIndex < 0 || catalogIndex >= skins.Length) continue;
                var opt = skins[catalogIndex];
                string file = System.IO.Path.GetFileNameWithoutExtension(opt.path);
                bool shogunPool = boss || floor >= 90;
                bool ok = shogunPool
                    ? opt.kind == SkinKind.HotondoShogun
                    : IsKillerFile(file);
                if (ok) matches.Add(a);
            }
            if (matches.Count == 0)
            {
                int n = avail.Count;
                if (n <= 1) return avoidLook;
                return Wrap(avoidLook + 1 + Mathf.Abs(floor), n);
            }
            if (floor >= 100)
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    if (SkinLabel(matches[i]) == "将軍") return matches[i];
                }
            }
            int seed = (boss && floor < 90)
                ? Mathf.Max(0, (floor - 1) / 10)
                : Mathf.Abs(floor * 31 + (boss ? 97 : 13));
            seed += retry;
            int pick = matches[seed % matches.Count];
            int avoid = Wrap(avoidLook, Mathf.Max(1, avail.Count));
            if (matches.Count > 1 && pick == avoid)
                pick = matches[(seed + 1) % matches.Count];
            return pick;
        }

        static bool IsKillerFile(string file)
        {
            return StartsWithName(file, "Character_Killer");
        }

        static bool PlayableHumanSkin(string file)
        {
            return !IsBrokenSkinFile(file);
        }

        static bool IsBrokenSkinFile(string file)
        {
            if (string.IsNullOrEmpty(file)) return false;
            if (IsMonsterFile(file)) return true;
            if (file.IndexOf("Ronin", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (file.IndexOf("Modular_Warrior", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        static bool IsMonsterFile(string file)
        {
            return StartsWithName(file, "Character_Monster");
        }

        static bool StartsWithName(string file, string token)
        {
            if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(token)) return false;
            if (!file.StartsWith(token, StringComparison.OrdinalIgnoreCase)) return false;
            if (file.Length == token.Length) return true;
            char c = file[token.Length];
            return c == '_' || c == '-' || (c >= '0' && c <= '9');
        }

        public static string SkinLabel(int lookIndex)
        {
            return OptionLabel(SkinKind.Synty, SlotSkin, lookIndex, Available().Count);
        }

        public static string BodyPathAtLook(int lookIndex)
        {
            var avail = Available();
            if (avail.Count == 0) return "";
            int i = avail[Wrap(lookIndex, avail.Count)];
            var skins = Catalog();
            if (i < 0 || i >= skins.Length) return "";
            return skins[i].path;
        }

        public static string SkinTitle(int lookIndex)
        {
            string label = SkinLabel(lookIndex);
            int ver = StudyStore.BodyLevel(BodyPathAtLook(lookIndex));
            if (ver <= 0) return label;
            if (ver >= 6) return StudyStore.SkillTitle(label, ver);
            return label + " " + StudyStore.VersionMark(ver);
        }

        public static void DressSpecial(GameObject go, int lookIndex)
        {
            if (go == null || !IsPumpkinLook(lookIndex)) return;
            var anim = go.GetComponentInChildren<Animator>();
            Transform head = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head == null) return;
            var rends = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                string n = rends[i].name;
                if (n.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0)
                    rends[i].enabled = false;
            }
            var mesh = PlayerAssets.Load<GameObject>(PumpkinMeshPath);
            if (mesh == null) return;
            var hat = UnityEngine.Object.Instantiate(mesh, head);
            hat.name = "PumpkinHead";
            hat.transform.localPosition = new Vector3(0f, 0.08f, 0.02f);
            hat.transform.localRotation = Quaternion.identity;
            hat.transform.localScale = Vector3.one * 0.42f;
        }

        static bool IsPumpkinLook(int lookIndex)
        {
            var avail = Available();
            if (avail.Count == 0) return false;
            int i = avail[Wrap(lookIndex, avail.Count)];
            var skins = Catalog();
            return i >= 0 && i < skins.Length && skins[i].path == PumpkinBodyId;
        }

        public static GameObject SpawnLooked(Transform parent, Vector3 localPosition, Quaternion localRotation, Func<int, int> look, out SkinKind kind)
        {
            kind = SkinKind.GanzSe;
            var prefab = LoadSkinPrefab(look != null ? look(SlotSkin) : 0, out kind);
            if (prefab == null) return null;
            var go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = Vector3.one;
            Apply(go.transform, kind, look ?? (i => 0));
            DressSpecial(go, look != null ? look(SlotSkin) : 0);
            var anim = go.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            return go;
        }

        public static GameObject LoadFirstShogunPrefab(out SkinKind kind)
        {
            kind = SkinKind.HotondoShogun;
            var skins = Catalog();
            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i].kind != SkinKind.HotondoShogun) continue;
                var prefab = PlayerAssets.Load<GameObject>(skins[i].path);
                if (prefab != null) return prefab;
            }
            return null;
        }

        public static GameObject LoadSkinPrefab(int skinLookIndex, out SkinKind kind)
        {
            kind = SkinKind.GanzSe;
            var avail = Available();
            if (avail.Count == 0) return null;
            int i = avail[Wrap(skinLookIndex, avail.Count)];
            var skins = Catalog();
            if (skins[i].path == PumpkinBodyId)
            {
                kind = SkinKind.GanzSe;
                return PlayerAssets.Load<GameObject>(AllSkins[0].path);
            }
            kind = skins[i].kind;
            return PlayerAssets.Load<GameObject>(skins[i].path);
        }

        public static bool IsPolytope(SkinKind kind)
        {
            return kind == SkinKind.PolytopeMale || kind == SkinKind.PolytopeFemale;
        }

        public static bool LooksFemale(SkinKind kind, string objectName)
        {
            if (kind == SkinKind.PolytopeFemale) return true;
            return !string.IsNullOrEmpty(objectName)
                && objectName.IndexOf("Female", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static int OptionCount(SkinKind currentKind, int slot)
        {
            if (slot == SlotSkin) return Available().Count;
            return 0;
        }

        public static string OptionLabel(SkinKind currentKind, int slot, int index, int count)
        {
            if (count <= 0) return "-";
            int i = ((index % count) + count) % count;
            if (slot == SlotSkin)
            {
                var avail = Available();
                if (i < avail.Count) return Catalog()[avail[i]].label;
                return "-";
            }
            return (i + 1) + " / " + count;
        }

        static readonly string[] WarriorTones = { "Medium", "Light", "Dark" };
        static readonly string[] WarriorToneLabels = { "普通", "浅", "濃" };
        static readonly string[] WarriorSets = { "B", "DS", "G", "I", "M" };
        static readonly string[] WarriorSetLabels = { "青銅", "闇鋼", "黄金", "鉄", "銀" };

        public static int ExtraSlotCount(Transform hero, SkinKind kind)
        {
            if (IsPolytope(kind)) return 1;
            if (kind == SkinKind.GanzSe) return GanzSeCategories.Length + 1;
            if (kind == SkinKind.HotondoWarrior) return 4;
            return 0;
        }

        public static string ExtraSlotLabel(SkinKind kind, int extraIndex)
        {
            if (IsPolytope(kind))
                return "防具";
            if (kind == SkinKind.Synty || kind == SkinKind.Hotondo || kind == SkinKind.HotondoShogun)
                return "-";
            if (kind == SkinKind.HotondoWarrior)
            {
                if (extraIndex == 0) return "肌";
                if (extraIndex == 1) return "防具";
                if (extraIndex == 2) return "髪";
                if (extraIndex == 3) return "髭";
                return "-";
            }
            if (kind == SkinKind.GanzSe)
            {
                if (extraIndex < GanzSeCategoryLabels.Length)
                    return GanzSeCategoryLabels[extraIndex];
                return "色";
            }
            return "-";
        }

        public static int ExtraOptionCount(Transform hero, SkinKind kind, int extraIndex)
        {
            if (IsPolytope(kind))
                return ArmorKeys.Length;
            if (kind == SkinKind.HotondoWarrior)
            {
                if (extraIndex == 0) return WarriorTones.Length;
                if (extraIndex == 1) return WarriorSets.Length;
                if (extraIndex == 2) return 5;
                if (extraIndex == 3) return 6;
                return 0;
            }
            if (kind != SkinKind.GanzSe) return 0;
            if (extraIndex == GanzSeCategories.Length)
                return Mathf.Max(1, MaxColor(hero));
            if (hero == null || extraIndex < 0 || extraIndex >= GanzSeCategories.Length)
                return 0;
            int types = UniqueTypes(hero, GanzSeCategories[extraIndex]).Count;
            if (types == 0) return 0;
            return AllowNone(extraIndex) ? types + 1 : types;
        }

        public static string ExtraOptionLabel(SkinKind kind, int extraIndex, int index, int count)
        {
            if (count <= 0) return "-";
            int i = ((index % count) + count) % count;
            if (IsPolytope(kind))
                return ArmorLabels[i];
            if (kind == SkinKind.HotondoWarrior)
            {
                if (extraIndex == 0) return WarriorToneLabels[Wrap(i, WarriorToneLabels.Length)];
                if (extraIndex == 1) return WarriorSetLabels[Wrap(i, WarriorSetLabels.Length)];
                if (extraIndex == 2) return "タイプ" + (i + 1);
                return i == 0 ? "なし" : "タイプ" + i;
            }
            if (kind != SkinKind.GanzSe) return (i + 1) + " / " + count;
            if (extraIndex == GanzSeCategories.Length)
                return "色" + (i + 1);
            if (AllowNone(extraIndex))
                return i == 0 ? "なし" : "タイプ" + i;
            return "タイプ" + (i + 1);
        }

        public static void Apply(Transform hero, SkinKind kind, Func<int, int> lookSlotValue)
        {
            if (hero == null) return;
            ClearEquip(hero);
            HeroWeaponRig.Teardown(hero);
            if (kind == SkinKind.GanzSe)
            {
                int colorPick = Wrap(lookSlotValue(GanzSeCategories.Length + 1), Mathf.Max(1, MaxColor(hero))) + 1;
                for (int i = 0; i < GanzSeCategories.Length; i++)
                {
                    int pick = lookSlotValue(i + 1);
                    if (IsArmorExtra(i) && pick > 0 && !StudyStore.OwnsArmor(ArmorPieceId(i, pick)))
                        pick = 0;
                    PickCategory(hero, i, pick, colorPick);
                }
                bool helmetOn = lookSlotValue(7) > 0;
                if (helmetOn) HidePrefix(hero, "Hair Type");
                return;
            }
            if (kind == SkinKind.HotondoWarrior)
            {
                ApplyWarrior(hero, lookSlotValue);
                return;
            }
            if (kind == SkinKind.HotondoShogun)
            {
                HideShogunJunk(hero);
                return;
            }
            if (kind == SkinKind.Synty || kind == SkinKind.Hotondo)
                return;
            if (!IsPolytope(kind)) return;
            EnableSet(hero, ArmorKeys[Wrap(lookSlotValue(1), ArmorKeys.Length)]);
        }

        static void HideShogunJunk(Transform hero)
        {
            var rends = hero.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                if (IsShogunJunk(rends[i].gameObject.name))
                    rends[i].gameObject.SetActive(false);
            }
        }

        static bool IsShogunJunk(string n)
        {
            if (string.IsNullOrEmpty(n)) return false;
            return n.IndexOf("UI_", StringComparison.OrdinalIgnoreCase) >= 0
                || n.StartsWith("UI.", StringComparison.OrdinalIgnoreCase)
                || n.IndexOf("Smoke", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Tissue", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Info_circle", StringComparison.OrdinalIgnoreCase) >= 0
                || string.Equals(n, "UI_Geo", StringComparison.OrdinalIgnoreCase)
                || string.Equals(n, "UI_Ring", StringComparison.OrdinalIgnoreCase)
                || n.IndexOf("Katana", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Scabbard", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Stick_Geo", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Pipe_Geo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void ApplyWarrior(Transform hero, Func<int, int> look)
        {
            string tone = WarriorTones[Wrap(look(1), WarriorTones.Length)];
            string set = WarriorSets[Wrap(look(2), WarriorSets.Length)];
            int hairPick = Wrap(look(3), 5) + 1;
            int beardPick = Wrap(look(4), 6);
            var hair = CollectNumbered(hero, "Hair.");
            var beard = CollectNumbered(hero, "Beard.");
            var brows = CollectNumbered(hero, "Eyebrows.");
            var rends = hero.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                string n = r.gameObject.name;
                if (IsWarriorWeapon(n) || n.StartsWith("BeltAttch", StringComparison.Ordinal) || n.StartsWith("HeadAttach", StringComparison.Ordinal)
                    || n.StartsWith("Male_Pants", StringComparison.Ordinal) || n.StartsWith("Male_Shirt", StringComparison.Ordinal)
                    || n.StartsWith("Mustache", StringComparison.Ordinal))
                {
                    r.gameObject.SetActive(false);
                    continue;
                }
                if (n.StartsWith("Default_Male_", StringComparison.Ordinal))
                {
                    r.gameObject.SetActive(n.EndsWith(tone, StringComparison.Ordinal));
                    continue;
                }
                if (n.StartsWith("Headgear.", StringComparison.Ordinal))
                {
                    r.gameObject.SetActive(false);
                    continue;
                }
                if (IsWarriorArmor(n))
                {
                    r.gameObject.SetActive(n.IndexOf("." + set + ".", StringComparison.Ordinal) >= 0 && EndsWithFirstVariant(n));
                    continue;
                }
                if (n.StartsWith("Hair.", StringComparison.Ordinal))
                {
                    r.gameObject.SetActive(hairPick > 0 && hair.Count > 0 && r == hair[Wrap(hairPick - 1, hair.Count)]);
                    continue;
                }
                if (n.StartsWith("Beard.", StringComparison.Ordinal))
                {
                    r.gameObject.SetActive(beardPick > 0 && beard.Count > 0 && r == beard[Wrap(beardPick - 1, beard.Count)]);
                    continue;
                }
                if (n.StartsWith("Eyebrows.", StringComparison.Ordinal))
                {
                    r.gameObject.SetActive(brows.Count > 0 && r == brows[0]);
                    continue;
                }
            }
        }

        static List<Renderer> CollectNumbered(Transform hero, string prefix)
        {
            var list = new List<Renderer>();
            var rends = hero.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i].gameObject.name.StartsWith(prefix, StringComparison.Ordinal))
                    list.Add(rends[i]);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));
            return list;
        }

        static bool IsWarriorWeapon(string n)
        {
            return n.IndexOf("Axe", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Dagger", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Hammer", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Halberd", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Rapier", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Shield", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsWarriorArmor(string n)
        {
            return n.StartsWith("Belt.", StringComparison.Ordinal)
                || n.StartsWith("Boots.", StringComparison.Ordinal)
                || n.StartsWith("Chestplate.", StringComparison.Ordinal)
                || n.StartsWith("Gauntlets.", StringComparison.Ordinal)
                || n.StartsWith("Headgear.", StringComparison.Ordinal)
                || n.StartsWith("Legguards.", StringComparison.Ordinal)
                || n.StartsWith("Shoulder.", StringComparison.Ordinal);
        }

        static bool EndsWithFirstVariant(string n)
        {
            return n.EndsWith(".001", StringComparison.Ordinal);
        }

        static bool AllowNone(int categoryIndex)
        {
            return categoryIndex == 1 || categoryIndex >= 6;
        }

        static List<Renderer> Collect(Transform hero, string prefix)
        {
            var list = new List<Renderer>();
            var rends = hero.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i].gameObject.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    list.Add(rends[i]);
            }
            return list;
        }

        static List<int> UniqueTypes(Transform hero, string prefix)
        {
            var set = new List<int>();
            var parts = Collect(hero, prefix);
            for (int i = 0; i < parts.Count; i++)
            {
                ParseTypeColor(parts[i].gameObject.name, out int type, out _);
                if (!set.Contains(type)) set.Add(type);
            }
            set.Sort();
            return set;
        }

        static int MaxColor(Transform hero)
        {
            int max = 1;
            if (hero == null) return 5;
            var rends = hero.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                ParseTypeColor(rends[i].gameObject.name, out _, out int color);
                if (color > max) max = color;
            }
            return max;
        }

        static void ParseTypeColor(string name, out int type, out int color)
        {
            type = 1;
            color = 1;
            int t = IndexAfter(name, "Type ");
            if (t >= 0) type = Math.Max(1, ReadInt(name, t));
            int c = IndexAfter(name, "Color ");
            if (c >= 0) color = Math.Max(1, ReadInt(name, c));
        }

        static int IndexAfter(string name, string token)
        {
            int i = name.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            return i < 0 ? -1 : i + token.Length;
        }

        static int ReadInt(string s, int start)
        {
            int n = 0;
            bool any = false;
            for (int i = start; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch < '0' || ch > '9')
                {
                    if (any) break;
                    continue;
                }
                any = true;
                n = n * 10 + (ch - '0');
            }
            return any ? n : 1;
        }

        static void PickCategory(Transform hero, int categoryIndex, int optionIndex, int color)
        {
            string prefix = GanzSeCategories[categoryIndex];
            var parts = Collect(hero, prefix);
            if (parts.Count == 0) return;
            var types = UniqueTypes(hero, prefix);
            bool noneOk = AllowNone(categoryIndex);
            if (noneOk && optionIndex <= 0)
            {
                for (int i = 0; i < parts.Count; i++)
                    parts[i].gameObject.SetActive(false);
                return;
            }
            int typePick = noneOk ? optionIndex - 1 : optionIndex;
            typePick = Wrap(typePick, types.Count);
            int wantedType = types[typePick];
            int best = -1;
            int fallback = -1;
            for (int i = 0; i < parts.Count; i++)
            {
                ParseTypeColor(parts[i].gameObject.name, out int type, out int col);
                if (type != wantedType) continue;
                if (fallback < 0) fallback = i;
                if (col == color) { best = i; break; }
            }
            int on = best >= 0 ? best : fallback;
            for (int i = 0; i < parts.Count; i++)
                parts[i].gameObject.SetActive(i == on);
        }

        static void HidePrefix(Transform hero, string prefix)
        {
            var parts = Collect(hero, prefix);
            for (int i = 0; i < parts.Count; i++)
                parts[i].gameObject.SetActive(false);
        }

        enum Kind { Body, Boots, Legs, Gauntlets, Other }

        static Kind KindOf(string n)
        {
            string s = n.ToLowerInvariant();
            if (s.Contains("boots")) return Kind.Boots;
            if (s.Contains("gauntlet")) return Kind.Gauntlets;
            if (s.Contains("legs")) return Kind.Legs;
            if (s.Contains("body")) return Kind.Body;
            return Kind.Other;
        }

        static bool IsArmorKind(Kind k)
        {
            return k == Kind.Body || k == Kind.Boots || k == Kind.Legs || k == Kind.Gauntlets;
        }

        static bool MatchesSet(string n, string key)
        {
            return n.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void EnableSet(Transform hero, string setKey)
        {
            var rends = hero.GetComponentsInChildren<Renderer>(true);
            var seen = new HashSet<Kind>();
            for (int i = 0; i < rends.Length; i++)
            {
                Kind k = KindOf(rends[i].gameObject.name);
                if (!IsArmorKind(k)) continue;
                bool match = MatchesSet(rends[i].gameObject.name, setKey);
                if (match && seen.Add(k))
                    rends[i].gameObject.SetActive(true);
                else
                    rends[i].gameObject.SetActive(false);
            }
        }

        static readonly string[] RightHandNames =
        {
            "PT_Right_Hand_Weapon_slot", "prop_r", "PT_RightHand", "hand_r", "Hand_R",
            "mixamorig:RightHand", "RightHand", "DEF-hand.R", "hand.R"
        };
        static readonly string[] LeftHandNames =
        {
            "PT_Left_Hand_Shield_slot", "prop_l", "PT_LeftHand", "hand_l", "Hand_L",
            "mixamorig:LeftHand", "LeftHand", "DEF-hand.L", "hand.L"
        };

        static readonly Vector3 MaleRightSlotPos = new Vector3(-0.0888f, -0.0223f, 0.0384f);
        static readonly Quaternion MaleRightSlotRot = new Quaternion(0.71240455f, 0.26327476f, -0.0033995237f, 0.65050334f);
        static readonly Vector3 MaleLeftSlotPos = new Vector3(-0.0826f, -0.0279f, -0.0250f);
        static readonly Quaternion MaleLeftSlotRot = new Quaternion(-0.2200834f, 0.66038984f, 0.716275f, -0.048977084f);
        static readonly Vector3 SyntyPropRPos = new Vector3(0.0746f, 0.0257f, 0.0007f);
        static readonly Quaternion SyntyPropRRot = new Quaternion(0.0023f, 0.70674f, 0.70711f, 0.02266f);
        static readonly Vector3 SyntyPropLPos = new Vector3(-0.0746f, 0.0257f, 0.0007f);
        static readonly Quaternion SyntyPropLRot = Quaternion.Euler(87.98f, 91.67f, 90.02f);

        static void AttachWeapon(Transform hero, int weaponIndex, SkinKind kind)
        {
            ClearEquip(hero);
            if (weaponIndex <= 0 || weaponIndex >= Weapons.Length)
            {
                HeroWeaponRig.Teardown(hero);
                return;
            }
            var item = Weapons[weaponIndex];
            if (item.path == null) return;
            var prefab = PlayerAssets.Load<GameObject>(item.path);
            if (prefab == null) return;
            var bone = FindOrCreateGrip(hero, item.leftHand, kind);
            if (bone == null) return;
            var go = UnityEngine.Object.Instantiate(prefab, bone);
            go.name = "Eq_" + prefab.name;
            int layer = hero.gameObject.layer;
            var trs = go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < trs.Length; i++) trs[i].gameObject.layer = layer;
            var anim = go.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
            FlattenSkinnedWeapon(go);
            FillMissingMaterials(go, item.fallbackColor);
            FitWeaponGrip(go.transform, item, kind, hero);
            var fitter = hero.GetComponent<HeroGripFinalizer>();
            if (fitter == null) fitter = hero.gameObject.AddComponent<HeroGripFinalizer>();
            fitter.kind = kind;
            fitter.armed = true;
            fitter.framesLeft = 16;
        }

        static Transform FindOrCreateGrip(Transform hero, bool left, SkinKind kind)
        {
            if (IsPolytope(kind))
            {
                string slotName = left ? "PT_Left_Hand_Shield_slot" : "PT_Right_Hand_Weapon_slot";
                var slot = FindNamed(hero, slotName);
                if (slot != null) return slot;
                var ptHand = FindNamed(hero, left ? "PT_LeftHand" : "PT_RightHand");
                if (ptHand == null) return FindNamedAny(hero, left ? LeftHandNames : RightHandNames);
                var created = new GameObject(slotName).transform;
                created.SetParent(ptHand, false);
                created.localPosition = left ? MaleLeftSlotPos : MaleRightSlotPos;
                created.localRotation = left ? MaleLeftSlotRot : MaleRightSlotRot;
                created.localScale = Vector3.one;
                return created;
            }

            string propName = left ? "prop_l" : "prop_r";
            var prop = FindNamed(hero, propName);
            if (prop != null) return prop;
            var hand = FindNamed(hero, left ? "hand_l" : "hand_r");
            if (hand == null) return FindNamedAny(hero, left ? LeftHandNames : RightHandNames);
            var grip = new GameObject("EqGrip_" + propName).transform;
            grip.SetParent(hand, false);
            grip.localPosition = left ? SyntyPropLPos : SyntyPropRPos;
            grip.localRotation = left ? SyntyPropLRot : SyntyPropRRot;
            grip.localScale = Vector3.one;
            return grip;
        }

        static void FlattenSkinnedWeapon(GameObject go)
        {
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < smrs.Length; i++)
            {
                var smr = smrs[i];
                if (smr.sharedMesh == null) continue;
                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                var host = smr.gameObject;
                var mf = host.GetComponent<MeshFilter>();
                if (mf == null) mf = host.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = host.GetComponent<MeshRenderer>();
                if (mr == null) mr = host.AddComponent<MeshRenderer>();
                mr.sharedMaterials = smr.sharedMaterials;
                UnityEngine.Object.DestroyImmediate(smr);
            }
        }

        static void FillMissingMaterials(GameObject go, Color fallback)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Study/VertexColorLit");
            var rends = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                var mats = rends[i].sharedMaterials;
                bool dirty = false;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null) continue;
                    var mat = new Material(shader);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fallback);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", fallback);
                    mats[m] = mat;
                    dirty = true;
                }
                if (dirty) rends[i].sharedMaterials = mats;
            }
        }

        static void FitWeaponGrip(Transform weapon, WeaponItem item, SkinKind kind, Transform hero)
        {
            weapon.localScale = Vector3.one;
            weapon.localPosition = Vector3.zero;
            weapon.localRotation = Quaternion.identity;
        }

        public static void FinalizeHeldWeapon(Transform hero, SkinKind kind)
        {
            if (hero == null) return;
            var all = hero.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var weapon = all[i];
                if (weapon == null) continue;
                if (!weapon.name.StartsWith("Eq_")) continue;
                if (weapon.name.StartsWith("EqGrip_")) continue;
                PoseHeldWeapon(hero, weapon);
            }
        }

        static void PoseHeldWeapon(Transform hero, Transform weapon)
        {
            bool left = weapon.parent != null && (
                weapon.parent.name.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.parent.name == "prop_l" ||
                weapon.name.IndexOf("Shield", StringComparison.OrdinalIgnoreCase) >= 0);
            weapon.localPosition = Vector3.zero;
            if (left)
            {
                weapon.localRotation = Quaternion.identity;
                return;
            }

            bool polyWeapon = weapon.name.IndexOf("PT_", StringComparison.Ordinal) >= 0;
            bool gun = weapon.name.IndexOf("Gun", StringComparison.OrdinalIgnoreCase) >= 0;
            bool syntySocket = weapon.parent != null && (
                weapon.parent.name == "prop_r" ||
                weapon.parent.name.StartsWith("EqGrip_prop_r"));

            if (gun)
            {
                weapon.localRotation = Quaternion.Euler(SyntyGunEuler);
                return;
            }

            if (syntySocket)
            {
                weapon.localRotation = Quaternion.Euler(polyWeapon ? PolytopeOnPropEuler : SyntyMeleeEuler);
                return;
            }

            // Polytope official hand slot is authored for PT weapons at identity.
            weapon.localRotation = polyWeapon ? Quaternion.identity : Quaternion.Euler(SyntyMeleeEuler);
        }

        public static bool HasEquippedWeapon(Transform hero)
        {
            if (hero == null) return false;
            var all = hero.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name.StartsWith("Eq_")) return true;
            }
            return false;
        }

        static void ClearEquip(Transform hero)
        {
            var all = hero.GetComponentsInChildren<Transform>(true);
            for (int i = all.Length - 1; i >= 0; i--)
            {
                if (all[i] != null && (all[i].name.StartsWith("Eq_") || all[i].name.StartsWith("EqGrip_")))
                    UnityEngine.Object.DestroyImmediate(all[i].gameObject);
            }
        }

        static Transform FindNamedAny(Transform root, string[] names)
        {
            for (int n = 0; n < names.Length; n++)
            {
                var t = FindNamed(root, names[n]);
                if (t != null) return t;
            }
            return null;
        }

        static Transform FindNamed(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) return all[i];
            }
            return null;
        }

        static int Wrap(int v, int n)
        {
            if (n <= 0) return 0;
            v %= n;
            if (v < 0) v += n;
            return v;
        }
    }

    public class HeroGripFinalizer : MonoBehaviour
    {
        public HeroAppearance.SkinKind kind;
        public bool armed;
        public int framesLeft = 16;

        void LateUpdate()
        {
            if (!armed) return;
            HeroAppearance.FinalizeHeldWeapon(transform, kind);
            framesLeft--;
            if (framesLeft > 0) return;
            armed = false;
            HeroWeaponRig.Setup(transform);
            Destroy(this);
        }
    }
}
