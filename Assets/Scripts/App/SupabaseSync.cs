using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    public static class SupabaseSync
    {
        const string DefaultRoom = "takestudy";

        [Serializable]
        class Row
        {
            public string code;
            public string name;
            public int level;
            public int week;
            public int floor;
            public string room;
            public string bio;
            public string school;
            public string goal;
            public int daily_goal;
            public int weekly_goal;
            public int week_minutes;
            public int lifetime_minutes;
            public string look_csv;
            public string skills_csv;
            public string cosmetics_csv;
        }

        [Serializable]
        class RowList
        {
            public Row[] items;
        }

        static string url;
        static string key;
        static bool loaded;

        public static bool Ready
        {
            get
            {
                LoadConfig();
                return !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(key) && url.StartsWith("https://");
            }
        }

        public static string Room()
        {
            string abs = Application.absoluteURL;
            if (!string.IsNullOrEmpty(abs))
            {
                int q = abs.IndexOf("room=", StringComparison.OrdinalIgnoreCase);
                if (q >= 0)
                {
                    string rest = abs.Substring(q + 5);
                    int cut = rest.IndexOf('&');
                    if (cut >= 0) rest = rest.Substring(0, cut);
                    rest = Uri.UnescapeDataString(rest).Trim();
                    if (rest.Length > 0 && rest.Length <= 32) return rest;
                }
            }
            return DefaultRoom;
        }

        public static IEnumerator PushSelf()
        {
            if (!Ready) yield break;
            var row = new Row
            {
                code = StudyStore.MyFriendCode(),
                name = StudyStore.ProfileName(),
                level = StudyStore.PlayerLevel(),
                week = Mathf.Max(1, StudyStore.DungeonWeek),
                floor = Mathf.Max(1, StudyStore.DungeonFloor),
                room = Room(),
                bio = StudyStore.Profile != null ? StudyStore.Profile.bio : "",
                school = StudyStore.Profile != null ? StudyStore.Profile.school : "",
                goal = StudyStore.Profile != null ? StudyStore.Profile.goal : "",
                daily_goal = StudyStore.Profile != null ? StudyStore.Profile.dailyGoalMinutes : 0,
                weekly_goal = StudyStore.WeeklyGoalMinutes(),
                week_minutes = StudyStore.MinutesThisWeek(),
                lifetime_minutes = StudyStore.LifetimeMinutes(),
                look_csv = StudyStore.MyLookCsv(),
                skills_csv = StudyStore.MySkillsCsv(),
                cosmetics_csv = StudyStore.MyCosmeticsCsv()
            };
            string body = JsonUtility.ToJson(row);
            yield return Send("POST", "/rest/v1/profiles", body, "resolution=merge-duplicates,return=minimal", null);
            if (LastHttp == 404 || (LastHttp >= 400 && LastHttp != 404))
            {
                // extra columns may not exist yet; retry with the base row so basic sync keeps working
                var basic = new Row
                {
                    code = row.code,
                    name = row.name,
                    level = row.level,
                    week = row.week,
                    floor = row.floor,
                    room = row.room
                };
                yield return Send("POST", "/rest/v1/profiles", JsonUtility.ToJson(basic), "resolution=merge-duplicates,return=minimal", null);
            }
        }

        public static IEnumerator RefreshFriends()
        {
            if (!Ready) yield break;
            yield return PushSelf();
            var friends = StudyStore.FriendList;
            if (friends.Count == 0) yield break;
            var sb = new StringBuilder();
            for (int i = 0; i < friends.Count; i++)
            {
                if (friends[i] == null || string.IsNullOrEmpty(friends[i].code)) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(friends[i].code);
            }
            if (sb.Length == 0) yield break;
            string json = null;
            string path = "/rest/v1/profiles?select=code,name,level,week,floor,room,bio,school,goal,daily_goal,weekly_goal,week_minutes,lifetime_minutes,look_csv,skills_csv,cosmetics_csv&code=in.(" + sb + ")";
            yield return Send("GET", path, null, null, text => json = text);
            if (LastHttp >= 400)
            {
                path = "/rest/v1/profiles?select=code,name,level,week,floor,room&code=in.(" + sb + ")";
                yield return Send("GET", path, null, null, text => json = text);
            }
            ApplyList(json);
        }

        public static IEnumerator LookupAndApply(string code)
        {
            if (!Ready) yield break;
            string json = null;
            string path = "/rest/v1/profiles?select=code,name,level,week,floor,room,bio,school,goal,daily_goal,weekly_goal,week_minutes,lifetime_minutes,look_csv,skills_csv,cosmetics_csv&code=eq." + UnityWebRequest.EscapeURL(code);
            yield return Send("GET", path, null, null, text => json = text);
            if (LastHttp >= 400)
            {
                path = "/rest/v1/profiles?select=code,name,level,week,floor,room&code=eq." + UnityWebRequest.EscapeURL(code);
                yield return Send("GET", path, null, null, text => json = text);
            }
            ApplyList(json);
        }

        public static IEnumerator FetchProfile(string code, Action<FriendEntry> done)
        {
            var entry = new FriendEntry { code = code ?? "" };
            if (!Ready || string.IsNullOrEmpty(code))
            {
                if (done != null) done(entry);
                yield break;
            }
            string json = null;
            string path = "/rest/v1/profiles?select=code,name,level,week,floor,room,bio,school,goal,daily_goal,weekly_goal,week_minutes,lifetime_minutes,look_csv,skills_csv,cosmetics_csv&code=eq."
                + UnityWebRequest.EscapeURL(code);
            yield return Send("GET", path, null, null, text => json = text);
            if (LastHttp >= 400)
            {
                path = "/rest/v1/profiles?select=code,name,level,week,floor,room&code=eq." + UnityWebRequest.EscapeURL(code);
                yield return Send("GET", path, null, null, text => json = text);
            }
            var rows = ParseRows(json);
            if (rows.Count > 0)
            {
                var row = rows[0];
                entry.code = row.code ?? code;
                entry.name = row.name ?? "";
                entry.level = row.level;
                entry.week = row.week;
                entry.floor = row.floor;
                entry.bio = row.bio ?? "";
                entry.school = row.school ?? "";
                entry.goal = row.goal ?? "";
                entry.dailyGoalMinutes = row.daily_goal;
                entry.weeklyGoalMinutes = row.weekly_goal;
                entry.weekMinutes = row.week_minutes;
                entry.lifetimeMinutes = row.lifetime_minutes;
                entry.lookCsv = row.look_csv ?? "";
                entry.skillsCsv = row.skills_csv ?? "";
                entry.cosmeticsCsv = row.cosmetics_csv ?? "";
            }
            if (done != null) done(entry);
        }

        static void ApplyList(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length < 2 || json[0] != '[') return;
            var list = JsonUtility.FromJson<RowList>("{\"items\":" + json + "}");
            if (list == null || list.items == null) return;
            for (int i = 0; i < list.items.Length; i++)
            {
                var row = list.items[i];
                if (row == null || string.IsNullOrEmpty(row.code)) continue;
                StudyStore.ApplyRemoteFriend(row.code, row.name, row.level, row.week, row.floor);
                bool hasLook = json.IndexOf("look_csv", StringComparison.Ordinal) >= 0;
                if (hasLook)
                    StudyStore.ApplyRemoteFriendDetail(row.code, row.bio, row.school, row.goal,
                        row.daily_goal, row.weekly_goal, row.look_csv, row.skills_csv, row.cosmetics_csv);
                if (json.IndexOf("week_minutes", StringComparison.Ordinal) >= 0)
                    StudyStore.ApplyRemoteFriendStudy(row.code, row.week_minutes, row.lifetime_minutes);
            }
        }

        public class SocialRequest
        {
            public string id;
            public string fromCode;
            public string toCode;
            public string fromName;
            public string status;
        }

        [Serializable]
        class ReqRow
        {
            public string id;
            public string from_code;
            public string to_code;
            public string from_name;
            public string status;
            public string room;
        }

        [Serializable]
        class ReqList
        {
            public ReqRow[] items;
        }

        [Serializable]
        class PostRow
        {
            public string id;
            public string author_code;
            public string author_name;
            public string material_name;
            public int subject;
            public int minutes;
            public string started_at;
            public string room;
        }

        [Serializable]
        class PostList
        {
            public PostRow[] items;
        }

        public static readonly List<SocialRequest> Incoming = new List<SocialRequest>();
        public static readonly List<SocialRequest> Outgoing = new List<SocialRequest>();
        public static string LastMessage = "";
        public static long LastHttp;

        public static IEnumerator SyncSocial()
        {
            Incoming.Clear();
            Outgoing.Clear();
            if (!Ready) yield break;
            yield return PushSelf();
            string me = StudyStore.MyFriendCode();
            string json = null;
            string path = "/rest/v1/friend_requests?select=id,from_code,to_code,from_name,status,room"
                + "&room=eq." + UnityWebRequest.EscapeURL(Room())
                + "&or=(from_code.eq." + me + ",to_code.eq." + me + ")";
            yield return Send("GET", path, null, null, text => json = text);
            if (LastHttp == 404)
            {
                yield return RefreshFriends();
                yield break;
            }
            var rows = ParseReqs(json);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                bool mine = string.Equals(row.from_code, me, StringComparison.OrdinalIgnoreCase);
                string other = mine ? row.to_code : row.from_code;
                if (row.status == "accepted")
                {
                    string name = mine ? other : row.from_name;
                    StudyStore.AddFriend(other, name);
                }
                else if (row.status == "pending")
                {
                    var item = new SocialRequest
                    {
                        id = row.id,
                        fromCode = row.from_code,
                        toCode = row.to_code,
                        fromName = string.IsNullOrEmpty(row.from_name) ? row.from_code : row.from_name,
                        status = row.status
                    };
                    if (mine) Outgoing.Add(item);
                    else Incoming.Add(item);
                }
            }
            yield return RefreshFriends();
        }

        public static IEnumerator RequestFriend(string code, string hintedName)
        {
            LastMessage = "";
            if (!Ready)
            {
                LastMessage = "通信の準備ができていません";
                yield break;
            }
            code = StudyStore.NormalizeFriendCode(code);
            if (code.Length != 6)
            {
                LastMessage = "コードは6文字です";
                yield break;
            }
            if (code == StudyStore.MyFriendCode())
            {
                LastMessage = "自分のコードです";
                yield break;
            }
            if (StudyStore.IsFriend(code))
            {
                LastMessage = "すでにフレンドです";
                yield break;
            }
            string json = null;
            yield return Send("GET", "/rest/v1/profiles?select=code,name&code=eq." + code, null, null, text => json = text);
            if (LastHttp == 404 || string.IsNullOrEmpty(json) || json == "[]")
            {
                LastMessage = LastHttp == 404 ? "サーバーの準備がまだです" : "そのコードはまだ登録されていません";
                yield break;
            }
            var found = ParseRows(json);
            string realName = found.Count > 0 ? found[0].name : null;
            if (!string.IsNullOrEmpty(realName)) hintedName = realName;
            string me = StudyStore.MyFriendCode();
            string id = me + "_" + code;
            string reverse = code + "_" + me;
            string existing = null;
            yield return Send("GET", "/rest/v1/friend_requests?select=id,status&id=in.(" + id + "," + reverse + ")", null, null, text => existing = text);
            if (LastHttp == 404)
            {
                LastMessage = "サーバーの準備がまだです";
                yield break;
            }
            if (!string.IsNullOrEmpty(existing) && existing.IndexOf("\"accepted\"", StringComparison.Ordinal) >= 0)
            {
                string name = hintedName;
                StudyStore.AddFriend(code, string.IsNullOrEmpty(name) ? code : name);
                yield return LookupAndApply(code);
                LastMessage = "すでにフレンドです";
                yield break;
            }
            if (!string.IsNullOrEmpty(existing) && existing.IndexOf(reverse, StringComparison.Ordinal) >= 0
                && existing.IndexOf("\"pending\"", StringComparison.Ordinal) >= 0)
            {
                LastMessage = "相手から申請が来ています";
                yield break;
            }
            if (!string.IsNullOrEmpty(existing) && existing.IndexOf(id, StringComparison.Ordinal) >= 0
                && existing.IndexOf("\"pending\"", StringComparison.Ordinal) >= 0)
            {
                LastMessage = "申請中です";
                yield break;
            }
            var row = new ReqRow
            {
                id = id,
                from_code = me,
                to_code = code,
                from_name = StudyStore.ProfileName(),
                status = "pending",
                room = Room()
            };
            yield return Send("POST", "/rest/v1/friend_requests", JsonUtility.ToJson(row), "resolution=merge-duplicates,return=minimal", null);
            LastMessage = LastHttp == 404 ? "サーバーの準備がまだです" : (LastHttp >= 200 && LastHttp < 300 ? "申請しました" : "申請できませんでした");
            if (LastMessage == "申請しました")
            {
                Outgoing.Add(new SocialRequest
                {
                    id = id,
                    fromCode = me,
                    toCode = code,
                    fromName = string.IsNullOrEmpty(hintedName) ? code : hintedName.Trim(),
                    status = "pending"
                });
            }
        }

        public static IEnumerator AnswerRequest(string fromCode, string fromName, bool accept)
        {
            LastMessage = "";
            if (!Ready) yield break;
            fromCode = StudyStore.NormalizeFriendCode(fromCode);
            string id = fromCode + "_" + StudyStore.MyFriendCode();
            string body = "{\"status\":\"" + (accept ? "accepted" : "rejected") + "\"}";
            yield return Send("PATCH", "/rest/v1/friend_requests?id=eq." + id, body, "return=minimal", null);
            if (LastHttp < 200 || LastHttp >= 300)
            {
                LastMessage = "更新できませんでした";
                yield break;
            }
            Incoming.RemoveAll(r => r != null && string.Equals(r.fromCode, fromCode, StringComparison.OrdinalIgnoreCase));
            if (accept)
            {
                StudyStore.AddFriend(fromCode, fromName);
                yield return LookupAndApply(fromCode);
                LastMessage = "フレンドになりました";
            }
            else LastMessage = "申請を断りました";
        }

        public static IEnumerator DropFriend(string code)
        {
            if (!Ready) yield break;
            code = StudyStore.NormalizeFriendCode(code);
            string me = StudyStore.MyFriendCode();
            string path = "/rest/v1/friend_requests?or=(id.eq." + me + "_" + code + ",id.eq." + code + "_" + me + ")";
            yield return Send("DELETE", path, null, "return=minimal", null);
            Incoming.RemoveAll(r => r != null && (string.Equals(r.fromCode, code, StringComparison.OrdinalIgnoreCase) || string.Equals(r.toCode, code, StringComparison.OrdinalIgnoreCase)));
            Outgoing.RemoveAll(r => r != null && (string.Equals(r.fromCode, code, StringComparison.OrdinalIgnoreCase) || string.Equals(r.toCode, code, StringComparison.OrdinalIgnoreCase)));
        }

        public static IEnumerator PushMyLogs()
        {
            if (!Ready) yield break;
            var logs = StudyStore.LogsNewestFirst();
            string me = StudyStore.MyFriendCode();
            string name = Esc(StudyStore.ProfileName());
            string room = Esc(Room());
            var sb = new StringBuilder();
            sb.Append('[');
            int n = 0;
            for (int i = 0; i < logs.Count && n < 200; i++)
            {
                var log = logs[i];
                if (log == null || log.open || log.minutes <= 0 || string.IsNullOrEmpty(log.id)) continue;
                if (!StudyStore.IsSelfAuthor(log.authorCode)) continue;
                if (n > 0) sb.Append(',');
                sb.Append("{\"id\":\"").Append(Esc(log.id))
                    .Append("\",\"author_code\":\"").Append(me)
                    .Append("\",\"author_name\":\"").Append(name)
                    .Append("\",\"material_name\":\"").Append(Esc(log.materialName))
                    .Append("\",\"subject\":").Append(log.subject)
                    .Append(",\"minutes\":").Append(log.minutes)
                    .Append(",\"started_at\":\"").Append(Esc(log.startedAt))
                    .Append("\",\"room\":\"").Append(room)
                    .Append("\"}");
                n++;
            }
            if (n == 0) yield break;
            sb.Append(']');
            yield return Send("POST", "/rest/v1/study_posts", sb.ToString(), "resolution=merge-duplicates,return=minimal", null);
        }

        public static IEnumerator PullFriendLogs()
        {
            if (!Ready)
            {
                StudyStore.ReplaceRemoteLogs(null);
                yield break;
            }
            var friends = StudyStore.FriendList;
            if (friends.Count == 0)
            {
                StudyStore.ReplaceRemoteLogs(null);
                yield break;
            }
            var sb = new StringBuilder();
            for (int i = 0; i < friends.Count; i++)
            {
                if (friends[i] == null || string.IsNullOrEmpty(friends[i].code)) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(friends[i].code);
            }
            if (sb.Length == 0)
            {
                StudyStore.ReplaceRemoteLogs(null);
                yield break;
            }
            string json = null;
            string path = "/rest/v1/study_posts?select=id,author_code,author_name,material_name,subject,minutes,started_at"
                + "&author_code=in.(" + sb + ")&order=started_at.desc&limit=200";
            yield return Send("GET", path, null, null, text => json = text);
            var logs = new List<StudyLogEntry>();
            if (!string.IsNullOrEmpty(json) && json.Length > 2 && json[0] == '[')
            {
                var list = JsonUtility.FromJson<PostList>("{\"items\":" + json + "}");
                if (list != null && list.items != null)
                {
                    for (int i = 0; i < list.items.Length; i++)
                    {
                        var row = list.items[i];
                        if (row == null || string.IsNullOrEmpty(row.id)) continue;
                        logs.Add(new StudyLogEntry
                        {
                            id = row.id,
                            materialName = row.material_name,
                            subject = row.subject,
                            minutes = row.minutes,
                            startedAt = row.started_at,
                            authorCode = row.author_code
                        });
                    }
                }
            }
            StudyStore.ReplaceRemoteLogs(logs);
        }

        static List<Row> ParseRows(string json)
        {
            var list = new List<Row>();
            if (string.IsNullOrEmpty(json) || json.Length < 2 || json[0] != '[') return list;
            var parsed = JsonUtility.FromJson<RowList>("{\"items\":" + json + "}");
            if (parsed == null || parsed.items == null) return list;
            for (int i = 0; i < parsed.items.Length; i++)
                if (parsed.items[i] != null) list.Add(parsed.items[i]);
            return list;
        }

        static List<ReqRow> ParseReqs(string json)
        {
            var list = new List<ReqRow>();
            if (string.IsNullOrEmpty(json) || json.Length < 2 || json[0] != '[') return list;
            var parsed = JsonUtility.FromJson<ReqList>("{\"items\":" + json + "}");
            if (parsed == null || parsed.items == null) return list;
            for (int i = 0; i < parsed.items.Length; i++)
                if (parsed.items[i] != null) list.Add(parsed.items[i]);
            return list;
        }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }

        public static IEnumerator Rest(string method, string path, string body, string prefer, Action<string> onText)
        {
            yield return Send(method, path, body, prefer, onText);
        }

        static IEnumerator Send(string method, string path, string body, string prefer, Action<string> onText)
        {
            LastHttp = 0;
            var req = new UnityWebRequest(url + path, method);
            if (!string.IsNullOrEmpty(body))
            {
                byte[] raw = Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(raw);
            }
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("apikey", key);
            req.SetRequestHeader("Authorization", "Bearer " + key);
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(prefer)) req.SetRequestHeader("Prefer", prefer);
            yield return req.SendWebRequest();
            LastHttp = req.responseCode;
            string text = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (req.result == UnityWebRequest.Result.Success && onText != null)
                onText(text);
            req.Dispose();
        }

        static void LoadConfig()
        {
            if (loaded) return;
            loaded = true;
            var asset = Resources.Load<TextAsset>("supabase_config");
            if (asset == null) return;
            string[] lines = asset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("url=", StringComparison.OrdinalIgnoreCase))
                    url = line.Substring(4).Trim().TrimEnd('/');
                else if (line.StartsWith("key=", StringComparison.OrdinalIgnoreCase))
                    key = line.Substring(4).Trim();
                else if (line.StartsWith("https://"))
                    url = line.TrimEnd('/');
                else if (string.IsNullOrEmpty(key))
                    key = line;
            }
        }
    }
}
