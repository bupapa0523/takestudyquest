using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShiftingMetropolis.Progress
{
    [Serializable]
    public class StudyMaterial
    {
        public string id;
        public string name;
        public int subject;
    }

    [Serializable]
    public class StudyLogEntry
    {
        public string id;
        public string materialId;
        public string materialName;
        public int subject;
        public int minutes;
        public int durationSeconds;
        public string startedAt;
        public bool open;
        public string authorCode;
    }

    [Serializable]
    public class ActiveSessionState
    {
        public bool running;
        public bool paused;
        public string materialId;
        public string logId;
        public float sessionSeconds;
        public float bankedSeconds;
        public long lastUtcTicks;
    }

    [Serializable]
    public class UserProfile
    {
        public string displayName = "";
        public string bio = "";
        public string school = "";
        public string goal = "";
        public int colorIndex;
        public int dailyGoalMinutes = 180;
        public int weeklyGoalMinutes;
        public string goalWeekStart = "";
        public string friendCode = "";
    }

    [Serializable]
    public class FriendEntry
    {
        public string code;
        public string name;
        public int colorIndex;
        public int week;
        public int floor;
        public int level;
        public string bio = "";
        public string school = "";
        public string goal = "";
        public int dailyGoalMinutes;
        public int weeklyGoalMinutes;
        public int weekMinutes;
        public int lifetimeMinutes;
        public string lookCsv = "";
        public string skillsCsv = "";
        public string cosmeticsCsv = "";
    }

    [Serializable]
    class StudySaveBlob
    {
        public int totalBp;
        public int totalEarnedBp;
        public int currentBp;
        public int todayBp;
        public int diamonds;
        public int brainSmashUsesToday;
        public string businessDate;
        public UserProfile profile;
        public int seedCleaned;
        public int growthBp;
        public int growthInited;
        public int hpSteps;
        public int atkSteps;
        public int defSteps;
        public int spdSteps;
        public int luckSteps;
        public int growthRespecUsed;
        public int[] lookSlots;
        public StudyMaterial[] materials;
        public StudyLogEntry[] logs;
        public ActiveSessionState session;
        public TimelineEvent[] events;
        public FriendEntry[] friends;
        public int dungeonFloor;
        public int dungeonWeek;
        public int dungeonDeepestClear;
        public string[] ownedUltimateIds;
        public int[] ownedUltimateLevels;
        public string[] loadoutIds;
        public string[] skillLoadoutIds;
        public string[] ownedArmorIds;
        public int[] ownedArmorLevels;
        public string[] ownedBodyIds;
        public int[] ownedBodyLevels;
        public int gachaMigrated;
        public string[] ownedCosmeticIds;
        public string equipFrame;
        public string equipTitle;
        public string equipBorder;
        public string equipBackdrop;
        public string equipCorner;
        public string equipSeal;
        public int sealEquipped;
        public int[] subjectDyes;
        public int[] subjectOrder;
        public PausedBattleState pausedBattle;
    }

    [Serializable]
    public class PausedFighterState
    {
        public string name;
        public int hp;
        public int gauge;
        public int gear;
        public int smash;
        public int maxHp;
        public int atk;
        public int def;
        public int spd;
        public int luck;
        public int burn;
        public int burnDmg;
        public int soak;
        public int soakDmg;
        public int stun;
        public int atkBoost;
        public int defBoost;
        public int evade;
        public int sense;
        public int shogun;
        public int voidRealm;
        public int lightMark;
        public float atkMul = 1f;
        public float defMul = 1f;
        public float evadeBonus;
    }

    [Serializable]
    public class PausedBattleState
    {
        public int active;
        public int floor;
        public int turnIndex;
        public int pausedAtCommand;
        public int enemyJob;
        public PausedFighterState player;
        public PausedFighterState enemy;
        public int[] lookSlots;
        public string[] skillLoadout;
        public string[] loadoutSlots;
        public int growthSnap;
        public int hpSteps;
        public int atkSteps;
        public int defSteps;
        public int spdSteps;
        public int luckSteps;
    }

    [Serializable]
    public class TimelineEvent
    {
        public string id;
        public string kind;
        public string createdAt;
        public int goalMinutes;
        public int doneMinutes;
        public string weekStart;
        public string authorCode;
    }

    public static class StudyStore
    {
        const string PrefsKey = "sm_study_store_v2";
        const string LegacyBp = "sm_total_bp";
        const string FileName = "study_save.json";

        public const int BattleCostBp = 60;
        public const int BrainSmashMinutesPerUse = 30;
        public const int WinDiamonds = 5;
        public const int MaxWeeklyGoalMinutes = 167 * 60 + 59;
        public const int MaxLevel = 1000;
        public const int HoursPerLoop = 1000;
        public const int MaxDungeonWeek = 999;
        // 2 が二乗、1 が一次。1.8 で少しだけ直線に寄せ、10000時間でちょうど Lv.1000。
        public const int HoursForMaxLevel = 10000;
        public const int BpForMaxLevel = HoursForMaxLevel * 60;
        const double LevelCurvePower = 1.8;
        public const int GrowthStatCost = 30;
        public const int RespecDiamondCost = 5;
        public const int BaseHp = 150;
        public const int BaseAttack = 25;
        public const int BaseDefense = 12;
        public const int BaseSpeed = 14;
        public const int MaxSpdLuckSteps = 200;
        public const int BaseLuck = 10;
        public static readonly string[] LookSlotIds =
        {
            "skin", "look1", "look2", "look3", "look4", "look5", "look6", "look7",
            "look8", "look9", "look10", "look11", "look12", "look13", "look14", "look15"
        };

        static readonly List<StudyMaterial> Materials = new List<StudyMaterial>();
        static readonly List<StudyLogEntry> Logs = new List<StudyLogEntry>();
        static readonly List<TimelineEvent> Events = new List<TimelineEvent>();
        static readonly List<FriendEntry> Friends = new List<FriendEntry>();
        static bool loaded;

        public static int TotalEarnedBp { get; private set; }
        public static int CurrentBp { get; private set; }
        public static int TodayBp { get; private set; }
        public static int Diamonds { get; private set; }
        public static int BrainSmashUsesToday { get; private set; }
        public static string BusinessDateKey { get; private set; }
        public static int GrowthBp { get; private set; }
        public static int HpSteps { get; private set; }
        public static int AtkSteps { get; private set; }
        public static int DefSteps { get; private set; }
        public static int SpdSteps { get; private set; }
        public static int LuckSteps { get; private set; }
        public static bool GrowthRespecUsed { get; private set; }
        static int[] lookSlots = new int[LookSlotIds.Length];

        public const int DungeonMaxFloor = 100;
        public const int DungeonBossInterval = 10;
        public static int DungeonFloor { get; private set; } = 1;
        public static int DungeonWeek { get; private set; } = 1;
        public static int DungeonDeepestClear { get; private set; } = 0;
        static PausedBattleState pausedBattle;
        static readonly List<string> ownedUltimateIds = new List<string>();
        static readonly List<int> ownedUltimateLevels = new List<int>();
        static readonly List<string> ownedArmorIds = new List<string>();
        static readonly List<int> ownedArmorLevels = new List<int>();
        static readonly List<string> ownedBodyIds = new List<string>();
        static readonly List<int> ownedBodyLevels = new List<int>();
        static int gachaMigrated;
        static readonly List<string> ownedCosmeticIds = new List<string>();
        static string equipFrame = "";
        static string equipTitle = "";
        static string equipBorder = "";
        static string equipBackdrop = "";
        static string equipCorner = "";
        static string equipSeal = "";
        static int sealEquipped;
        static readonly int[] subjectDyes = new int[6];
        static int[] subjectOrder = { 0, 1, 2, 3, 4, 5 };

        public static readonly string[] FreeSkillIds =
        {
            "gunplay_shot",
            "drop_kick",
            "uppercut_jab",
            "kick_2",
            "fist_fight",
            "element_fire",
            "element_fire_core"
        };
        public static readonly string[] LoadoutSlots = new string[4];
        public static readonly string[] SkillLoadout = new string[4];

        public static DateTime Now
        {
            get { return DateTime.Now; }
        }

        public static int TotalBp => CurrentBp;

        public static UserProfile Profile { get; private set; } = new UserProfile();

        public static ActiveSessionState Session { get; private set; } = new ActiveSessionState();

        static string SaveRoot
        {
            get
            {
                string current = Application.persistentDataPath;
                try
                {
                    string parent = Directory.GetParent(current)?.FullName;
                    if (!string.IsNullOrEmpty(parent))
                    {
                        string legacy = Path.Combine(parent, "peach natsu");
                        if (File.Exists(Path.Combine(legacy, FileName))) return legacy;
                    }
                }
                catch (Exception)
                {
                }
                return current;
            }
        }

        static string FilePath => Path.Combine(SaveRoot, FileName);

        public static IReadOnlyList<StudyMaterial> AllMaterials
        {
            get { Load(); return Materials; }
        }

        public static IReadOnlyList<StudyLogEntry> AllLogs
        {
            get { Load(); return Logs; }
        }

        public static void Load(bool force = false)
        {
            if (loaded && !force) return;
            loaded = true;
            Materials.Clear();
            Logs.Clear();
            Events.Clear();
            Friends.Clear();
            Session = new ActiveSessionState();
            Profile = new UserProfile();
            CurrentBp = 0;
            TotalEarnedBp = 0;
            TodayBp = 0;
            Diamonds = 0;
            BrainSmashUsesToday = 0;
            BusinessDateKey = string.Empty;
            GrowthBp = 0;
            HpSteps = 0;
            AtkSteps = 0;
            DefSteps = 0;
            SpdSteps = 0;
            LuckSteps = 0;
            GrowthRespecUsed = false;
            lookSlots = new int[LookSlotIds.Length];
            DungeonFloor = 1;
            DungeonWeek = 1;
            DungeonDeepestClear = 0;
            pausedBattle = null;
            ownedUltimateIds.Clear();
            ownedUltimateLevels.Clear();
            ownedArmorIds.Clear();
            ownedArmorLevels.Clear();
            ownedBodyIds.Clear();
            ownedBodyLevels.Clear();
            ownedCosmeticIds.Clear();
            equipFrame = "";
            equipTitle = "";
            equipBorder = "";
            equipBackdrop = "";
            equipCorner = "";
            equipSeal = "";
            sealEquipped = 0;
            for (int i = 0; i < subjectDyes.Length; i++) subjectDyes[i] = 0;
            subjectOrder = new[] { 0, 1, 2, 3, 4, 5 };
            gachaMigrated = 0;
            for (int i = 0; i < LoadoutSlots.Length; i++) LoadoutSlots[i] = null;
            for (int i = 0; i < SkillLoadout.Length; i++) SkillLoadout[i] = null;

            string json = ReadJson();
            if (!string.IsNullOrEmpty(json))
            {
                var blob = JsonUtility.FromJson<StudySaveBlob>(json);
                if (blob != null)
                {
                    CurrentBp = blob.currentBp;
                    TotalEarnedBp = blob.totalEarnedBp;
                    TodayBp = blob.todayBp;
                    Diamonds = blob.diamonds;
                    BrainSmashUsesToday = blob.brainSmashUsesToday;
                    BusinessDateKey = blob.businessDate;
                    if (TotalEarnedBp <= 0 && blob.totalBp > 0)
                    {
                        TotalEarnedBp = blob.totalBp;
                        if (CurrentBp <= 0) CurrentBp = blob.totalBp;
                    }
                    if (blob.materials != null) Materials.AddRange(blob.materials);
                    if (blob.logs != null) Logs.AddRange(blob.logs);
                    if (blob.events != null) Events.AddRange(blob.events);
                    if (blob.friends != null)
                    {
                        for (int i = 0; i < blob.friends.Length; i++)
                        {
                            if (blob.friends[i] == null || string.IsNullOrEmpty(blob.friends[i].code)) continue;
                            Friends.Add(blob.friends[i]);
                        }
                    }
                    if (blob.session != null) Session = blob.session;
                    if (blob.profile != null) Profile = blob.profile;
                    GrowthBp = blob.growthBp;
                    HpSteps = Mathf.Max(0, blob.hpSteps);
                    AtkSteps = Mathf.Max(0, blob.atkSteps);
                    DefSteps = Mathf.Max(0, blob.defSteps);
                    SpdSteps = Mathf.Max(0, blob.spdSteps);
                    LuckSteps = Mathf.Max(0, blob.luckSteps);
                    GrowthRespecUsed = blob.growthRespecUsed > 0;
                    if (blob.lookSlots != null)
                    {
                        int n = Mathf.Min(LookSlotIds.Length, blob.lookSlots.Length);
                        for (int i = 0; i < n; i++)
                            lookSlots[i] = Mathf.Max(0, blob.lookSlots[i]);
                    }
                    if (blob.growthInited < 1)
                    {
                        GrowthBp = TotalEarnedBp;
                    }
                    if (blob.seedCleaned < 1)
                    {
                        StripSeedMaterials();
                    }
                    DungeonFloor = Mathf.Clamp(blob.dungeonFloor <= 0 ? 1 : blob.dungeonFloor, 1, DungeonMaxFloor);
                    DungeonWeek = Mathf.Clamp(blob.dungeonWeek <= 0 ? 1 : blob.dungeonWeek, 1, MaxDungeonWeek);
                    DungeonDeepestClear = Mathf.Max(0, blob.dungeonDeepestClear);
                    if (blob.ownedUltimateIds != null)
                    {
                        for (int i = 0; i < blob.ownedUltimateIds.Length; i++)
                        {
                            ownedUltimateIds.Add(blob.ownedUltimateIds[i]);
                            int lvl = blob.ownedUltimateLevels != null && i < blob.ownedUltimateLevels.Length ? blob.ownedUltimateLevels[i] : 1;
                            ownedUltimateLevels.Add(Mathf.Max(1, lvl));
                        }
                    }
                    if (blob.loadoutIds != null)
                    {
                        int n = Mathf.Min(LoadoutSlots.Length, blob.loadoutIds.Length);
                        for (int i = 0; i < n; i++) LoadoutSlots[i] = blob.loadoutIds[i];
                    }
                    if (blob.skillLoadoutIds != null)
                    {
                        int n = Mathf.Min(SkillLoadout.Length, blob.skillLoadoutIds.Length);
                        for (int i = 0; i < n; i++) SkillLoadout[i] = blob.skillLoadoutIds[i];
                    }
                    if (blob.ownedArmorIds != null)
                    {
                        for (int i = 0; i < blob.ownedArmorIds.Length; i++)
                        {
                            if (string.IsNullOrEmpty(blob.ownedArmorIds[i])) continue;
                            ownedArmorIds.Add(blob.ownedArmorIds[i]);
                            int lvl = blob.ownedArmorLevels != null && i < blob.ownedArmorLevels.Length ? blob.ownedArmorLevels[i] : 1;
                            ownedArmorLevels.Add(Mathf.Clamp(lvl, 1, SkillVersionMax));
                        }
                    }
                    if (blob.ownedBodyIds != null)
                    {
                        for (int i = 0; i < blob.ownedBodyIds.Length; i++)
                        {
                            if (string.IsNullOrEmpty(blob.ownedBodyIds[i])) continue;
                            if (ownedBodyIds.Contains(blob.ownedBodyIds[i])) continue;
                            ownedBodyIds.Add(blob.ownedBodyIds[i]);
                            int lvl = blob.ownedBodyLevels != null && i < blob.ownedBodyLevels.Length
                                ? blob.ownedBodyLevels[i] : 1;
                            ownedBodyLevels.Add(Mathf.Clamp(lvl, 1, SkillVersionMax));
                        }
                    }
                    gachaMigrated = blob.gachaMigrated;
                    if (blob.ownedCosmeticIds != null)
                    {
                        for (int i = 0; i < blob.ownedCosmeticIds.Length; i++)
                        {
                            if (string.IsNullOrEmpty(blob.ownedCosmeticIds[i])) continue;
                            if (!ownedCosmeticIds.Contains(blob.ownedCosmeticIds[i]))
                                ownedCosmeticIds.Add(blob.ownedCosmeticIds[i]);
                        }
                    }
                    equipFrame = blob.equipFrame ?? "";
                    equipTitle = blob.equipTitle ?? "";
                    equipBorder = blob.equipBorder ?? "";
                    equipBackdrop = blob.equipBackdrop ?? "";
                    equipCorner = blob.equipCorner ?? "";
                    equipSeal = blob.equipSeal ?? "";
                    sealEquipped = blob.sealEquipped == 1 ? 1 : 0;
                    if (string.IsNullOrEmpty(equipSeal) && sealEquipped == 1) equipSeal = "seal_goal";
                    if (blob.subjectDyes != null)
                    {
                        int n = Mathf.Min(subjectDyes.Length, blob.subjectDyes.Length);
                        for (int i = 0; i < n; i++) subjectDyes[i] = blob.subjectDyes[i] == 1 ? 1 : 0;
                    }
                    if (blob.subjectOrder != null && blob.subjectOrder.Length == 6)
                        subjectOrder = blob.subjectOrder;
                    EnsureSubjectOrder();
                    if (blob.pausedBattle != null && blob.pausedBattle.active == 1)
                        pausedBattle = blob.pausedBattle;
                }
            }
            else
            {
                int legacy = PlayerPrefs.GetInt(LegacyBp, 0);
                CurrentBp = legacy;
                TotalEarnedBp = legacy;
                StripSeedMaterials();
            }
            EnsureStarterUltimate();
            EnsureStarterSkills();
            EnsureFriendCode();

            RolloverIfNeeded();
            RolloverWeeklyGoal();
            Save();
        }

        static string ReadJson()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return File.ReadAllText(FilePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("StudyStore file read failed: " + e.Message);
            }

            string prefs = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(prefs)) return prefs;
            return PlayerPrefs.GetString("sm_study_store_v1", string.Empty);
        }

        static void StripSeedMaterials()
        {
            Materials.RemoveAll(m => m != null && (m.name == "線形代数" || m.name == "TOEIC単語"));
        }

        public static void Save()
        {
            var blob = new StudySaveBlob
            {
                totalBp = CurrentBp,
                totalEarnedBp = TotalEarnedBp,
                currentBp = CurrentBp,
                todayBp = TodayBp,
                diamonds = Diamonds,
                brainSmashUsesToday = BrainSmashUsesToday,
                businessDate = BusinessDateKey,
                seedCleaned = 1,
                growthBp = GrowthBp,
                growthInited = 1,
                hpSteps = HpSteps,
                atkSteps = AtkSteps,
                defSteps = DefSteps,
                spdSteps = SpdSteps,
                luckSteps = LuckSteps,
                growthRespecUsed = GrowthRespecUsed ? 1 : 0,
                lookSlots = lookSlots,
                materials = Materials.ToArray(),
                logs = Logs.ToArray(),
                events = Events.ToArray(),
                friends = Friends.ToArray(),
                session = Session ?? new ActiveSessionState(),
                profile = Profile ?? new UserProfile(),
                dungeonFloor = DungeonFloor,
                dungeonWeek = DungeonWeek,
                dungeonDeepestClear = DungeonDeepestClear,
                ownedUltimateIds = ownedUltimateIds.ToArray(),
                ownedUltimateLevels = ownedUltimateLevels.ToArray(),
                loadoutIds = LoadoutSlots,
                skillLoadoutIds = SkillLoadout,
                ownedArmorIds = ownedArmorIds.ToArray(),
                ownedArmorLevels = ownedArmorLevels.ToArray(),
                ownedBodyIds = ownedBodyIds.ToArray(),
                ownedBodyLevels = ownedBodyLevels.ToArray(),
                gachaMigrated = gachaMigrated,
                ownedCosmeticIds = ownedCosmeticIds.ToArray(),
                equipFrame = equipFrame,
                equipTitle = equipTitle,
                equipBorder = equipBorder,
                equipBackdrop = equipBackdrop,
                equipCorner = equipCorner,
                equipSeal = equipSeal,
                sealEquipped = string.IsNullOrEmpty(equipSeal) ? 0 : 1,
                subjectDyes = subjectDyes,
                subjectOrder = subjectOrder,
                pausedBattle = pausedBattle
            };
            string json = JsonUtility.ToJson(blob);
            try
            {
                Directory.CreateDirectory(SaveRoot);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("StudyStore file write failed: " + e.Message);
            }

            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.SetInt(LegacyBp, CurrentBp);
            PlayerPrefs.Save();
        }

        public static void EraseAllProgress()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                if (File.Exists(ProfileIconPath)) File.Delete(ProfileIconPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("StudyStore erase failed: " + e.Message);
            }
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.DeleteKey("sm_study_store_v1");
            PlayerPrefs.DeleteKey(LegacyBp);
            PlayerPrefs.Save();
            loaded = false;
            Load(true);
        }

        public static void SaveSession(bool running, bool paused, StudyMaterial material, StudyLogEntry log, float sessionSeconds, float bankedSeconds)
        {
            Load();
            Session.running = running;
            Session.paused = paused;
            Session.materialId = material != null ? material.id : string.Empty;
            Session.logId = log != null ? log.id : string.Empty;
            Session.sessionSeconds = sessionSeconds;
            Session.bankedSeconds = bankedSeconds;
            Session.lastUtcTicks = DateTime.UtcNow.Ticks;
            Save();
        }

        public static DateTime BusinessDate(DateTime now)
        {
            return now.Hour < 3 ? now.Date.AddDays(-1) : now.Date;
        }

        static void RolloverIfNeeded()
        {
            string todayKey = BusinessDate(Now).ToString("yyyy-MM-dd");
            if (BusinessDateKey == todayKey) return;
            BusinessDateKey = todayKey;
            TodayBp = 0;
            BrainSmashUsesToday = 0;
            Save();
        }

        public static void GrantBp(int amount)
        {
            if (amount <= 0) return;
            Load();
            RolloverIfNeeded();
            CurrentBp += amount;
            TotalEarnedBp += amount;
            TodayBp += amount;
            GrowthBp += amount;
            Save();
        }

        public static void ConfiscateBp(int amount, bool hitsToday)
        {
            if (amount <= 0) return;
            Load();
            CurrentBp = Mathf.Max(0, CurrentBp - amount);
            TotalEarnedBp = Mathf.Max(0, TotalEarnedBp - amount);
            GrowthBp -= amount;
            if (hitsToday) TodayBp -= amount;
            Save();
        }

        public static bool DeleteLog(string id)
        {
            Load();
            var log = FindLog(id);
            if (log == null || log.open) return false;
            bool today = DateKey(log.startedAt) == BusinessDate(Now).ToString("yyyy-MM-dd");
            int minutes = Mathf.Max(0, log.minutes);
            Logs.Remove(log);
            if (minutes > 0) ConfiscateBp(minutes, today);
            else Save();
            return true;
        }

        public static bool ReviseLog(string id, StudyMaterial material, int hours, int minutes, DateTime startedAt)
        {
            Load();
            var log = FindLog(id);
            if (log == null || log.open || material == null) return false;
            int totalMinutes = Mathf.Max(0, hours) * 60 + Mathf.Max(0, minutes);
            if (totalMinutes <= 0) return false;
            int delta = totalMinutes - log.minutes;
            bool today = DateKey(log.startedAt) == BusinessDate(Now).ToString("yyyy-MM-dd");
            log.materialId = material.id;
            log.materialName = material.name;
            log.subject = material.subject;
            log.minutes = totalMinutes;
            log.durationSeconds = totalMinutes * 60;
            log.startedAt = startedAt.ToString("o");
            if (delta > 0) GrantBp(delta);
            else if (delta < 0) ConfiscateBp(-delta, today);
            else Save();
            return true;
        }

        public static int PlayerLevel()
        {
            Load();
            return LevelFromBp(TotalEarnedBp);
        }

        public static int LevelFromBp(int earned)
        {
            if (earned <= 0) return 1;
            int lo = 1;
            int hi = MaxLevel;
            int ans = 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (BpRequiredForLevel(mid) <= earned)
                {
                    ans = mid;
                    lo = mid + 1;
                }
                else hi = mid - 1;
            }
            return ans;
        }

        public static int BpRequiredForLevel(int level)
        {
            if (level <= 1) return 0;
            if (level > MaxLevel) level = MaxLevel;
            if (level == MaxLevel) return BpForMaxLevel;
            double t = (double)(level - 1) / (MaxLevel - 1);
            return Math.Max(1, (int)Math.Round(Math.Pow(t, LevelCurvePower) * BpForMaxLevel));
        }

        public static int BpForNextLevel()
        {
            Load();
            int lv = PlayerLevel();
            if (lv >= MaxLevel) return BpRequiredForLevel(MaxLevel);
            return BpRequiredForLevel(lv + 1);
        }

        public static float LevelProgress()
        {
            Load();
            int lv = PlayerLevel();
            if (lv >= MaxLevel) return 1f;
            int cur = BpRequiredForLevel(lv);
            int next = BpRequiredForLevel(lv + 1);
            if (next <= cur) return 1f;
            return Mathf.Clamp01((TotalEarnedBp - cur) / (float)(next - cur));
        }

        public const float GodStatScale = 1.1f;

        public static bool EquippedSkinIsGod()
        {
            Load();
            string path = ShiftingMetropolis.App.HeroAppearance.BodyPathAtLook(LookSlot(0));
            return BodyLevel(path) >= SkillVersionMax;
        }

        static int WithGod(int value)
        {
            if (!EquippedSkinIsGod()) return value;
            return Mathf.Max(1, Mathf.RoundToInt(value * GodStatScale));
        }

        public static int BattleMaxHp()
        {
            Load();
            return WithGod(BaseHp + HpSteps * 5);
        }

        public static int BattleAttack()
        {
            Load();
            return WithGod(BaseAttack + AtkSteps);
        }

        public static int BattleDefense()
        {
            Load();
            return WithGod(BaseDefense + DefSteps + EquippedArmorDefense());
        }

        public static int ArmorPieceDefense(int type, int version)
        {
            if (type <= 0 || version <= 0) return 0;
            return type + Mathf.Clamp(version, 1, SkillVersionMax) - 1;
        }

        public static int EquippedArmorDefense()
        {
            Load();
            int sum = 0;
            for (int extra = 6; extra <= 11; extra++)
            {
                int type = LookSlot(extra + 1);
                if (type <= 0) continue;
                int ver = ArmorLevel("armor:" + extra + ":" + type);
                sum += ArmorPieceDefense(type, ver);
            }
            return sum;
        }

        public static int BattleSpeed()
        {
            Load();
            return WithGod(BaseSpeed + Mathf.Min(SpdSteps, MaxSpdLuckSteps));
        }

        public static int BattleLuck()
        {
            Load();
            return WithGod(BaseLuck + Mathf.Min(LuckSteps, MaxSpdLuckSteps));
        }

        public static string ClimbLine(int week, int floor)
        {
            if (week <= 0) week = 1;
            if (floor <= 0) floor = 1;
            return week + "周目の" + floor + "階";
        }

        public static string MyClimbLine()
        {
            Load();
            return ClimbLine(DungeonWeek, DungeonFloor);
        }

        public static bool OwnsCosmetic(string id)
        {
            Load();
            return !string.IsNullOrEmpty(id) && ownedCosmeticIds.Contains(id);
        }

        public static string EquippedCosmetic(string slot)
        {
            Load();
            if (slot == "frame") return equipFrame ?? "";
            if (slot == "title") return equipTitle ?? "";
            if (slot == "border") return equipBorder ?? "";
            if (slot == "backdrop") return equipBackdrop ?? "";
            if (slot == "corner") return equipCorner ?? "";
            if (slot == "seal") return equipSeal ?? "";
            return "";
        }

        public static string TitleLabel(string id)
        {
            if (id == "title_study") return "勉強中";
            if (id == "title_night") return "夜学";
            if (id == "title_book") return "書生";
            if (id == "title_tower") return "塔の者";
            if (id == "title_ace") return "一番";
            if (id == "title_god") return "神";
            if (id == "title_moon") return "月下";
            if (id == "title_lord") return "塔の主";
            if (id == "title_void") return "虚の者";
            if (id == "title_hundred") return "百階";
            if (id == "title_summer") return "夏の覇者";
            return "";
        }

        public static string EquippedTitleLabel()
        {
            return TitleLabel(EquippedCosmetic("title"));
        }

        public static bool SubjectDyeOn(int index)
        {
            Load();
            return index >= 0 && index < subjectDyes.Length && subjectDyes[index] == 1;
        }

        public static Color SubjectDyeColor(int index)
        {
            if (index == 0) return new Color(0.92f, 0.28f, 0.38f);
            if (index == 1) return new Color(0.95f, 0.72f, 0.22f);
            if (index == 2) return new Color(0.25f, 0.55f, 0.95f);
            if (index == 3) return new Color(0.95f, 0.55f, 0.62f);
            if (index == 4) return new Color(0.20f, 0.72f, 0.48f);
            return new Color(0.62f, 0.42f, 0.86f);
        }

        public static Color BackdropColor()
        {
            string id = EquippedCosmetic("backdrop");
            if (id == "back_paper") return new Color(0.93f, 0.86f, 0.72f, 1f);
            if (id == "back_night") return new Color(0.05f, 0.04f, 0.14f, 1f);
            if (id == "back_peach") return new Color(0.55f, 0.22f, 0.32f, 1f);
            return new Color(0f, 0f, 0f, 0f);
        }

        public static void ClearCosmetic(string slot)
        {
            Load();
            ForceEquip(slot, "");
            Save();
        }

        public static string TryBuyCosmetic(string id, string slot, int cost)
        {
            Load();
            if (string.IsNullOrEmpty(id)) return "ありません";
            if (OwnsCosmetic(id))
            {
                ToggleEquip(slot, id);
                return null;
            }
            if (!TrySpendDiamonds(cost)) return "ダイヤが足りません";
            ownedCosmeticIds.Add(id);
            ForceEquip(slot, id);
            Save();
            return null;
        }

        static void ToggleEquip(string slot, string id)
        {
            if (EquippedCosmetic(slot) == id) ForceEquip(slot, "");
            else ForceEquip(slot, id);
            Save();
        }

        static void ForceEquip(string slot, string id)
        {
            if (slot == "frame") equipFrame = id ?? "";
            else if (slot == "title") equipTitle = id ?? "";
            else if (slot == "border") equipBorder = id ?? "";
            else if (slot == "backdrop") equipBackdrop = id ?? "";
            else if (slot == "corner") equipCorner = id ?? "";
            else if (slot == "seal")
            {
                equipSeal = !string.IsNullOrEmpty(id) && id.StartsWith("seal_") ? id : "";
                sealEquipped = string.IsNullOrEmpty(equipSeal) ? 0 : 1;
            }
            else if (slot == "dye")
            {
                int index;
                if (string.IsNullOrEmpty(id) || !id.StartsWith("dye_")) return;
                if (!int.TryParse(id.Substring(4), out index)) return;
                if (index < 0 || index >= subjectDyes.Length) return;
                subjectDyes[index] = subjectDyes[index] == 1 ? 0 : 1;
            }
        }

        public static int StatSteps(int index)
        {
            Load();
            if (index == 0) return HpSteps;
            if (index == 1) return AtkSteps;
            if (index == 2) return DefSteps;
            if (index == 3) return SpdSteps;
            return LuckSteps;
        }

        public static bool GrowthLocked()
        {
            return HasPausedBattle();
        }

        public static float PausedEnemyHpRatio()
        {
            if (!HasPausedBattle() || pausedBattle.enemy == null) return 1f;
            int max = Mathf.Max(1, pausedBattle.enemy.maxHp);
            return Mathf.Clamp01(pausedBattle.enemy.hp / (float)max);
        }

        public static bool TryAddStat(int index)
        {
            Load();
            if (GrowthLocked()) return false;
            if ((index == 3 && SpdSteps >= MaxSpdLuckSteps) || (index >= 4 && LuckSteps >= MaxSpdLuckSteps)) return false;
            if (GrowthBp < GrowthStatCost) return false;
            GrowthBp -= GrowthStatCost;
            if (index == 0) HpSteps++;
            else if (index == 1) AtkSteps++;
            else if (index == 2) DefSteps++;
            else if (index == 3) SpdSteps++;
            else LuckSteps++;
            Save();
            return true;
        }

        public static int LookSlot(int index)
        {
            Load();
            if (index < 0 || index >= lookSlots.Length) return 0;
            return lookSlots[index];
        }

        public static void CycleLookSlot(int index, int delta, int optionCount)
        {
            Load();
            if (GrowthLocked()) return;
            if (index < 0 || index >= lookSlots.Length || optionCount <= 0) return;
            int v = lookSlots[index] + delta;
            v %= optionCount;
            if (v < 0) v += optionCount;
            lookSlots[index] = v;
            Save();
        }

        public static bool TryRespec()
        {
            Load();
            if (GrowthLocked()) return false;
            int spent = (HpSteps + AtkSteps + DefSteps + SpdSteps + LuckSteps) * GrowthStatCost;
            if (spent <= 0) return false;
            if (GrowthRespecUsed)
            {
                if (Diamonds < RespecDiamondCost) return false;
                Diamonds -= RespecDiamondCost;
            }
            else
            {
                GrowthRespecUsed = true;
            }
            GrowthBp += spent;
            HpSteps = 0;
            AtkSteps = 0;
            DefSteps = 0;
            SpdSteps = 0;
            LuckSteps = 0;
            Save();
            return true;
        }


        public static bool CanEnterBattle()
        {
            Load();
            RolloverIfNeeded();
            if (HasPausedBattle()) return true;
            return TodayBp >= BattleCostBp;
        }

        public static bool HasPausedBattle()
        {
            Load();
            return pausedBattle != null && pausedBattle.active == 1 && pausedBattle.player != null && pausedBattle.enemy != null;
        }

        public static PausedBattleState GetPausedBattle()
        {
            Load();
            return HasPausedBattle() ? pausedBattle : null;
        }

        public static void SavePausedBattle(PausedBattleState state)
        {
            Load();
            pausedBattle = state;
            if (pausedBattle != null)
            {
                pausedBattle.active = 1;
                StampGrowthSnapshot(pausedBattle);
            }
            Save();
        }

        static void StampGrowthSnapshot(PausedBattleState state)
        {
            if (state == null) return;
            state.lookSlots = (int[])lookSlots.Clone();
            state.skillLoadout = (string[])SkillLoadout.Clone();
            state.loadoutSlots = (string[])LoadoutSlots.Clone();
            state.growthSnap = 1;
            state.hpSteps = HpSteps;
            state.atkSteps = AtkSteps;
            state.defSteps = DefSteps;
            state.spdSteps = SpdSteps;
            state.luckSteps = LuckSteps;
        }

        public static void RestorePausedGrowth(PausedBattleState state)
        {
            Load();
            if (state == null) return;
            bool dirty = false;
            if (state.growthSnap == 1)
            {
                HpSteps = Mathf.Max(0, state.hpSteps);
                AtkSteps = Mathf.Max(0, state.atkSteps);
                DefSteps = Mathf.Max(0, state.defSteps);
                SpdSteps = Mathf.Max(0, state.spdSteps);
                LuckSteps = Mathf.Max(0, state.luckSteps);
                dirty = true;
            }
            if (state.lookSlots != null && state.lookSlots.Length > 0)
            {
                int n = Mathf.Min(lookSlots.Length, state.lookSlots.Length);
                for (int i = 0; i < n; i++)
                    lookSlots[i] = Mathf.Max(0, state.lookSlots[i]);
                dirty = true;
            }
            if (state.skillLoadout != null && state.skillLoadout.Length > 0)
            {
                int n = Mathf.Min(SkillLoadout.Length, state.skillLoadout.Length);
                for (int i = 0; i < n; i++)
                    SkillLoadout[i] = state.skillLoadout[i];
                dirty = true;
            }
            if (state.loadoutSlots != null && state.loadoutSlots.Length > 0)
            {
                int n = Mathf.Min(LoadoutSlots.Length, state.loadoutSlots.Length);
                for (int i = 0; i < n; i++)
                    LoadoutSlots[i] = state.loadoutSlots[i];
                dirty = true;
            }
            if (dirty) Save();
        }

        public static void ClearPausedBattle()
        {
            Load();
            if (pausedBattle == null) return;
            pausedBattle = null;
            Save();
        }

        public static bool IsBossFloor(int floor)
        {
            return floor % DungeonBossInterval == 0;
        }

        public static bool CurrentFloorIsBoss()
        {
            Load();
            return IsBossFloor(DungeonFloor);
        }

        public static int MinutesForTower(int week, int floor)
        {
            week = Mathf.Clamp(week, 1, MaxDungeonWeek);
            floor = Mathf.Clamp(floor, 1, DungeonMaxFloor);
            float loops = (week - 1) + (floor - 1) / 99f;
            const float floorOneHours = 3f;
            float hours = floorOneHours + loops * (HoursPerLoop - floorOneHours);
            return Mathf.RoundToInt(hours * 60f);
        }

        public static string WeekText(int week)
        {
            week = Mathf.Clamp(week, 1, MaxDungeonWeek);
            return week + "周目";
        }

        public static string WeekFloorText()
        {
            Load();
            string text = WeekText(DungeonWeek) + "  " + DungeonFloor + "F";
            if (IsBossFloor(DungeonFloor)) text += " ボス";
            return text;
        }

        public static void AdvanceFloorOnWin()
        {
            Load();
            int score = DungeonWeek * 100 + DungeonFloor;
            if (score > DungeonDeepestClear) DungeonDeepestClear = score;
            if (DungeonFloor >= DungeonMaxFloor)
            {
                if (DungeonWeek < MaxDungeonWeek) DungeonWeek++;
                DungeonFloor = 1;
            }
            else
            {
                DungeonFloor++;
            }
            Save();
        }

        static void EnsureStarterUltimate()
        {
            bool dirty = false;
            int oldRay = ownedUltimateIds.IndexOf("raygust_guard");
            if (oldRay >= 0)
            {
                ownedUltimateIds.RemoveAt(oldRay);
                if (oldRay < ownedUltimateLevels.Count) ownedUltimateLevels.RemoveAt(oldRay);
                dirty = true;
            }
            if (LoadoutSlots != null && LoadoutSlots.Length > 0 && LoadoutSlots[0] == "raygust_guard")
            {
                LoadoutSlots[0] = "shogun_henshin";
                dirty = true;
            }
            if (gachaMigrated < 1)
            {
                for (int i = ownedUltimateIds.Count - 1; i >= 0; i--)
                {
                    if (IsFreeSkill(ownedUltimateIds[i])) continue;
                    ownedUltimateIds.RemoveAt(i);
                    if (i < ownedUltimateLevels.Count) ownedUltimateLevels.RemoveAt(i);
                }
                ownedArmorIds.Clear();
                ownedArmorLevels.Clear();
                for (int i = 7; i <= 12 && i < lookSlots.Length; i++)
                    lookSlots[i] = 0;
                gachaMigrated = 1;
                dirty = true;
            }
            for (int i = 0; i < FreeSkillIds.Length; i++)
            {
                if (ownedUltimateIds.Contains(FreeSkillIds[i])) continue;
                ownedUltimateIds.Add(FreeSkillIds[i]);
                ownedUltimateLevels.Add(1);
                dirty = true;
            }
            if (string.IsNullOrEmpty(LoadoutSlots[0]) || !IsUltimateOwned(LoadoutSlots[0]) || !IsBrainstormId(LoadoutSlots[0]))
            {
                LoadoutSlots[0] = "element_fire_core";
                dirty = true;
            }
            if (dirty) Save();
        }

        public static string ActiveUltimateId()
        {
            Load();
            if (IsBrainstormId(LoadoutSlots[0])) return LoadoutSlots[0];
            return FirstBrainstormId();
        }

        public static void EquipActiveUltimate(string id)
        {
            EquipSlotUltimate(ShiftingMetropolis.Battle.SkillCommandType.BrainSmash, id);
        }

        public static string CycleActiveUltimate()
        {
            return CycleSkill(ShiftingMetropolis.Battle.SkillCommandType.BrainSmash);
        }

        static void EnsureStarterSkills()
        {
            bool dirty = false;
            string[] fallbacks = { "element_fire", "gunplay_shot", "drop_kick", "uppercut_jab" };
            for (int i = 0; i < SkillLoadout.Length; i++)
            {
                if (string.IsNullOrEmpty(SkillLoadout[i]) || IsLegacyBasicId(SkillLoadout[i]))
                {
                    SkillLoadout[i] = fallbacks[i];
                    dirty = true;
                }
                else
                {
                    var placed = ShiftingMetropolis.Battle.UltimateCatalog.Get(SkillLoadout[i]);
                    if (placed == null || !IsUltimateOwned(SkillLoadout[i]) || placed.gear == ShiftingMetropolis.Battle.UltimateGear.Brainstorm)
                    {
                        SkillLoadout[i] = fallbacks[i];
                        dirty = true;
                    }
                }
            }
            if (dirty) Save();
        }

        static bool IsLegacyBasicId(string id)
        {
            return ShiftingMetropolis.Battle.SkillCatalog.Get(id) != null;
        }

        static int SkillSlotIndex(ShiftingMetropolis.Battle.SkillCommandType type)
        {
            if (type == ShiftingMetropolis.Battle.SkillCommandType.Skill1) return 0;
            if (type == ShiftingMetropolis.Battle.SkillCommandType.Skill2) return 1;
            if (type == ShiftingMetropolis.Battle.SkillCommandType.Skill3) return 2;
            if (type == ShiftingMetropolis.Battle.SkillCommandType.Charge) return 3;
            return -1;
        }

        public static string SlotUltimateId(ShiftingMetropolis.Battle.SkillCommandType type)
        {
            Load();
            if (type == ShiftingMetropolis.Battle.SkillCommandType.BrainSmash)
                return ActiveUltimateId();
            int i = SkillSlotIndex(type);
            if (i < 0) return null;
            if (!string.IsNullOrEmpty(SkillLoadout[i]) && IsUltimateOwned(SkillLoadout[i]) && ShiftingMetropolis.Battle.UltimateCatalog.Get(SkillLoadout[i]) != null)
                return SkillLoadout[i];
            if (i == 0) return "element_fire";
            if (i == 1) return "gunplay_shot";
            if (i == 2) return "drop_kick";
            return "uppercut_jab";
        }

        public static string ActiveSkillId(ShiftingMetropolis.Battle.SkillCommandType type)
        {
            return SlotUltimateId(type);
        }

        public static void EquipSlotUltimate(ShiftingMetropolis.Battle.SkillCommandType type, string id)
        {
            Load();
            if (GrowthLocked()) return;
            if (string.IsNullOrEmpty(id) || ShiftingMetropolis.Battle.UltimateCatalog.Get(id) == null) return;
            if (ownedUltimateIds.Count > 0 && !ownedUltimateIds.Contains(id)) return;
            if (!CanPlaceInSlot(type, id)) return;
            var slots = new[]
            {
                ShiftingMetropolis.Battle.SkillCommandType.Skill1,
                ShiftingMetropolis.Battle.SkillCommandType.Skill2,
                ShiftingMetropolis.Battle.SkillCommandType.Skill3,
                ShiftingMetropolis.Battle.SkillCommandType.Charge,
                ShiftingMetropolis.Battle.SkillCommandType.BrainSmash
            };
            string current = SlotUltimateId(type);
            for (int s = 0; s < slots.Length; s++)
            {
                if (slots[s] == type) continue;
                if (SlotUltimateId(slots[s]) != id) continue;
                if (CanPlaceInSlot(slots[s], current))
                    WriteSlotUltimate(slots[s], current);
                else
                    WriteSlotUltimate(slots[s], DefaultSlotUltimate(slots[s]));
            }
            WriteSlotUltimate(type, id);
            Save();
        }

        static bool IsBrainstormId(string id)
        {
            var def = ShiftingMetropolis.Battle.UltimateCatalog.Get(id);
            return def != null && def.gear == ShiftingMetropolis.Battle.UltimateGear.Brainstorm;
        }

        static bool CanPlaceInSlot(ShiftingMetropolis.Battle.SkillCommandType type, string id)
        {
            bool smash = type == ShiftingMetropolis.Battle.SkillCommandType.BrainSmash;
            return IsBrainstormId(id) == smash;
        }

        static string FirstBrainstormId()
        {
            for (int i = 0; i < ownedUltimateIds.Count; i++)
            {
                if (IsBrainstormId(ownedUltimateIds[i])) return ownedUltimateIds[i];
            }
            return "element_fire_core";
        }

        public static bool IsFreeSkill(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < FreeSkillIds.Length; i++)
                if (FreeSkillIds[i] == id) return true;
            return false;
        }

        public static bool OwnsArmor(string id)
        {
            Load();
            return !string.IsNullOrEmpty(id) && ownedArmorIds.Contains(id);
        }

        public static bool GrantSkill(string id)
        {
            Load();
            if (string.IsNullOrEmpty(id) || ownedUltimateIds.Contains(id)) return false;
            if (ShiftingMetropolis.Battle.UltimateCatalog.Get(id) == null) return false;
            ownedUltimateIds.Add(id);
            ownedUltimateLevels.Add(1);
            Save();
            return true;
        }

        public static bool GrantArmor(string id)
        {
            Load();
            if (string.IsNullOrEmpty(id) || ownedArmorIds.Contains(id)) return false;
            ownedArmorIds.Add(id);
            ownedArmorLevels.Add(1);
            Save();
            return true;
        }

        public static bool OwnsBody(string id)
        {
            Load();
            return !string.IsNullOrEmpty(id) && ownedBodyIds.Contains(id);
        }

        public static bool GrantBody(string id)
        {
            Load();
            if (string.IsNullOrEmpty(id) || ownedBodyIds.Contains(id)) return false;
            ownedBodyIds.Add(id);
            ownedBodyLevels.Add(1);
            Save();
            return true;
        }

        public static int BodyLevel(string id)
        {
            Load();
            int idx = ownedBodyIds.IndexOf(id);
            if (idx < 0) return 0;
            if (idx >= ownedBodyLevels.Count) return 1;
            return Mathf.Clamp(ownedBodyLevels[idx], 1, SkillVersionMax);
        }

        public static int RaiseBodyVersion(string id)
        {
            Load();
            int idx = ownedBodyIds.IndexOf(id);
            if (idx < 0) return 0;
            while (ownedBodyLevels.Count <= idx) ownedBodyLevels.Add(1);
            if (ownedBodyLevels[idx] < SkillVersionMax)
            {
                ownedBodyLevels[idx]++;
                Save();
            }
            return ownedBodyLevels[idx];
        }

        public static int ArmorLevel(string id)
        {
            Load();
            int idx = ownedArmorIds.IndexOf(id);
            if (idx < 0) return 0;
            if (idx >= ownedArmorLevels.Count) return 1;
            return Mathf.Clamp(ownedArmorLevels[idx], 1, SkillVersionMax);
        }

        public static int RaiseArmorVersion(string id)
        {
            Load();
            int idx = ownedArmorIds.IndexOf(id);
            if (idx < 0) return 0;
            while (ownedArmorLevels.Count <= idx) ownedArmorLevels.Add(1);
            if (ownedArmorLevels[idx] < SkillVersionMax)
            {
                ownedArmorLevels[idx]++;
                Save();
            }
            return ownedArmorLevels[idx];
        }

        public static bool TrySpendDiamonds(int cost)
        {
            Load();
            if (cost < 0 || Diamonds < cost) return false;
            Diamonds -= cost;
            Save();
            return true;
        }

        public static void SetLookSlot(int index, int value)
        {
            Load();
            if (GrowthLocked()) return;
            if (index < 0 || index >= lookSlots.Length) return;
            lookSlots[index] = Mathf.Max(0, value);
            Save();
        }

        static string DefaultSlotUltimate(ShiftingMetropolis.Battle.SkillCommandType type)
        {
            if (type == ShiftingMetropolis.Battle.SkillCommandType.Skill1) return "element_fire";
            if (type == ShiftingMetropolis.Battle.SkillCommandType.Skill2) return "gunplay_shot";
            if (type == ShiftingMetropolis.Battle.SkillCommandType.Skill3) return "drop_kick";
            if (type == ShiftingMetropolis.Battle.SkillCommandType.Charge) return "uppercut_jab";
            return "element_fire_core";
        }

        static void WriteSlotUltimate(ShiftingMetropolis.Battle.SkillCommandType type, string id)
        {
            if (type == ShiftingMetropolis.Battle.SkillCommandType.BrainSmash)
                LoadoutSlots[0] = id;
            else
            {
                int i = SkillSlotIndex(type);
                if (i >= 0) SkillLoadout[i] = id;
            }
        }

        public static void EquipSkill(ShiftingMetropolis.Battle.SkillCommandType type, string id)
        {
            EquipSlotUltimate(type, id);
        }

        public static string CycleSkill(ShiftingMetropolis.Battle.SkillCommandType type)
        {
            Load();
            if (GrowthLocked()) return SlotUltimateId(type);
            if (ownedUltimateIds.Count == 0) return SlotUltimateId(type);
            string cur = SlotUltimateId(type);
            int start = ownedUltimateIds.IndexOf(cur);
            for (int n = 1; n <= ownedUltimateIds.Count; n++)
            {
                int idx = start < 0
                    ? (n - 1) % ownedUltimateIds.Count
                    : (start + n) % ownedUltimateIds.Count;
                if (!CanPlaceInSlot(type, ownedUltimateIds[idx])) continue;
                EquipSlotUltimate(type, ownedUltimateIds[idx]);
                return ownedUltimateIds[idx];
            }
            return cur;
        }

        public static IReadOnlyList<string> OwnedUltimateIds()
        {
            Load();
            return ownedUltimateIds;
        }

        public static bool IsUltimateOwned(string id)
        {
            Load();
            return ownedUltimateIds.Contains(id);
        }

        public const int SkillVersionMax = 8;

        public static string VersionMark(int version)
        {
            if (version >= 8) return "神";
            if (version == 7) return "真";
            if (version == 6) return "改";
            return "V" + Mathf.Max(1, version);
        }

        public static string SkillTitle(string name, int version)
        {
            if (string.IsNullOrEmpty(name)) return VersionMark(version);
            if (version >= 8) return "神 " + name;
            if (version == 7) return "真 " + name;
            if (version == 6) return name + " 改";
            return name;
        }

        public static int UltimateLevel(string id)
        {
            Load();
            int idx = ownedUltimateIds.IndexOf(id);
            return idx < 0 ? 0 : ownedUltimateLevels[idx];
        }

        public static int RaiseSkillVersion(string id)
        {
            Load();
            int idx = ownedUltimateIds.IndexOf(id);
            if (idx < 0) return 0;
            if (ownedUltimateLevels[idx] < SkillVersionMax)
            {
                ownedUltimateLevels[idx]++;
                Save();
            }
            return ownedUltimateLevels[idx];
        }

        public static bool UnlockUltimate(string id, int diamondCost)
        {
            Load();
            if (GrowthLocked()) return false;
            if (ownedUltimateIds.Contains(id)) return true;
            if (Diamonds < diamondCost) return false;
            Diamonds -= diamondCost;
            ownedUltimateIds.Add(id);
            ownedUltimateLevels.Add(1);
            Save();
            return true;
        }

        public static bool UpgradeUltimate(string id, int diamondCost, int levelMax)
        {
            Load();
            if (GrowthLocked()) return false;
            int idx = ownedUltimateIds.IndexOf(id);
            if (idx < 0) return false;
            if (ownedUltimateLevels[idx] >= levelMax) return false;
            if (Diamonds < diamondCost) return false;
            Diamonds -= diamondCost;
            ownedUltimateLevels[idx]++;
            Save();
            return true;
        }

        public static string[] GetLoadout()
        {
            Load();
            return (string[])LoadoutSlots.Clone();
        }

        public static void SetLoadoutSlot(int slot, string ultimateId)
        {
            Load();
            if (GrowthLocked()) return;
            if (slot < 0 || slot >= LoadoutSlots.Length) return;
            LoadoutSlots[slot] = ultimateId;
            Save();
        }

        public static bool TrySpendForBattle()
        {
            Load();
            RolloverIfNeeded();
            if (HasPausedBattle()) return true;
            if (TodayBp < BattleCostBp) return false;
            TodayBp -= BattleCostBp;
            Save();
            return true;
        }

        public static int BrainSmashMaxToday()
        {
            Load();
            RolloverIfNeeded();
            return TodayBp / BrainSmashMinutesPerUse;
        }

        public static int BrainSmashLeftToday()
        {
            return Mathf.Max(0, BrainSmashMaxToday() - BrainSmashUsesToday);
        }

        public static bool CanUseBrainSmash()
        {
            return BrainSmashLeftToday() > 0;
        }

        public static bool TryUseBrainSmash()
        {
            Load();
            RolloverIfNeeded();
            if (BrainSmashLeftToday() <= 0) return false;
            BrainSmashUsesToday += 1;
            Save();
            return true;
        }

        public static void SaveProfile(string displayName, string bio, string school, string goal, int colorIndex)
        {
            Load();
            if (Profile == null) Profile = new UserProfile();
            Profile.displayName = displayName != null ? displayName.Trim() : string.Empty;
            Profile.bio = bio != null ? bio.Trim() : string.Empty;
            Profile.school = school != null ? school.Trim() : string.Empty;
            Profile.goal = goal != null ? goal.Trim() : string.Empty;
            Profile.colorIndex = Mathf.Clamp(colorIndex, 0, 5);
            Save();
        }

        public static int WeeklyGoalMinutes()
        {
            Load();
            RolloverWeeklyGoal();
            if (Profile == null || Profile.weeklyGoalMinutes < 30) return 0;
            return Mathf.Clamp(Profile.weeklyGoalMinutes, 30, MaxWeeklyGoalMinutes);
        }

        public static bool HasLockedGoal()
        {
            Load();
            RolloverWeeklyGoal();
            if (Profile == null || Profile.weeklyGoalMinutes < 30) return false;
            return Profile.goalWeekStart == WeekStart(Now).ToString("yyyy-MM-dd");
        }

        public static bool SetWeeklyGoalMinutes(int minutes)
        {
            Load();
            RolloverWeeklyGoal();
            if (HasLockedGoal()) return false;
            if (Profile == null) Profile = new UserProfile();
            Profile.weeklyGoalMinutes = Mathf.Clamp(minutes, 30, MaxWeeklyGoalMinutes);
            var start = WeekStart(Now);
            Profile.goalWeekStart = start.ToString("yyyy-MM-dd");
            Events.Add(new TimelineEvent
            {
                id = Guid.NewGuid().ToString("N"),
                kind = "goal_set",
                createdAt = Now.ToString("o"),
                goalMinutes = Profile.weeklyGoalMinutes,
                doneMinutes = 0,
                weekStart = Profile.goalWeekStart
            });
            Save();
            return true;
        }

        public static bool RolloverWeeklyGoal()
        {
            if (!loaded) Load();
            if (Profile == null) return false;
            bool changed = false;
            if (Profile.weeklyGoalMinutes >= 30 && string.IsNullOrEmpty(Profile.goalWeekStart))
            {
                Profile.goalWeekStart = WeekStart(Now).ToString("yyyy-MM-dd");
                changed = true;
            }
            if (string.IsNullOrEmpty(Profile.goalWeekStart) || Profile.weeklyGoalMinutes < 30)
            {
                if (changed) Save();
                return changed;
            }
            DateTime start;
            if (!DateTime.TryParse(Profile.goalWeekStart, out start)) return changed;
            start = start.Date;
            var current = WeekStart(Now);
            if (current <= start)
            {
                if (changed) Save();
                return changed;
            }
            int done = MinutesBetween(start, start.AddDays(7));
            Events.Add(new TimelineEvent
            {
                id = Guid.NewGuid().ToString("N"),
                kind = "week_result",
                createdAt = start.AddDays(7).ToString("o"),
                goalMinutes = Profile.weeklyGoalMinutes,
                doneMinutes = done,
                weekStart = start.ToString("yyyy-MM-dd")
            });
            Profile.weeklyGoalMinutes = 0;
            Profile.goalWeekStart = string.Empty;
            Save();
            return true;
        }

        public static IReadOnlyList<TimelineEvent> AllEvents
        {
            get { Load(); RolloverWeeklyGoal(); return Events; }
        }

        public static IReadOnlyList<FriendEntry> FriendList
        {
            get { Load(); return Friends; }
        }

        public static string MyFriendCode()
        {
            Load();
            EnsureFriendCode();
            return Profile != null ? Profile.friendCode : "";
        }

        public static bool IsFriend(string code)
        {
            Load();
            code = NormalizeFriendCode(code);
            if (string.IsNullOrEmpty(code)) return false;
            for (int i = 0; i < Friends.Count; i++)
            {
                if (Friends[i] != null && Friends[i].code == code) return true;
            }
            return false;
        }

        public static bool IsFriendAuthor(string authorCode)
        {
            return IsFriend(authorCode);
        }

        public static bool IsSelfAuthor(string authorCode)
        {
            if (string.IsNullOrEmpty(authorCode)) return true;
            return string.Equals(NormalizeFriendCode(authorCode), MyFriendCode(), StringComparison.OrdinalIgnoreCase);
        }

        public static FriendEntry FindFriend(string code)
        {
            Load();
            code = NormalizeFriendCode(code);
            for (int i = 0; i < Friends.Count; i++)
            {
                if (Friends[i] != null && Friends[i].code == code) return Friends[i];
            }
            return null;
        }

        public static string AddFriend(string code, string name)
        {
            Load();
            code = NormalizeFriendCode(code);
            if (code.Length != 6) return "コードは6文字です";
            if (code == MyFriendCode()) return "自分のコードです";
            if (IsFriend(code)) return "すでにフレンドです";
            name = name != null ? name.Trim() : "";
            if (string.IsNullOrEmpty(name)) name = code;
            if (name.Length > 16) name = name.Substring(0, 16);
            int color = 0;
            for (int i = 0; i < code.Length; i++) color += code[i];
            Friends.Add(new FriendEntry
            {
                code = code,
                name = name,
                colorIndex = Mathf.Abs(color) % 6,
                week = 1,
                floor = 1,
                level = 1
            });
            Save();
            return null;
        }

        public static void ApplyRemoteFriend(string code, string name, int level, int week, int floor)
        {
            Load();
            var friend = FindFriend(code);
            if (friend == null) return;
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                if (name.Length > 16) name = name.Substring(0, 16);
                friend.name = name;
            }
            friend.level = Mathf.Max(1, level);
            friend.week = Mathf.Max(1, week);
            friend.floor = Mathf.Max(1, floor);
            Save();
        }

        public static void ApplyRemoteFriendDetail(string code, string bio, string school, string goal,
            int dailyGoalMinutes, int weeklyGoalMinutes, string lookCsv, string skillsCsv, string cosmeticsCsv)
        {
            Load();
            var friend = FindFriend(code);
            if (friend == null) return;
            friend.bio = bio ?? "";
            friend.school = school ?? "";
            friend.goal = goal ?? "";
            friend.dailyGoalMinutes = Mathf.Max(0, dailyGoalMinutes);
            friend.weeklyGoalMinutes = Mathf.Max(0, weeklyGoalMinutes);
            friend.lookCsv = lookCsv ?? "";
            friend.skillsCsv = skillsCsv ?? "";
            friend.cosmeticsCsv = cosmeticsCsv ?? "";
            Save();
        }

        public static void ApplyRemoteFriendStudy(string code, int weekMinutes, int lifetimeMinutes)
        {
            Load();
            var friend = FindFriend(code);
            if (friend == null) return;
            friend.weekMinutes = Mathf.Max(0, weekMinutes);
            friend.lifetimeMinutes = Mathf.Max(0, lifetimeMinutes);
            Save();
        }

        public static int MinutesThisWeek()
        {
            var start = WeekStart(Now);
            return MinutesBetween(start, start.AddDays(7));
        }

        public static string MyLookCsv()
        {
            Load();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lookSlots.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(lookSlots[i]);
            }
            return sb.ToString();
        }

        public static string MySkillsCsv()
        {
            Load();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < SkillLoadout.Length; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(SkillLoadout[i] ?? "");
            }
            sb.Append('|');
            sb.Append(ActiveUltimateId() ?? "");
            return sb.ToString();
        }

        static readonly string[] CosmeticSlots = { "frame", "title", "border", "backdrop", "corner", "seal" };

        public static string MyCosmeticsCsv()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < CosmeticSlots.Length; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(CosmeticSlots[i]).Append(':').Append(EquippedCosmetic(CosmeticSlots[i]));
            }
            return sb.ToString();
        }

        public static int[] ParseLookCsv(string csv)
        {
            var result = new int[LookSlotIds.Length];
            if (string.IsNullOrEmpty(csv)) return result;
            var parts = csv.Split(',');
            for (int i = 0; i < parts.Length && i < result.Length; i++)
            {
                int v;
                if (int.TryParse(parts[i], out v)) result[i] = Mathf.Max(0, v);
            }
            return result;
        }

        public static string[] ParseSkillsCsv(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return new string[5];
            var parts = csv.Split('|');
            var result = new string[5];
            for (int i = 0; i < parts.Length && i < 5; i++) result[i] = parts[i];
            return result;
        }

        public static string ParseCosmeticFromCsv(string csv, string slot)
        {
            if (string.IsNullOrEmpty(csv) || string.IsNullOrEmpty(slot)) return "";
            var parts = csv.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                int c = parts[i].IndexOf(':');
                if (c <= 0) continue;
                if (parts[i].Substring(0, c) == slot) return parts[i].Substring(c + 1);
            }
            return "";
        }

        public static void RemoveFriend(string code)
        {
            Load();
            code = NormalizeFriendCode(code);
            Friends.RemoveAll(f => f != null && f.code == code);
            Save();
        }

        public static string NormalizeFriendCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            var chars = new List<char>(6);
            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];
                if (c == ' ' || c == '-') continue;
                if (c >= 'a' && c <= 'z') c = (char)(c - 32);
                chars.Add(c);
            }
            return new string(chars.ToArray());
        }

        static void EnsureFriendCode()
        {
            if (Profile == null) Profile = new UserProfile();
            string code = NormalizeFriendCode(Profile.friendCode);
            if (code.Length == 6)
            {
                Profile.friendCode = code;
                return;
            }
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new System.Random();
            var chars = new char[6];
            for (int i = 0; i < 6; i++) chars[i] = alphabet[rng.Next(alphabet.Length)];
            Profile.friendCode = new string(chars);
        }

        public static DateTime WeekStart(DateTime day)
        {
            int mondayOffset = ((int)day.DayOfWeek + 6) % 7;
            return day.Date.AddDays(-mondayOffset);
        }

        public static int MinutesBetween(DateTime fromInclusive, DateTime toExclusive)
        {
            Load();
            int sum = 0;
            for (int i = 0; i < Logs.Count; i++)
            {
                DateTime dt;
                if (!DateTime.TryParse(Logs[i].startedAt, out dt)) continue;
                if (dt >= fromInclusive && dt < toExclusive) sum += Logs[i].minutes;
            }
            return sum;
        }

        public static int[,] DaysFrom(DateTime start, int dayCount)
        {
            Load();
            if (dayCount < 1) dayCount = 1;
            var grid = new int[dayCount, 6];
            var begin = start.Date;
            for (int i = 0; i < Logs.Count; i++)
            {
                DateTime dt;
                if (!DateTime.TryParse(Logs[i].startedAt, out dt)) continue;
                int col = (int)(dt.Date - begin).TotalDays;
                if (col < 0 || col >= dayCount) continue;
                int s = Logs[i].subject;
                if (s >= 0 && s < 6) grid[col, s] += Logs[i].minutes;
            }
            return grid;
        }

        public static int[,] MonthsOfYear(int year)
        {
            Load();
            var grid = new int[12, 6];
            for (int i = 0; i < Logs.Count; i++)
            {
                DateTime dt;
                if (!DateTime.TryParse(Logs[i].startedAt, out dt)) continue;
                if (dt.Year != year) continue;
                int col = dt.Month - 1;
                int s = Logs[i].subject;
                if (s >= 0 && s < 6) grid[col, s] += Logs[i].minutes;
            }
            return grid;
        }

        public static string ProfileName()
        {
            Load();
            if (Profile == null || string.IsNullOrEmpty(Profile.displayName)) return "プレイヤー";
            return Profile.displayName;
        }

        public static string ProfileBio()
        {
            Load();
            if (Profile == null || string.IsNullOrEmpty(Profile.bio)) return "プロフィール未設定";
            return Profile.bio;
        }

        public static Color ProfileColor()
        {
            Load();
            int i = Profile != null ? Mathf.Clamp(Profile.colorIndex, 0, 5) : 0;
            return StudySubjectColors.Color((StudySubject)i);
        }

        public static string ProfileInitial()
        {
            string name = ProfileName();
            return name.Substring(0, 1);
        }

        const string IconFileName = "profile_icon.png";
        static Sprite cachedIconSprite;
        static Texture2D cachedIconTex;
        static bool iconCacheReady;

        public static string ProfileIconPath => Path.Combine(SaveRoot, IconFileName);

        public static bool HasProfileIcon()
        {
            Load();
            try { return File.Exists(ProfileIconPath); }
            catch { return false; }
        }

        public static Sprite ProfileIconSprite()
        {
            Load();
            if (!iconCacheReady)
            {
                iconCacheReady = true;
                try
                {
                    if (File.Exists(ProfileIconPath))
                    {
                        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Bilinear;
                        if (tex.LoadImage(File.ReadAllBytes(ProfileIconPath)))
                        {
                            cachedIconTex = tex;
                            cachedIconSprite = Sprite.Create(
                                tex,
                                new Rect(0f, 0f, tex.width, tex.height),
                                new Vector2(0.5f, 0.5f),
                                100f);
                        }
                        else
                        {
                            UnityEngine.Object.Destroy(tex);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Profile icon load failed: " + e.Message);
                }
            }
            return cachedIconSprite;
        }

        public static bool SaveProfileIconFromPath(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return false;
            Load();
            try
            {
                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!src.LoadImage(File.ReadAllBytes(sourcePath)))
                {
                    UnityEngine.Object.Destroy(src);
                    return false;
                }
                bool ok = SaveProfileIconTexture(src);
                UnityEngine.Object.Destroy(src);
                return ok;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Profile icon save failed: " + e.Message);
                return false;
            }
        }

        public static bool SaveProfileIconFromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return false;
            Load();
            try
            {
                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!src.LoadImage(bytes))
                {
                    UnityEngine.Object.Destroy(src);
                    return false;
                }
                bool ok = SaveProfileIconTexture(src);
                UnityEngine.Object.Destroy(src);
                return ok;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Profile icon save failed: " + e.Message);
                return false;
            }
        }

        static bool SaveProfileIconTexture(Texture2D src)
        {
            var small = ScaleTexture(src, 256);
            File.WriteAllBytes(ProfileIconPath, small.EncodeToPNG());
            UnityEngine.Object.Destroy(small);
            InvalidateIconCache();
            return true;
        }

        public static void ClearProfileIcon()
        {
            Load();
            try
            {
                if (File.Exists(ProfileIconPath)) File.Delete(ProfileIconPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Profile icon delete failed: " + e.Message);
            }
            InvalidateIconCache();
        }

        static void InvalidateIconCache()
        {
            iconCacheReady = false;
            if (cachedIconSprite != null)
            {
                UnityEngine.Object.Destroy(cachedIconSprite);
                cachedIconSprite = null;
            }
            if (cachedIconTex != null)
            {
                UnityEngine.Object.Destroy(cachedIconTex);
                cachedIconTex = null;
            }
        }

        static Texture2D ScaleTexture(Texture2D source, int size)
        {
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            var dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
            dst.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
            dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }

        public static void AddDiamonds(int amount)
        {
            if (amount <= 0) return;
            Load();
            Diamonds += amount;
            Save();
        }

        public static StudyMaterial AddMaterial(string name, StudySubject subject)
        {
            Load();
            var item = new StudyMaterial
            {
                id = Guid.NewGuid().ToString("N"),
                name = name.Trim(),
                subject = (int)subject
            };
            Materials.Add(item);
            Save();
            return item;
        }

        public static bool IsMaterialMeasuring(string id)
        {
            Load();
            if (string.IsNullOrEmpty(id)) return false;
            if (Session != null && Session.materialId == id && (Session.running || Session.paused)) return true;
            return FindOpenLog(id) != null;
        }

        public static bool DeleteMaterial(string id)
        {
            Load();
            if (IsMaterialMeasuring(id)) return false;
            Materials.RemoveAll(m => m.id == id);
            if (Session != null && Session.materialId == id)
            {
                Session = new ActiveSessionState();
            }
            Save();
            return true;
        }

        public static StudyMaterial FindMaterial(string id)
        {
            Load();
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].id == id) return Materials[i];
            }
            return null;
        }

        public static StudySubject SubjectAt(int index)
        {
            Load();
            EnsureSubjectOrder();
            return (StudySubject)subjectOrder[Mathf.Clamp(index, 0, 5)];
        }

        public static void MoveSubject(StudySubject subject, int delta)
        {
            Load();
            EnsureSubjectOrder();
            int index = -1;
            for (int i = 0; i < subjectOrder.Length; i++)
            {
                if (subjectOrder[i] == (int)subject) { index = i; break; }
            }
            int next = index + delta;
            if (index < 0 || next < 0 || next >= subjectOrder.Length) return;
            int tmp = subjectOrder[index];
            subjectOrder[index] = subjectOrder[next];
            subjectOrder[next] = tmp;
            Save();
        }

        public static bool MoveMaterial(string id, int delta)
        {
            Load();
            if (string.IsNullOrEmpty(id) || delta == 0) return false;
            int index = -1;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i] != null && Materials[i].id == id) { index = i; break; }
            }
            if (index < 0) return false;
            int subject = Materials[index].subject;
            int step = delta > 0 ? 1 : -1;
            int neighbor = -1;
            for (int i = index + step; i >= 0 && i < Materials.Count; i += step)
            {
                if (Materials[i] != null && Materials[i].subject == subject)
                {
                    neighbor = i;
                    break;
                }
            }
            if (neighbor < 0) return false;
            var held = Materials[index];
            Materials[index] = Materials[neighbor];
            Materials[neighbor] = held;
            Save();
            return true;
        }

        static void EnsureSubjectOrder()
        {
            if (subjectOrder == null || subjectOrder.Length != 6)
            {
                subjectOrder = new[] { 0, 1, 2, 3, 4, 5 };
                return;
            }
            var seen = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                int v = subjectOrder[i];
                if (v < 0 || v > 5 || seen[v])
                {
                    subjectOrder = new[] { 0, 1, 2, 3, 4, 5 };
                    return;
                }
                seen[v] = true;
            }
        }

        public static List<StudyMaterial> MaterialsFor(StudySubject subject)
        {
            Load();
            var list = new List<StudyMaterial>();
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].subject == (int)subject) list.Add(Materials[i]);
            }
            return list;
        }

        public static StudyLogEntry FindLog(string id)
        {
            Load();
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Logs.Count; i++)
            {
                if (Logs[i].id == id) return Logs[i];
            }
            return null;
        }

        public static StudyLogEntry FindOpenLog(string materialId)
        {
            Load();
            for (int i = Logs.Count - 1; i >= 0; i--)
            {
                if (Logs[i].open && Logs[i].materialId == materialId) return Logs[i];
            }
            return null;
        }

        public static StudyLogEntry BeginSession(StudyMaterial material)
        {
            Load();
            var existing = FindOpenLog(material.id);
            if (existing != null) return existing;

            CloseOpenSessions();
            var log = new StudyLogEntry
            {
                id = Guid.NewGuid().ToString("N"),
                materialId = material.id,
                materialName = material.name,
                subject = material.subject,
                minutes = 0,
                startedAt = Now.ToString("o"),
                open = true
            };
            Logs.Add(log);
            Save();
            return log;
        }

        public static void AddLiveMinute(StudyLogEntry openLog)
        {
            Load();
            GrantBp(1);
            var log = openLog;
            if (log != null && !string.IsNullOrEmpty(log.id))
            {
                log = FindLog(log.id) ?? openLog;
            }
            if (log != null)
            {
                log.minutes += 1;
                log.durationSeconds = Mathf.Max(log.durationSeconds, log.minutes * 60);
            }
            Save();
        }

        public static void CommitTimedSession(StudyLogEntry openLog, float sessionSeconds)
        {
            Load();
            var log = openLog != null ? FindLog(openLog.id) ?? openLog : null;
            int duration = Mathf.Max(0, Mathf.FloorToInt(sessionSeconds));
            int minutes = duration / 60;
            if (log != null)
            {
                int extra = minutes - log.minutes;
                if (extra > 0) GrantBp(extra);
                log.minutes = Mathf.Max(log.minutes, minutes);
                log.durationSeconds = Mathf.Max(log.durationSeconds, duration);
                log.open = false;
            }
            CloseOpenSessions();
            Session = new ActiveSessionState();
            Save();
        }

        public static StudyLogEntry AddManualLog(StudyMaterial material, int hours, int minutes, DateTime startedAt)
        {
            Load();
            if (material == null) return null;
            int totalMinutes = Mathf.Max(0, hours) * 60 + Mathf.Max(0, minutes);
            if (totalMinutes <= 0) return null;

            var log = new StudyLogEntry
            {
                id = Guid.NewGuid().ToString("N"),
                materialId = material.id,
                materialName = material.name,
                subject = material.subject,
                minutes = totalMinutes,
                durationSeconds = totalMinutes * 60,
                startedAt = startedAt.ToString("o"),
                open = false
            };
            Logs.Add(log);
            GrantBp(totalMinutes);
            Session.running = false;
            Save();
            return log;
        }

        public static void EndSession()
        {
            Load();
            CloseOpenSessions();
            Session.running = false;
            Session.lastUtcTicks = DateTime.UtcNow.Ticks;
            Save();
        }

        static void CloseOpenSessions()
        {
            for (int i = 0; i < Logs.Count; i++)
            {
                Logs[i].open = false;
            }
        }

        public static int LifetimeMinutes()
        {
            Load();
            int sum = 0;
            for (int i = 0; i < Logs.Count; i++) sum += Logs[i].minutes;
            return sum;
        }

        public static int MinutesOnDate(DateTime day)
        {
            Load();
            string key = day.ToString("yyyy-MM-dd");
            int sum = 0;
            for (int i = 0; i < Logs.Count; i++)
            {
                if (DateKey(Logs[i].startedAt) == key) sum += Logs[i].minutes;
            }
            return sum;
        }

        public static int MinutesForMaterial(string materialId)
        {
            Load();
            if (string.IsNullOrEmpty(materialId)) return 0;
            int sum = 0;
            for (int i = 0; i < Logs.Count; i++)
            {
                if (Logs[i].materialId == materialId) sum += Logs[i].minutes;
            }
            return sum;
        }

        public static int SessionCountForMaterial(string materialId)
        {
            Load();
            if (string.IsNullOrEmpty(materialId)) return 0;
            int n = 0;
            for (int i = 0; i < Logs.Count; i++)
            {
                if (Logs[i].materialId != materialId) continue;
                if (Logs[i].minutes > 0 || Logs[i].open) n++;
            }
            return n;
        }

        public static int CurrentStreakDays()
        {
            Load();
            var days = StudiedBusinessDays();
            var cursor = BusinessDate(Now);
            if (!days.Contains(cursor.ToString("yyyy-MM-dd")))
                cursor = cursor.AddDays(-1);
            int n = 0;
            while (days.Contains(cursor.ToString("yyyy-MM-dd")))
            {
                n++;
                cursor = cursor.AddDays(-1);
            }
            return n;
        }

        public static int BestStreakDays()
        {
            Load();
            var days = StudiedBusinessDays();
            if (days.Count == 0) return 0;
            var list = new List<string>(days);
            list.Sort();
            int best = 1;
            int run = 1;
            for (int i = 1; i < list.Count; i++)
            {
                DateTime a;
                DateTime b;
                if (!DateTime.TryParse(list[i - 1], out a) || !DateTime.TryParse(list[i], out b))
                {
                    run = 1;
                    continue;
                }
                if ((b - a).TotalDays == 1) run++;
                else run = 1;
                if (run > best) best = run;
            }
            int current = CurrentStreakDays();
            return current > best ? current : best;
        }

        static HashSet<string> StudiedBusinessDays()
        {
            var days = new HashSet<string>();
            for (int i = 0; i < Logs.Count; i++)
            {
                if (Logs[i].minutes <= 0 && !Logs[i].open) continue;
                DateTime dt;
                if (!DateTime.TryParse(Logs[i].startedAt, out dt)) continue;
                days.Add(BusinessDate(dt).ToString("yyyy-MM-dd"));
            }
            return days;
        }

        public static int MinutesThisMonth()
        {
            Load();
            var now = Now;
            int sum = 0;
            for (int i = 0; i < Logs.Count; i++)
            {
                DateTime dt;
                if (!DateTime.TryParse(Logs[i].startedAt, out dt)) continue;
                if (dt.Year == now.Year && dt.Month == now.Month) sum += Logs[i].minutes;
            }
            return sum;
        }

        public static int[] SubjectTotals()
        {
            return SubjectTotalsSince(DateTime.MinValue);
        }

        public static int[] SubjectTotalsSince(DateTime fromInclusive)
        {
            Load();
            var totals = new int[6];
            for (int i = 0; i < Logs.Count; i++)
            {
                DateTime dt;
                if (!DateTime.TryParse(Logs[i].startedAt, out dt)) continue;
                if (dt < fromInclusive) continue;
                int s = Logs[i].subject;
                if (s >= 0 && s < 6) totals[s] += Logs[i].minutes;
            }
            return totals;
        }

        public static int[,] Last7DaysBySubject()
        {
            return DaysBySubject(7);
        }

        public static int[,] DaysBySubject(int dayCount)
        {
            Load();
            if (dayCount < 1) dayCount = 1;
            var grid = new int[dayCount, 6];
            var today = Now.Date;
            for (int i = 0; i < Logs.Count; i++)
            {
                DateTime dt;
                if (!DateTime.TryParse(Logs[i].startedAt, out dt)) continue;
                int dayIndex = (int)(today - dt.Date).TotalDays;
                if (dayIndex < 0 || dayIndex >= dayCount) continue;
                int col = dayCount - 1 - dayIndex;
                int s = Logs[i].subject;
                if (s >= 0 && s < 6) grid[col, s] += Logs[i].minutes;
            }
            return grid;
        }

        public static int[,] MonthsBySubject(int monthCount)
        {
            Load();
            if (monthCount < 1) monthCount = 1;
            var grid = new int[monthCount, 6];
            var now = Now;
            var start = new DateTime(now.Year, now.Month, 1).AddMonths(1 - monthCount);
            for (int i = 0; i < Logs.Count; i++)
            {
                DateTime dt;
                if (!DateTime.TryParse(Logs[i].startedAt, out dt)) continue;
                var monthStart = new DateTime(dt.Year, dt.Month, 1);
                int col = (monthStart.Year - start.Year) * 12 + (monthStart.Month - start.Month);
                if (col < 0 || col >= monthCount) continue;
                int s = Logs[i].subject;
                if (s >= 0 && s < 6) grid[col, s] += Logs[i].minutes;
            }
            return grid;
        }

        public static List<StudyLogEntry> LogsNewestFirst()
        {
            Load();
            var list = new List<StudyLogEntry>(Logs);
            list.RemoveAll(l => l.minutes <= 0 && !l.open);
            list.Reverse();
            return list;
        }

        static readonly List<StudyLogEntry> RemoteLogs = new List<StudyLogEntry>();

        public static void ReplaceRemoteLogs(List<StudyLogEntry> logs)
        {
            RemoteLogs.Clear();
            if (logs == null) return;
            for (int i = 0; i < logs.Count; i++)
                if (logs[i] != null) RemoteLogs.Add(logs[i]);
        }

        public static IReadOnlyList<StudyLogEntry> RemoteLogList => RemoteLogs;

        public static string FormatDuration(StudyLogEntry log)
        {
            return FormatMinutes(log.minutes);
        }

        public static string FormatMinutes(int minutes)
        {
            if (minutes < 60) return minutes + "分";
            int h = minutes / 60;
            int m = minutes % 60;
            if (m == 0) return h + "時間";
            return h + "時間" + m + "分";
        }

        public static string FormatLogTime(string iso)
        {
            DateTime dt;
            if (!DateTime.TryParse(iso, out dt)) return iso;
            string week = "日月火水木金土".Substring((int)dt.DayOfWeek, 1);
            return dt.ToString("M月d日(") + week + ") " + dt.ToString("HH:mm");
        }

        static string DateKey(string iso)
        {
            DateTime dt;
            if (!DateTime.TryParse(iso, out dt)) return string.Empty;
            return dt.ToString("yyyy-MM-dd");
        }
    }
}
