using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ShiftingMetropolis.Battle;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    /// <summary>
    /// レイド専用画面。開くたびに順位とTOP3の見た目を取り直す。
    /// </summary>
    public class RaidScreen
    {
        StudyAppUI ui;
        RectTransform root;
        Font font;
        GameObject page;
        GameObject studio;
        Camera cam;
        RenderTexture preview;
        int token;
        bool busy;

        public bool IsOpen => page != null;
        public Transform Host => page != null ? page.transform : null;

        public void Open(StudyAppUI host, RectTransform parent, Font uiFont)
        {
            Close();
            ui = host;
            root = parent;
            font = uiFont;
            token++;
            page = new GameObject("RaidScreen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            page.transform.SetParent(root, false);
            var rt = page.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var backdrop = page.GetComponent<Image>();
            backdrop.sprite = Solid();
            backdrop.type = Image.Type.Simple;
            backdrop.color = RpgTheme.Bg;
            page.transform.SetAsLastSibling();
            Label(page.transform, "Loading", new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f),
                "レイドを読み込んでいます", 22, Color.white, TextAnchor.MiddleCenter);
            if (ui != null) ui.Run(Load(token));
        }

        public void Reload()
        {
            if (page == null || ui == null) return;
            token++;
            if (ui != null) ui.Run(Load(token));
        }

        public void Close()
        {
            token++;
            if (page != null) UnityEngine.Object.Destroy(page);
            page = null;
            ClearStudio();
        }

        IEnumerator Load(int stamp)
        {
            yield return RaidSync.Refresh();
            if (stamp != token || page == null) yield break;
            Build();
        }

        void Build()
        {
            if (page == null) return;
            for (int i = page.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(page.transform.GetChild(i).gameObject);
            ClearStudio();

            var boss = RaidStore.Boss;
            var back = Button(page.transform, "Back", new Vector2(0.03f, 0.935f), new Vector2(0.22f, 0.985f),
                "戻る", 18, new Color(0.18f, 0.18f, 0.22f), Color.white);
            back.onClick.AddListener(Close);
            Label(page.transform, "Title", new Vector2(0.24f, 0.935f), new Vector2(0.68f, 0.985f),
                "レイド", 28, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
            int gifts = RaidStore.UnclaimedCount();
            string box = gifts > 0 ? "贈り物 " + gifts : "贈り物";
            var present = Button(page.transform, "Gifts", new Vector2(0.70f, 0.935f), new Vector2(0.97f, 0.985f),
                box, 16, gifts > 0 ? RpgTheme.Gold : RpgTheme.ButtonFill, gifts > 0 ? RpgTheme.Bg : Color.white);
            present.onClick.AddListener(ShowGifts);

            if (boss == null)
            {
                Label(page.transform, "Empty", new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.6f),
                    "レイドを用意できません", 20, RpgTheme.Muted, TextAnchor.MiddleCenter);
                return;
            }

            BuildHpPlate(boss);
            string layer = boss.week + "周目  " + boss.floor + "階" + (boss.milestone ? "  節目" : "");
            Label(page.transform, "Meta", new Vector2(0.06f, 0.705f), new Vector2(0.94f, 0.755f),
                layer, 26, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            Label(page.transform, "Sub", new Vector2(0.06f, 0.655f), new Vector2(0.94f, 0.705f),
                "参加者 " + Mathf.Max(0, boss.participants)
                + "    今日 " + RaidStore.TurnsLeft() + " / " + RaidRules.DailyTurns,
                18, RpgTheme.Muted, TextAnchor.MiddleLeft);

            string go = RaidStore.HasPause() ? "続きから" : "レイドバトル";
            if (RaidStore.TurnsLeft() <= 0 && !RaidStore.HasPause()) go = "本日の15ターンを使い切った";
            else if (RaidStore.TurnsLeft() <= 0) go = "続きは明日";
            var enter = Button(page.transform, "Enter", new Vector2(0.08f, 0.575f), new Vector2(0.92f, 0.648f),
                go, 24, new Color(0.55f, 0.08f, 0.12f), Color.white);
            enter.onClick.AddListener(EnterBattle);

            Label(page.transform, "RankHead", new Vector2(0.05f, 0.515f), new Vector2(0.95f, 0.568f),
                "ダメージ順位", 18, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            BuildPodium();
            BuildList();
        }

        void BuildHpPlate(RaidBossView boss)
        {
            var plate = new GameObject("DynamaxPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            plate.transform.SetParent(page.transform, false);
            Stretch(plate.GetComponent<RectTransform>(), new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.925f));
            RpgTheme.PaintGoldFrame(plate.GetComponent<Image>());

            Label(plate.transform, "Name", new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.92f),
                boss.enemyName, 26, Color.white, TextAnchor.MiddleLeft);
            float ratio = boss.maxHp <= 0 ? 0f : Mathf.Clamp01(boss.hp / (float)boss.maxHp);
            var track = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            track.transform.SetParent(plate.transform, false);
            Stretch(track.GetComponent<RectTransform>(), new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.48f));
            var trackImage = track.GetComponent<Image>();
            trackImage.sprite = Solid();
            trackImage.color = RpgTheme.Navy;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(Mathf.Max(0.02f, ratio), 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = Solid();
            fillImage.color = ratio > 0.5f
                ? new Color(1f, 0.28f, 0.32f, 1f)
                : ratio > 0.25f
                    ? new Color(0.95f, 0.42f, 0.12f, 1f)
                    : new Color(0.62f, 0.05f, 0.08f, 1f);
            var core = new GameObject("Core", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            core.transform.SetParent(fill.transform, false);
            Stretch(core.GetComponent<RectTransform>(), new Vector2(0f, 0.62f), new Vector2(1f, 0.82f));
            var coreImage = core.GetComponent<Image>();
            coreImage.sprite = Solid();
            coreImage.color = new Color(1f, 0.92f, 0.75f, 0.9f);
            Label(plate.transform, "Nums", new Vector2(0.04f, 0.0f), new Vector2(0.96f, 0.20f),
                RaidRules.FormatHp(boss.hp) + "  /  " + RaidRules.FormatHp(boss.maxHp),
                14, new Color(1f, 0.82f, 0.82f), TextAnchor.MiddleRight);
        }

        void BuildPodium()
        {
            var ranked = RaidStore.Ranked();
            var host = new GameObject("Podium", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            host.transform.SetParent(page.transform, false);
            var hrt = host.GetComponent<RectTransform>();
            Stretch(hrt, new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.51f));
            var raw = host.GetComponent<RawImage>();
            raw.color = Color.white;
            BuildStudio(ranked);

            for (int slot = 0; slot < 3; slot++)
            {
                int index = slot == 0 ? 1 : slot == 1 ? 0 : 2;
                float x0 = slot / 3f;
                float x1 = (slot + 1) / 3f;
                var member = index < ranked.Count ? ranked[index] : null;
                Color tint = RankColor(index);
                string caption = member == null
                    ? (index + 1) + "位  —"
                    : (index + 1) + "位  " + Display(member) + "\n" + RaidRules.FormatHp(member.damage);
                var hit = Button(page.transform, "Podium" + index,
                    new Vector2(0.04f + 0.92f * x0, 0.30f), new Vector2(0.04f + 0.92f * x1, 0.36f),
                    caption, index == 0 ? 14 : 12, tint, index == 0 ? new Color(0.15f, 0.08f, 0.02f) : Color.white);
                if (member != null)
                {
                    var captured = member;
                    hit.onClick.AddListener(() => OpenProfile(captured));
                }
            }
        }

        void BuildList()
        {
            var scrollGo = new GameObject("RankScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(page.transform, false);
            Stretch(scrollGo.GetComponent<RectTransform>(), new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.295f));
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.sprite = Solid();
            scrollBg.color = new Color(0f, 0f, 0f, 0.28f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = crt;

            var ranked = RaidStore.Ranked();
            if (ranked.Count == 0)
            {
                Label(content.transform, "None", new Vector2(0.05f, 0f), new Vector2(0.95f, 1f),
                    "まだ参加者はいません", 18, RpgTheme.Muted, TextAnchor.MiddleCenter);
                crt.sizeDelta = new Vector2(0f, 80f);
                return;
            }
            float y = -8f;
            for (int i = 0; i < ranked.Count; i++)
            {
                var member = ranked[i];
                int rank = i;
                Color tint = i < 3 ? RankColor(i) : RpgTheme.CardFill;
                var row = new GameObject("Row" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                row.transform.SetParent(content.transform, false);
                var rrt = row.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0.02f, 1f);
                rrt.anchorMax = new Vector2(0.98f, 1f);
                rrt.pivot = new Vector2(0.5f, 1f);
                rrt.anchoredPosition = new Vector2(0f, y);
                rrt.sizeDelta = new Vector2(0f, i == 0 ? 78f : 64f);
                var rowImage = row.GetComponent<Image>();
                rowImage.sprite = Solid();
                rowImage.color = tint;
                var icon = Button(row.transform, "Icon", new Vector2(0.02f, 0.12f), new Vector2(0.16f, 0.88f),
                    Initial(member), i == 0 ? 22 : 18, new Color(0.08f, 0.06f, 0.08f), Color.white);
                var captured = member;
                icon.onClick.AddListener(() => OpenProfile(captured));
                Label(row.transform, "Place", new Vector2(0.18f, 0.50f), new Vector2(0.34f, 0.92f),
                    (rank + 1) + "位", i == 0 ? 20 : 16, i < 3 ? Color.white : RpgTheme.GoldHi, TextAnchor.MiddleLeft);
                Label(row.transform, "Name", new Vector2(0.34f, 0.50f), new Vector2(0.96f, 0.92f),
                    Display(member), i == 0 ? 20 : 16, Color.white, TextAnchor.MiddleLeft);
                Label(row.transform, "Dmg", new Vector2(0.18f, 0.06f), new Vector2(0.96f, 0.48f),
                    RaidRules.FormatHp(member.damage), 16, new Color(1f, 0.9f, 0.75f), TextAnchor.MiddleLeft);
                y -= (i == 0 ? 86f : 72f);
            }
            crt.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 12f);
        }

        void EnterBattle()
        {
            if (busy || ui == null) return;
            if (RaidStore.TurnsLeft() <= 0)
            {
                ui.Toast(RaidStore.HasPause()
                    ? "今日の15ターンを使い切った。続きは明日"
                    : "今日の15ターンを使い切った");
                return;
            }
            busy = true;
            ui.Run(EnterCo());
        }

        IEnumerator EnterCo()
        {
            int stamp = token;
            yield return RaidSync.JoinSelf();
            busy = false;
            if (page == null || stamp != token) yield break;
            var flow = AppFlow.Instance;
            if (flow == null) flow = UnityEngine.Object.FindFirstObjectByType<AppFlow>();
            if (flow != null) flow.ShowRaidBattle();
        }

        void ShowGifts()
        {
            if (page == null) return;
            var old = page.transform.Find("GiftBox");
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            var box = new GameObject("GiftBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.transform.SetParent(page.transform, false);
            Stretch(box.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var boxImage = box.GetComponent<Image>();
            boxImage.sprite = Solid();
            boxImage.color = RpgTheme.Bg;
            Label(box.transform, "Title", new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f),
                "プレゼントボックス", 26, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
            var list = RaidStore.Unclaimed();
            var scrollGo = new GameObject("GiftScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(box.transform, false);
            Stretch(scrollGo.GetComponent<RectTransform>(), new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.88f));
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = crt;
            if (list.Count == 0)
            {
                Label(content.transform, "None", new Vector2(0.08f, 0f), new Vector2(0.92f, 1f),
                    "届いている贈り物はありません", 18, RpgTheme.Muted, TextAnchor.MiddleCenter);
                crt.sizeDelta = new Vector2(0f, 80f);
            }
            else
            {
                float y = -8f;
                for (int i = 0; i < list.Count; i++)
                {
                    var gift = list[i];
                    var row = new GameObject("Gift" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    row.transform.SetParent(content.transform, false);
                    var rrt = row.GetComponent<RectTransform>();
                    rrt.anchorMin = new Vector2(0.02f, 1f);
                    rrt.anchorMax = new Vector2(0.98f, 1f);
                    rrt.pivot = new Vector2(0.5f, 1f);
                    rrt.anchoredPosition = new Vector2(0f, y);
                    rrt.sizeDelta = new Vector2(0f, 92f);
                    RpgTheme.PaintGoldFrame(row.GetComponent<Image>());
                    string title = string.IsNullOrEmpty(gift.itemName) ? "ガチャの中身" : gift.itemName;
                    Label(row.transform, "Name", new Vector2(0.04f, 0.42f), new Vector2(0.62f, 0.92f),
                        title, 16, Color.white, TextAnchor.MiddleLeft);
                    Label(row.transform, "Sub", new Vector2(0.04f, 0.08f), new Vector2(0.62f, 0.42f),
                        bossLine(gift) + "   ダイヤ +" + gift.diamonds, 14, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
                    var captured = gift;
                    var claim = Button(row.transform, "Claim", new Vector2(0.66f, 0.18f), new Vector2(0.96f, 0.82f),
                        "受け取る", 16, RpgTheme.Gold, RpgTheme.Bg);
                    claim.onClick.AddListener(() =>
                    {
                        if (ui != null) ui.Run(ClaimCo(captured));
                    });
                    y -= 102f;
                }
                crt.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 12f);
            }
            var close = Button(box.transform, "CloseGifts", new Vector2(0.22f, 0.03f), new Vector2(0.78f, 0.11f),
                "閉じる", 20, new Color(0.22f, 0.22f, 0.26f), Color.white);
            close.onClick.AddListener(() =>
            {
                if (box != null) UnityEngine.Object.Destroy(box);
                Build();
            });
        }

        static string bossLine(RaidGift gift)
        {
            return gift.week + "周 " + gift.floor + "階";
        }

        IEnumerator ClaimCo(RaidGift gift)
        {
            string message = "";
            yield return RaidSync.Claim(gift, text => message = text);
            if (ui != null && !string.IsNullOrEmpty(message)) ui.Toast(message);
            if (page != null && page.transform.Find("GiftBox") != null) ShowGifts();
            else if (page != null) Build();
        }

        void OpenProfile(RaidMemberView member)
        {
            if (member == null || ui == null) return;
            if (member.code == StudyStore.MyFriendCode())
            {
                ui.OpenSelfProfile();
                return;
            }
            ui.Run(ProfileCo(member));
        }

        IEnumerator ProfileCo(RaidMemberView member)
        {
            FriendEntry fetched = null;
            yield return SupabaseSync.FetchProfile(member.code, value => fetched = value);
            if (page == null || member == null) yield break;
            if (fetched == null) fetched = new FriendEntry { code = member.code, name = member.name };
            if (string.IsNullOrEmpty(fetched.name)) fetched.name = member.name;
            if (string.IsNullOrEmpty(fetched.lookCsv)) fetched.lookCsv = member.lookCsv;
            if (StudyStore.IsFriend(member.code))
            {
                var known = StudyStore.FindFriend(member.code);
                if (known != null)
                {
                    ui.ShowFriendProfile(known);
                    yield break;
                }
            }
            ShowStranger(fetched, member.damage);
        }

        void ShowStranger(FriendEntry friend, int damage)
        {
            var old = page.transform.Find("RaidProfile");
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            var dialog = new GameObject("RaidProfile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dialog.transform.SetParent(page.transform, false);
            Stretch(dialog.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var dim = dialog.GetComponent<Image>();
            dim.sprite = Solid();
            dim.color = RpgTheme.Bg;
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(dialog.transform, false);
            Stretch(panel.GetComponent<RectTransform>(), new Vector2(0.03f, 0.62f), new Vector2(0.97f, 0.985f));
            dialog.transform.SetAsLastSibling();
            RpgTheme.PaintGoldFrame(panel.GetComponent<Image>());
            string name = string.IsNullOrEmpty(friend.name) ? friend.code : friend.name;
            Label(panel.transform, "Name", new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f),
                name, 26, Color.white, TextAnchor.MiddleLeft);
            Label(panel.transform, "Code", new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.82f),
                "Lv." + Mathf.Max(1, friend.level) + "    " + friend.code, 16, RpgTheme.Muted, TextAnchor.MiddleLeft);
            string goal = string.IsNullOrEmpty(friend.goal) ? "目標はまだありません" : friend.goal;
            Label(panel.transform, "Goal", new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.70f),
                goal, 20, RpgTheme.GoldHi, TextAnchor.UpperLeft);
            Label(panel.transform, "Climb", new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.50f),
                StudyStore.ClimbLine(friend.week, friend.floor) + "\n与ダメージ " + RaidRules.FormatHp(damage),
                16, Color.white, TextAnchor.MiddleLeft);
            string school = string.IsNullOrEmpty(friend.school) ? "未設定" : friend.school;
            string bio = string.IsNullOrEmpty(friend.bio) ? "未設定" : friend.bio;
            Label(panel.transform, "Bio", new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.40f),
                "志望校  " + school + "\n自己紹介  " + bio, 16, Color.white, TextAnchor.UpperLeft);
            var request = Button(panel.transform, "Request", new Vector2(0.08f, 0.05f), new Vector2(0.48f, 0.16f),
                "フレンド申請", 16, RpgTheme.Gold, RpgTheme.Bg);
            var captured = friend;
            request.onClick.AddListener(() =>
            {
                if (ui != null) ui.Run(RequestCo(captured.code, captured.name));
            });
            var close = Button(panel.transform, "Close", new Vector2(0.52f, 0.05f), new Vector2(0.92f, 0.16f),
                "閉じる", 16, new Color(0.22f, 0.22f, 0.26f), Color.white);
            close.onClick.AddListener(() => UnityEngine.Object.Destroy(dialog));
        }

        IEnumerator RequestCo(string code, string name)
        {
            yield return SupabaseSync.RequestFriend(code, name);
            if (ui != null) ui.Toast(string.IsNullOrEmpty(SupabaseSync.LastMessage) ? "申請しました" : SupabaseSync.LastMessage);
        }

        void BuildStudio(List<RaidMemberView> ranked)
        {
            studio = new GameObject("RaidPodiumStudio");
            studio.transform.position = new Vector3(420f, -8200f, 0f);
            preview = new RenderTexture(900, 420, 16, RenderTextureFormat.ARGB32);
            preview.Create();
            var rawHost = page.transform.Find("Podium");
            var raw = rawHost != null ? rawHost.GetComponent<RawImage>() : null;
            if (raw != null) raw.texture = preview;

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(studio.transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(38f, 28f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.cullingMask = 1 << 30;

            float[] xs = { -1.55f, 0f, 1.55f };
            float[] scales = { 0.92f, 1.12f, 0.88f };
            int[] order = { 1, 0, 2 };
            for (int slot = 0; slot < 3; slot++)
            {
                int index = order[slot];
                if (index >= ranked.Count) continue;
                var member = ranked[index];
                string lookCsv = member.lookCsv;
                if (member.code == StudyStore.MyFriendCode()) lookCsv = StudyStore.MyLookCsv();
                var look = StudyStore.ParseLookCsv(lookCsv);
                var pivot = new GameObject("Hero" + index).transform;
                pivot.SetParent(studio.transform, false);
                pivot.localPosition = new Vector3(xs[slot], 0f, index == 0 ? 0.25f : 0f);
                HeroAppearance.SkinKind kind;
                var hero = HeroAppearance.SpawnLooked(pivot, Vector3.zero, Quaternion.Euler(0f, 180f, 0f),
                    i => (i >= 0 && i < look.Length) ? look[i] : 0, out kind);
                if (hero == null) continue;
                hero.transform.localScale = Vector3.one * scales[slot];
                SetLayer(hero, 30);
                var fighter = hero.AddComponent<BattleFighter>();
                fighter.Bind(hero.transform, false, kind, hero.name);
            }

            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(studio.transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.03f, 0.04f, 1f);
            cam.fieldOfView = 28f;
            cam.aspect = 900f / 420f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 30f;
            cam.targetTexture = preview;
            cam.cullingMask = 1 << 30;
            cam.allowHDR = false;
            cam.transform.position = studio.transform.position + new Vector3(0f, 1.15f, -4.6f);
            cam.transform.LookAt(studio.transform.position + Vector3.up * 1.05f);
        }

        void ClearStudio()
        {
            if (studio != null) UnityEngine.Object.Destroy(studio);
            studio = null;
            cam = null;
            if (preview != null)
            {
                preview.Release();
                UnityEngine.Object.Destroy(preview);
                preview = null;
            }
        }

        static void SetLayer(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            var tr = go.transform;
            for (int i = 0; i < tr.childCount; i++)
                SetLayer(tr.GetChild(i).gameObject, layer);
        }

        static Color RankColor(int index)
        {
            if (index == 0) return new Color(0.72f, 0.52f, 0.08f, 1f);
            if (index == 1) return new Color(0.45f, 0.48f, 0.54f, 1f);
            if (index == 2) return new Color(0.55f, 0.32f, 0.16f, 1f);
            return RpgTheme.CardFill;
        }

        static string Display(RaidMemberView member)
        {
            if (member == null) return "";
            if (!string.IsNullOrEmpty(member.name)) return member.name;
            return member.code;
        }

        static string Initial(RaidMemberView member)
        {
            string name = Display(member);
            if (string.IsNullOrEmpty(name)) return "?";
            return name.Substring(0, 1);
        }

        static Sprite solid;

        static Sprite Solid()
        {
            if (solid != null) return solid;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            solid = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
            return solid;
        }

        static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Text Label(Transform parent, string name, Vector2 min, Vector2 max, string text, int size, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var label = go.GetComponent<Text>();
            label.font = font != null ? font : RpgTheme.UiFont();
            label.text = text ?? "";
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        Button Button(Transform parent, string name, Vector2 min, Vector2 max, string text, int size, Color bg, Color fg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var image = go.GetComponent<Image>();
            RpgTheme.PaintButton(image, bg);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            Label(go.transform, "Label", Vector2.zero, Vector2.one, text, size, fg, TextAnchor.MiddleCenter);
            return button;
        }
    }
}
