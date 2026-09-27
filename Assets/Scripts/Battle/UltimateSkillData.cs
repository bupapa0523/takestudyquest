using System.Collections.Generic;
using UnityEngine;

namespace ShiftingMetropolis.Battle
{
    public enum UltimateMotionType
    {
        MeleeRush,
        MagicBolt,
        TelekinesisSlam,
        FieldNuke,
        SwordVolley
    }

    public enum UltimateGear
    {
        Gear1,
        Gear2,
        Gear3,
        Brainstorm
    }

    public enum FallingObjectKind
    {
        Sword,
        IceChunk
    }

    /// <summary>
    /// 頭上から武器(剣または氷塊)を降らせて叩きつける演出用データ。
    /// キャラクター本体には持たせず、独立オブジェクトとして落とす。
    /// </summary>
    public class TelekinesisWeaponData
    {
        public FallingObjectKind kind = FallingObjectKind.Sword;
        public string weaponPrefabPath;
        public int weaponCount = 1;
        public Color auraColor = new Color(0.6f, 0.85f, 1f);
        public float floatRadius = 0.5f;
        public float spinSpeed = 220f;
        /// <summary>true なら対象の真上から剣を落とす。</summary>
        public bool rainFromAbove;
        /// <summary>true なら剣を出さず、領域を広げて爆発させる。</summary>
        public bool domainExpand;
    }

    public class UltimateSkillDefinition
    {
        public string id;
        public string displayName;
        public UltimateGear gear;
        public UltimateMotionType motionType;
        public Color vfxColor;
        /// <summary>Assets/anime/ 内のファイル名(拡張子含む)。必殺技の演出はここだけから使う。</summary>
        public string animeClip;
        public float damageMultiplierBase;
        public int levelMax = 8;
        public float multiplierPerLevel = 0.08f;
        /// <summary>基礎倍率にかける。戦闘が数発で終わらないようにする調整。</summary>
        public const float BalanceScale = 0.40f;
        public int diamondCostToUnlock;
        public int diamondCostPerLevel;
        public TelekinesisWeaponData telekinesis;
        /// <summary>モーション再生時間に対する「当たった瞬間」の割合(0-1)。エフェクト/ダメージ発生タイミングに使う。</summary>
        public float impactFraction = 0.5f;
        /// <summary>モーションが元データの向きのまま体が横/後ろを向いてしまう場合の補正(度)。</summary>
        public float facingOffsetY = 0f;
        /// <summary>1より大きいとモーションを速く再生する。</summary>
        public float animSpeed = 1f;
        /// <summary>0より大きいと、その距離まで敵へ踏み込む。0なら既存のキック/パンチ接近。</summary>
        public float lungeMeters;
        /// <summary>接近時に敵から離れて止まる距離。0なら 0.95。</summary>
        public float lungeStop;
        /// <summary>0より大きいと、その再生割合まで敵の前に留まる。0ならヒット直後に下がる。</summary>
        public float lungeHoldUntil;
        /// <summary>0より大きいと、モーション中にその高さまで追加で跳ぶ。</summary>
        public float jumpBoost;
        /// <summary>1より大きいと、その倍率のでかい魔弾を飛ばす。</summary>
        public float boltScale;
        /// <summary>1より大きいと、小さい弾をその数だけ連射する。</summary>
        public int boltCount;
        /// <summary>true なら立方体の弾を扇状に連射する（アステロイド）。</summary>
        public bool asteroid;
        /// <summary>true なら両手に立方体を出して一斉射撃する（フルアタック）。</summary>
        public bool asteroidFull;
        /// <summary>かめはめ波 / 虚閃 / 逆撫 などの波動演出キー。</summary>
        public string waveKind;
        /// <summary>true なら自分にバフをかけて敵は殴らない。</summary>
        public bool selfBuff;
        /// <summary>属性技。Unset なら属性なし。</summary>
        public BattleElement element = BattleElement.Unset;

        public float MultiplierAtLevel(int level)
        {
            level = Mathf.Clamp(level, 1, levelMax);
            if (damageMultiplierBase <= 0.01f) return 0f;
            return damageMultiplierBase * BalanceScale * GearPower(gear) + (level - 1) * multiplierPerLevel;
        }

        public static float GearPower(UltimateGear gear)
        {
            switch (gear)
            {
                case UltimateGear.Gear2: return 1.45f;
                case UltimateGear.Gear3: return 2.75f;
                case UltimateGear.Brainstorm: return 3.2f;
                default: return 1f;
            }
        }
    }

    /// <summary>
    /// 必殺技の定義一覧。Assets/anime に入っているモーション(ためる/やられる以外)を
    /// すべて必殺技として使う。プレイヤーは1つを選んでロードアウトに装備する。
    /// </summary>
    public static class UltimateCatalog
    {
        const string LongswordPrefab = "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/PT_Longsword_01_a.prefab";
        static readonly Dictionary<string, UltimateSkillDefinition> All = new Dictionary<string, UltimateSkillDefinition>();

        static UltimateCatalog()
        {
            // ---- 剣/氷塊を降らせるタイプ ----
            AddFalling("telekinesis_barrage", "氷塊乱舞", "Sword And Shield Casting.fbx",
                new Color(0.6f, 0.9f, 1f), 2.6f, 0, 10, FallingObjectKind.IceChunk, 4, floatRadius: 1.1f, impactFraction: 0.2f);

            AddFalling("field_nuke", "終焉領域", "Standing 2H Magic Attack 04.fbx",
                new Color(1f, 0.35f, 0.2f), 3.6f, 0, 14, FallingObjectKind.Sword, 0, floatRadius: 1.3f, impactFraction: 0.28f, domainExpand: true);

            // ---- 魔法/遠距離タイプ(弾が飛ぶ) ----
            AddMagic("telekinesis_slam", "アステロイド", "Standing 1H Magic Attack 02.fbx",
                new Color(0.55f, 0.9f, 1f), 3.0f, 0, 8, 0.25f, boltCount: 24, asteroid: true);
            AddMagic("asteroid_full_attack", "フルアタック", "Wide Arm Spell Casting.fbx",
                new Color(0.55f, 0.9f, 1f), 4.2f, 0, 8, 0.22f, asteroidFull: true);
            AddMagic("arcane_bolt", "極大魔弾", "Standing 1H Magic Attack 02.fbx",
                new Color(0.72f, 0.28f, 1f), 2.9f, 0, 9, 0.25f, boltScale: 2.1f);
            AddMagic("wide_cast_bolt", "広域詠唱弾", "Wide Arm Spell Casting.fbx",
                new Color(0.75f, 0.4f, 1f), 2.8f, 0, 9, 0.25f, boltScale: 1.65f);
            AddMagic("arrow_shot", "疾風の矢", "Shooting Arrow.fbx",
                new Color(0.55f, 0.85f, 0.35f), 2.7f, 10, 7, 0.53f, facingOffsetY: 90f);
            AddBowSwords("bow_sword_shot", "飛剣の矢", "Shooting Arrow.fbx",
                new Color(0.95f, 0.75f, 0.35f), 3.0f, 0, 8, 3, 0.53f, 90f);
            AddMagic("gunplay_shot", "連射掃射", "Gunplay.fbx",
                new Color(1f, 0.82f, 0.35f), 2.6f, 0, 7, 0.12f, boltCount: 14);

            // ---- アニメ波動 ----
            AddWave("kamehameha", "かめはめ波", "Standing 2H Magic Area Attack 02.fbx",
                new Color(0.28f, 0.72f, 1f), 3.4f, 0.18f);
            AddWave("cero", "虚閃", "Sword And Shield Power Up.fbx",
                new Color(0.03f, 0.02f, 0.04f), 3.3f, 0.22f);
            AddWave("gran_rey_cero", "王虚の閃光", "Sword And Shield Power Up.fbx",
                new Color(0.7f, 0.04f, 0.08f), 4.6f, 0.22f);
            AddBuff("sakanade", "逆撫", "Sword And Shield Casting.fbx",
                new Color(1f, 0.42f, 0.72f), 0.28f);
            AddWave("getsuga", "月牙天衝", "Standing 2H Magic Attack 04.fbx",
                new Color(0.04f, 0.02f, 0.03f), 3.4f, 0.36f);
            AddWave("spirit_gun", "霊丸", "Gunplay.fbx",
                new Color(1f, 0.92f, 0.35f), 3.2f, 0.2f);
            AddWave("shinra_tensei", "神羅天征", "Wide Arm Spell Casting.fbx",
                new Color(0.08f, 0.02f, 0.12f), 3.6f, 0.24f);
            AddWave("makankosappo", "魔貫光殺砲", "Standing 1H Magic Attack 02.fbx",
                new Color(0.95f, 0.9f, 0.35f), 3.5f, 0.16f);
            AddWave("genkidama", "元気玉", "Standing 2H Magic Area Attack 02.fbx",
                new Color(0.45f, 0.85f, 1f), 3.8f, 0.12f);
            AddWave("hound", "ハウンド", "Standing 1H Magic Attack 02.fbx",
                new Color(0.55f, 0.9f, 1f), 3.1f, 0.25f);
            AddWave("meteora", "メテオラ", "Standing 1H Magic Attack 02.fbx",
                new Color(0.7f, 0.95f, 1f), 3.4f, 0.25f);
            AddWave("kurohitsugi", "黒棺", "Wide Arm Spell Casting.fbx",
                new Color(0.04f, 0.02f, 0.05f), 3.7f, 0.28f);
            AddWave("kyoshiki_murasaki", "虚式「紫」", "Wide Arm Spell Casting.fbx",
                new Color(0.72f, 0.18f, 1f), 4.4f, 0.22f);
            AddWave("hakai", "破壊", "Standing 2H Magic Attack 04.fbx",
                new Color(0.78f, 0.32f, 1f), 4.7f, 0.28f);
            AddBuff("kaioken", "界王拳", "Sword And Shield Power Up.fbx",
                new Color(1f, 0.18f, 0.08f), 0.55f);
            AddBuff("reiatsu", "霊圧解放", "Standing 2H Magic Attack 04.fbx",
                new Color(0.28f, 0.08f, 0.42f), 0.32f);
            AddBuff("shogun_henshin", "将軍変身", "Sword And Shield Power Up.fbx",
                new Color(0.06f, 0.02f, 0.08f), 0.55f);

            // ---- 属性技（1属性2技 + ブレインストーム1技） ----
            AddElement("element_fire", "劫炎・一弾", BattleElement.Fire, "Gunplay.fbx",
                new Color(1f, 0.38f, 0.08f), 3.05f, 0.48f);
            AddElement("element_fire_burn", "劫炎焚獄", BattleElement.Fire, "Standing 2H Magic Attack 04.fbx",
                new Color(1f, 0.32f, 0.06f), 3.1f, 0.38f);
            AddElement("element_water", "蒼刃・時雨", BattleElement.Water, "Butterfly Twirl.fbx",
                new Color(0.28f, 0.72f, 1f), 2.95f, 0.46f);
            AddElement("element_water_drown", "蒼球溺獄", BattleElement.Water, "Standing 2H Magic Area Attack 02.fbx",
                new Color(0.18f, 0.58f, 0.92f), 3.15f, 0.32f);
            AddElement("element_light", "刹那・爆身", BattleElement.Light, "Wide Arm Spell Casting.fbx",
                new Color(1f, 0.94f, 0.55f), 2.85f, 0.18f);
            AddElement("element_light_punch", "光迅拳", BattleElement.Light, "Uppercut Jab.fbx",
                new Color(1f, 0.96f, 0.62f), 3.05f, 0.14f);
            AddElement("element_nature", "神樹絶獄", BattleElement.Nature, "Sword And Shield Casting.fbx",
                new Color(0.32f, 0.82f, 0.38f), 2.9f, 0.36f);
            AddElement("element_nature_pierce", "神樹貫縛", BattleElement.Nature, "Sword And Shield Casting.fbx",
                new Color(0.28f, 0.72f, 0.32f), 3.2f, 0.34f);
            AddElement("element_dark", "縮星・葬刃", BattleElement.Dark, "Standing 2H Magic Attack 04.fbx",
                new Color(0.42f, 0.06f, 0.58f), 3.15f, 0.42f);
            AddElement("element_dark_swallow", "黒淵吞獄", BattleElement.Dark, "Wide Arm Spell Casting.fbx",
                new Color(0.12f, 0.02f, 0.18f), 3.25f, 0.3f);
            AddElement("element_mu", "虚白・霧域", BattleElement.Mu, "Standing 2H Magic Area Attack 02.fbx",
                new Color(0.92f, 0.94f, 0.98f), 0f, 0.35f, selfBuff: true);
            AddElement("element_mu_bind", "虚柱封縛", BattleElement.Mu, "Sword And Shield Casting.fbx",
                new Color(0.78f, 0.8f, 0.88f), 3.0f, 0.34f);
            AddElement("element_fire_core", "劫炎熔墜", BattleElement.Fire, "Standing 2H Magic Attack 04.fbx",
                new Color(0.95f, 0.32f, 0.05f), 4.35f, 0.2f);
            AddElement("element_water_spear", "蒼渦穿潮", BattleElement.Water, "Standing 2H Magic Area Attack 02.fbx",
                new Color(0.1f, 0.38f, 0.72f), 4.25f, 0.2f);
            AddElement("element_light_cross", "光戟十字", BattleElement.Light, "Wide Arm Spell Casting.fbx",
                new Color(1f, 0.9f, 0.42f), 4.3f, 0.18f);
            AddElement("element_nature_jaw", "神樹顎殺", BattleElement.Nature, "Sword And Shield Casting.fbx",
                new Color(0.36f, 0.24f, 0.12f), 4.4f, 0.22f);
            AddElement("element_dark_hex", "黒棘穿星", BattleElement.Dark, "Standing 2H Magic Attack 04.fbx",
                new Color(0.08f, 0.02f, 0.1f), 4.45f, 0.2f);
            AddElement("element_mu_press", "虚匣圧潰", BattleElement.Mu, "Wide Arm Spell Casting.fbx",
                new Color(0.86f, 0.88f, 0.92f), 4.2f, 0.2f);

            // ---- 近接タイプ(当たったらエフェクトだけ) ----
            AddMelee("fist_fight", "連撃乱打", "Fist Fight B.fbx",
                new Color(1f, 0.75f, 0.3f), 2.8f, 0, 6, 0.50f, facingOffsetY: 20f, animSpeed: 2.0f, lungeMeters: 3.5f, lungeStop: 0.08f);
            AddMelee("kick_1", "旋風脚", "Kicking.fbx",
                new Color(1f, 0.5f, 0.15f), 2.9f, 0, 6, 0.53f, facingOffsetY: -90f, animSpeed: 2.5f);
            AddMelee("kick_2", "乱れ蹴り", "Kicking (1).fbx",
                new Color(1f, 0.4f, 0.3f), 2.9f, 0, 6, 0.58f, facingOffsetY: 70f, animSpeed: 2.4f);
            AddMelee("kick_3", "疾風蹴り", "Kicking (2).fbx",
                new Color(1f, 0.6f, 0.1f), 2.9f, 0, 6, 0.55f, facingOffsetY: 70f, animSpeed: 2.2f);
            AddMelee("jump_smash", "跳躍撃", "Jumping.fbx",
                new Color(0.5f, 0.85f, 1f), 3.0f, 0, 7, 0.53f, facingOffsetY: -40f, animSpeed: 1.8f);
            AddMelee("chapa_kick", "回転旋脚", "Chapa-Giratoria.fbx",
                new Color(1f, 0.5f, 0.75f), 3.1f, 0, 7, 0.74f, animSpeed: 2.0f, lungeMeters: 2.4f);
            AddMelee("capoeira_kick", "舞踏脚", "Capoeira.fbx",
                new Color(0.4f, 0.85f, 0.8f), 3.1f, 0, 7, 0.65f, facingOffsetY: 90f, animSpeed: 2.0f, lungeMeters: 2.4f);
            AddMelee("hurricane_kick", "旋風連脚", "Hurricane Kick.fbx",
                new Color(0.4f, 0.9f, 0.95f), 3.2f, 0, 8, 0.53f, facingOffsetY: 46f, animSpeed: 2.4f, lungeMeters: 2.4f);
            AddFalling("choke_lift", "天剣落とし", "Superhuman Choke Lift.fbx",
                new Color(0.75f, 0.55f, 1f), 3.4f, 0, 9, FallingObjectKind.Sword, 4, floatRadius: 0.85f, impactFraction: 0.64f, rainFromAbove: true, animSpeed: 2.2f);
            AddMelee("knee_combo", "飛拳乱舞", "Flying Knee Punch Combo (1).fbx",
                new Color(1f, 0.35f, 0.35f), 3.1f, 0, 8, 0.66f, animSpeed: 2.3f, jumpBoost: 1.2f);
            AddMelee("ryuken", "龍拳", "Flying Knee Punch Combo (1).fbx",
                new Color(1f, 0.78f, 0.18f), 3.9f, 0, 8, 0.62f, animSpeed: 2.3f);
            AddMelee("rasengan", "螺旋丸", "Flying Knee Punch Combo (1).fbx",
                new Color(0.28f, 0.82f, 1f), 3.6f, 0, 8, 0.55f, animSpeed: 2.3f);
            AddMelee("drop_kick", "ドロップキック", "Drop Kick.fbx",
                new Color(1f, 0.55f, 0.2f), 3.0f, 0, 6, 0.45f, facingOffsetY: -30f, animSpeed: 1.8f, lungeMeters: 2.4f);
            AddMelee("bicycle_kick", "自転車蹴り", "Flying Bicycle Kick.fbx",
                new Color(0.7f, 0.9f, 0.3f), 3.1f, 0, 8, 0.5f, animSpeed: 1.3f, lungeMeters: 2.4f, jumpBoost: 1.5f);
            AddMelee("butterfly_twirl", "蝶旋撃", "Butterfly Twirl.fbx",
                new Color(0.95f, 0.4f, 0.85f), 3.0f, 0, 7, 0.52f, facingOffsetY: -30f, animSpeed: 2.0f);
            AddMelee("uppercut_jab", "強襲アッパー", "Uppercut Jab.fbx",
                new Color(1f, 0.85f, 0.3f), 2.9f, 0, 6, 0.45f, animSpeed: 2.2f, lungeMeters: 2.8f, lungeStop: 0.4f);
            AddMelee("shoulder_throw", "投げ落とし", "Flying Shoulder Throw.fbx",
                new Color(0.8f, 0.55f, 0.3f), 3.3f, 0, 9, 0.56f, facingOffsetY: -30f, animSpeed: 1.6f, lungeMeters: 2.8f, lungeStop: 0.3f);

            SetGear("telekinesis_barrage", UltimateGear.Gear2);
            SetGear("field_nuke", UltimateGear.Gear2);
            SetGear("telekinesis_slam", UltimateGear.Gear3);
            SetGear("asteroid_full_attack", UltimateGear.Brainstorm);
            SetGear("arcane_bolt", UltimateGear.Gear2);
            SetGear("wide_cast_bolt", UltimateGear.Gear3);
            SetGear("arrow_shot", UltimateGear.Gear1);
            SetGear("bow_sword_shot", UltimateGear.Gear2);
            SetGear("gunplay_shot", UltimateGear.Gear1);
            SetGear("kamehameha", UltimateGear.Gear3);
            SetGear("cero", UltimateGear.Gear2);
            SetGear("gran_rey_cero", UltimateGear.Brainstorm);
            SetGear("sakanade", UltimateGear.Brainstorm);
            SetGear("getsuga", UltimateGear.Gear2);
            SetGear("spirit_gun", UltimateGear.Gear1);
            SetGear("shinra_tensei", UltimateGear.Gear3);
            SetGear("makankosappo", UltimateGear.Gear3);
            SetGear("genkidama", UltimateGear.Brainstorm);
            SetGear("hound", UltimateGear.Gear2);
            SetGear("meteora", UltimateGear.Gear2);
            SetGear("kurohitsugi", UltimateGear.Gear3);
            SetGear("kyoshiki_murasaki", UltimateGear.Brainstorm);
            SetGear("ryuken", UltimateGear.Gear3);
            SetGear("hakai", UltimateGear.Brainstorm);
            SetGear("rasengan", UltimateGear.Gear3);
            SetGear("kaioken", UltimateGear.Brainstorm);
            SetGear("reiatsu", UltimateGear.Brainstorm);
            SetGear("shogun_henshin", UltimateGear.Brainstorm);
            SetGear("fist_fight", UltimateGear.Gear2);
            SetGear("kick_1", UltimateGear.Gear1);
            SetGear("kick_2", UltimateGear.Gear1);
            SetGear("kick_3", UltimateGear.Gear1);
            SetGear("jump_smash", UltimateGear.Gear1);
            SetGear("chapa_kick", UltimateGear.Gear1);
            SetGear("capoeira_kick", UltimateGear.Gear2);
            SetGear("hurricane_kick", UltimateGear.Gear2);
            SetGear("choke_lift", UltimateGear.Gear3);
            SetGear("knee_combo", UltimateGear.Gear2);
            SetGear("drop_kick", UltimateGear.Gear1);
            SetGear("bicycle_kick", UltimateGear.Gear1);
            SetGear("butterfly_twirl", UltimateGear.Gear2);
            SetGear("uppercut_jab", UltimateGear.Gear1);
            SetGear("shoulder_throw", UltimateGear.Gear2);
            SetGear("element_fire", UltimateGear.Gear1);
            SetGear("element_water", UltimateGear.Gear1);
            SetGear("element_light_punch", UltimateGear.Gear1);
            SetGear("element_mu_bind", UltimateGear.Gear1);
            SetGear("element_fire_burn", UltimateGear.Gear2);
            SetGear("element_water_drown", UltimateGear.Gear2);
            SetGear("element_light", UltimateGear.Gear2);
            SetGear("element_nature", UltimateGear.Gear2);
            SetGear("element_nature_pierce", UltimateGear.Gear3);
            SetGear("element_dark", UltimateGear.Gear3);
            SetGear("element_dark_swallow", UltimateGear.Gear3);
            SetGear("element_mu", UltimateGear.Gear3);
            SetGear("element_fire_core", UltimateGear.Brainstorm);
            SetGear("element_water_spear", UltimateGear.Brainstorm);
            SetGear("element_light_cross", UltimateGear.Brainstorm);
            SetGear("element_nature_jaw", UltimateGear.Brainstorm);
            SetGear("element_dark_hex", UltimateGear.Brainstorm);
            SetGear("element_mu_press", UltimateGear.Brainstorm);
            var ryu = Get("ryuken");
            if (ryu != null) ryu.waveKind = "ryuken";
            var ras = Get("rasengan");
            if (ras != null)
            {
                ras.waveKind = "rasengan";
                ras.lungeMeters = 2.35f;
                ras.lungeStop = 0.42f;
            }
            var hk = Get("hakai");
            if (hk != null)
            {
                hk.lungeMeters = 1.4f;
                hk.lungeStop = 0.22f;
                hk.lungeHoldUntil = 0f;
                hk.facingOffsetY = 40f;
            }
        }

        static void AddMelee(string id, string name, string clip, Color color, float baseMul, int unlockCost, int perLevelCost, float impactFraction = 0.5f, float facingOffsetY = 0f, float animSpeed = 1f, float lungeMeters = 0f, float jumpBoost = 0f, float lungeStop = 0f, float lungeHoldUntil = 0f)
        {
            Add(new UltimateSkillDefinition
            {
                id = id,
                displayName = name,
                motionType = UltimateMotionType.MeleeRush,
                vfxColor = color,
                animeClip = clip,
                damageMultiplierBase = baseMul,
                diamondCostToUnlock = unlockCost,
                diamondCostPerLevel = perLevelCost,
                impactFraction = impactFraction,
                facingOffsetY = facingOffsetY,
                animSpeed = animSpeed,
                lungeMeters = lungeMeters,
                jumpBoost = jumpBoost,
                lungeStop = lungeStop,
                lungeHoldUntil = lungeHoldUntil
            });
        }

        static void AddMagic(string id, string name, string clip, Color color, float baseMul, int unlockCost, int perLevelCost, float impactFraction = 0.42f, float facingOffsetY = 0f, float boltScale = 0f, int boltCount = 0, bool asteroid = false, bool asteroidFull = false)
        {
            Add(new UltimateSkillDefinition
            {
                id = id,
                displayName = name,
                motionType = UltimateMotionType.MagicBolt,
                vfxColor = color,
                animeClip = clip,
                damageMultiplierBase = baseMul,
                diamondCostToUnlock = unlockCost,
                diamondCostPerLevel = perLevelCost,
                impactFraction = impactFraction,
                facingOffsetY = facingOffsetY,
                boltScale = boltScale,
                boltCount = boltCount,
                asteroid = asteroid,
                asteroidFull = asteroidFull
            });
        }

        static void AddWave(string id, string name, string clip, Color color, float baseMul, float impactFraction)
        {
            Add(new UltimateSkillDefinition
            {
                id = id,
                displayName = name,
                motionType = UltimateMotionType.MagicBolt,
                vfxColor = color,
                animeClip = clip,
                damageMultiplierBase = baseMul,
                diamondCostToUnlock = 0,
                diamondCostPerLevel = 8,
                impactFraction = impactFraction,
                waveKind = id
            });
        }

        static void AddElement(string id, string name, BattleElement element, string clip, Color color, float baseMul, float impactFraction, bool selfBuff = false)
        {
            Add(new UltimateSkillDefinition
            {
                id = id,
                displayName = name,
                motionType = UltimateMotionType.MagicBolt,
                element = element,
                vfxColor = color,
                animeClip = clip,
                damageMultiplierBase = baseMul,
                diamondCostToUnlock = 0,
                diamondCostPerLevel = 8,
                impactFraction = impactFraction,
                waveKind = id,
                selfBuff = selfBuff
            });
        }

        static void AddBuff(string id, string name, string clip, Color color, float impactFraction)
        {
            Add(new UltimateSkillDefinition
            {
                id = id,
                displayName = name,
                motionType = UltimateMotionType.MagicBolt,
                vfxColor = color,
                animeClip = clip,
                damageMultiplierBase = 0f,
                diamondCostToUnlock = 0,
                diamondCostPerLevel = 8,
                impactFraction = impactFraction,
                waveKind = id,
                selfBuff = true
            });
        }

        static void AddBowSwords(string id, string name, string clip, Color color, float baseMul, int unlockCost, int perLevelCost, int count, float impactFraction, float facingOffsetY)
        {
            Add(new UltimateSkillDefinition
            {
                id = id,
                displayName = name,
                motionType = UltimateMotionType.SwordVolley,
                vfxColor = color,
                animeClip = clip,
                damageMultiplierBase = baseMul,
                diamondCostToUnlock = unlockCost,
                diamondCostPerLevel = perLevelCost,
                impactFraction = impactFraction,
                facingOffsetY = facingOffsetY,
                telekinesis = new TelekinesisWeaponData
                {
                    kind = FallingObjectKind.Sword,
                    weaponPrefabPath = LongswordPrefab,
                    weaponCount = count,
                    auraColor = color,
                    floatRadius = 0.35f,
                    spinSpeed = 420f
                }
            });
        }

        static void AddFalling(string id, string name, string clip, Color color, float baseMul, int unlockCost, int perLevelCost,
            FallingObjectKind kind, int count, float floatRadius, float impactFraction = 0.5f, bool rainFromAbove = false, float animSpeed = 1f, bool domainExpand = false)
        {
            var motion = domainExpand || (kind == FallingObjectKind.Sword && count >= 3)
                ? UltimateMotionType.FieldNuke
                : UltimateMotionType.TelekinesisSlam;
            Add(new UltimateSkillDefinition
            {
                id = id,
                displayName = name,
                motionType = motion,
                vfxColor = color,
                animeClip = clip,
                damageMultiplierBase = baseMul,
                diamondCostToUnlock = unlockCost,
                diamondCostPerLevel = perLevelCost,
                impactFraction = impactFraction,
                animSpeed = animSpeed,
                telekinesis = new TelekinesisWeaponData
                {
                    kind = kind,
                    weaponPrefabPath = (!domainExpand && kind == FallingObjectKind.Sword) ? LongswordPrefab : null,
                    weaponCount = count,
                    auraColor = color,
                    floatRadius = floatRadius,
                    spinSpeed = 260f,
                    rainFromAbove = rainFromAbove,
                    domainExpand = domainExpand
                }
            });
        }

        static void Add(UltimateSkillDefinition def) => All[def.id] = def;

        static void SetGear(string id, UltimateGear gear)
        {
            if (All.TryGetValue(id, out var def)) def.gear = gear;
        }

        static void SetDamage(string id, float mul)
        {
            if (All.TryGetValue(id, out var def)) def.damageMultiplierBase = mul;
        }

        public static UltimateSkillDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == "raygust_guard") id = "shogun_henshin";
            return All.TryGetValue(id, out var d) ? d : null;
        }

        public static IEnumerable<UltimateSkillDefinition> AllDefs => All.Values;

        public static readonly UltimateGear[] GearOrder =
        {
            UltimateGear.Gear1,
            UltimateGear.Gear2,
            UltimateGear.Gear3,
            UltimateGear.Brainstorm
        };

        public static int GearNeed(UltimateGear gear)
        {
            switch (gear)
            {
                case UltimateGear.Gear1: return 1;
                case UltimateGear.Gear2: return 2;
                case UltimateGear.Gear3: return 3;
                default: return 4;
            }
        }

        public static string GearNeedLabel(UltimateGear gear)
        {
            switch (gear)
            {
                case UltimateGear.Gear1: return "ギア1";
                case UltimateGear.Gear2: return "ギア2";
                case UltimateGear.Gear3: return "ギア3";
                default: return "超必殺";
            }
        }

        public static string GearNeedLabel(int rank)
        {
            if (rank <= 1) return "ギア1";
            if (rank == 2) return "ギア2";
            if (rank == 3) return "ギア3";
            return "超必殺";
        }

        public static string GearLabel(UltimateGear gear)
        {
            switch (gear)
            {
                case UltimateGear.Gear1: return "ギア1";
                case UltimateGear.Gear2: return "ギア2";
                case UltimateGear.Gear3: return "ギア3";
                case UltimateGear.Brainstorm: return "超必殺技（ブレインストーム）";
                default: return "";
            }
        }

        public static IEnumerable<UltimateSkillDefinition> DefsIn(UltimateGear gear)
        {
            foreach (var def in All.Values)
                if (def.gear == gear) yield return def;
        }

        public static IEnumerable<UltimateSkillDefinition> ElementDefs()
        {
            foreach (var def in All.Values)
            {
                if (BattleElementUtil.IsElementSkill(def.element))
                    yield return def;
            }
        }
    }
}
