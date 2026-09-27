using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ShiftingMetropolis.Battle;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    /// <summary>
    /// 部屋全員で一体。HPの増減だけサーバーに書き、見た目と自分の戦闘は端末側。
    /// </summary>
    public static class RaidSync
    {
        const string RaidFloorResetKey = "takestudyquest_raid_floor_reset_v1";
        public static bool Fighting;
        public static bool Cleared;
        public static int SessionDamage;
        public static string StatusNote = "";

        static int pending;
        static int pendingGen;
        static bool serverMissing;
        static bool flushing;

        [Serializable]
        class BossRow
        {
            public string room;
            public int week;
            public int floor;
            public int clears;
            public int skin;
            public string skin_path;
            public string enemy_name;
            public int max_hp;
            public int hp;
            public int participants;
            public int generation;
        }

        [Serializable]
        class BossList
        {
            public BossRow[] items;
        }

        [Serializable]
        class MemberRow
        {
            public string code;
            public string name;
            public int damage;
            public string look_csv;
            public int generation;
        }

        [Serializable]
        class MemberList
        {
            public MemberRow[] items;
        }

        [Serializable]
        class GiftRow
        {
            public string id;
            public string code;
            public int generation;
            public int week;
            public int floor;
            public string item_id;
            public string item_name;
            public string item_kind;
            public int diamonds;
            public int claimed;
        }

        [Serializable]
        class GiftList
        {
            public GiftRow[] items;
        }

        public static void ResetBattleSession()
        {
            Cleared = false;
            SessionDamage = 0;
            Fighting = true;
        }

        public static void Report(int amount)
        {
            if (amount <= 0 || !Fighting) return;
            if (pending <= 0) pendingGen = RaidStore.BattleGeneration;
            pending += amount;
            SessionDamage += amount;
        }

        public static IEnumerator Flush()
        {
            while (flushing) yield return null;
            flushing = true;
            while (pending > 0)
            {
                int chunk = pending;
                bool ok = false;
                yield return SendHit(chunk, value => ok = value);
                if (!ok)
                {
                    flushing = false;
                    yield break;
                }
                pending -= chunk;
                if (pending <= 0)
                {
                    pending = 0;
                    pendingGen = 0;
                }
            }
            flushing = false;
        }

        public static IEnumerator Refresh()
        {
            yield return Flush();
            RaidStore.Load();
            if (!SupabaseSync.Ready || serverMissing)
            {
                RaidStore.EnsureLocal(Room());
                if (!PlayerPrefs.HasKey(RaidFloorResetKey) && RaidStore.ResetLocalToFloorOne())
                {
                    PlayerPrefs.SetInt(RaidFloorResetKey, 1);
                    PlayerPrefs.Save();
                }
                StatusNote = SupabaseSync.Ready
                    ? "共有テーブルがまだないので、この端末のレイドです"
                    : "通信なし。この端末のレイドです";
                yield break;
            }
            StatusNote = "";
            BossRow row = null;
            yield return ReadBoss(value => row = value);
            if (serverMissing || row == null && SupabaseSync.LastHttp >= 400)
            {
                RaidStore.EnsureLocal(Room());
                if (!PlayerPrefs.HasKey(RaidFloorResetKey) && RaidStore.ResetLocalToFloorOne())
                {
                    PlayerPrefs.SetInt(RaidFloorResetKey, 1);
                    PlayerPrefs.Save();
                }
                StatusNote = "共有テーブルがまだないので、この端末のレイドです";
                yield break;
            }
            if (row == null)
            {
                var created = RaidStore.MakeBoss(Room(), 1, 1, 0, 1);
                bool inserted = false;
                yield return InsertBoss(created, ok => inserted = ok);
                if (!inserted)
                {
                    yield return ReadBoss(value => row = value);
                    if (row == null)
                    {
                        RaidStore.EnsureLocal(Room());
                        if (!PlayerPrefs.HasKey(RaidFloorResetKey) && RaidStore.ResetLocalToFloorOne())
                        {
                            PlayerPrefs.SetInt(RaidFloorResetKey, 1);
                            PlayerPrefs.Save();
                        }
                        StatusNote = "レイドの作成に失敗したので、この端末のレイドです";
                        yield break;
                    }
                }
                else row = ToRow(created);
            }
            if (!PlayerPrefs.HasKey(RaidFloorResetKey) && row.floor != 1)
            {
                var reset = RaidStore.MakeBoss(row.room, 1, 1, 0, row.generation + 1);
                bool resetOk = false;
                string resetPath = "/rest/v1/raid_bosses?room=eq." + UnityWebRequest.EscapeURL(row.room)
                    + "&generation=eq." + row.generation;
                yield return SupabaseSync.Rest("PATCH", resetPath, BossJson(reset), "return=representation", text =>
                {
                    resetOk = !string.IsNullOrEmpty(text) && text != "[]";
                });
                if (resetOk)
                {
                    PlayerPrefs.SetInt(RaidFloorResetKey, 1);
                    PlayerPrefs.Save();
                    row = ToRow(reset);
                }
            }
            if (row.hp <= 0)
            {
                var dead = FromRow(row);
                yield return Settle(dead);
                yield return ReadBoss(value => row = value);
                if (row == null) row = ToRow(RaidStore.Boss);
            }
            var boss = FromRow(row);
            var ranking = new List<RaidMemberView>();
            yield return ReadMembers(boss.generation, ranking);
            bool wrote = false;
            yield return Reconcile(boss, ranking, ok => wrote = ok);
            if (wrote)
                yield return ReadBoss(value => { if (value != null) boss = FromRow(value); });
            RaidStore.AdoptRemote(boss, ranking);
            yield return PullGifts();
        }

        public static IEnumerator JoinSelf()
        {
            yield return Refresh();
            string code = StudyStore.MyFriendCode();
            if (string.IsNullOrEmpty(code) || RaidStore.Boss == null) yield break;
            if (!RaidStore.Remote)
            {
                RaidStore.UpsertMember(code, StudyStore.ProfileName(), StudyStore.MyLookCsv(), 0);
                RaidStore.ReconcileLocal();
                yield break;
            }
            bool inserted = false;
            yield return InsertMember(code, StudyStore.ProfileName(), StudyStore.MyLookCsv(), 0, true, ok => inserted = ok);
            if (!inserted)
                yield return TouchMember(code, StudyStore.ProfileName(), StudyStore.MyLookCsv());
            var ranking = new List<RaidMemberView>();
            yield return ReadMembers(RaidStore.Boss.generation, ranking);
            bool wrote = false;
            yield return Reconcile(RaidStore.Boss, ranking, ok => wrote = ok);
            if (wrote)
            {
                BossRow row = null;
                yield return ReadBoss(value => row = value);
                if (row != null) RaidStore.AdoptRemote(FromRow(row), ranking);
            }
            else RaidStore.AdoptRemote(RaidStore.Boss, ranking);
        }

        public static IEnumerator Claim(RaidGift gift, Action<string> done)
        {
            if (gift == null || gift.claimed == 1)
            {
                if (done != null) done("");
                yield break;
            }
            if (gift.remote == 1 && SupabaseSync.Ready && !serverMissing)
            {
                bool updated = false;
                string path = "/rest/v1/raid_presents?id=eq." + UnityWebRequest.EscapeURL(gift.id) + "&claimed=eq.0";
                yield return SupabaseSync.Rest("PATCH", path, "{\"claimed\":1}", "return=representation", text =>
                {
                    updated = !string.IsNullOrEmpty(text) && text.Length > 2 && text != "[]";
                });
                if (!updated && SupabaseSync.LastHttp < 400)
                {
                    RaidStore.MarkGiftClaimed(gift.id);
                    if (done != null) done("受け取り済みです");
                    yield break;
                }
            }
            string line = GachaCatalog.GrantGift(gift.itemId, gift.itemKind);
            StudyStore.AddDiamonds(gift.diamonds > 0 ? gift.diamonds : RaidRules.ClearDiamonds);
            RaidStore.MarkGiftClaimed(gift.id);
            if (string.IsNullOrEmpty(line)) line = "ダイヤ +" + RaidRules.ClearDiamonds;
            else line += "\nダイヤ +" + RaidRules.ClearDiamonds;
            if (done != null) done(line);
        }

        static IEnumerator SendHit(int damage, Action<bool> done)
        {
            if (damage <= 0)
            {
                if (done != null) done(true);
                yield break;
            }
            if (!RaidStore.Remote || !SupabaseSync.Ready || serverMissing)
            {
                bool dead = RaidStore.ApplyLocalDamage(
                    StudyStore.MyFriendCode(), StudyStore.ProfileName(), StudyStore.MyLookCsv(), damage);
                if (dead) Cleared = true;
                if (done != null) done(true);
                yield break;
            }
            for (int attempt = 0; attempt < 5; attempt++)
            {
                BossRow row = null;
                yield return ReadBoss(value => row = value);
                if (row == null)
                {
                    if (done != null) done(false);
                    yield break;
                }
                int fightGen = RaidStore.BattleGeneration;
                if (fightGen > 0 && row.generation != fightGen)
                {
                    Cleared = true;
                    RaidStore.AdoptRemote(FromRow(row), null);
                    if (done != null) done(true);
                    yield break;
                }
                if (pendingGen > 0 && row.generation != pendingGen)
                {
                    if (done != null) done(true);
                    yield break;
                }
                if (row.hp <= 0)
                {
                    yield return Settle(FromRow(row));
                    Cleared = true;
                    if (done != null) done(true);
                    yield break;
                }
                int next = Mathf.Max(0, row.hp - damage);
                bool wrote = false;
                string path = "/rest/v1/raid_bosses?room=eq." + UnityWebRequest.EscapeURL(row.room)
                    + "&generation=eq." + row.generation + "&hp=eq." + row.hp;
                yield return SupabaseSync.Rest("PATCH", path, "{\"hp\":" + next + "}", "return=representation", text =>
                {
                    wrote = !string.IsNullOrEmpty(text) && text != "[]";
                });
                if (!wrote) continue;
                yield return AddDamage(row.room, row.generation, damage);
                var updated = FromRow(row);
                updated.hp = next;
                RaidStore.AdoptRemote(updated, CopyMembers());
                if (next <= 0)
                {
                    yield return Settle(updated);
                    Cleared = true;
                }
                if (done != null) done(true);
                yield break;
            }
            if (done != null) done(false);
        }

        static List<RaidMemberView> CopyMembers()
        {
            var list = new List<RaidMemberView>();
            var src = RaidStore.Members;
            for (int i = 0; i < src.Count; i++)
                if (src[i] != null) list.Add(src[i]);
            return list;
        }

        static IEnumerator Settle(RaidBossView dead)
        {
            if (dead == null) yield break;
            var ranking = new List<RaidMemberView>();
            yield return ReadMembers(dead.generation, ranking);
            if (ranking.Count == 0)
            {
                var mine = RaidStore.FindMember(StudyStore.MyFriendCode());
                if (mine != null) ranking.Add(mine);
            }
            for (int i = 0; i < ranking.Count; i++)
            {
                var member = ranking[i];
                if (member == null || string.IsNullOrEmpty(member.code)) continue;
                var roll = GachaCatalog.RollGift(RaidStore.GiftSeed(dead.generation, member.code));
                string id = RaidStore.GiftId(dead.room, dead.generation, member.code);
                RaidStore.RememberGift(new RaidGift
                {
                    id = id,
                    code = member.code,
                    generation = dead.generation,
                    week = dead.week,
                    floor = dead.floor,
                    itemId = roll.id,
                    itemName = roll.name,
                    itemKind = roll.kind,
                    diamonds = RaidRules.ClearDiamonds,
                    claimed = 0,
                    remote = 1
                });
                string body = "{"
                    + "\"id\":\"" + Esc(id) + "\","
                    + "\"code\":\"" + Esc(member.code) + "\","
                    + "\"room\":\"" + Esc(dead.room) + "\","
                    + "\"generation\":" + dead.generation + ","
                    + "\"week\":" + dead.week + ","
                    + "\"floor\":" + dead.floor + ","
                    + "\"item_id\":\"" + Esc(roll.id) + "\","
                    + "\"item_name\":\"" + Esc(roll.name) + "\","
                    + "\"item_kind\":\"" + Esc(roll.kind) + "\","
                    + "\"diamonds\":" + RaidRules.ClearDiamonds + ","
                    + "\"claimed\":0}";
                yield return SupabaseSync.Rest(
                    "POST", "/rest/v1/raid_presents", body, "resolution=ignore-duplicates,return=minimal", null);
            }
            var next = RaidStore.MakeBoss(dead.room, NextWeek(dead), NextFloor(dead), dead.clears + 1, dead.generation + 1);
            string patch = BossJson(next);
            string filter = "/rest/v1/raid_bosses?room=eq." + UnityWebRequest.EscapeURL(dead.room)
                + "&generation=eq." + dead.generation;
            bool advanced = false;
            yield return SupabaseSync.Rest("PATCH", filter, patch, "return=representation", text =>
            {
                advanced = !string.IsNullOrEmpty(text) && text != "[]";
            });
            if (advanced) RaidStore.AdoptRemote(next, new List<RaidMemberView>());
        }

        static int NextFloor(RaidBossView boss)
        {
            int floor = boss.floor + 1;
            return floor > StudyStore.DungeonMaxFloor ? 1 : floor;
        }

        static int NextWeek(RaidBossView boss)
        {
            if (boss.floor + 1 > StudyStore.DungeonMaxFloor)
                return Mathf.Min(StudyStore.MaxDungeonWeek, boss.week + 1);
            return boss.week;
        }

        static IEnumerator Reconcile(RaidBossView boss, List<RaidMemberView> ranking, Action<bool> wrote)
        {
            if (boss == null || ranking == null)
            {
                if (wrote != null) wrote(false);
                yield break;
            }
            int count = ranking.Count;
            if (count <= 0)
            {
                if (wrote != null) wrote(false);
                yield break;
            }
            int per = RaidRules.PerHead(boss.week, boss.floor, boss.clears);
            int newMax = RaidRules.PoolHp(per, count);
            int newHp = RaidRules.ScaleRemaining(boss.hp, boss.maxHp, newMax);
            if (boss.participants == count && boss.maxHp == newMax && boss.hp == newHp)
            {
                if (wrote != null) wrote(false);
                yield break;
            }
            string path = "/rest/v1/raid_bosses?room=eq." + UnityWebRequest.EscapeURL(boss.room)
                + "&generation=eq." + boss.generation
                + "&hp=eq." + boss.hp
                + "&max_hp=eq." + boss.maxHp;
            string body = "{\"participants\":" + count + ",\"max_hp\":" + newMax + ",\"hp\":" + newHp + "}";
            bool ok = false;
            yield return SupabaseSync.Rest("PATCH", path, body, "return=representation", text =>
            {
                ok = !string.IsNullOrEmpty(text) && text != "[]";
            });
            if (ok)
            {
                boss.participants = count;
                boss.maxHp = newMax;
                boss.hp = newHp;
            }
            if (wrote != null) wrote(ok);
        }

        static IEnumerator AddDamage(string room, int generation, int amount)
        {
            string code = StudyStore.MyFriendCode();
            MemberRow mine = null;
            string path = "/rest/v1/raid_members?select=code,name,damage,look_csv,generation&room=eq."
                + UnityWebRequest.EscapeURL(room)
                + "&generation=eq." + generation
                + "&code=eq." + UnityWebRequest.EscapeURL(code);
            string json = null;
            yield return SupabaseSync.Rest("GET", path, null, null, text => json = text);
            var rows = ParseMembers(json);
            if (rows.Count > 0) mine = rows[0];
            int old = mine != null ? mine.damage : 0;
            long sum = (long)old + amount;
            int next = sum > int.MaxValue ? int.MaxValue : (int)sum;
            if (mine == null)
            {
                yield return InsertMember(code, StudyStore.ProfileName(), StudyStore.MyLookCsv(), next, false, null);
                yield break;
            }
            string patchPath = "/rest/v1/raid_members?room=eq." + UnityWebRequest.EscapeURL(room)
                + "&generation=eq." + generation
                + "&code=eq." + UnityWebRequest.EscapeURL(code)
                + "&damage=eq." + old;
            string body = "{\"damage\":" + next
                + ",\"name\":\"" + Esc(StudyStore.ProfileName())
                + "\",\"look_csv\":\"" + Esc(StudyStore.MyLookCsv()) + "\"}";
            yield return SupabaseSync.Rest("PATCH", patchPath, body, "return=minimal", null);
            var member = RaidStore.FindMember(code);
            if (member != null) member.damage = next;
        }

        static IEnumerator InsertMember(string code, string name, string look, int damage, bool ignoreDup, Action<bool> inserted)
        {
            var boss = RaidStore.Boss;
            if (boss == null)
            {
                if (inserted != null) inserted(false);
                yield break;
            }
            string body = "{"
                + "\"room\":\"" + Esc(boss.room) + "\","
                + "\"generation\":" + boss.generation + ","
                + "\"code\":\"" + Esc(code) + "\","
                + "\"name\":\"" + Esc(name) + "\","
                + "\"damage\":" + damage + ","
                + "\"look_csv\":\"" + Esc(look) + "\"}";
            string prefer = ignoreDup
                ? "resolution=ignore-duplicates,return=representation"
                : "return=representation";
            bool created = false;
            yield return SupabaseSync.Rest("POST", "/rest/v1/raid_members", body, prefer, text =>
            {
                created = !string.IsNullOrEmpty(text) && text != "[]";
            });
            if (inserted != null) inserted(created && SupabaseSync.LastHttp < 400);
        }

        static IEnumerator TouchMember(string code, string name, string look)
        {
            var boss = RaidStore.Boss;
            if (boss == null) yield break;
            string path = "/rest/v1/raid_members?room=eq." + UnityWebRequest.EscapeURL(boss.room)
                + "&generation=eq." + boss.generation
                + "&code=eq." + UnityWebRequest.EscapeURL(code);
            string body = "{\"name\":\"" + Esc(name) + "\",\"look_csv\":\"" + Esc(look) + "\"}";
            yield return SupabaseSync.Rest("PATCH", path, body, "return=minimal", null);
        }

        static IEnumerator InsertBoss(RaidBossView boss, Action<bool> done)
        {
            bool ok = false;
            yield return SupabaseSync.Rest("POST", "/rest/v1/raid_bosses", BossJson(boss), "return=representation", text =>
            {
                ok = SupabaseSync.LastHttp < 300 && !string.IsNullOrEmpty(text) && text != "[]";
            });
            if (SupabaseSync.LastHttp == 404) serverMissing = true;
            if (done != null) done(ok);
        }

        static IEnumerator ReadBoss(Action<BossRow> done)
        {
            string path = "/rest/v1/raid_bosses?select=room,week,floor,clears,skin,skin_path,enemy_name,max_hp,hp,participants,generation&room=eq."
                + UnityWebRequest.EscapeURL(Room());
            string json = null;
            yield return SupabaseSync.Rest("GET", path, null, null, text => json = text);
            if (SupabaseSync.LastHttp == 404)
            {
                serverMissing = true;
                if (done != null) done(null);
                yield break;
            }
            var list = ParseBosses(json);
            if (done != null) done(list.Count > 0 ? list[0] : null);
        }

        static IEnumerator ReadMembers(int generation, List<RaidMemberView> into)
        {
            into.Clear();
            string path = "/rest/v1/raid_members?select=code,name,damage,look_csv,generation&room=eq."
                + UnityWebRequest.EscapeURL(Room())
                + "&generation=eq." + generation;
            string json = null;
            yield return SupabaseSync.Rest("GET", path, null, null, text => json = text);
            var rows = ParseMembers(json);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || string.IsNullOrEmpty(row.code)) continue;
                into.Add(new RaidMemberView
                {
                    code = row.code,
                    name = row.name ?? "",
                    damage = Mathf.Max(0, row.damage),
                    lookCsv = row.look_csv ?? "",
                    generation = generation
                });
            }
        }

        static IEnumerator PullGifts()
        {
            string code = StudyStore.MyFriendCode();
            if (string.IsNullOrEmpty(code)) yield break;
            string path = "/rest/v1/raid_presents?select=id,code,generation,week,floor,item_id,item_name,item_kind,diamonds,claimed&code=eq."
                + UnityWebRequest.EscapeURL(code);
            string json = null;
            yield return SupabaseSync.Rest("GET", path, null, null, text => json = text);
            var rows = ParseGifts(json);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || string.IsNullOrEmpty(row.id)) continue;
                var existing = RaidStore.FindGift(row.id);
                if (existing != null)
                {
                    if (row.claimed == 1) existing.claimed = 1;
                    continue;
                }
                RaidStore.RememberGift(new RaidGift
                {
                    id = row.id,
                    code = row.code,
                    generation = row.generation,
                    week = row.week,
                    floor = row.floor,
                    itemId = row.item_id ?? "",
                    itemName = row.item_name ?? "",
                    itemKind = row.item_kind ?? "",
                    diamonds = row.diamonds > 0 ? row.diamonds : RaidRules.ClearDiamonds,
                    claimed = row.claimed == 1 ? 1 : 0,
                    remote = 1
                });
            }
        }

        static string Room()
        {
            string room = SupabaseSync.Room();
            return string.IsNullOrEmpty(room) ? "takestudy" : room;
        }

        static string BossJson(RaidBossView boss)
        {
            return "{"
                + "\"room\":\"" + Esc(boss.room) + "\","
                + "\"week\":" + boss.week + ","
                + "\"floor\":" + boss.floor + ","
                + "\"clears\":" + boss.clears + ","
                + "\"skin\":" + boss.skin + ","
                + "\"skin_path\":\"" + Esc(boss.skinPath) + "\","
                + "\"enemy_name\":\"" + Esc(boss.enemyName) + "\","
                + "\"max_hp\":" + boss.maxHp + ","
                + "\"hp\":" + boss.hp + ","
                + "\"participants\":" + boss.participants + ","
                + "\"generation\":" + boss.generation + "}";
        }

        static BossRow ToRow(RaidBossView boss)
        {
            if (boss == null) return null;
            return new BossRow
            {
                room = boss.room,
                week = boss.week,
                floor = boss.floor,
                clears = boss.clears,
                skin = boss.skin,
                skin_path = boss.skinPath,
                enemy_name = boss.enemyName,
                max_hp = boss.maxHp,
                hp = boss.hp,
                participants = boss.participants,
                generation = boss.generation
            };
        }

        static RaidBossView FromRow(BossRow row)
        {
            if (row == null) return null;
            bool milestone = row.floor % 10 == 0;
            return new RaidBossView
            {
                room = row.room ?? "",
                week = Mathf.Max(1, row.week),
                floor = Mathf.Clamp(row.floor <= 0 ? 1 : row.floor, 1, StudyStore.DungeonMaxFloor),
                clears = Mathf.Max(0, row.clears),
                skin = row.skin,
                skinPath = row.skin_path ?? "",
                enemyName = string.IsNullOrEmpty(row.enemy_name) ? "巨影" : row.enemy_name,
                maxHp = Mathf.Max(1, row.max_hp),
                hp = Mathf.Clamp(row.hp, 0, Mathf.Max(1, row.max_hp)),
                participants = Mathf.Max(0, row.participants),
                generation = Mathf.Max(1, row.generation),
                milestone = milestone
            };
        }

        static List<BossRow> ParseBosses(string json)
        {
            var list = new List<BossRow>();
            if (string.IsNullOrEmpty(json) || json.Length < 2 || json[0] != '[') return list;
            var parsed = JsonUtility.FromJson<BossList>("{\"items\":" + json + "}");
            if (parsed == null || parsed.items == null) return list;
            for (int i = 0; i < parsed.items.Length; i++)
                if (parsed.items[i] != null) list.Add(parsed.items[i]);
            return list;
        }

        static List<MemberRow> ParseMembers(string json)
        {
            var list = new List<MemberRow>();
            if (string.IsNullOrEmpty(json) || json.Length < 2 || json[0] != '[') return list;
            var parsed = JsonUtility.FromJson<MemberList>("{\"items\":" + json + "}");
            if (parsed == null || parsed.items == null) return list;
            for (int i = 0; i < parsed.items.Length; i++)
                if (parsed.items[i] != null) list.Add(parsed.items[i]);
            return list;
        }

        static List<GiftRow> ParseGifts(string json)
        {
            var list = new List<GiftRow>();
            if (string.IsNullOrEmpty(json) || json.Length < 2 || json[0] != '[') return list;
            var parsed = JsonUtility.FromJson<GiftList>("{\"items\":" + json + "}");
            if (parsed == null || parsed.items == null) return list;
            for (int i = 0; i < parsed.items.Length; i++)
                if (parsed.items[i] != null) list.Add(parsed.items[i]);
            return list;
        }

        static string Esc(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }
    }
}
