using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ShiftingMetropolis.Battle;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    [Serializable]
    public class RaidBossView
    {
        public string room = "";
        public int week = 1;
        public int floor = 1;
        public int clears;
        public int skin;
        public string skinPath = "";
        public string enemyName = "巨影";
        public int maxHp = 1;
        public int hp = 1;
        public int participants;
        public int generation = 1;
        public bool milestone;
    }

    [Serializable]
    public class RaidMemberView
    {
        public string code = "";
        public string name = "";
        public int damage;
        public string lookCsv = "";
        public int generation = 1;
    }

    [Serializable]
    public class RaidGift
    {
        public string id = "";
        public string code = "";
        public int generation;
        public int week = 1;
        public int floor = 1;
        public string itemId = "";
        public string itemName = "";
        public string itemKind = "";
        public int diamonds = RaidRules.ClearDiamonds;
        public int claimed;
        public int remote;
    }

    [Serializable]
    class RaidSaveFile
    {
        public string turnDate;
        public int turnsUsed;
        public int pauseActive;
        public int pauseGen;
        public PausedBattleState pause;
        public int remote;
        public RaidBossView boss;
        public RaidMemberView[] members;
        public RaidGift[] gifts;
    }

    /// <summary>
    /// 端末側のレイド。ターン、中断、プレゼント、オフライン時のボス。
    /// 通信できるときは RaidSync がサーバーの値で上書きする。
    /// </summary>
    public static class RaidStore
    {
        const string FileName = "raid_save.json";

        static bool loaded;
        static string turnDate = "";
        static int turnsUsed;
        static PausedBattleState pause;
        static int pauseGen;
        static readonly List<RaidMemberView> members = new List<RaidMemberView>();
        static readonly List<RaidGift> gifts = new List<RaidGift>();

        public static RaidBossView Boss { get; private set; }
        public static bool Remote { get; private set; }
        public static int BattleGeneration { get; set; }
        public static IReadOnlyList<RaidMemberView> Members => members;
        public static IReadOnlyList<RaidGift> Gifts => gifts;

        public static int Generation => Boss != null ? Boss.generation : 1;

        static string SavePath
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
                        if (File.Exists(Path.Combine(legacy, "study_save.json")))
                            return Path.Combine(legacy, FileName);
                    }
                }
                catch (Exception)
                {
                }
                return Path.Combine(current, FileName);
            }
        }

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            members.Clear();
            gifts.Clear();
            Boss = null;
            Remote = false;
            pause = null;
            pauseGen = 0;
            turnsUsed = 0;
            turnDate = "";
            try
            {
                if (!File.Exists(SavePath)) return;
                var file = JsonUtility.FromJson<RaidSaveFile>(File.ReadAllText(SavePath));
                if (file == null) return;
                turnDate = file.turnDate ?? "";
                turnsUsed = Mathf.Max(0, file.turnsUsed);
                pauseGen = file.pauseGen;
                if (file.pause != null && file.pauseActive == 1) pause = file.pause;
                Remote = file.remote == 1;
                Boss = file.boss;
                if (file.members != null)
                {
                    for (int i = 0; i < file.members.Length; i++)
                        if (file.members[i] != null) members.Add(file.members[i]);
                }
                if (file.gifts != null)
                {
                    for (int i = 0; i < file.gifts.Length; i++)
                        if (file.gifts[i] != null && !string.IsNullOrEmpty(file.gifts[i].id))
                            gifts.Add(file.gifts[i]);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Raid save read failed: " + e.Message);
            }
        }

        public static void Save()
        {
            Load();
            var file = new RaidSaveFile
            {
                turnDate = turnDate,
                turnsUsed = turnsUsed,
                pauseActive = pause != null ? 1 : 0,
                pauseGen = pauseGen,
                pause = pause,
                remote = Remote ? 1 : 0,
                boss = Boss,
                members = members.ToArray(),
                gifts = gifts.ToArray()
            };
            try
            {
                string path = SavePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
                File.WriteAllText(path, JsonUtility.ToJson(file));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Raid save write failed: " + e.Message);
            }
        }

        public static void RollTurns()
        {
            Load();
            string key = StudyStore.BusinessDate(StudyStore.Now).ToString("yyyy-MM-dd");
            if (turnDate == key) return;
            turnDate = key;
            turnsUsed = 0;
            Save();
        }

        public static int TurnsLeft()
        {
            RollTurns();
            return Mathf.Max(0, RaidRules.DailyTurns - turnsUsed);
        }

        public static bool TrySpendTurn()
        {
            RollTurns();
            if (turnsUsed >= RaidRules.DailyTurns) return false;
            turnsUsed++;
            Save();
            return true;
        }

        public static bool HasPause()
        {
            Load();
            if (pause == null || pause.active != 1 || pause.player == null) return false;
            if (Boss != null && pauseGen != Boss.generation)
            {
                pause = null;
                pauseGen = 0;
                Save();
                return false;
            }
            return Boss != null;
        }

        public static PausedBattleState PeekPause()
        {
            return HasPause() ? pause : null;
        }

        public static void SavePause(PausedBattleState state)
        {
            Load();
            pause = state;
            pauseGen = Boss != null ? Boss.generation : pauseGen;
            if (pause != null) pause.active = 1;
            Save();
        }

        public static void ClearPause()
        {
            Load();
            if (pause == null) return;
            pause = null;
            pauseGen = 0;
            Save();
        }

        public static int UnclaimedCount()
        {
            Load();
            string me = StudyStore.MyFriendCode();
            int n = 0;
            for (int i = 0; i < gifts.Count; i++)
            {
                var gift = gifts[i];
                if (gift == null || gift.claimed == 1) continue;
                if (!string.IsNullOrEmpty(me) && gift.code != me) continue;
                n++;
            }
            return n;
        }

        public static List<RaidGift> Unclaimed()
        {
            Load();
            string me = StudyStore.MyFriendCode();
            var list = new List<RaidGift>();
            for (int i = gifts.Count - 1; i >= 0; i--)
            {
                var gift = gifts[i];
                if (gift == null || gift.claimed == 1) continue;
                if (!string.IsNullOrEmpty(me) && gift.code != me) continue;
                list.Add(gift);
            }
            return list;
        }

        public static RaidGift FindGift(string id)
        {
            Load();
            for (int i = 0; i < gifts.Count; i++)
                if (gifts[i] != null && gifts[i].id == id) return gifts[i];
            return null;
        }

        public static void RememberGift(RaidGift gift)
        {
            if (gift == null || string.IsNullOrEmpty(gift.id)) return;
            Load();
            if (FindGift(gift.id) != null) return;
            gifts.Add(gift);
            Save();
        }

        public static void MarkGiftClaimed(string id)
        {
            var gift = FindGift(id);
            if (gift == null) return;
            gift.claimed = 1;
            Save();
        }

        public static void EnsureLocal(string room)
        {
            Load();
            if (Boss != null && !string.IsNullOrEmpty(Boss.room))
            {
                if (Remote)
                {
                    Remote = false;
                    Save();
                }
                RetunePool();
                return;
            }
            Remote = false;
            Boss = MakeBoss(string.IsNullOrEmpty(room) ? "local" : room, 1, 1, 0, 1);
            members.Clear();
            Save();
        }

        public static bool ResetLocalToFloorOne()
        {
            Load();
            if (Boss == null || Boss.floor == 1) return false;
            Boss = MakeBoss(Boss.room, 1, 1, 0, Boss.generation + 1);
            members.Clear();
            Remote = false;
            Save();
            return true;
        }

        public static RaidBossView MakeBoss(string room, int week, int floor, int clears, int generation)
        {
            var pick = HeroAppearance.PickRaidOpponent(week, floor, generation, room);
            int per = RaidRules.PerHead(week, floor, clears);
            string name = pick.label;
            if (pick.milestone && name.IndexOf("主", StringComparison.Ordinal) < 0)
                name = "主・" + name;
            return new RaidBossView
            {
                room = room ?? "",
                week = Mathf.Max(1, week),
                floor = Mathf.Clamp(floor, 1, StudyStore.DungeonMaxFloor),
                clears = Mathf.Max(0, clears),
                skin = pick.look,
                skinPath = pick.path ?? "",
                enemyName = name,
                maxHp = per,
                hp = per,
                participants = 0,
                generation = Mathf.Max(1, generation),
                milestone = pick.milestone
            };
        }

        public static void AdoptRemote(RaidBossView boss, List<RaidMemberView> ranking)
        {
            Load();
            Remote = true;
            Boss = boss;
            members.Clear();
            if (ranking != null)
            {
                for (int i = 0; i < ranking.Count; i++)
                    if (ranking[i] != null) members.Add(ranking[i]);
            }
            Save();
        }

        public static void AdoptLocal(RaidBossView boss, List<RaidMemberView> ranking)
        {
            Load();
            Remote = false;
            Boss = boss;
            members.Clear();
            if (ranking != null)
            {
                for (int i = 0; i < ranking.Count; i++)
                    if (ranking[i] != null) members.Add(ranking[i]);
            }
            Save();
        }

        public static RaidMemberView FindMember(string code)
        {
            Load();
            for (int i = 0; i < members.Count; i++)
                if (members[i] != null && members[i].code == code) return members[i];
            return null;
        }

        public static void UpsertMember(string code, string name, string look, int addDamage)
        {
            Load();
            if (string.IsNullOrEmpty(code) || Boss == null) return;
            var member = FindMember(code);
            if (member == null)
            {
                member = new RaidMemberView
                {
                    code = code,
                    generation = Boss.generation
                };
                members.Add(member);
            }
            if (!string.IsNullOrEmpty(name)) member.name = name;
            if (!string.IsNullOrEmpty(look)) member.lookCsv = look;
            if (addDamage > 0)
            {
                long sum = (long)member.damage + addDamage;
                member.damage = sum > int.MaxValue ? int.MaxValue : (int)sum;
            }
            member.generation = Boss.generation;
        }

        public static void RetunePool()
        {
            Load();
            if (Boss == null || Remote) return;
            int count = 0;
            for (int i = 0; i < members.Count; i++)
                if (members[i] != null && members[i].generation == Boss.generation) count++;
            if (count <= 0) count = Mathf.Max(0, Boss.participants);
            int per = RaidRules.PerHead(Boss.week, Boss.floor, Boss.clears);
            int newMax = count <= 0 ? per : RaidRules.PoolHp(per, count);
            if (newMax == Boss.maxHp) return;
            int newHp = RaidRules.ScaleRemaining(Boss.hp, Boss.maxHp, newMax);
            Boss.maxHp = newMax;
            Boss.hp = newHp;
            Boss.participants = count;
            Save();
        }

        public static void ReconcileLocal()
        {
            Load();
            if (Boss == null || Remote) return;
            int count = 0;
            for (int i = 0; i < members.Count; i++)
                if (members[i] != null && members[i].generation == Boss.generation) count++;
            if (count <= 0)
            {
                Boss.participants = 0;
                Save();
                return;
            }
            int per = RaidRules.PerHead(Boss.week, Boss.floor, Boss.clears);
            int newMax = RaidRules.PoolHp(per, count);
            int newHp = RaidRules.ScaleRemaining(Boss.hp, Boss.maxHp, newMax);
            Boss.participants = count;
            Boss.maxHp = newMax;
            Boss.hp = newHp;
            Save();
        }

        public static bool ApplyLocalDamage(string code, string name, string look, int amount)
        {
            Load();
            EnsureLocal(Boss != null ? Boss.room : "local");
            if (Boss == null || amount <= 0) return false;
            UpsertMember(code, name, look, amount);
            ReconcileLocal();
            Boss.hp = Mathf.Max(0, Boss.hp - amount);
            bool dead = Boss.hp <= 0;
            if (dead) SettleLocal();
            else Save();
            return dead;
        }

        public static void SettleLocal()
        {
            Load();
            if (Boss == null) return;
            int gen = Boss.generation;
            var rewarded = new List<RaidMemberView>();
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] != null && members[i].generation == gen)
                    rewarded.Add(members[i]);
            }
            for (int i = 0; i < rewarded.Count; i++)
                AddGiftFor(Boss, rewarded[i].code);
            Advance(Boss);
            members.Clear();
            Save();
        }

        public static void AddGiftFor(RaidBossView boss, string code)
        {
            if (boss == null || string.IsNullOrEmpty(code)) return;
            string id = GiftId(boss.room, boss.generation, code);
            if (FindGift(id) != null) return;
            var roll = GachaCatalog.RollGift(GiftSeed(boss.generation, code));
            gifts.Add(new RaidGift
            {
                id = id,
                code = code,
                generation = boss.generation,
                week = boss.week,
                floor = boss.floor,
                itemId = roll.id,
                itemName = roll.name,
                itemKind = roll.kind,
                diamonds = RaidRules.ClearDiamonds,
                claimed = 0,
                remote = 0
            });
        }

        public static void Advance(RaidBossView boss)
        {
            if (boss == null) return;
            int week = boss.week;
            int floor = boss.floor + 1;
            if (floor > StudyStore.DungeonMaxFloor)
            {
                floor = 1;
                week = Mathf.Min(StudyStore.MaxDungeonWeek, week + 1);
            }
            int clears = boss.clears + 1;
            int generation = boss.generation + 1;
            var next = MakeBoss(boss.room, week, floor, clears, generation);
            next.participants = 0;
            Boss = next;
        }

        public static int GiftSeed(int generation, string code)
        {
            return HeroAppearance.StableHash(generation + ":" + (code ?? ""));
        }

        public static string GiftId(string room, int generation, string code)
        {
            return (room ?? "") + ":" + generation + ":" + (code ?? "");
        }

        public static List<RaidMemberView> Ranked()
        {
            Load();
            var list = new List<RaidMemberView>();
            int gen = Boss != null ? Boss.generation : 0;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] == null) continue;
                if (gen > 0 && members[i].generation != gen) continue;
                list.Add(members[i]);
            }
            list.Sort((a, b) =>
            {
                int d = b.damage.CompareTo(a.damage);
                if (d != 0) return d;
                return string.Compare(a.name, b.name, StringComparison.Ordinal);
            });
            return list;
        }
    }
}
