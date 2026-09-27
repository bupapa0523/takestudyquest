using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ShiftingMetropolis.Progress;
using ShiftingMetropolis.Dungeon;
using ShiftingMetropolis.App;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// ターン制バトルのプロトタイプコア。
    /// ターン順の決定、コマンド選択、命中/カリティカル判定、ダメージ計算、
    /// 勝敗判定までをこのスクリプトだけで完結させる。
    /// UI/フィーレビズマッピングはGameObject.Findで自動探索し、インスキィーでの手動接続を不要にしている。
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("UI GameObject 名(シーン内で一意であること)")]
        public string playerHpSliderName = "PlayerHPSlider";
        public string enemyHpSliderName = "EnemyHPSlider";
        public string logTextName = "LogText";
        public string playerVisualName = "PlayerVisual";
        public string enemyVisualName = "EnemyVisual";
        public string resultTextName = "ResultText";

        private Slider playerHpSlider;
        private Slider enemyHpSlider;
        private Text logText;
        private Text resultText;
        private PlaceholderVisual playerVisual;
        private PlaceholderVisual enemyVisual;

        private readonly Dictionary<SkillCommandType, Button> buttons = new Dictionary<SkillCommandType, Button>();
        private readonly Dictionary<SkillCommandType, string> buttonNames = new Dictionary<SkillCommandType, string>
        {
            { SkillCommandType.Charge, "Button_Charge" },
            { SkillCommandType.Skill1, "Button_Skill1" },
            { SkillCommandType.Skill2, "Button_Skill2" },
            { SkillCommandType.Skill3, "Button_Skill3" },
            { SkillCommandType.BrainSmash, "Button_BrainSmash" },
        };

        [System.NonSerialized]
        public BattleCharacter player;
        [System.NonSerialized]
        public BattleCharacter enemy;

        private List<BattleCharacter> turnOrder;
        private int turnIndex;
        private bool waitingForPlayerInput;
        bool suppressFollowUp;
        private SkillCommandType? pendingCommand;
        private bool pendingGearUp;
        private bool battleOver;

        EnemyJob enemyJob;
        int currentFloor;
        bool floorIsBoss;
        bool raidMode;
        int raidGen;
        bool raidStop;
        bool raidWon;
        bool raidLeave;
        ShadowQuality savedShadowQuality;
        bool mobileRaidQualityApplied;
        UltimateSkillDefinition currentUltimate;
        UltimateSkillDefinition castUltimate;
        bool resumeSkipBeginTurn;
        static bool sessionEntered;

        static UltimateSkillDefinition ResolveEquippedUltimate()
        {
            return UltimateCatalog.Get(StudyStore.SlotUltimateId(SkillCommandType.BrainSmash))
                ?? UltimateCatalog.Get("telekinesis_slam")
                ?? UltimateCatalog.Get("kick_1");
        }

        static UltimateSkillDefinition ResolveSlotUltimate(SkillCommandType type)
        {
            return UltimateCatalog.Get(StudyStore.SlotUltimateId(type));
        }

        void ApplyEquippedSkills()
        {
            if (player == null) return;
            ApplySlot(SkillCommandType.Skill1);
            ApplySlot(SkillCommandType.Skill2);
            ApplySlot(SkillCommandType.Skill3);
            ApplySlot(SkillCommandType.Charge);
            ApplyEquippedUltimate();
        }

        void ApplySlot(SkillCommandType type)
        {
            var ult = ResolveSlotUltimate(type);
            var skill = player.GetSkill(type);
            if (ult == null || skill == null) return;
            int lv = StudyStore.UltimateLevel(ult.id);
            skill.skillName = StudyStore.SkillTitle(ult.displayName, lv);
            skill.requiredGauge = 0;
            skill.requiredGear = UltimateCatalog.GearNeed(ult.gear);
            skill.powerMultiplier = ult.MultiplierAtLevel(lv);
            skill.accuracy = 0.88f;
            skill.isUltimate = true;
        }

        static Skill MakePlayerSkill(SkillCommandType type)
        {
            var ult = ResolveSlotUltimate(type);
            if (ult == null) return new Skill("技", type, 0, 1.2f, 0.9f, true);
            int lv = StudyStore.UltimateLevel(ult.id);
            var skill = new Skill(StudyStore.SkillTitle(ult.displayName, lv), type, 0, ult.MultiplierAtLevel(lv), 0.9f, true);
            skill.requiredGear = UltimateCatalog.GearNeed(ult.gear);
            return skill;
        }

        static float CamPull(UltimateSkillDefinition ultimate)
        {
            if (ultimate == null) return 1f;
            if (ultimate.id == "sakanade") return 1.35f;
            if (ultimate.id == "asteroid_full_attack") return 1.75f;
            if (ultimate.id == "kyoshiki_murasaki") return 1.45f;
            if (ultimate.id == "hakai") return 1.4f;
            if (ultimate.id == "rasengan") return 1.35f;
            return 1f;
        }

        static bool CamFullBody(UltimateSkillDefinition ultimate)
        {
            return ultimate != null && (ultimate.id == "reiatsu" || ultimate.id == "shogun_henshin" || ultimate.id == "kaioken");
        }

        BasicSkillDefinition EquippedBasic(SkillCommandType type)
        {
            if (type != SkillCommandType.Skill1 && type != SkillCommandType.Skill2 && type != SkillCommandType.Skill3)
                return null;
            if (player != null && player.shogunFormTurns > 0)
                return ShogunKit.ForSlot(type);
            return SkillCatalog.Get(StudyStore.ActiveSkillId(type));
        }

        void ApplyEquippedUltimate()
        {
            var equipped = ResolveEquippedUltimate();
            currentUltimate = equipped;
            if (player == null) return;
            var smash = player.GetSkill(SkillCommandType.BrainSmash);
            if (player.senseInvertTurns > 0)
            {
                var cero = UltimateCatalog.Get("cero");
                if (cero != null) currentUltimate = cero;
            }
            else if (player.shogunFormTurns > 0)
            {
                if (smash != null)
                {
                    smash.skillName = "黒弾";
                    smash.powerMultiplier = 1.7f;
                }
                return;
            }
            if (smash == null || currentUltimate == null) return;
            int lv = StudyStore.UltimateLevel(equipped != null ? equipped.id : currentUltimate.id);
            smash.skillName = StudyStore.SkillTitle(currentUltimate.displayName, lv);
            smash.requiredGear = 0;
            smash.powerMultiplier = currentUltimate.MultiplierAtLevel(lv);
        }

        void ApplyShogunKit()
        {
            if (player == null) return;
            var bolt = ShogunKit.Bolt;
            player.SetSkill(new Skill(bolt.displayName, SkillCommandType.Skill1, 0, bolt.powerMultiplier, bolt.accuracy, true));
            player.SetSkill(new Skill(bolt.displayName, SkillCommandType.Skill2, 0, bolt.powerMultiplier, bolt.accuracy, true));
            player.SetSkill(new Skill(bolt.displayName, SkillCommandType.Skill3, 0, bolt.powerMultiplier, bolt.accuracy, true));
            player.SetSkill(new Skill(bolt.displayName, SkillCommandType.Charge, 0, bolt.powerMultiplier, bolt.accuracy, true));
            player.SetSkill(new Skill(bolt.displayName, SkillCommandType.BrainSmash, 0, bolt.powerMultiplier, bolt.accuracy, true));
            ApplyEquippedUltimate();
        }

        void RestorePlayerKit()
        {
            if (player == null) return;
            player.SetSkill(MakePlayerSkill(SkillCommandType.Skill1));
            player.SetSkill(MakePlayerSkill(SkillCommandType.Skill2));
            player.SetSkill(MakePlayerSkill(SkillCommandType.Skill3));
            player.SetSkill(MakePlayerSkill(SkillCommandType.Charge));
            ApplyEquippedUltimate();
        }

        bool uiWired;

        public static void MarkSessionEntered()
        {
            sessionEntered = true;
        }

        void OnEnable()
        {
            BeginBattle();
        }

        void OnDisable()
        {
            if (raidMode)
            {
                if (battleOver || player == null || !player.IsAlive || enemy == null || !enemy.IsAlive)
                    RaidStore.ClearPause();
                else if (sessionEntered)
                    RaidStore.SavePause(CapturePausedBattle());
                if (enemy != null) enemy.onHpLoss = null;
                RaidSync.Fighting = false;
                StopAllCoroutines();
                waitingForPlayerInput = false;
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                raidMode = false;
                BattleStage.RaidMode = false;
                BattleHudSkin.ClearRaid();
                RestoreMobileRaidQuality();
                return;
            }
            if (sessionEntered && !battleOver && player != null && enemy != null && player.IsAlive && enemy.IsAlive)
                StudyStore.SavePausedBattle(CapturePausedBattle());
            StopAllCoroutines();
            waitingForPlayerInput = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        public void BeginBattle()
        {
            raidMode = BattleStage.RaidMode;
            if (raidMode)
            {
                BeginRaidBattle();
                return;
            }
            StopAllCoroutines();
            var paused = StudyStore.GetPausedBattle();
            bool resume = paused != null;
            turnIndex = resume ? Mathf.Max(0, paused.turnIndex) : 0;
            battleOver = false;
            pendingCommand = null;
            waitingForPlayerInput = false;
            resumeSkipBeginTurn = resume;
            if (resume) StudyStore.RestorePausedGrowth(paused);
            SetupCharacters();
            if (resume) ApplyPausedBattle(paused);
            WireUi();
            ApplyBattleLabels();
            if (resultText != null) resultText.gameObject.SetActive(false);
            BattleHudSkin.HideResult();
            UpdateHpBars();
            UpdateButtonInteractable();
            SyncAilments();
            if (!resume)
            {
                if (floorIsBoss)
                {
                    BattleAudio.GhostSting();
                    Talk(enemy, "……ここは、通さない。");
                }
                else Talk(enemy, "……来たな。");
            }
            else Talk(player, "……続きからだ。");
            StartCoroutine(BattleLoop());
        }

        void BeginRaidBattle()
        {
            StopAllCoroutines();
            raidMode = true;
            BattleStage.RaidMode = true;
            ApplyMobileRaidQuality();
            RaidSync.ResetBattleSession();
            var pause = RaidStore.PeekPause();
            bool resume = pause != null;
            turnIndex = resume ? Mathf.Max(0, pause.turnIndex) : 0;
            battleOver = false;
            pendingCommand = null;
            waitingForPlayerInput = false;
            resumeSkipBeginTurn = resume && pause.pausedAtCommand == 1;
            raidStop = false;
            raidWon = false;
            raidLeave = false;
            SetupRaidCharacters();
            raidGen = RaidStore.Boss != null ? RaidStore.Boss.generation : 0;
            RaidStore.BattleGeneration = raidGen;
            if (resume) ApplyRaidResume(pause);
            WireUi();
            ApplyBattleLabels();
            if (resultText != null) resultText.gameObject.SetActive(false);
            BattleHudSkin.HideResult();
            UpdateHpBars();
            UpdateButtonInteractable();
            SyncAilments();
            Talk(enemy, resume ? "……まだ、倒れてはいない。" : "……まとめて、かかってこい。");
            StartCoroutine(BattleLoop());
        }

        void ApplyMobileRaidQuality()
        {
            if (!Application.isMobilePlatform || mobileRaidQualityApplied) return;
            savedShadowQuality = QualitySettings.shadows;
            QualitySettings.shadows = ShadowQuality.Disable;
            mobileRaidQualityApplied = true;
        }

        void RestoreMobileRaidQuality()
        {
            if (!mobileRaidQualityApplied) return;
            QualitySettings.shadows = savedShadowQuality;
            mobileRaidQualityApplied = false;
        }

        void SetupRaidCharacters()
        {
            var boss = RaidStore.Boss;
            if (boss == null)
            {
                RaidStore.EnsureLocal(SupabaseSync.Room());
                boss = RaidStore.Boss;
            }
            BattleStage.RaidMode = true;
            BattleStage.RaidFloor = boss != null ? boss.floor : 1;
            BattleStage.RaidWeek = boss != null ? boss.week : 1;
            BattleStage.RaidSkin = boss != null ? boss.skin : 0;
            BattleStage.RaidSkinPath = boss != null ? boss.skinPath : "";
            BattleStage.RaidEnemyName = boss != null ? boss.enemyName : "巨影";

            var battleRoot = GameObject.Find("BattleRoot");
            BattleStage.Rebuild(battleRoot != null ? battleRoot.transform : transform);

            var playerStats = new CharacterStats(
                maxHp: StudyStore.BattleMaxHp(),
                attack: StudyStore.BattleAttack(),
                defense: StudyStore.BattleDefense(),
                speed: StudyStore.BattleSpeed(),
                luck: StudyStore.BattleLuck());
            var playerSkills = new List<Skill>
            {
                MakePlayerSkill(SkillCommandType.Charge),
                MakePlayerSkill(SkillCommandType.Skill1),
                MakePlayerSkill(SkillCommandType.Skill2),
                MakePlayerSkill(SkillCommandType.Skill3),
                new Skill("必殺", SkillCommandType.BrainSmash, 0, 3.2f, 0.86f, isUltimate: true),
            };
            playerSkills[playerSkills.Count - 1].requiredGear = 0;
            Transform playerTf = BattleStage.PlayerFighter != null ? BattleStage.PlayerFighter.Root : GameObject.Find(playerVisualName)?.transform;
            player = new BattleCharacter(BattleStage.PlayerLabel, playerStats, playerSkills, true, playerTf);

            enemyJob = EnemyKit.JobFromKind(BattleStage.EnemyKind);
            currentFloor = BattleStage.Floor;
            floorIsBoss = true;
            int sharedMax = boss != null ? Mathf.Max(1, boss.maxHp) : 1;
            int sharedHp = boss != null ? Mathf.Clamp(boss.hp, 0, sharedMax) : sharedMax;
            var enemyStats = RaidRules.MakeStats(
                boss != null ? boss.week : 1,
                currentFloor,
                boss != null ? boss.clears : 0,
                BattleStage.EnemyKind,
                sharedMax);
            var enemySkills = EnemyKit.Build(currentFloor, true, BattleStage.EnemyKind);
            Transform enemyTf = BattleStage.EnemyFighter != null ? BattleStage.EnemyFighter.Root : GameObject.Find(enemyVisualName)?.transform;
            enemy = new BattleCharacter(BattleStage.EnemyLabel, enemyStats, enemySkills, false, enemyTf);
            enemy.brainSmashChargesLeft = 3;
            enemy.onHpLoss = amount => RaidSync.Report(amount);
            enemy.currentHp = sharedHp;

            ApplyEquippedSkills();
            ApplyEquippedUltimate();
            turnOrder = new List<BattleCharacter>();
            if (player.baseStats.speed >= enemy.baseStats.speed)
            {
                turnOrder.Add(player);
                turnOrder.Add(enemy);
            }
            else
            {
                turnOrder.Add(enemy);
                turnOrder.Add(player);
            }
        }

        void ApplyRaidResume(PausedBattleState pause)
        {
            if (pause == null) return;
            resumeSkipBeginTurn = pause.pausedAtCommand == 1;
            turnIndex = Mathf.Max(0, pause.turnIndex);
            ApplyFighter(player, pause.player, false);
            if (player != null && player.baseStats != null)
                player.currentHp = Mathf.Clamp(player.currentHp, 1, Mathf.Max(1, player.baseStats.maxHp));
            if (player != null && player.shogunFormTurns > 0) ApplyShogunKit();
            else RestorePlayerKit();
            ApplyEquippedUltimate();
            turnOrder = new List<BattleCharacter>();
            if (player.baseStats.speed >= enemy.baseStats.speed)
            {
                turnOrder.Add(player);
                turnOrder.Add(enemy);
            }
            else
            {
                turnOrder.Add(enemy);
                turnOrder.Add(player);
            }
        }

        IEnumerator RaidGate(bool beforePlayerInput)
        {
            raidStop = false;
            raidWon = false;
            raidLeave = false;
            if (!raidMode) yield break;
            yield return RaidSync.Flush();
            var boss = RaidStore.Boss;
            if (RaidSync.Cleared || (boss != null && raidGen > 0 && boss.generation != raidGen))
            {
                raidStop = true;
                raidWon = true;
                yield break;
            }
            if (boss != null && enemy != null && enemy.baseStats != null && boss.generation == raidGen)
            {
                enemy.baseStats.maxHp = Mathf.Max(1, boss.maxHp);
                enemy.currentHp = Mathf.Clamp(boss.hp, 0, enemy.baseStats.maxHp);
                UpdateHpBars();
            }
            if (enemy != null && !enemy.IsAlive)
            {
                raidStop = true;
                raidWon = true;
                yield break;
            }
            if (player != null && !player.IsAlive)
            {
                raidStop = true;
                raidWon = false;
                yield break;
            }
            if (beforePlayerInput && RaidStore.TurnsLeft() <= 0)
            {
                Talk(player, "今日の15ターンを使い切った。");
                yield return new WaitForSeconds(0.7f);
                var flow = AppFlow.Instance;
                if (flow == null) flow = UnityEngine.Object.FindFirstObjectByType<AppFlow>();
                if (flow != null) flow.ShowHome();
                raidLeave = true;
            }
        }

        private void SetupCharacters()
        {
            var battleRoot = GameObject.Find("BattleRoot");
            BattleStage.Rebuild(battleRoot != null ? battleRoot.transform : transform);

            var playerStats = new CharacterStats(
                maxHp: StudyStore.BattleMaxHp(),
                attack: StudyStore.BattleAttack(),
                defense: StudyStore.BattleDefense(),
                speed: StudyStore.BattleSpeed(),
                luck: StudyStore.BattleLuck());
            var playerSkills = new List<Skill>
            {
                MakePlayerSkill(SkillCommandType.Charge),
                MakePlayerSkill(SkillCommandType.Skill1),
                MakePlayerSkill(SkillCommandType.Skill2),
                MakePlayerSkill(SkillCommandType.Skill3),
                new Skill("必殺", SkillCommandType.BrainSmash, 0, 3.2f, 0.86f, isUltimate: true),
            };
            playerSkills[playerSkills.Count - 1].requiredGear = 0;
            Transform playerTf = BattleStage.PlayerFighter != null ? BattleStage.PlayerFighter.Root : GameObject.Find(playerVisualName)?.transform;
            player = new BattleCharacter(BattleStage.PlayerLabel, playerStats, playerSkills, true, playerTf);

            enemyJob = EnemyKit.JobFromKind(BattleStage.EnemyKind);
            currentFloor = BattleStage.Floor;
            floorIsBoss = BattleStage.IsBoss;
            var enemyStats = DungeonRunner.ScaleEnemyStats(
                new CharacterStats(maxHp: 1, attack: 1, defense: 1, speed: 12, luck: 8),
                currentFloor, floorIsBoss);
            if (enemyJob == EnemyJob.Knight)
            {
                enemyStats.maxHp = Mathf.RoundToInt(enemyStats.maxHp * 1.08f);
                enemyStats.attack = Mathf.RoundToInt(enemyStats.attack * 0.92f);
                enemyStats.defense = Mathf.RoundToInt(enemyStats.defense * 1.12f);
            }
            else if (enemyJob == EnemyJob.Mage)
            {
                enemyStats.maxHp = Mathf.RoundToInt(enemyStats.maxHp * 0.92f);
                enemyStats.attack = Mathf.RoundToInt(enemyStats.attack * 1.1f);
                enemyStats.defense = Mathf.Max(1, Mathf.RoundToInt(enemyStats.defense * 0.9f));
            }
            var enemySkills = EnemyKit.Build(currentFloor, floorIsBoss, BattleStage.EnemyKind);
            Transform enemyTf = BattleStage.EnemyFighter != null ? BattleStage.EnemyFighter.Root : GameObject.Find(enemyVisualName)?.transform;
            enemy = new BattleCharacter(BattleStage.EnemyLabel, enemyStats, enemySkills, false, enemyTf);
            if (floorIsBoss) enemy.brainSmashChargesLeft = 2;

            ApplyEquippedSkills();
            ApplyEquippedUltimate();

            // 素早さのある方が先行する固定スターター順。同じならプレイヤー優先。
            turnOrder = new List<BattleCharacter>();
            if (player.baseStats.speed >= enemy.baseStats.speed)
            {
                turnOrder.Add(player);
                turnOrder.Add(enemy);
            }
            else
            {
                turnOrder.Add(enemy);
                turnOrder.Add(player);
            }
        }

        PausedBattleState CapturePausedBattle()
        {
            var state = new PausedBattleState
            {
                active = 1,
                floor = currentFloor,
                turnIndex = turnIndex,
                pausedAtCommand = waitingForPlayerInput ? 1 : 0,
                enemyJob = (int)enemyJob,
                player = CaptureFighter(player),
                enemy = CaptureFighter(enemy)
            };
            return state;
        }

        static PausedFighterState CaptureFighter(BattleCharacter c)
        {
            if (c == null) return null;
            var s = c.baseStats;
            return new PausedFighterState
            {
                name = c.characterName,
                hp = c.currentHp,
                gauge = c.currentGauge,
                gear = c.currentGear,
                smash = c.brainSmashChargesLeft,
                maxHp = s != null ? s.maxHp : c.currentHp,
                atk = s != null ? s.attack : 0,
                def = s != null ? s.defense : 0,
                spd = s != null ? s.speed : 0,
                luck = s != null ? s.luck : 0,
                burn = c.burnTurns,
                burnDmg = c.burnDamage,
                soak = c.soakTurns,
                soakDmg = c.soakDamage,
                stun = c.stunTurns,
                atkBoost = c.atkBoostTurns,
                defBoost = c.defBoostTurns,
                evade = c.evadeTurns,
                sense = c.senseInvertTurns,
                shogun = c.shogunFormTurns,
                voidRealm = c.voidRealmTurns,
                lightMark = c.lightMarkTurns,
                atkMul = c.atkBoostMul,
                defMul = c.defBoostMul,
                evadeBonus = c.evadeBonus
            };
        }

        void ApplyPausedBattle(PausedBattleState paused)
        {
            if (paused == null) return;
            resumeSkipBeginTurn = paused.pausedAtCommand == 1;
            turnIndex = Mathf.Max(0, paused.turnIndex);
            ApplyFighter(player, paused.player, true);
            ApplyFighter(enemy, paused.enemy, true);
            if (paused.enemy != null && !string.IsNullOrEmpty(paused.enemy.name))
                enemy.characterName = paused.enemy.name;
            enemyJob = (EnemyJob)paused.enemyJob;
            if (player != null && player.shogunFormTurns > 0) ApplyShogunKit();
            else RestorePlayerKit();
            ApplyEquippedUltimate();
            turnOrder = new List<BattleCharacter>();
            if (player.baseStats.speed >= enemy.baseStats.speed)
            {
                turnOrder.Add(player);
                turnOrder.Add(enemy);
            }
            else
            {
                turnOrder.Add(enemy);
                turnOrder.Add(player);
            }
        }

        static void ApplyFighter(BattleCharacter c, PausedFighterState snap, bool overwriteStats)
        {
            if (c == null || snap == null) return;
            if (overwriteStats)
            {
                c.baseStats.maxHp = Mathf.Max(1, snap.maxHp);
                c.baseStats.attack = snap.atk;
                c.baseStats.defense = snap.def;
                c.baseStats.speed = snap.spd;
                c.baseStats.luck = snap.luck;
                if (!string.IsNullOrEmpty(snap.name)) c.characterName = snap.name;
            }
            int max = c.baseStats != null ? Mathf.Max(1, c.baseStats.maxHp) : Mathf.Max(1, snap.maxHp);
            c.currentHp = Mathf.Clamp(snap.hp, 1, max);
            c.currentGauge = Mathf.Max(0, snap.gauge);
            c.currentGear = Mathf.Clamp(snap.gear, 1, BattleCharacter.MaxGear);
            c.brainSmashChargesLeft = Mathf.Max(0, snap.smash);
            c.burnTurns = Mathf.Max(0, snap.burn);
            c.burnDamage = Mathf.Max(0, snap.burnDmg);
            c.soakTurns = Mathf.Max(0, snap.soak);
            c.soakDamage = Mathf.Max(0, snap.soakDmg);
            c.stunTurns = Mathf.Max(0, snap.stun);
            c.atkBoostTurns = Mathf.Max(0, snap.atkBoost);
            c.defBoostTurns = Mathf.Max(0, snap.defBoost);
            c.evadeTurns = Mathf.Max(0, snap.evade);
            c.senseInvertTurns = Mathf.Max(0, snap.sense);
            c.shogunFormTurns = Mathf.Max(0, snap.shogun);
            c.voidRealmTurns = Mathf.Max(0, snap.voidRealm);
            c.lightMarkTurns = Mathf.Max(0, snap.lightMark);
            c.atkBoostMul = snap.atkMul > 0.01f ? snap.atkMul : 1f;
            c.defBoostMul = snap.defMul > 0.01f ? snap.defMul : 1f;
            c.evadeBonus = snap.evadeBonus;
        }

        private void WireUi()
        {
            playerHpSlider = FindComponent<Slider>(playerHpSliderName);
            enemyHpSlider = FindComponent<Slider>(enemyHpSliderName);
            logText = FindComponent<Text>(logTextName);
            resultText = FindComponent<Text>(resultTextName);
            playerVisual = FindComponent<PlaceholderVisual>(playerVisualName);
            enemyVisual = FindComponent<PlaceholderVisual>(enemyVisualName);

            if (!uiWired)
            {
                foreach (var kv in buttonNames)
                {
                    var button = FindComponent<Button>(kv.Value);
                    if (button == null) continue;
                    var commandType = kv.Key;
                    button.onClick.AddListener(() => OnPlayerCommand(commandType));
                    buttons[commandType] = button;
                }

                BattleHudSkin.BindGearUp(OnGearUp);

                var homeButton = FindComponent<Button>("Button_BattleHome");
                if (homeButton != null)
                {
                    var homeLabel = homeButton.GetComponentInChildren<Text>();
                    if (homeLabel != null) homeLabel.text = "ポーズ";
                    homeButton.onClick.AddListener(OpenPauseMenu);
                }

                uiWired = true;
            }

            BattleHudSkin.Apply();
        }

        private         static T FindComponent<T>(string name) where T : Component
        {
            var all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.name == name && all[i].gameObject.scene.IsValid())
                    return all[i];
            }
            return null;
        }

        void OpenPauseMenu()
        {
            if (battleOver) return;
            var canvas = GameObject.Find("BattleCanvas");
            if (canvas == null) return;
            var old = canvas.transform.Find("BattlePause");
            if (old != null) Destroy(old.gameObject);
            var dialog = new GameObject("BattlePause", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dialog.transform.SetParent(canvas.transform, false);
            var rt = dialog.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            dialog.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var font = BattleHudSkin.GetFont();
            float suspendY = raidMode ? 0.50f : 0.58f;
            float backY = raidMode ? 0.34f : 0.26f;
            var suspend = MakePauseButton(dialog.transform, font, "中断", suspendY, new Color(0.85f, 0.72f, 0.28f), Color.black);
            suspend.onClick.AddListener(() =>
            {
                Destroy(dialog);
                var flow = App.AppFlow.Instance;
                if (flow == null) flow = UnityEngine.Object.FindFirstObjectByType<App.AppFlow>();
                if (flow != null) flow.ShowHome();
            });
            if (!raidMode)
            {
                var end = MakePauseButton(dialog.transform, font, "終了", 0.42f, new Color(0.45f, 0.16f, 0.16f), Color.white);
                end.onClick.AddListener(() =>
                {
                    battleOver = true;
                    StudyStore.ClearPausedBattle();
                    Destroy(dialog);
                    var flow = App.AppFlow.Instance;
                    if (flow == null) flow = UnityEngine.Object.FindFirstObjectByType<App.AppFlow>();
                    if (flow != null) flow.ShowHome();
                });
            }
            var back = MakePauseButton(dialog.transform, font, "戻る", backY, new Color(0.22f, 0.22f, 0.26f), Color.white);
            back.onClick.AddListener(() => Destroy(dialog));
        }

        static Button MakePauseButton(Transform parent, Font font, string label, float y, Color bg, Color fg)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.18f, y);
            rt.anchorMax = new Vector2(0.82f, y + 0.12f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = bg;
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 28;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 28;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = fg;
            return go.GetComponent<Button>();
        }

        public void OnPlayerCommand(SkillCommandType commandType)
        {
            if (!waitingForPlayerInput || battleOver) return;

            var skill = player.GetSkill(commandType);
            if (!player.CanUse(skill))
                return;

            pendingCommand = commandType;
            pendingGearUp = false;
            waitingForPlayerInput = false;
        }

        public void OnGearUp()
        {
            if (!waitingForPlayerInput || battleOver) return;
            if (player == null || !player.CanRaiseGear()) return;
            pendingGearUp = true;
            pendingCommand = null;
            waitingForPlayerInput = false;
        }

        private IEnumerator BattleLoop()
        {
            while (!battleOver)
            {
                var actor = turnOrder[turnIndex % turnOrder.Count];
                var other = actor == player ? enemy : player;

                if (!actor.IsAlive)
                {
                    turnIndex++;
                    continue;
                }

                bool canAct;
                if (resumeSkipBeginTurn)
                {
                    resumeSkipBeginTurn = false;
                    canAct = actor.IsAlive;
                    actor.skippedFromStun = false;
                    actor.lastBurnDamage = 0;
                    actor.lastSoakDamage = 0;
                }
                else
                {
                    canAct = actor.BeginTurn();
                }
                if (actor.senseInvertEnded)
                {
                    actor.senseInvertEnded = false;
                    if (actor.isPlayer) Talk(actor, "逆撫が解けた。必殺技が戻った。");
                }
                if (actor.shogunFormEnded)
                {
                    actor.shogunFormEnded = false;
                    if (actor.isPlayer)
                    {
                        RestorePlayerKit();
                        Talk(actor, "将軍の技が解けた。技が戻った。");
                    }
                }
                SyncSenseInvert();
                if (actor.lastBurnDamage > 0)
                {
                    var hurt = FighterOf(actor);
                    if (hurt != null)
                    {
                        hurt.Flash(new Color(1f, 0.4f, 0.1f));
                        Vector3 pos = actor.visual != null ? actor.visual.position + Vector3.up * 1.2f : Vector3.up;
                        BattleFx.Popup(pos, actor.lastBurnDamage.ToString(), false);
                        if (actor.IsAlive) StartCoroutine(hurt.PlayHit());
                    }
                    UpdateHpBars();
                }
                else if (actor.lastSoakDamage > 0)
                {
                    var hurt = FighterOf(actor);
                    if (hurt != null)
                    {
                        hurt.Flash(new Color(0.25f, 0.55f, 1f));
                        Vector3 pos = actor.visual != null ? actor.visual.position + Vector3.up * 1.2f : Vector3.up;
                        BattleFx.Popup(pos, actor.lastSoakDamage.ToString(), false);
                        if (actor.IsAlive) StartCoroutine(hurt.PlayHit());
                    }
                    UpdateHpBars();
                }
                SyncAilments();
                if (!actor.IsAlive || (raidMode && !actor.isPlayer))
                {
                    if (raidMode)
                    {
                        yield return RaidGate(false);
                        if (raidLeave) yield break;
                        if (raidStop)
                        {
                            EndBattle(raidWon);
                            yield break;
                        }
                    }
                    if (!actor.IsAlive)
                    {
                        EndBattle(player.IsAlive);
                        yield break;
                    }
                }
                if (!canAct)
                {
                    if (actor.skippedFromStun)
                    {
                        SyncAilments();
                        Talk(actor, "……体が、言うことを聞かない。");
                    }
                    turnIndex++;
                    yield return new WaitForSeconds(0.35f);
                    continue;
                }

                if (actor.isPlayer)
                {
                    if (raidMode)
                    {
                        yield return RaidGate(true);
                        if (raidLeave) yield break;
                        if (raidStop)
                        {
                            EndBattle(raidWon);
                            yield break;
                        }
                    }
                    waitingForPlayerInput = true;
                    pendingCommand = null;
                    pendingGearUp = false;
                    UpdateButtonInteractable();
                    BattleHudSkin.ShowCommands();
                    yield return new WaitUntil(() => pendingCommand.HasValue || pendingGearUp);
                    if (pendingGearUp)
                        yield return PerformGearUp(actor);
                    else
                        yield return PerformCommand(actor, other, pendingCommand.Value);
                }
                else
                {
                    yield return new WaitForSeconds(0.45f);
                    var cmd = EnemyAI.ChooseCommand(actor, other, enemyJob);
                    yield return PerformCommand(actor, other, cmd);
                }

                if (player.IsAlive && enemy.IsAlive)
                    yield return MaybeFollowUp(actor, other);

                UpdateHpBars();
                UpdateButtonInteractable();

                if (raidMode || !player.IsAlive || !enemy.IsAlive)
                {
                    if (raidMode)
                    {
                        yield return RaidGate(false);
                        if (raidLeave) yield break;
                        if (raidStop)
                        {
                            EndBattle(raidWon);
                            yield break;
                        }
                    }
                    if (!player.IsAlive || !enemy.IsAlive)
                    {
                        EndBattle(player.IsAlive);
                        yield break;
                    }
                }

                turnIndex++;
                yield return new WaitForSeconds(0.3f);
            }
        }

        IEnumerator PerformGearUp(BattleCharacter actor)
        {
            if (raidMode && actor != null && actor.isPlayer && !RaidStore.TrySpendTurn())
            {
                Talk(actor, "今日の15ターンを使い切った。");
                yield break;
            }
            var actorFx = FighterOf(actor);
            Transform me = actor != null ? actor.visual : null;
            Transform foe = enemy != null ? enemy.visual : null;
            suppressFollowUp = true;
            BattleAudio.UiClick();
            StartCoroutine(BattleCam.Windup(me, foe, false, false));
            if (actorFx != null)
                yield return actorFx.PlaySkill(SkillCommandType.Charge, actor.visual, null);
            actor.RaiseGear();
            Talk(actor, "ギアアップ！　" + UltimateCatalog.GearNeedLabel(actor.currentGear) + "。");
            yield return BattleCam.Recover();
        }

        private IEnumerator PerformCommand(BattleCharacter actor, BattleCharacter target, SkillCommandType commandType)
        {
            var skill = actor.GetSkill(commandType);
            if (skill == null)
            {
                Talk(actor, "……動けなかった。");
                yield break;
            }
            if (!actor.CanUse(skill))
            {
                Talk(actor, actor.isPlayer && skill.type == SkillCommandType.BrainSmash
                    ? "今日の勉強が足りない。30分で1回。"
                    : "……今は無理だ。");
                yield break;
            }
            if (raidMode && actor.isPlayer && !RaidStore.TrySpendTurn())
            {
                Talk(actor, "今日の15ターンを使い切った。");
                yield break;
            }
            bool formReplace = actor.isPlayer && player != null
                && (player.senseInvertTurns > 0 || player.shogunFormTurns > 0);
            if (actor.isPlayer && skill.type == SkillCommandType.BrainSmash && !formReplace
                && !StudyStore.TryUseBrainSmash())
            {
                Talk(actor, "今日の勉強が足りない。30分で1回。");
                yield break;
            }

            actor.SpendForSkill(skill);
            suppressFollowUp = skill.type == SkillCommandType.Charge
                || skill.isUltimate
                || skill.type == SkillCommandType.BrainSmash;
            var actorFx = FighterOf(actor);
            var targetFx = FighterOf(target);
            bool invertCero = actor.isPlayer && player != null && player.senseInvertTurns > 0;
            var ceroDef = invertCero ? UltimateCatalog.Get("cero") : null;
            if (ceroDef == null) invertCero = false;
            bool ultimate = skill.isUltimate || skill.type == SkillCommandType.BrainSmash || invertCero;
            Transform me = player != null && player.visual != null ? player.visual : actor.visual;
            Transform foe = enemy != null && enemy.visual != null ? enemy.visual : target.visual;

            if (!invertCero && !actor.isPlayer && skill.type == SkillCommandType.Charge)
            {
                BattleAudio.UiClick();
                if (actorFx != null)
                    yield return actorFx.PlaySkill(skill.type, target.visual, null);
                actor.ApplyCharge();
                Talk(actor, "ギアを上げた。ギア" + actor.currentGear + "。");
                yield break;
            }

            UltimateSkillDefinition ultimateForThis = actor.isPlayer
                ? ResolveSlotUltimate(skill.type)
                : UltimateCatalog.Get(skill.ultimateId);
            BasicSkillDefinition basicForThis = null;
            Skill impactSkill = skill;
            bool shogunBolt = !invertCero && actor.isPlayer && player != null && player.shogunFormTurns > 0;
            if (invertCero)
            {
                ultimateForThis = ceroDef;
                basicForThis = null;
                impactSkill = actor.GetSkill(SkillCommandType.BrainSmash) ?? skill;
                if (impactSkill != null)
                {
                    int ceroLv = StudyStore.UltimateLevel(ceroDef.id);
                    impactSkill.skillName = StudyStore.SkillTitle(ceroDef.displayName, ceroLv);
                    impactSkill.powerMultiplier = ceroDef.MultiplierAtLevel(ceroLv);
                }
            }
            else if (shogunBolt)
            {
                ultimateForThis = null;
                basicForThis = ShogunKit.Bolt;
                impactSkill = new Skill("黒弾", SkillCommandType.Skill3, 0, 1.7f, 0.88f, true);
            }
            castUltimate = ultimateForThis;
            bool selfBuff = !invertCero && ultimateForThis != null && ultimateForThis.selfBuff;
            Transform playTarget = selfBuff ? actor.visual : target.visual;
            SkillCommandType playType = skill.type;
            if (shogunBolt) playType = SkillCommandType.Skill3;
            else if (ultimateForThis != null) playType = SkillCommandType.BrainSmash;
            bool camShot = actor.isPlayer;
            bool bossFront = false;

            bool usedFocus = actor.focused;
            bool isHit = selfBuff || shogunBolt || RollHit(actor, target, impactSkill);
            bool isCrit = !selfBuff && !shogunBolt && isHit && RollCrit(actor);
            BattleElement hitElement = ultimateForThis != null ? ultimateForThis.element : BattleElement.Unset;
            int damage = (!selfBuff && isHit) ? CalculateDamage(actor, target, impactSkill, isCrit, hitElement) : 0;
            bool impacted = false;

            if (camShot && (ultimate || shogunBolt))
            {
                Time.timeScale = 0.42f;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
            if (camShot) StartCoroutine(BattleCam.Windup(me, foe, ultimate || shogunBolt, bossFront,
                CamPull(ultimateForThis), invertCero, CamFullBody(ultimateForThis)));

            if (actorFx != null)
            {
                var play = actorFx.PlaySkill(playType, playTarget, () =>
                {
                    if (impacted) return;
                    impacted = true;
                    if (camShot) BattleCam.Punch();
                    ApplyImpact(actor, target, impactSkill, isHit, isCrit, damage, targetFx, usedFocus);
                }, ultimateForThis, basicForThis);
                while (true)
                {
                    bool more;
                    try { more = play.MoveNext(); }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogWarning("Battle command failed: " + ex.Message);
                        break;
                    }
                    if (!more) break;
                    yield return play.Current;
                }
            }
            else
            {
                if (camShot) BattleCam.Punch();
                ApplyImpact(actor, target, impactSkill, isHit, isCrit, damage, targetFx, usedFocus);
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            if (!impacted)
            {
                if (camShot) BattleCam.Punch();
                ApplyImpact(actor, target, skill, isHit, isCrit, damage, targetFx, usedFocus);
            }

            if (camShot && (ultimate || shogunBolt))
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
            if (camShot) yield return BattleCam.Recover();

            if (isHit && !target.IsAlive && targetFx != null)
                yield return targetFx.PlayDown();
        }

        private void ApplyImpact(BattleCharacter actor, BattleCharacter target, Skill skill, bool isHit, bool isCrit, int damage, BattleFighter targetFx, bool usedFocus)
        {
            Vector3 hitPos = BattleVfx.AtBody(target != null ? target.visual : null);

            if (!isHit)
            {
                BattleAudio.Miss();
                Talk(actor, "「" + skill.skillName + "」……外した。");
                return;
            }

            bool hasUlt = castUltimate != null;
            if (hasUlt && castUltimate.selfBuff)
            {
                if (castUltimate.id == "sakanade")
                {
                    actor.ApplyUltimateBuff("sakanade");
                    if (actor.isPlayer) BattleCam.SetSenseInvert(true);
                    Talk(actor, "「逆撫」……感覚が逆転した。回避率アップ。すべての技が虚閃になった。");
                    if (actor.isPlayer)
                    {
                        ApplyEquippedUltimate();
                        UpdateButtonInteractable();
                    }
                    SyncAilments();
                    return;
                }
                actor.ApplyUltimateBuff(castUltimate.waveKind);
                Color buffCol = castUltimate.vfxColor;
                if (castUltimate.id == "shogun_henshin")
                {
                    var fx = FighterOf(actor);
                    if (fx != null) fx.TransformToShogun();
                }
                var selfFx = FighterOf(actor);
                if (selfFx != null) selfFx.Flash(buffCol);
                UpdateHpBars();
                if (castUltimate.id == "kaioken")
                    Talk(actor, "「界王拳」……赤い気を纏った。攻撃力が上がった。体に負担がかかる。");
                else if (castUltimate.id == "reiatsu")
                    Talk(actor, "「霊圧開放」……霊圧を解放した。攻撃力が上がった。次の攻撃に集中できる。");
                else if (castUltimate.id == "shogun_henshin")
                    Talk(actor, "「将軍変身」……将軍の姿になった。技は黒弾だけになった。");
                else if (castUltimate.id == "element_mu")
                    Talk(actor, "「虚白・霧域」……戦場が静かな無色の霧に包まれた。3ターン、すべての技が1.3倍になる。");
                else
                    Talk(actor, "「" + skill.skillName + "」！");
                SyncVoidWash();
                SyncAilments();
                if (castUltimate.id == "shogun_henshin" && actor.isPlayer)
                {
                    ApplyShogunKit();
                    UpdateButtonInteractable();
                }
                return;
            }
            var basic = actor.isPlayer ? EquippedBasic(skill.type) : null;
            Color burst = hasUlt
                ? castUltimate.vfxColor
                : basic != null
                    ? basic.vfxColor
                    : skill.type == SkillCommandType.Skill2
                        ? new Color(1f, 0.5f, 0.15f)
                        : skill.type == SkillCommandType.Skill3
                            ? new Color(0.35f, 0.8f, 1f)
                            : skill.type == SkillCommandType.BrainSmash
                                ? new Color(0.7f, 0.4f, 1f)
                                : new Color(1f, 0.9f, 0.45f);
            VfxHitKind kind = VfxHitKind.Slash;
            if (skill.isUltimate || skill.type == SkillCommandType.BrainSmash) kind = VfxHitKind.Ultimate;
            else if ((basic != null && basic.motion == BasicMotion.Kick) || skill.type == SkillCommandType.Skill2) kind = VfxHitKind.Kick;
            else if ((basic != null && basic.motion == BasicMotion.Bolt) || skill.type == SkillCommandType.Skill3) kind = VfxHitKind.Magic;
            else if (skill.type == SkillCommandType.Skill1) kind = VfxHitKind.Slash;
            else kind = VfxHitKind.Smash;
            BattleVfx.PlayHit(hitPos, burst, kind, isCrit);
            BattleFx.MarkImpactFxPlayed();
            if (targetFx != null) targetFx.Flash(isCrit ? new Color(1f, 0.82f, 0.25f) : new Color(1f, 0.95f, 0.9f));
            BattleFx.Popup(hitPos, isCrit ? damage + "!" : damage.ToString(), isCrit);
            if (basic != null && basic.motion == BasicMotion.Kick) BattleAudio.Kick();
            else if (basic != null && basic.motion == BasicMotion.Bolt) BattleAudio.SwordSlice();
            else BattleAudio.PlayHitBySkill(skill.type, skill.isUltimate);
            StartCoroutine(BattleFx.HitStop(skill.isUltimate ? 0.11f : 0.045f));

            target.TakeDamage(damage);
            UpdateHpBars();
            if (basic != null)
                ApplyBasicHit(actor, target, basic);
            else if (!hasUlt)
            {
                actor.OnDealHit(skill);
                target.OnReceiveHit(skill, actor.baseStats.attack, actor.baseStats.luck);
            }

            if (hasUlt && BattleElementUtil.IsElementSkill(castUltimate.element) && isHit && target != null)
            {
                ElementCombat.ApplyOnHit(actor, target, castUltimate.element, actor.baseStats.attack, damage, castUltimate.id);
                UpdateHpBars();
            }

            if (targetFx != null)
            {
                targetFx.Flash(new Color(1f, 0.25f, 0.2f));
                targetFx.Shake(skill.isUltimate ? 0.28f : 0.16f, skill.isUltimate ? 0.18f : 0.1f);
                bool ultimateHit = skill.isUltimate || skill.type == SkillCommandType.BrainSmash;
                if (target.IsAlive) StartCoroutine(targetFx.PlayHit(ultimateHit));
            }
            else
            {
                var placeholder = target == player ? playerVisual : enemyVisual;
                if (placeholder != null)
                {
                    placeholder.Flash(Color.red);
                    if (skill.isUltimate) placeholder.Shake();
                }
            }
            SyncAilments();

            string hitLine = "「" + skill.skillName + "」  " + damage;
            if (isCrit) hitLine += "  会心";
            else if (usedFocus) hitLine += "  集中";
            Talk(actor, hitLine);
        }

        static void ApplyBasicHit(BattleCharacter actor, BattleCharacter target, BasicSkillDefinition basic)
        {
            if (basic == null) return;
            if (basic.hitEffect == BasicHitEffect.GaugeOnHit)
                actor.currentGauge = Mathf.Min(actor.maxGauge, actor.currentGauge + 8);
            if (basic.hitEffect == BasicHitEffect.Stun)
            {
                float ccChance = BattleCharacter.CrowdControlChance(actor.baseStats.luck);
                if (Random.value < ccChance) target.stunTurns = Mathf.Max(target.stunTurns, 1);
            }
            if (basic.hitEffect == BasicHitEffect.Burn)
            {
                float dmgChance = BattleCharacter.DamageAilmentChance(actor.baseStats.luck);
                if (Random.value < dmgChance)
                {
                    target.burnTurns = Mathf.Max(target.burnTurns, 2);
                    target.burnDamage = Mathf.Max(6, actor.baseStats.attack / 5);
                }
            }
        }

        static BattleFighter FighterOf(BattleCharacter ch)
        {
            if (ch == null) return null;
            return ch.isPlayer ? BattleStage.PlayerFighter : BattleStage.EnemyFighter;
        }

        void SyncSenseInvert()
        {
            BattleCam.SetSenseInvert(player != null && player.senseInvertTurns > 0);
            ApplyEquippedUltimate();
            UpdateButtonInteractable();
        }

        private static bool RollHit(BattleCharacter actor, BattleCharacter target, Skill skill)
        {
            float evasion = target.baseStats.luck * 0.00025f;
            if (target.evadeTurns > 0) evasion += target.evadeBonus;
            evasion -= ElementCombat.LightMarkEvasionPenalty(target);
            evasion = Mathf.Clamp(evasion, 0f, 0.06f);
            float chance = Mathf.Clamp(Mathf.Max(skill.accuracy, 0.97f) - evasion, 0.92f, 0.995f);
            if (target.stunTurns > 0) chance = Mathf.Min(0.99f, chance + 0.12f);
            return Random.value <= chance;
        }

        IEnumerator MaybeFollowUp(BattleCharacter actor, BattleCharacter target)
        {
            if (actor == null || target == null) yield break;
            if (suppressFollowUp || actor.senseInvertTurns > 0 || actor.shogunFormTurns > 0)
            {
                suppressFollowUp = false;
                yield break;
            }
            int extra = FollowUpCount(actor);
            for (int i = 0; i < extra; i++)
            {
                if (!actor.IsAlive || !target.IsAlive) yield break;
                var skill = FollowUpSkill(actor);
                if (skill == null) yield break;
                Talk(actor, "追撃！");
                actor.followUpStrike = true;
                try
                {
                    yield return PerformCommand(actor, target, skill.type);
                }
                finally
                {
                    actor.followUpStrike = false;
                }
            }
        }

        static int SpeedSteps(BattleCharacter actor)
        {
            if (actor == null || actor.baseStats == null) return 0;
            return Mathf.Clamp(actor.baseStats.speed - StudyStore.BaseSpeed, 0, StudyStore.MaxSpdLuckSteps);
        }

        static int FollowUpCount(BattleCharacter actor)
        {
            int steps = SpeedSteps(actor);
            if (steps >= 160) return 3;
            if (steps >= 90) return 2;
            if (steps >= 30) return 1;
            return 0;
        }

        static Skill FollowUpSkill(BattleCharacter actor)
        {
            if (actor == null || actor.skills == null) return null;
            float u = SpeedSteps(actor) / (float)StudyStore.MaxSpdLuckSteps;
            float roll = Random.value;
            int want = 1;
            if (roll < u * 0.20f) want = 3;
            else if (roll < u * 0.58f) want = 2;
            Skill fallback = null;
            Skill match = null;
            for (int i = 0; i < actor.skills.Count; i++)
            {
                var s = actor.skills[i];
                if (s == null || s.type == SkillCommandType.Charge || s.type == SkillCommandType.BrainSmash) continue;
                if (s.requiredGear > 3) continue;
                if (fallback == null || s.requiredGear < fallback.requiredGear) fallback = s;
                if (s.requiredGear == want) match = s;
            }
            return match != null ? match : fallback;
        }

        private static bool RollCrit(BattleCharacter actor)
        {
            if (actor == null || actor.baseStats == null) return false;
            float u = Mathf.Clamp01((actor.baseStats.luck - StudyStore.BaseLuck) / (float)StudyStore.MaxSpdLuckSteps);
            float critChance = Mathf.Lerp(0.08f, 0.42f, u);
            return Random.value <= critChance;
        }

        static float CritMultiplier(BattleCharacter actor)
        {
            if (actor == null || actor.baseStats == null) return 1.5f;
            float u = Mathf.Clamp01((actor.baseStats.luck - StudyStore.BaseLuck) / (float)StudyStore.MaxSpdLuckSteps);
            return Mathf.Lerp(1.5f, 2.4f, u);
        }

        static float BalanceFactor(BattleCharacter who)
        {
            if (who == null || !who.isPlayer) return 0f;
            int low = Mathf.Min(
                StudyStore.HpSteps,
                Mathf.Min(StudyStore.AtkSteps, Mathf.Min(StudyStore.DefSteps, Mathf.Min(StudyStore.SpdSteps, StudyStore.LuckSteps))));
            return Mathf.Clamp(low * 0.004f, 0f, 0.12f);
        }

        private static int CalculateDamage(BattleCharacter actor, BattleCharacter target, Skill skill, bool isCrit, BattleElement element = BattleElement.Unset)
        {
            int defense = target.baseStats.defense;
            if (skill.type == SkillCommandType.BrainSmash)
                defense = Mathf.RoundToInt(defense * 0.75f);
            else if (skill.type == SkillCommandType.Skill3)
                defense = Mathf.RoundToInt(defense * 0.88f);

            // 単純な attack - defense だと防御力が高いときに1ダメ固定になるため、
            // 攻撃力の一定割合（28%）を最低貫通保証とし、防御減衰を滑らかにする
            float rawBase = actor.baseStats.attack - defense * 0.72f;
            float minBase = actor.baseStats.attack * 0.28f;
            float baseDamage = Mathf.Max(minBase, rawBase);

            float scaled = baseDamage * skill.powerMultiplier;
            scaled *= ElementCombat.ActorDamageMultiplier(actor);
            scaled *= ElementCombat.SoakDamageFactor(target);
            float variance = Random.Range(0.9f, 1.1f);
            float dmg = scaled * variance;
            if (actor.atkBoostTurns > 0 && actor.atkBoostMul > 1.01f)
                dmg *= actor.atkBoostMul;
            if (target.defBoostTurns > 0 && target.defBoostMul > 1.01f)
                dmg /= target.defBoostMul;
            if (actor.focused)
            {
                dmg *= 1.15f;
                actor.focused = false;
            }
            if (target.guarding) dmg *= 0.55f;
            if (isCrit) dmg *= CritMultiplier(actor);
            if (actor != null && actor.isPlayer) dmg *= 1f + BalanceFactor(actor);
            if (target != null && target.isPlayer) dmg *= 1f - BalanceFactor(target);
            int amount = Mathf.Max(1, Mathf.RoundToInt(dmg));
            return amount;
        }

        private void UpdateHpBars()
        {
            if (player != null)
                BattleHudSkin.SetHp(true, player.HpRatio, player.currentHp, player.baseStats.maxHp);
            if (enemy != null)
                BattleHudSkin.SetHp(false, enemy.HpRatio, enemy.currentHp, enemy.baseStats.maxHp);
        }

        private void UpdateButtonInteractable()
        {
            bool isPlayerTurn = waitingForPlayerInput && !battleOver;
            foreach (var kv in buttons)
            {
                var skill = player.GetSkill(kv.Key);
                kv.Value.interactable = isPlayerTurn && player.CanUse(skill);
                if (player == null) continue;
                var named = player.GetSkill(kv.Key);
                string skillName = named != null ? named.skillName : "技";
                if (player.senseInvertTurns > 0) skillName = "虚閃";
                else if (player.shogunFormTurns > 0) skillName = "黒弾";
                else if (kv.Key == SkillCommandType.BrainSmash && currentUltimate != null)
                    skillName = StudyStore.SkillTitle(currentUltimate.displayName, StudyStore.UltimateLevel(currentUltimate.id));
                string gearText;
                bool gearReady;
                if (player.senseInvertTurns > 0 || player.shogunFormTurns > 0)
                {
                    gearText = "";
                    gearReady = true;
                }
                else if (kv.Key == SkillCommandType.BrainSmash)
                {
                    int left = StudyStore.BrainSmashLeftToday();
                    gearText = "残り" + left;
                    gearReady = left > 0;
                }
                else
                {
                    int need = named != null ? Mathf.Max(1, named.requiredGear) : 1;
                    gearText = UltimateCatalog.GearNeedLabel(need);
                    gearReady = player.currentGear >= need;
                }
                BattleHudSkin.SetCommandLabels(kv.Value.gameObject, skillName, gearText, gearReady);
            }
            BattleHudSkin.SetGear(player != null ? player.currentGear : 1, BattleCharacter.MaxGear, isPlayerTurn && player != null && player.CanRaiseGear());
            if (raidMode) BattleHudSkin.SetRaidTurns(RaidStore.TurnsLeft());
        }

        void ApplyBattleLabels()
        {
            SetLabel("PlayerNameLabel", player != null ? player.characterName : BattleStage.PlayerLabel, 12);
            SetLabel("EnemyNameLabel", enemy != null ? enemy.characterName : BattleStage.EnemyLabel, 7);
        }

        static void SetLabel(string name, string value, int maxChars)
        {
            var go = GameObject.Find(name);
            if (go == null) return;
            var text = go.GetComponent<Text>();
            if (text != null) text.text = Fit(value, maxChars);
        }

        static string Fit(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max) return value;
            return value.Substring(0, max - 1) + "…";
        }

        void EndRaidBattle(bool playerWon)
        {
            battleOver = true;
            waitingForPlayerInput = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            RaidStore.ClearPause();
            UpdateButtonInteractable();
            string message;
            if (playerWon)
            {
                message = "討伐！  報酬はプレゼントへ\n与ダメージ " + RaidSync.SessionDamage.ToString("N0");
                BattleAudio.LevelComplete();
                BattleAudio.Coin();
            }
            else
            {
                message = "倒れた…  レイドはまだ続いている";
                BattleAudio.Defeated();
            }
            Talk(playerWon ? player : enemy, playerWon ? "やった！" : "……まだ、終わらない。");
            if (resultText != null) resultText.gameObject.SetActive(false);
            BattleHudSkin.ShowResult(playerWon, playerWon ? "★ 討伐 ★" : "撤退…", message);
        }

        private void EndBattle(bool playerWon)
        {
            if (raidMode)
            {
                EndRaidBattle(playerWon);
                return;
            }
            battleOver = true;
            waitingForPlayerInput = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            StudyStore.ClearPausedBattle();
            UpdateButtonInteractable();
            string message = playerWon ? "勝利!" : "敗北...";
            if (playerWon)
            {
                StudyStore.AddDiamonds(StudyStore.WinDiamonds);
                int clearedFloor = currentFloor;
                int clearedWeek = StudyStore.DungeonWeek;
                StudyStore.AdvanceFloorOnWin();
                if (SupabaseSync.Ready) StartCoroutine(SupabaseSync.PushSelf());
                if (clearedFloor >= StudyStore.DungeonMaxFloor && StudyStore.DungeonWeek > clearedWeek)
                    message = clearedWeek + "周目クリア!  " + StudyStore.DungeonWeek + "周目へ  ダイヤ +" + StudyStore.WinDiamonds;
                else
                    message = clearedWeek + "周目 " + clearedFloor + "F クリア!  ダイヤ +" + StudyStore.WinDiamonds;
                BattleAudio.LevelComplete();
                if (floorIsBoss) BattleAudio.Coin();
            }
            else
            {
                message = "敗北...  勉強してステを伸ばそう";
                BattleAudio.Defeated();
            }
            Talk(playerWon ? player : enemy, playerWon ? "やった！" : "……負けた。");
            if (resultText != null) resultText.gameObject.SetActive(false);
            string title = playerWon ? "★ 勝利 ★" : "敗北…";
            BattleHudSkin.ShowResult(playerWon, title, message);
        }

        void Talk(BattleCharacter who, string line)
        {
            BattleHudSkin.Talk(who != null ? who.characterName : "", line);
        }

        void SyncVoidWash()
        {
            BattleCam.SetVoidWash(0f);
            Vector3 mid = Vector3.zero;
            if (player != null && enemy != null && player.visual != null && enemy.visual != null)
                mid = Vector3.Lerp(player.visual.position, enemy.visual.position, 0.5f);
            else if (player != null && player.visual != null)
                mid = player.visual.position;
            bool field = player != null && player.voidRealmTurns > 0;
            BattleFx.SetVoidBattleField(field, mid);
        }

        void SyncAilments()
        {
            if (BattleStage.PlayerFighter != null && player != null)
                BattleStage.PlayerFighter.SetAilments(player.burnTurns > 0, player.stunTurns > 0 || player.skippedFromStun);
            if (BattleStage.EnemyFighter != null && enemy != null)
                BattleStage.EnemyFighter.SetAilments(enemy.burnTurns > 0, enemy.stunTurns > 0 || enemy.skippedFromStun);
            SyncVoidWash();
        }
    }
}
