using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ShiftingMetropolis.App;
using ShiftingMetropolis.Battle;
using ShiftingMetropolis.Progress;
using UnityEditor;
using UnityEngine;

namespace ShiftingMetropolis.EditorTools
{
    public static class WeekTwentyRun
    {
        struct Offer
        {
            public string id;
            public string slot;
            public int cost;
        }

        public static void Run()
        {
            string backupJson = null;
            string backupFile = null;
            string iconBackup = Path.Combine("Temp", "week20-icon-backup.png");
            bool hadIcon = false;
            try
            {
                backupJson = PlayerPrefs.GetString("sm_study_store_v2", "");
                string persistent = Application.persistentDataPath;
                string legacy = Path.Combine(Directory.GetParent(persistent).FullName, "peach natsu");
                foreach (string dir in new[] { legacy, persistent })
                {
                    string path = Path.Combine(dir, "study_save.json");
                    if (File.Exists(path))
                    {
                        backupFile = path;
                        backupJson = File.ReadAllText(path);
                        break;
                    }
                }
                string icon = StudyStore.ProfileIconPath;
                if (File.Exists(icon))
                {
                    hadIcon = true;
                    Directory.CreateDirectory("Temp");
                    File.Copy(icon, iconBackup, true);
                }

                StudyStore.EraseAllProgress();
                var shop = Shop();
                Array.Sort(shop, (a, b) => a.cost.CompareTo(b.cost));
                var bugs = new List<string>();
                var lines = new List<string>();
                int pulls = 0;
                int refunds = 0;
                int earned = 0;
                var offers = shop;

                for (int n = 0; n < 2000; n++)
                {
                    int week = StudyStore.DungeonWeek;
                    int floor = StudyStore.DungeonFloor;
                    int need = StudyStore.MinutesForTower(week, floor);
                    int have = StudyStore.TotalEarnedBp;
                    if (need > have) StudyStore.GrantBp(need - have);
                    if (!StudyStore.TrySpendForBattle())
                        bugs.Add(week + "周" + floor + "階で出撃BPが足りない");
                    StudyStore.AddDiamonds(StudyStore.WinDiamonds);
                    earned += StudyStore.WinDiamonds;
                    StudyStore.AdvanceFloorOnWin();
                    if (floor % 20 == 0)
                    {
                        BuyAll(offers);
                        while (StudyStore.Diamonds >= GachaCatalog.PullCost)
                        {
                            var result = GachaCatalog.Pull();
                            if (result == null || !result.ok)
                            {
                                bugs.Add(week + "周" + floor + "階でガチャ失敗 " + (result != null ? result.message : ""));
                                break;
                            }
                            pulls++;
                            refunds += result.refund;
                        }
                        SpendStats();
                        EquipBest();
                        lines.Add(Line(week, floor, pulls, refunds, earned));
                    }
                    if (n == 899 && StudyStore.DungeonWeek < 10)
                        bugs.Add("900勝時点でまだ" + StudyStore.DungeonWeek + "周。上限が残っている");
                }

                if (StudyStore.DungeonWeek < 21)
                    bugs.Add("2000勝後が" + StudyStore.DungeonWeek + "周" + StudyStore.DungeonFloor + "階。20周を抜けていない");

                var sb = new StringBuilder();
                sb.Append("{\"earned\":").Append(earned);
                sb.Append(",\"diamondsLeft\":").Append(StudyStore.Diamonds);
                sb.Append(",\"pulls\":").Append(pulls);
                sb.Append(",\"refunds\":").Append(refunds);
                sb.Append(",\"week\":").Append(StudyStore.DungeonWeek);
                sb.Append(",\"floor\":").Append(StudyStore.DungeonFloor);
                sb.Append(",\"level\":").Append(StudyStore.PlayerLevel());
                sb.Append(",\"hp\":").Append(StudyStore.BattleMaxHp());
                sb.Append(",\"atk\":").Append(StudyStore.BattleAttack());
                sb.Append(",\"def\":").Append(StudyStore.BattleDefense());
                sb.Append(",\"spd\":").Append(StudyStore.BattleSpeed());
                sb.Append(",\"luck\":").Append(StudyStore.BattleLuck());
                sb.Append(",\"bugs\":[");
                for (int i = 0; i < bugs.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(bugs[i].Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
                }
                sb.Append("],\"rows\":[");
                for (int i = 0; i < lines.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(lines[i]);
                }
                sb.Append("]}");
                Directory.CreateDirectory("Temp");
                File.WriteAllText(Path.Combine("Temp", "week20-run.json"), sb.ToString());
                Debug.Log("WeekTwentyRun done pulls=" + pulls + " bugs=" + bugs.Count);
            }
            catch (Exception e)
            {
                Debug.LogError("WeekTwentyRun failed: " + e);
                File.WriteAllText(Path.Combine("Temp", "week20-run.json"), "{\"error\":\"" + e.Message.Replace("\"", "'") + "\"}");
            }
            finally
            {
                Restore(backupJson, backupFile, hadIcon, iconBackup);
                EditorApplication.Exit(0);
            }
        }

        static void Restore(string backupJson, string backupFile, bool hadIcon, string iconBackup)
        {
            try
            {
                if (!string.IsNullOrEmpty(backupFile) && backupJson != null)
                    File.WriteAllText(backupFile, backupJson);
                else
                {
                    string persistent = Application.persistentDataPath;
                    string legacy = Path.Combine(Directory.GetParent(persistent).FullName, "peach natsu");
                    foreach (string dir in new[] { legacy, persistent })
                    {
                        string path = Path.Combine(dir, "study_save.json");
                        if (File.Exists(path)) File.Delete(path);
                    }
                }
                if (hadIcon && File.Exists(iconBackup))
                    File.Copy(iconBackup, StudyStore.ProfileIconPath, true);
                if (string.IsNullOrEmpty(backupJson))
                {
                    PlayerPrefs.DeleteKey("sm_study_store_v2");
                    PlayerPrefs.DeleteKey("sm_total_bp");
                }
                else PlayerPrefs.SetString("sm_study_store_v2", backupJson);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError("WeekTwentyRun restore failed: " + e);
            }
        }

        static void BuyAll(Offer[] offers)
        {
            bool bought = true;
            while (bought)
            {
                bought = false;
                for (int i = 0; i < offers.Length; i++)
                {
                    if (StudyStore.OwnsCosmetic(offers[i].id)) continue;
                    if (StudyStore.Diamonds < offers[i].cost) continue;
                    string err = StudyStore.TryBuyCosmetic(offers[i].id, offers[i].slot, offers[i].cost);
                    if (err != null) return;
                    bought = true;
                    break;
                }
            }
        }

        static void SpendStats()
        {
            int guard = 0;
            while (StudyStore.GrowthBp >= StudyStore.GrowthStatCost && guard < 20000)
            {
                guard++;
                int pool = StudyStore.HpSteps + StudyStore.AtkSteps + StudyStore.DefSteps
                    + StudyStore.SpdSteps + StudyStore.LuckSteps + StudyStore.GrowthBp / StudyStore.GrowthStatCost;
                int spdT = Mathf.Min(StudyStore.MaxSpdLuckSteps, pool * 8 / 100);
                int luckT = spdT;
                int rest = pool - spdT - luckT;
                int atkT = rest * 45 / 100;
                int hpT = rest * 35 / 100;
                int index = 2;
                if (StudyStore.SpdSteps < spdT) index = 3;
                else if (StudyStore.LuckSteps < luckT) index = 4;
                else if (StudyStore.AtkSteps < atkT) index = 1;
                else if (StudyStore.HpSteps < hpT) index = 0;
                if (!StudyStore.TryAddStat(index)) break;
            }
        }

        static void EquipBest()
        {
            var ranked = new List<string>();
            string smash = null;
            int smashScore = -1;
            var owned = StudyStore.OwnedUltimateIds();
            for (int i = 0; i < owned.Count; i++)
            {
                var def = UltimateCatalog.Get(owned[i]);
                if (def == null) continue;
                int score = (int)def.gear * 100 + StudyStore.UltimateLevel(owned[i]);
                if (def.gear == UltimateGear.Brainstorm)
                {
                    if (score > smashScore)
                    {
                        smashScore = score;
                        smash = owned[i];
                    }
                }
                else ranked.Add(owned[i]);
            }
            ranked.Sort((a, b) =>
            {
                var da = UltimateCatalog.Get(a);
                var db = UltimateCatalog.Get(b);
                int sa = (da != null ? (int)da.gear * 100 : 0) + StudyStore.UltimateLevel(a);
                int sb = (db != null ? (int)db.gear * 100 : 0) + StudyStore.UltimateLevel(b);
                return sb.CompareTo(sa);
            });
            var slots = new[]
            {
                SkillCommandType.Skill1, SkillCommandType.Skill2, SkillCommandType.Skill3, SkillCommandType.Charge
            };
            for (int i = 0; i < slots.Length && i < ranked.Count; i++)
                StudyStore.EquipSlotUltimate(slots[i], ranked[i]);
            if (!string.IsNullOrEmpty(smash))
                StudyStore.EquipSlotUltimate(SkillCommandType.BrainSmash, smash);
        }

        static string Line(int week, int floor, int pulls, int refunds, int earned)
        {
            return "{\"week\":" + week
                + ",\"floor\":" + floor
                + ",\"hours\":" + (StudyStore.TotalEarnedBp / 60)
                + ",\"earned\":" + earned
                + ",\"left\":" + StudyStore.Diamonds
                + ",\"pulls\":" + pulls
                + ",\"refunds\":" + refunds
                + ",\"level\":" + StudyStore.PlayerLevel()
                + ",\"hp\":" + StudyStore.BattleMaxHp()
                + ",\"atk\":" + StudyStore.BattleAttack()
                + ",\"def\":" + StudyStore.BattleDefense()
                + ",\"loadout\":\"" + LoadoutText().Replace("\"", "'") + "\"}";
        }

        static string LoadoutText()
        {
            string Name(SkillCommandType type)
            {
                var def = UltimateCatalog.Get(StudyStore.SlotUltimateId(type));
                if (def == null) return "未設定";
                return def.displayName + StudyStore.VersionMark(StudyStore.UltimateLevel(def.id));
            }
            return Name(SkillCommandType.Skill1) + " / "
                + Name(SkillCommandType.Skill2) + " / "
                + Name(SkillCommandType.Skill3) + " / "
                + Name(SkillCommandType.Charge) + " / 必殺 "
                + Name(SkillCommandType.BrainSmash);
        }

        static Offer[] Shop()
        {
            return new[]
            {
                O("title_study", "title", 15), O("title_night", "title", 20), O("title_book", "title", 25),
                O("title_tower", "title", 30), O("title_ace", "title", 40), O("title_god", "title", 50),
                O("title_moon", "title", 70), O("title_lord", "title", 90), O("title_void", "title", 110),
                O("title_hundred", "title", 120), O("title_summer", "title", 140),
                O("frame_gold", "frame", 20), O("frame_ink", "frame", 20), O("frame_peach", "frame", 25),
                O("frame_jade", "frame", 30), O("frame_sea", "frame", 30), O("frame_violet", "frame", 35),
                O("frame_double", "frame", 50), O("frame_zhu", "frame", 70), O("frame_silver", "frame", 80),
                O("frame_jade_double", "frame", 90), O("frame_triple", "frame", 100), O("frame_god", "frame", 160),
                O("border_gold", "border", 20), O("border_ink", "border", 20), O("border_peach", "border", 25),
                O("border_jade", "border", 25), O("border_sea", "border", 25), O("border_violet", "border", 30),
                O("border_silver", "border", 55), O("border_double", "border", 60), O("border_god", "border", 120),
                O("back_paper", "backdrop", 40), O("back_night", "backdrop", 50), O("back_jade", "backdrop", 55),
                O("back_sea", "backdrop", 55), O("back_peach", "backdrop", 60), O("back_violet", "backdrop", 70),
                O("back_silver", "backdrop", 90), O("back_royal", "backdrop", 100), O("back_god", "backdrop", 180),
                O("seal_goal", "seal", 30), O("seal_jade", "seal", 50), O("seal_sea", "seal", 50),
                O("seal_gold", "seal", 80), O("seal_night", "seal", 100), O("seal_god", "seal", 140),
                O("corner_gold", "corner", 20), O("corner_ink", "corner", 20), O("corner_jade", "corner", 25),
                O("corner_sea", "corner", 25), O("corner_peach", "corner", 30), O("corner_violet", "corner", 35),
                O("corner_silver", "corner", 70), O("corner_double", "corner", 80), O("corner_god", "corner", 150)
            };
        }

        static Offer O(string id, string slot, int cost)
        {
            return new Offer { id = id, slot = slot, cost = cost };
        }
    }
}
