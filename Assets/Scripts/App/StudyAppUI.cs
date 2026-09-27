using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ShiftingMetropolis.Progress;
using ShiftingMetropolis.Battle;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftingMetropolis.App
{
    public class StudyAppUI : MonoBehaviour
    {
        enum Tab { Timeline, Record, Report, Growth, Gacha, Battle }
        enum ReportRange { Week, Month, Year }

        Font font;
        RectTransform root;
        GameObject timelinePage;
        GameObject recordPage;
        GameObject reportPage;
        GameObject growthPage;
        RectTransform growthHeroHost;
        RectTransform growthStatHost;
        RawImage growthHeroImage;
        Transform growthHeroPivot;
        Transform growthHeroRoot;
        Camera growthHeroCam;
        RenderTexture growthHeroRt;
        [SerializeField] GameObject growthHeroPrefab;
        bool growthLookMode;
        ScrollRect growthScroll;
        float growthScrollNorm = 1f;
        int growthScrollToken;
        HeroAppearance.SkinKind growthHeroKind;
        GameObject timerOverlay;
        GameObject addDialog;
        GameObject manualDialog;
        GameObject profileDialog;
        Text profileClimbText;
        GameObject friendDialog;
        InputField friendCodeInput;
        InputField friendNameInput;
        Text friendMyCodeText;
        Text friendStatusText;
        RectTransform friendListContent;
        GameObject confirmDialog;
        GameObject materialDetail;
        Text materialDetailName;
        Text materialDetailStats;
        Image materialDetailBook;
        StudyMaterial detailMaterial;
        GameObject reportDetail;
        RectTransform reportDetailContent;
        GameObject goalDialog;
        GameObject choicePicker;
        Text choiceTitle;
        RectTransform choiceList;
        Action<int> choicePicked;
        GameObject toastGo;
        Text toastText;
        float toastUntil;
        GameObject bpWarn;
        Text bpWarnText;
        InputField profileNameInput;
        InputField profileBioInput;
        InputField profileSchoolInput;
        InputField profileGoalInput;
        RectTransform profileAvatarHost;
        int profileColorIndex;
        readonly Button[] profileColorButtons = new Button[6];
        Text headerTitle;
        Text recordBanner;
        Text walletBpText;
        Text walletSubText;
        Text navBattleLabel;
        Text overlayTitle;
        Text overlayTime;
        Text overlayHint;
        Text overlayPauseLabel;
        Text liveBannerLabel;
        InputField addInput;
        int manualYear;
        int manualMonth;
        int manualDay;
        int manualHour;
        int manualMinute;
        int manualDurHours;
        string editingLogId;
        int manualDurMins;
        Text manualYearLabel;
        Text manualMonthLabel;
        Text manualDayLabel;
        Text manualHourLabel;
        Text manualMinuteLabel;
        Text manualDurHourLabel;
        Text manualDurMinLabel;
        int goalHour = 3;
        int goalMinute;
        Text goalHourLabel;
        Text goalMinuteLabel;
        ReportRange reportRange = ReportRange.Week;
        DateTime reportCursor = StudyStore.Now.Date;
        Text logMaterialTitle;
        StudyMaterial manualMaterial;
        StudySubject manualSubject = StudySubject.Math;
        readonly Button[] manualCatButtons = new Button[6];
        RectTransform manualMaterialHost;
        Text confirmText;
        Button confirmDeleteBtn;
        bool recordEditMode;
        bool timelineEditMode;
        bool rankRefreshing;
        Button headerEditBtn;
        Text headerEditLabel;
        Button headerAddBtn;
        Button headerLogEditBtn;
        Text headerLogEditLabel;
        Button headerManualBtn;
        GameObject gachaRatePanel;
        readonly Image[] navIcons = new Image[6];
        readonly Text[] navLabels = new Text[6];
        readonly Button[] navButtons = new Button[6];
        GameObject gachaPage;
        GameObject battlePage;
        RaidScreen raidScreen;
        GameObject exchangePage;
        Tab tab = Tab.Record;
        StudySubject addSubject = StudySubject.Math;
        readonly Button[] addSubjectButtons = new Button[6];
        string pendingDeleteId;
        string pendingDeleteLogId;
        bool ignoreNextClick;

        static readonly Color Bg = RpgTheme.Bg;
        static readonly Color Card = RpgTheme.CardFill;
        static readonly Color Accent = RpgTheme.Gold;
        static readonly Color Muted = RpgTheme.Muted;

        void Awake()
        {
            font = RpgTheme.UiFont();
            root = GetComponent<RectTransform>();
            if (root != null && root.localScale.sqrMagnitude < 0.01f)
                root.localScale = Vector3.one;
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas.pixelPerfect = false;
            ClearChildren();
            BuildShell();
            ShowTab(Tab.Record);
        }

        void Update()
        {
            RefreshTimerWidgets();
            RefreshWallet();
            if (toastGo != null && toastGo.activeSelf && Time.unscaledTime >= toastUntil)
                toastGo.SetActive(false);
            if (growthHeroRoot != null)
                growthHeroRoot.localPosition = Vector3.zero;
        }

        public void RefreshGrowthView()
        {
            if (tab == Tab.Growth) RebuildGrowth();
        }

        void RefreshWallet()
        {
            StudyStore.CanEnterBattle();
            if (StudyStore.RolloverWeeklyGoal())
            {
                if (tab == Tab.Timeline) RebuildTimeline();
                if (tab == Tab.Report) RebuildReport();
            }
            if (walletBpText != null)
            {
                walletBpText.text = "BP  " + StudyStore.TodayBp + "   育成  " + StudyStore.GrowthBp
                    + "   ダイヤ  " + StudyStore.Diamonds;
            }
            if (walletSubText != null)
            {
                walletSubText.text = "Lv." + StudyStore.PlayerLevel()
                    + "    " + StudyStore.WeekFloorText()
                    + "    総獲得 " + StudyStore.TotalEarnedBp
                    + "\nブレイン残り\u00A0" + StudyStore.BrainSmashLeftToday() + "回";
            }
            if (navBattleLabel != null)
            {
                if (StudyStore.HasPausedBattle())
                {
                    navBattleLabel.color = Color.white;
                    navBattleLabel.fontSize = 22;
                    navBattleLabel.text = "再開";
                }
                else
                {
                    bool ok = StudyStore.TodayBp >= StudyStore.BattleCostBp;
                    navBattleLabel.color = Color.white;
                    navBattleLabel.fontSize = ok ? 18 : 22;
                    navBattleLabel.text = ok ? "バトル\n" + StudyStore.BattleCostBp + "BP" : "BP不足";
                }
            }
            var battleNav = ResolveNav(5);
            if (battleNav != null)
            {
                RpgTheme.PaintButton(battleNav.GetComponent<Image>(), RpgTheme.ButtonFill);
                RpgTheme.AddZhuAccent(battleNav);
                float hp = StudyStore.HasPausedBattle() ? StudyStore.PausedEnemyHpRatio() : 1f;
                RpgTheme.SetZhuAccentRatio(battleNav, hp);
                var lab = battleNav.GetComponentInChildren<Text>();
                if (lab != null) lab.color = Color.white;
            }
        }

        void ClearChildren()
        {
            var doomed = new List<GameObject>();
            foreach (Transform child in transform) doomed.Add(child.gameObject);
            for (int i = 0; i < doomed.Count; i++) DestroyImmediate(doomed[i]);
        }

        void BuildShell()
        {
            Kit.Image(root, "AppBg", Vector2.zero, Vector2.one, Bg).raycastTarget = true;

            headerTitle = Kit.Label(root, "HeaderTitle", new Vector2(0.03f, 0.93f), new Vector2(0.24f, 0.99f),
                "記録する", 32, RpgTheme.GoldHi, TextAnchor.MiddleLeft);

            headerEditBtn = Kit.Button(root, "HeaderEdit", new Vector2(0.25f, 0.935f), new Vector2(0.40f, 0.985f),
                "編集", 18, Card, Color.white, font);
            headerEditLabel = headerEditBtn.GetComponentInChildren<Text>();
            headerEditBtn.onClick.AddListener(() =>
            {
                if (tab == Tab.Timeline) OpenProfileDialog();
                else ToggleRecordEditMode();
            });

            var addBtn = Kit.Button(root, "HeaderAdd", new Vector2(0.75f, 0.935f), new Vector2(0.97f, 0.985f),
                "追加", 18, RpgTheme.Gold, RpgTheme.Bg, font);
            headerAddBtn = addBtn;
            addBtn.onClick.AddListener(() => OpenAddDialog(StudySubject.Math));
            headerLogEditBtn = Kit.Button(root, "HeaderLogEdit", new Vector2(0.75f, 0.935f), new Vector2(0.97f, 0.985f),
                "編集", 18, Card, Color.white, font);
            headerLogEditLabel = headerLogEditBtn.GetComponentInChildren<Text>();
            headerLogEditBtn.onClick.AddListener(ToggleTimelineEdit);
            headerLogEditBtn.gameObject.SetActive(false);
            var shopBtn = Kit.Button(root, "HeaderShop", new Vector2(0.59f, 0.935f), new Vector2(0.73f, 0.985f),
                "交換", 18, RpgTheme.Gold, RpgTheme.Bg, font);
            shopBtn.onClick.AddListener(OpenExchange);
            var manualBtn = Kit.Button(root, "HeaderManual", new Vector2(0.41f, 0.935f), new Vector2(0.58f, 0.985f),
                "手入力", 16, Card, Color.white, font);
            headerManualBtn = manualBtn;
            manualBtn.onClick.AddListener(OpenManualForSelected);

            Kit.Window(root, "WalletBg", new Vector2(0.03f, 0.775f), new Vector2(0.97f, 0.925f));
            walletBpText = Kit.Label(root, "WalletBp", new Vector2(0.06f, 0.845f), new Vector2(0.94f, 0.915f),
                "BP  0", 28, Color.white, TextAnchor.MiddleLeft);
            Kit.Fit(walletBpText, 28);
            walletSubText = Kit.Label(root, "WalletSub", new Vector2(0.06f, 0.785f), new Vector2(0.94f, 0.845f),
                "", 20, RpgTheme.Muted, TextAnchor.UpperLeft);
            Kit.Fit(walletSubText, 20);
            recordBanner = Kit.Label(root, "RecordBanner", new Vector2(0.05f, 0.745f), new Vector2(0.97f, 0.775f),
                "", 18, Muted, TextAnchor.MiddleLeft);
            Kit.Fit(recordBanner, 18);

            timelinePage = MakePage("TimelinePage");
            recordPage = MakePage("RecordPage");
            reportPage = MakePage("ReportPage");
            growthPage = MakePage("GrowthPage");
            gachaPage = MakePage("GachaPage");
            battlePage = MakePage("BattlePage");
            exchangePage = MakeFullPage("ExchangePage");
            exchangePage.SetActive(false);
            SetupGrowthLayout();

            BuildNav();
            BuildTimerOverlay();
            BuildAddDialog();
            BuildManualDialog();
            BuildProfileDialog();
            BuildFriendDialog();
            BuildConfirmDialog();
            BuildMaterialDetail();
            BuildReportDetail();
            BuildGoalDialog();
            BuildBpWarn();
            BuildChoicePicker();
            BuildToast();
        }

        GameObject MakePage(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.11f);
            rt.anchorMax = new Vector2(1f, 0.745f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        GameObject MakeFullPage(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        void SetupGrowthLayout()
        {
            var statGo = new GameObject("StatHost");
            statGo.transform.SetParent(growthPage.transform, false);
            growthStatHost = statGo.AddComponent<RectTransform>();
            growthStatHost.anchorMin = Vector2.zero;
            growthStatHost.anchorMax = Vector2.one;
            growthStatHost.offsetMin = Vector2.zero;
            growthStatHost.offsetMax = Vector2.zero;
            EnsureGrowthHeroStudio();
        }

        GameObject LoadGrowthHeroPrefab(out bool modularParts)
        {
            modularParts = false;
#if UNITY_EDITOR
            if (HeroAppearance.HasAnySkin())
            {
                var skin = HeroAppearance.LoadSkinPrefab(StudyStore.LookSlot(HeroAppearance.SlotSkin), out var kind);
                if (skin != null)
                {
                    growthHeroKind = kind;
                    modularParts = kind == HeroAppearance.SkinKind.GanzSe;
                    return skin;
                }
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DoubleL/Model/Armature (1).prefab");
#else
            var built = HeroAppearance.LoadSkinPrefab(StudyStore.LookSlot(HeroAppearance.SlotSkin), out var builtKind);
            if (built != null)
            {
                growthHeroKind = builtKind;
                modularParts = builtKind == HeroAppearance.SkinKind.GanzSe;
                return built;
            }
            return null;
#endif
        }

        void EnsureGrowthHeroStudio()
        {
            if (growthHeroPivot != null) return;
            var studio = new GameObject("GrowthHeroStudio");
            studio.transform.position = new Vector3(0f, -2500f, 0f);

            var pivotGo = new GameObject("HeroPivot");
            pivotGo.transform.SetParent(studio.transform, false);
            growthHeroPivot = pivotGo.transform;

            var prefab = LoadGrowthHeroPrefab(out bool modularParts);
            if (prefab != null)
            {
                SpawnGrowthHero(prefab, modularParts);
            }
            else if (growthHeroHost != null)
            {
                Kit.Label(growthHeroHost, "Missing", new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.6f),
                    "キャラが見つかりません", 18, Muted, TextAnchor.MiddleCenter);
            }

            var lightGo = new GameObject("HeroLight");
            lightGo.transform.SetParent(studio.transform, false);
            lightGo.transform.localPosition = new Vector3(-1.2f, 2.4f, -1.5f);
            lightGo.transform.localRotation = Quaternion.Euler(40f, 30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = Color.white;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << 31;

            var fillGo = new GameObject("HeroFill");
            fillGo.transform.SetParent(studio.transform, false);
            fillGo.transform.localRotation = Quaternion.Euler(15f, -140f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fill.color = new Color(0.55f, 0.65f, 0.9f);
            fill.shadows = LightShadows.None;
            fill.cullingMask = 1 << 31;

            if (growthHeroRt == null)
            {
                growthHeroRt = new RenderTexture(768, 768, 16, RenderTextureFormat.ARGB32);
                growthHeroRt.Create();
            }
            var camGo = new GameObject("HeroCam");
            camGo.transform.SetParent(studio.transform, false);
            growthHeroCam = camGo.AddComponent<Camera>();
            growthHeroCam.clearFlags = CameraClearFlags.SolidColor;
            growthHeroCam.backgroundColor = RpgTheme.Bg;
            growthHeroCam.fieldOfView = 32f;
            growthHeroCam.aspect = 1f;
            growthHeroCam.nearClipPlane = 0.05f;
            growthHeroCam.farClipPlane = 25f;
            growthHeroCam.targetTexture = growthHeroRt;
            growthHeroCam.enabled = false;
            growthHeroCam.allowHDR = false;
            SetLayerRecursively(studio, 31);
            growthHeroCam.cullingMask = 1 << 31;
            FrameGrowthHero();
            if (growthHeroImage != null) growthHeroImage.texture = growthHeroRt;
        }

        void FrameGrowthHero()
        {
            if (growthHeroCam == null || growthHeroPivot == null) return;
            var rends = growthHeroPivot.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0)
            {
                growthHeroCam.transform.localPosition = new Vector3(0f, 1f, -3.2f);
                growthHeroCam.transform.LookAt(growthHeroPivot.position + Vector3.up);
                return;
            }
            var bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                if (rends[i].enabled) bounds.Encapsulate(rends[i].bounds);
            }
            Vector3 center = bounds.center;
            float size = Mathf.Max(0.6f, Mathf.Max(bounds.size.y, bounds.extents.x * 2f));
            growthHeroCam.transform.position = center + new Vector3(0f, size * 0.03f, -size * 1.85f);
            growthHeroCam.transform.LookAt(center + Vector3.up * (size * 0.02f));
        }

        void SpawnGrowthHero(GameObject prefab, bool modularParts)
        {
            var hero = Instantiate(prefab, growthHeroPivot);
            hero.SetActive(false);
            hero.transform.localPosition = Vector3.zero;
            hero.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            hero.transform.localScale = Vector3.one;
            growthHeroRoot = hero.transform;
            if (!ApplyHeroLook(hero.transform) && modularParts)
                KeepOnePartPerSlot(hero.transform);
            var anim = hero.GetComponentInChildren<Animator>();
            if (anim != null && hero.activeInHierarchy)
            {
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                TryPlayStandIdle(anim);
                anim.Update(0f);
            }
            else if (anim != null)
            {
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                TryPlayStandIdle(anim);
            }
            HeroAppearance.DressSpecial(hero, StudyStore.LookSlot(HeroAppearance.SlotSkin));
            HeroAppearance.FinalizeHeldWeapon(hero.transform, growthHeroKind);
            SetLayerRecursively(hero, 31);
            hero.SetActive(true);
            if (anim != null && anim.gameObject.activeInHierarchy) anim.Update(0f);
        }

        void RebuildGrowthHero()
        {
            if (growthHeroPivot == null) return;
            Quaternion rot = growthHeroPivot.rotation;
            for (int i = growthHeroPivot.childCount - 1; i >= 0; i--)
                DestroyImmediate(growthHeroPivot.GetChild(i).gameObject);
            growthHeroRoot = null;
            var prefab = LoadGrowthHeroPrefab(out bool modularParts);
            if (prefab != null) SpawnGrowthHero(prefab, modularParts);
            growthHeroPivot.rotation = rot;
            FrameGrowthHero();
        }

        void TryPlayStandIdle(Animator anim)
        {
            if (anim == null) return;
            bool female = HeroAppearance.LooksFemale(growthHeroKind, growthHeroRoot != null ? growthHeroRoot.name : null);
            var ctrl = PlayerAssets.Load<RuntimeAnimatorController>(
                "Assets/DoubleL/Demo/Animator/OneHand_Up_Idle.controller");
            if (ctrl == null) return;
            string clipPath = female
                ? "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
                : "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx";
            AnimationClip idle = PlayerAssets.LoadClip(clipPath);
            if (idle != null)
            {
                var ov = new AnimatorOverrideController(ctrl);
                ov["OneHand_Up_Idle"] = idle;
                anim.runtimeAnimatorController = ov;
            }
            else
            {
                anim.runtimeAnimatorController = ctrl;
            }
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        static Transform FindLookSlot(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) return all[i];
            }
            return null;
        }

        bool ApplyHeroLook(Transform hero)
        {
            if (hero == null) return false;
            HeroAppearance.Apply(hero, growthHeroKind, idx => StudyStore.LookSlot(idx));
            SetLayerRecursively(hero.gameObject, 31);
            var anim = hero.GetComponentInChildren<Animator>();
            if (anim != null && hero.gameObject.activeInHierarchy)
            {
                TryPlayStandIdle(anim);
                anim.Update(0f);
            }
            else if (anim != null)
                TryPlayStandIdle(anim);
            HeroAppearance.FinalizeHeldWeapon(hero, growthHeroKind);
            return true;
        }

        // uiSlot 0 = skin selector, uiSlot 1.. = skin-specific extra options.
        int LookUiSlotCount()
        {
            return 1 + HeroAppearance.ExtraSlotCount(growthHeroRoot, growthHeroKind);
        }

        int LookOptionCount(int uiSlot)
        {
            if (uiSlot == 0) return HeroAppearance.OptionCount(growthHeroKind, HeroAppearance.SlotSkin);
            return HeroAppearance.ExtraOptionCount(growthHeroRoot, growthHeroKind, uiSlot - 1);
        }

        string LookSlotLabel(int uiSlot)
        {
            if (uiSlot == 0) return "スキン";
            return HeroAppearance.ExtraSlotLabel(growthHeroKind, uiSlot - 1);
        }

        string LookOptionLabel(int uiSlot, int index, int count)
        {
            if (uiSlot == 0) return HeroAppearance.OptionLabel(growthHeroKind, HeroAppearance.SlotSkin, index, count);
            return HeroAppearance.ExtraOptionLabel(growthHeroKind, uiSlot - 1, index, count);
        }

        string ArmorOptionText(int uiSlot, int index, int count)
        {
            string name = LookOptionLabel(uiSlot, index, count);
            int extra = uiSlot - 1;
            if (uiSlot <= 0 || !HeroAppearance.IsArmorExtra(extra) || index <= 0) return name;
            string id = HeroAppearance.ArmorPieceId(extra, index);
            int ver = StudyStore.ArmorLevel(id);
            if (ver <= 0) return name;
            int bonus = StudyStore.ArmorPieceDefense(index, ver);
            return name + "\n" + StudyStore.VersionMark(ver) + "  防御+" + bonus;
        }

        static int NextOwnedArmor(int extra, int current, int dir, int count)
        {
            if (count <= 1) return 0;
            int step = dir >= 0 ? 1 : -1;
            for (int k = 1; k <= count; k++)
            {
                int n = current + step * k;
                n %= count;
                if (n < 0) n += count;
                if (n == 0) return 0;
                if (StudyStore.OwnsArmor(HeroAppearance.ArmorPieceId(extra, n))) return n;
            }
            return 0;
        }

        static void KeepOnePartPerSlot(Transform root)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform slot = all[i];
                int meshKids = 0;
                foreach (Transform child in slot)
                {
                    if (child.GetComponent<Renderer>() != null) meshKids++;
                }
                if (meshKids < 3) continue;
                bool kept = false;
                foreach (Transform child in slot)
                {
                    if (child.GetComponent<Renderer>() == null) continue;
                    child.gameObject.SetActive(!kept);
                    if (!kept) kept = true;
                }
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var tr = go.transform;
            for (int i = 0; i < tr.childCount; i++)
                SetLayerRecursively(tr.GetChild(i).gameObject, layer);
        }

        void OnDestroy()
        {
            if (raidScreen != null) raidScreen.Close();
            if (growthHeroRt != null)
            {
                growthHeroRt.Release();
                Destroy(growthHeroRt);
            }
        }

        void BuildNav()
        {
            var bar = Kit.Window(root, "BottomNav", new Vector2(0f, 0f), new Vector2(1f, 0.11f));
            string[] labels = { "タイムライン", "記録する", "レポート", "育成", "ガチャ", "バトル" };
            for (int i = 0; i < 6; i++)
            {
                float x0 = i / 6f;
                string label = i == 5
                    ? (StudyStore.HasPausedBattle() ? "再開" : "バトル\n" + StudyStore.BattleCostBp + "BP")
                    : labels[i];
                Color navBg = i == 5 ? RpgTheme.ButtonFill : Color.clear;
                Color navFg = i == 5 ? Color.white : Muted;
                var btn = Kit.Button(bar.rectTransform, "Nav_" + i, new Vector2(x0, 0f), new Vector2(x0 + 1f / 6f, 1f),
                    label, 13, navBg, navFg, font);
                if (i == 5) RpgTheme.AddZhuAccent(btn.transform);
                navButtons[i] = btn;
                navLabels[i] = btn.GetComponentInChildren<Text>();
                navIcons[i] = btn.GetComponent<Image>();
                if (i == 5) navBattleLabel = navLabels[i];
                int captured = i;
                btn.onClick.AddListener(() => OnNav(captured));
            }
        }

        Transform ResolveNav(int index)
        {
            if (navButtons[index] != null) return navButtons[index].transform;
            if (root == null) return null;
            var bar = root.Find("BottomNav");
            if (bar == null) return null;
            return bar.Find("Nav_" + index);
        }

        void PaintNav(Tab next)
        {
            for (int i = 0; i < 5; i++)
            {
                bool on = (int)next == i;
                var tr = ResolveNav(i);
                if (tr == null) continue;
                var img = tr.GetComponent<Image>();
                var lab = tr.GetComponentInChildren<Text>();
                if (lab != null) lab.color = on ? RpgTheme.Bg : Muted;
                if (img != null) RpgTheme.PaintButton(img, on ? RpgTheme.Gold : RpgTheme.ButtonFill);
            }
            var battle = ResolveNav(5);
            if (battle != null)
            {
                RpgTheme.PaintButton(battle.GetComponent<Image>(), RpgTheme.ButtonFill);
                RpgTheme.AddZhuAccent(battle);
                float hp = StudyStore.HasPausedBattle() ? StudyStore.PausedEnemyHpRatio() : 1f;
                RpgTheme.SetZhuAccentRatio(battle, hp);
                var lab = battle.GetComponentInChildren<Text>();
                if (lab != null) lab.color = Color.white;
            }
        }

        void OnNav(int index)
        {
            ShowTab((Tab)index);
        }

        void ShowTab(Tab next)
        {
            if (raidScreen != null && raidScreen.IsOpen) raidScreen.Close();
            tab = next;
            timelinePage.SetActive(next == Tab.Timeline);
            recordPage.SetActive(next == Tab.Record);
            reportPage.SetActive(next == Tab.Report);
            if (growthPage != null) growthPage.SetActive(next == Tab.Growth);
            if (gachaPage != null) gachaPage.SetActive(next == Tab.Gacha);
            if (battlePage != null) battlePage.SetActive(next == Tab.Battle);
            if (next != Tab.Timeline) timelineEditMode = false;
            if (next != Tab.Record) recordEditMode = false;
            if (recordBanner != null && next != Tab.Record)
            {
                recordBanner.gameObject.SetActive(false);
            }
            RefreshEditButton();
            if (headerTitle != null)
            {
                if (next == Tab.Timeline) headerTitle.text = "タイムライン";
                else if (next == Tab.Report) headerTitle.text = "レポート";
                else if (next == Tab.Growth) headerTitle.text = "育成";
                else if (next == Tab.Gacha) headerTitle.text = "ガチャ";
                else if (next == Tab.Battle) headerTitle.text = "出撃";
                else headerTitle.text = "記録する";
            }
            bool studyHeader = next != Tab.Growth && next != Tab.Gacha && next != Tab.Battle;
            bool timeline = next == Tab.Timeline;
            if (headerAddBtn != null) headerAddBtn.gameObject.SetActive(studyHeader && !timeline);
            if (headerLogEditBtn != null) headerLogEditBtn.gameObject.SetActive(timeline);
            if (headerManualBtn != null) headerManualBtn.gameObject.SetActive(studyHeader);
            if (headerEditBtn != null)
            {
                bool record = next == Tab.Record;
                headerEditBtn.gameObject.SetActive(record || timeline);
                if (timeline)
                {
                    if (headerEditLabel != null)
                    {
                        headerEditLabel.text = "プロフィール";
                        headerEditLabel.color = Color.white;
                    }
                    var img = headerEditBtn.GetComponent<Image>();
                    if (img != null) RpgTheme.PaintButton(img, Card);
                }
            }
            PaintNav(next);

            if (growthHeroCam != null) growthHeroCam.enabled = next == Tab.Growth;
            Canvas.ForceUpdateCanvases();
            if (next == Tab.Timeline)
            {
                RebuildTimeline();
                if (SupabaseSync.Ready) StartCoroutine(RefreshTimelineFeed());
            }
            if (next == Tab.Record) RebuildRecord();
            if (next == Tab.Report) RebuildReport();
            if (next == Tab.Growth) RebuildGrowth();
            if (next == Tab.Gacha) RebuildGacha();
            if (next == Tab.Battle) RebuildSortie();
        }

        void ClearSpawned(Transform parent)
        {
            if (parent == null) return;
            var list = new List<GameObject>();
            foreach (Transform t in parent) list.Add(t.gameObject);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                list[i].SetActive(false);
                DestroyImmediate(list[i]);
            }
        }

        ScrollRect MakeScroll(Transform parent, string name)
        {
            var scrollGo = new GameObject(name);
            scrollGo.transform.SetParent(parent, false);
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.inertia = true;
            scroll.scrollSensitivity = 48f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(1, 1, 1, 0.002f);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0f, 20f);

            scroll.viewport = vrt;
            scroll.content = crt;
            return scroll;
        }

        float PageWidth()
        {
            if (root != null && root.rect.width > 40f) return root.rect.width;
            return Mathf.Max(320f, Screen.width);
        }

        void RebuildRecord()
        {
            float keepScroll = 1f;
            var oldScroll = recordPage.GetComponentInChildren<ScrollRect>();
            if (oldScroll != null) keepScroll = oldScroll.verticalNormalizedPosition;
            ClearSpawned(recordPage.transform);
            var scroll = MakeScroll(recordPage.transform, "RecordScroll");
            var content = scroll.content;
            float y = -12f;
            float width = recordPage != null ? recordPage.GetComponent<RectTransform>().rect.width : PageWidth();
            if (width < 40f) width = PageWidth();
            const int cols = 4;

            var timer = StudyTimer.Instance;
            liveBannerLabel = null;
            if (timer != null && timer.HasActiveSession && timer.CurrentMaterial != null)
            {
                string st = timer.IsPaused ? "一時停止" : "計測中";
                var banner = Kit.Button(content, "LiveBanner", new Vector2(0.04f, 0f), new Vector2(0.96f, 0f),
                    timer.CurrentMaterial.name + "  " + st + "  " + StudyTimer.FormatTime(timer.SessionSeconds),
                    22, RpgTheme.Navy, Color.white, font);
                RpgTheme.PaintBanner(banner.GetComponent<Image>(), RpgTheme.Zhu);
                var brt = banner.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.04f, 1f);
                brt.anchorMax = new Vector2(0.96f, 1f);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, y);
                brt.sizeDelta = new Vector2(0f, 64f);
                liveBannerLabel = banner.GetComponentInChildren<Text>();
                banner.onClick.AddListener(() =>
                {
                    timerOverlay.SetActive(true);
                    RefreshTimerWidgets();
                });
                y -= 76f;
            }

            for (int s = 0; s < 6; s++)
            {
                var subject = StudyStore.SubjectAt(s);
                float titleRight = recordEditMode ? 0.48f : 0.7f;
                var header = Kit.Label(content, "Cat_" + s, new Vector2(0.04f, 1f), new Vector2(titleRight, 1f),
                    StudySubjectNames.Display(subject), 28, Color.white, TextAnchor.MiddleLeft);
                var hrt = header.rectTransform;
                hrt.pivot = new Vector2(0f, 1f);
                hrt.anchorMin = new Vector2(0.04f, 1f);
                hrt.anchorMax = new Vector2(titleRight, 1f);
                hrt.anchoredPosition = new Vector2(0f, y);
                hrt.sizeDelta = new Vector2(0f, 42f);
                header.color = StudySubjectColors.Color(subject);
                if (recordEditMode)
                {
                    var up = Kit.Button(content, "CatUp_" + s, new Vector2(0.50f, 1f), new Vector2(0.63f, 1f),
                        "上", 16, RpgTheme.Gold, RpgTheme.Bg, font);
                    var urt = up.GetComponent<RectTransform>();
                    urt.pivot = new Vector2(0.5f, 1f);
                    urt.anchorMin = new Vector2(0.50f, 1f);
                    urt.anchorMax = new Vector2(0.63f, 1f);
                    urt.anchoredPosition = new Vector2(0f, y);
                    urt.sizeDelta = new Vector2(0f, 42f);
                    up.interactable = s > 0;
                    var upCols = up.colors;
                    upCols.disabledColor = Color.white;
                    up.colors = upCols;
                    var upSubject = subject;
                    up.onClick.AddListener(() =>
                    {
                        StudyStore.MoveSubject(upSubject, -1);
                        RebuildRecord();
                    });
                    var down = Kit.Button(content, "CatDown_" + s, new Vector2(0.64f, 1f), new Vector2(0.77f, 1f),
                        "下", 16, RpgTheme.Gold, RpgTheme.Bg, font);
                    var drt = down.GetComponent<RectTransform>();
                    drt.pivot = new Vector2(0.5f, 1f);
                    drt.anchorMin = new Vector2(0.64f, 1f);
                    drt.anchorMax = new Vector2(0.77f, 1f);
                    drt.anchoredPosition = new Vector2(0f, y);
                    drt.sizeDelta = new Vector2(0f, 42f);
                    down.interactable = s < 5;
                    var downCols = down.colors;
                    downCols.disabledColor = Color.white;
                    down.colors = downCols;
                    var downSubject = subject;
                    down.onClick.AddListener(() =>
                    {
                        StudyStore.MoveSubject(downSubject, 1);
                        RebuildRecord();
                    });
                }

                var plus = Kit.Button(content, "AddCat_" + s, new Vector2(0.78f, 1f), new Vector2(0.96f, 1f),
                    "", 28, Color.white, Color.white, font);
                RpgTheme.PaintPlus(plus.GetComponent<Image>());
                var prt = plus.GetComponent<RectTransform>();
                prt.pivot = new Vector2(1f, 1f);
                prt.anchorMin = new Vector2(0.78f, 1f);
                prt.anchorMax = new Vector2(0.96f, 1f);
                prt.anchoredPosition = new Vector2(0f, y);
                prt.sizeDelta = new Vector2(0f, 42f);
                var capturedSubject = subject;
                plus.onClick.AddListener(() => OpenAddDialog(capturedSubject));
                y -= 50f;

                var materials = StudyStore.MaterialsFor(subject);
                if (materials.Count == 0)
                {
                    var empty = Kit.Label(content, "Empty_" + s, new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                        "教材なし", 22, Muted, TextAnchor.MiddleLeft);
                    empty.rectTransform.pivot = new Vector2(0f, 1f);
                    empty.rectTransform.anchorMin = new Vector2(0.06f, 1f);
                    empty.rectTransform.anchorMax = new Vector2(0.94f, 1f);
                    empty.rectTransform.anchoredPosition = new Vector2(0f, y);
                    empty.rectTransform.sizeDelta = new Vector2(0f, 34f);
                    y -= 46f;
                    continue;
                }

                float cellW = (width - 48f) / cols;
                float cellH = 176f;
                for (int i = 0; i < materials.Count; i++)
                {
                    int col = i % cols;
                    int row = i / cols;
                    float x = 24f + col * cellW;
                    float cy = y - row * cellH;
                    CreateMaterialCard(content, materials[i], x, cy, cellW - 10f, cellH - 10f, i > 0, i < materials.Count - 1);
                }
                int rows = (materials.Count + cols - 1) / cols;
                y -= rows * cellH + 8f;
            }

            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 40f);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = keepScroll;
        }

        void ToggleTimelineEdit()
        {
            timelineEditMode = !timelineEditMode;
            RefreshEditButton();
            if (tab == Tab.Timeline) RebuildTimeline();
        }
        void ToggleRecordEditMode()
        {
            recordEditMode = !recordEditMode;
            RefreshEditButton();
            if (tab == Tab.Record) RebuildRecord();
        }

        void RefreshEditButton()
        {
            bool record = tab == Tab.Record;
            bool timeline = tab == Tab.Timeline;
            if (headerEditBtn != null) headerEditBtn.gameObject.SetActive(record || timeline);
            if (headerEditLabel != null)
            {
                headerEditLabel.text = timeline ? "プロフィール" : (recordEditMode ? "完了" : "編集");
            }
            var img = headerEditBtn != null ? headerEditBtn.GetComponent<Image>() : null;
            if (img != null)
            {
                bool editOn = !timeline && recordEditMode;
                RpgTheme.PaintButton(img, editOn ? RpgTheme.Gold : Card);
            }
            if (headerLogEditLabel != null)
                headerLogEditLabel.text = timelineEditMode ? "完了" : "編集";
            var logImg = headerLogEditBtn != null ? headerLogEditBtn.GetComponent<Image>() : null;
            if (logImg != null) RpgTheme.PaintButton(logImg, timelineEditMode ? RpgTheme.Gold : Card);
            if (headerLogEditLabel != null)
                headerLogEditLabel.color = timelineEditMode ? RpgTheme.Bg : Color.white;
            if (recordBanner != null && tab == Tab.Record)
            {
                recordBanner.gameObject.SetActive(true);
                var timer = StudyTimer.Instance;
                bool live = timer != null && timer.HasActiveSession && timer.CurrentMaterial != null;
                if (live)
                {
                    string st = timer.IsPaused ? "一時停止" : "計測中";
                    recordBanner.color = RpgTheme.Zhu;
                    recordBanner.text = timer.CurrentMaterial.name + "  " + st + "  " + StudyTimer.FormatTime(timer.SessionSeconds);
                }
                else
                {
                    recordBanner.color = Muted;
                    recordBanner.text = string.Empty;
                }
            }
        }

        void CreateMaterialCard(RectTransform parent, StudyMaterial material, float x, float y, float w, float h, bool canLeft, bool canRight)
        {
            var go = new GameObject("Mat_" + material.id);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.AddComponent<Image>();
            RpgTheme.PaintNavyCard(img);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var captured = material;
            bool measuring = StudyStore.IsMaterialMeasuring(material.id);
            btn.onClick.AddListener(() =>
            {
                if (ignoreNextClick)
                {
                    ignoreNextClick = false;
                    return;
                }
                if (recordEditMode)
                {
                    if (StudyStore.IsMaterialMeasuring(captured.id))
                        ShowToast("計測中の教材は削除できません");
                    return;
                }
                OnTapMaterial(captured);
            });

            var thumb = Kit.Image(rt, "Thumb", Vector2.zero, Vector2.one, new Color(0.04f, 0.06f, 0.16f, 1f));
            var trt = thumb.rectTransform;
            trt.anchorMin = new Vector2(0.08f, 0.50f);
            trt.anchorMax = new Vector2(0.92f, 0.97f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            thumb.raycastTarget = false;

            var book = Kit.Image(rt, "Book", new Vector2(0.16f, 0.52f), new Vector2(0.84f, 0.97f), Color.white);
            RpgTheme.PaintBook(book, StudySubjectColors.Color((StudySubject)material.subject), RpgTheme.BookVariant(material.id));

            Kit.Label(rt, "Name", new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.50f),
                material.name, 18, Color.white, TextAnchor.MiddleCenter);

            if (recordEditMode)
            {
                var left = Kit.Button(rt, "Left", new Vector2(0.04f, 0.02f), new Vector2(0.32f, 0.20f),
                    "左", 15, RpgTheme.Gold, RpgTheme.Bg, font);
                left.interactable = canLeft;
                var leftCols = left.colors;
                leftCols.disabledColor = Color.white;
                left.colors = leftCols;
                string moveId = captured.id;
                left.onClick.AddListener(() =>
                {
                    ignoreNextClick = true;
                    if (StudyStore.MoveMaterial(moveId, -1)) RebuildRecord();
                });
                if (measuring)
                {
                    var locked = Kit.Button(rt, "Locked", new Vector2(0.34f, 0.02f), new Vector2(0.66f, 0.20f),
                        "計測中", 12, RpgTheme.ButtonFill, RpgTheme.Zhu, font);
                    locked.interactable = false;
                }
                else
                {
                    var del = Kit.Button(rt, "Delete", new Vector2(0.34f, 0.02f), new Vector2(0.66f, 0.20f),
                        "削除", 14, RpgTheme.ButtonFill, RpgTheme.Zhu, font);
                    del.onClick.AddListener(() =>
                    {
                        ignoreNextClick = true;
                        OpenDeleteConfirm(captured.id);
                    });
                }
                var right = Kit.Button(rt, "Right", new Vector2(0.68f, 0.02f), new Vector2(0.96f, 0.20f),
                    "右", 15, RpgTheme.Gold, RpgTheme.Bg, font);
                right.interactable = canRight;
                var rightCols = right.colors;
                rightCols.disabledColor = Color.white;
                right.colors = rightCols;
                right.onClick.AddListener(() =>
                {
                    ignoreNextClick = true;
                    if (StudyStore.MoveMaterial(moveId, 1)) RebuildRecord();
                });
            }
            else
            {
                var manual = Kit.Button(rt, "Manual", new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.20f),
                    "手入力", 16, new Color(0.22f, 0.22f, 0.26f), Color.white, font);
                manual.onClick.AddListener(() =>
                {
                    ignoreNextClick = true;
                    OpenManualDialog(captured);
                });
            }
        }

        void OnTapMaterial(StudyMaterial material)
        {
            OpenMaterialDetail(material);
        }

        void RebuildTimeline()
        {
            ClearSpawned(timelinePage.transform);
            string backdrop = StudyStore.EquippedCosmetic("backdrop");
            if (!string.IsNullOrEmpty(backdrop))
                DressPageRim(timelinePage.transform, backdrop);
            var scroll = MakeScroll(timelinePage.transform, "TimelineScroll");
            var content = scroll.content;
            float y = -8f;
            y = DrawFriendManageRow(content, y);
            y = DrawStreakCard(content, y);

            var friends = StudyStore.FriendList;
            var items = CollectTimeline();
            if (items.Count == 0 && friends.Count == 0)
            {
                var empty = Kit.Label(content, "EmptyLogs", new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
                    "まだ記録がありません。\n教材を追加して計測を始めると、ここに履歴が出ます。", 22, Muted, TextAnchor.UpperLeft);
                empty.rectTransform.pivot = new Vector2(0.5f, 1f);
                empty.rectTransform.anchoredPosition = new Vector2(0f, y);
                empty.rectTransform.sizeDelta = new Vector2(0f, 120f);
                content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 140f);
                return;
            }

            var posted = new HashSet<string>();
            for (int i = 0; i < items.Count; i++)
            {
                string author = items[i].ev != null ? items[i].ev.authorCode : items[i].log.authorCode;
                if (!string.IsNullOrEmpty(author)) posted.Add(author.Trim().ToUpperInvariant());
            }
            const int TimelineShowMax = 80;
            int total = items.Count;
            if (items.Count > TimelineShowMax) items.RemoveRange(TimelineShowMax, items.Count - TimelineShowMax);
            for (int i = 0; i < items.Count; i++)
            {
                string author = items[i].ev != null ? items[i].ev.authorCode : items[i].log.authorCode;
                FriendEntry friend = StudyStore.IsSelfAuthor(author) ? null : StudyStore.FindFriend(author);
                if (items[i].ev != null) y = DrawGoalEventCard(content, items[i].ev, y, friend);
                else y = DrawLogCard(content, items[i].log, y, friend);
            }
            if (total > TimelineShowMax)
            {
                var more = Kit.Label(content, "More", new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
                    "これより前は省略（" + total + "件中 " + TimelineShowMax + "件）", 16, Muted, TextAnchor.MiddleCenter);
                more.rectTransform.pivot = new Vector2(0.5f, 1f);
                more.rectTransform.anchorMin = new Vector2(0.08f, 1f);
                more.rectTransform.anchorMax = new Vector2(0.92f, 1f);
                more.rectTransform.anchoredPosition = new Vector2(0f, y);
                more.rectTransform.sizeDelta = new Vector2(0f, 36f);
                y -= 40f;
            }
            for (int f = 0; f < friends.Count; f++)
            {
                var friend = friends[f];
                if (friend == null) continue;
                if (!posted.Contains(friend.code.Trim().ToUpperInvariant()))
                    y = DrawFriendQuietCard(content, friend, y);
            }
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 20f);
        }

        List<TimelineRow> CollectTimeline()
        {
            var logs = StudyStore.LogsNewestFirst();
            var events = StudyStore.AllEvents;
            var items = new List<TimelineRow>();
            var seen = new HashSet<string>();
            for (int i = 0; i < logs.Count; i++)
            {
                if (logs[i] == null || !VisibleOnTimeline(logs[i].authorCode)) continue;
                DateTime at;
                if (!DateTime.TryParse(logs[i].startedAt, out at)) at = DateTime.MinValue;
                items.Add(new TimelineRow { at = at, log = logs[i] });
                if (!string.IsNullOrEmpty(logs[i].id)) seen.Add(logs[i].id);
            }
            var remote = StudyStore.RemoteLogList;
            for (int i = 0; i < remote.Count; i++)
            {
                var log = remote[i];
                if (log == null || !VisibleOnTimeline(log.authorCode)) continue;
                if (!string.IsNullOrEmpty(log.id) && seen.Contains(log.id)) continue;
                DateTime at;
                if (!DateTime.TryParse(log.startedAt, out at)) at = DateTime.MinValue;
                items.Add(new TimelineRow { at = at, log = log });
            }
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] == null || !VisibleOnTimeline(events[i].authorCode)) continue;
                DateTime at;
                if (!DateTime.TryParse(events[i].createdAt, out at)) at = DateTime.MinValue;
                items.Add(new TimelineRow { at = at, ev = events[i] });
            }
            items.Sort((a, b) => b.at.CompareTo(a.at));
            return items;
        }

        static bool VisibleOnTimeline(string authorCode)
        {
            return StudyStore.IsSelfAuthor(authorCode) || StudyStore.IsFriendAuthor(authorCode);
        }

        static bool SameFriendCode(string author, string code)
        {
            if (string.IsNullOrEmpty(author) || string.IsNullOrEmpty(code)) return false;
            return string.Equals(author.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        float DrawFriendManageRow(RectTransform content, float y)
        {
            var btn = Kit.Button(content, "OpenFriends", new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                "フレンド", 22, RpgTheme.Gold, RpgTheme.Bg, font);
            var rt = btn.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 64f);
            btn.onClick.AddListener(OpenFriendRank);
            return y - 76f;
        }

        float DrawFriendQuietCard(RectTransform content, FriendEntry friend, float y)
        {
            var card = Kit.Card(content, "Friend", Vector2.zero, Vector2.one);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 108f);
            DressFriendCard(card, friend);
            DrawFriendAvatar(rt, friend, 64f, new Vector2(0f, 0.5f), new Vector2(46f, 0f));
            var name = Kit.Label(rt, "User", new Vector2(0f, 0.52f), new Vector2(0.94f, 0.92f),
                friend.name, 22, Color.white, TextAnchor.MiddleLeft);
            float nameLeft = PlaceHonorFor(rt, name, 96f, StudyStore.ParseCosmeticFromCsv(friend.cosmeticsCsv, "title"));
            name.rectTransform.offsetMin = new Vector2(nameLeft, 0f);
            var body = Kit.Label(rt, "Body", new Vector2(0f, 0.08f), new Vector2(0.96f, 0.52f),
                StudyStore.ClimbLine(friend.week, friend.floor), 18, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            body.rectTransform.offsetMin = new Vector2(96f, 0f);
            var open = card.gameObject.AddComponent<Button>();
            open.targetGraphic = card;
            FriendEntry captured = friend;
            open.onClick.AddListener(() => OpenFriendProfile(captured));
            return y - 120f;
        }

        float DrawStreakCard(RectTransform content, float y)
        {
            int current = StudyStore.CurrentStreakDays();
            int best = StudyStore.BestStreakDays();
            var card = Kit.Card(content, "Streak", Vector2.zero, Vector2.one);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 88f);
            Kit.Label(rt, "SCurT", new Vector2(0.06f, 0.52f), new Vector2(0.48f, 0.88f),
                "連続", 16, Muted, TextAnchor.MiddleLeft);
            Kit.Label(rt, "SCurV", new Vector2(0.06f, 0.10f), new Vector2(0.48f, 0.58f),
                current + "日", 32, Color.white, TextAnchor.MiddleLeft);
            Kit.Label(rt, "SBestT", new Vector2(0.52f, 0.52f), new Vector2(0.94f, 0.88f),
                "最長", 16, Muted, TextAnchor.MiddleLeft);
            Kit.Label(rt, "SBestV", new Vector2(0.52f, 0.10f), new Vector2(0.94f, 0.58f),
                best + "日", 32, Accent, TextAnchor.MiddleLeft);
            return y - 100f;
        }

        struct TimelineRow
        {
            public DateTime at;
            public StudyLogEntry log;
            public TimelineEvent ev;
        }

        float DrawLogCard(RectTransform content, StudyLogEntry log, float y, FriendEntry author)
        {
            var card = Kit.Card(content, "Log", Vector2.zero, Vector2.one);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 148f);
            if (author == null) DressOwnCard(card);
            else DressFriendCard(card, author);

            if (author != null) DrawFriendAvatar(rt, author, 64f, new Vector2(0f, 0.5f), new Vector2(46f, 0f));
            else DrawAvatar(rt, true, 64f, new Vector2(0f, 0.5f), new Vector2(46f, 0f));
            const float textLeft = 96f;
            string shownName = author != null ? author.name : StudyStore.ProfileName();
            var nameBtn = Kit.Label(rt, "User", new Vector2(0f, 0.70f), new Vector2(0.58f, 0.95f),
                shownName, 20, Color.white, TextAnchor.MiddleLeft);
            float nameLeft = author == null
                ? PlaceHonor(rt, nameBtn, textLeft)
                : PlaceHonorFor(rt, nameBtn, textLeft, StudyStore.ParseCosmeticFromCsv(author.cosmeticsCsv, "title"));
            nameBtn.rectTransform.offsetMin = new Vector2(nameLeft, 0f);
            if (author == null)
            {
                nameBtn.raycastTarget = true;
                var nameHit = nameBtn.gameObject.AddComponent<Button>();
                nameHit.targetGraphic = nameBtn;
                nameHit.onClick.AddListener(OpenProfileDialog);
            }
            else
            {
                nameBtn.raycastTarget = true;
                var nameHit = nameBtn.gameObject.AddComponent<Button>();
                nameHit.targetGraphic = nameBtn;
                FriendEntry shown = author;
                nameHit.onClick.AddListener(() => OpenFriendProfile(shown));
            }
            Kit.Label(rt, "When", new Vector2(0.58f, 0.70f), new Vector2(0.97f, 0.95f),
                StudyStore.FormatLogTime(log.startedAt), 20, Muted, TextAnchor.MiddleRight);
            var matLabel = Kit.Label(rt, "Name", new Vector2(0f, 0.38f), new Vector2(0.96f, 0.70f),
                log.materialName, 22, Color.white, TextAnchor.MiddleLeft);
            matLabel.rectTransform.offsetMin = new Vector2(textLeft, 0f);
            matLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            matLabel.verticalOverflow = VerticalWrapMode.Truncate;
            string subjectName = StudySubjectNames.Display((StudySubject)log.subject);
            string mins = log.open
                ? subjectName + "  計測中 " + StudyStore.FormatDuration(log)
                : subjectName + "  " + StudyStore.FormatDuration(log);
            var minLabel = Kit.Label(rt, "Min", new Vector2(0f, 0.06f), new Vector2(0.96f, 0.38f),
                mins, 26, Color.white, TextAnchor.MiddleLeft);
            minLabel.rectTransform.offsetMin = new Vector2(textLeft, 0f);
            minLabel.color = StudySubjectColors.Color((StudySubject)log.subject);
            if (timelineEditMode && author == null && !log.open)
            {
                var timeBtn = Kit.Button(rt, "Time", new Vector2(0.52f, 0.08f), new Vector2(0.74f, 0.36f),
                    "時間", 16, new Color(0.22f, 0.22f, 0.25f), Color.white, font);
                var delBtn = Kit.Button(rt, "Del", new Vector2(0.76f, 0.08f), new Vector2(0.96f, 0.36f),
                    "削除", 16, RpgTheme.ButtonFill, RpgTheme.Zhu, font);
                StudyLogEntry capturedLog = log;
                timeBtn.onClick.AddListener(() => OpenLogEditor(capturedLog));
                delBtn.onClick.AddListener(() => AskDeleteLog(capturedLog));
            }
            return y - 160f;
        }

        float DrawGoalEventCard(RectTransform content, TimelineEvent ev, float y, FriendEntry author)
        {
            bool result = ev.kind == "week_result";
            bool ok = result && ev.goalMinutes > 0 && ev.doneMinutes >= ev.goalMinutes;
            var card = Kit.Card(content, "Ev", Vector2.zero, Vector2.one);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 132f);
            if (author == null) DressOwnCard(card);
            else DressFriendCard(card, author);

            if (author != null) DrawFriendAvatar(rt, author, 64f, new Vector2(0f, 0.5f), new Vector2(46f, 0f));
            else DrawAvatar(rt, true, 64f, new Vector2(0f, 0.5f), new Vector2(46f, 0f));
            const float textLeft = 96f;
            Kit.Label(rt, "When", new Vector2(0.58f, 0.72f), new Vector2(0.97f, 0.95f),
                StudyStore.FormatLogTime(ev.createdAt), 18, Muted, TextAnchor.MiddleRight);
            var title = Kit.Label(rt, "Title", new Vector2(0f, 0.48f), new Vector2(0.96f, 0.78f),
                result ? "今週の振り返り" : "今週の目標を設定", 22, result ? (ok ? RpgTheme.GoldHi : RpgTheme.Danger) : Color.white, TextAnchor.MiddleLeft);
            title.rectTransform.offsetMin = new Vector2(textLeft, 0f);
            string body;
            if (result)
            {
                int pct = ev.goalMinutes <= 0 ? 0 : Mathf.RoundToInt(100f * ev.doneMinutes / ev.goalMinutes);
                body = "目標 " + StudyStore.FormatMinutes(ev.goalMinutes) + "  /  実際 "
                    + StudyStore.FormatMinutes(ev.doneMinutes) + "  (" + pct + "%)\n"
                    + (ok ? "達成！" : "未達成");
                if (ok && author == null)
                {
                    string mark = SealMark(StudyStore.EquippedCosmetic("seal"));
                    if (mark.Length > 0) body += "  " + mark;
                }
            }
            else
            {
                body = StudyStore.FormatMinutes(ev.goalMinutes) + "  でがんばる";
            }
            var b = Kit.Label(rt, "Body", new Vector2(0f, 0.06f), new Vector2(0.96f, 0.50f),
                body, 18, Color.white, TextAnchor.MiddleLeft);
            b.rectTransform.offsetMin = new Vector2(textLeft, 0f);
            return y - 144f;
        }

        void DrawFriendAvatar(RectTransform parent, FriendEntry friend, float size, Vector2 anchor, Vector2 pos)
        {
            string border = friend != null ? StudyStore.ParseCosmeticFromCsv(friend.cosmeticsCsv, "border") : "";
            if (!string.IsNullOrEmpty(border))
            {
                Color ring = border == "border_ink"
                    ? new Color(0.82f, 0.86f, 0.94f)
                    : border == "border_peach"
                        ? new Color(0.95f, 0.52f, 0.58f)
                        : RpgTheme.GoldHi;
                var ringGo = new GameObject("Ring");
                ringGo.transform.SetParent(parent, false);
                var ringRt = ringGo.AddComponent<RectTransform>();
                ringRt.anchorMin = anchor;
                ringRt.anchorMax = anchor;
                ringRt.pivot = new Vector2(0.5f, 0.5f);
                ringRt.sizeDelta = new Vector2(size + 10f, size + 10f);
                ringRt.anchoredPosition = pos;
                var ringImg = ringGo.AddComponent<Image>();
                Kit.MakeCircle(ringImg);
                ringImg.color = ring;
                ringImg.raycastTarget = false;
            }
            var go = new GameObject("Avatar");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            Kit.MakeCircle(img);
            int color = friend != null ? friend.colorIndex : 0;
            img.color = StudySubjectColors.Color((StudySubject)Mathf.Clamp(color, 0, 5));
            string ini = friend != null && !string.IsNullOrEmpty(friend.name) ? friend.name.Substring(0, 1) : "?";
            Kit.Label(rt, "Ini", Vector2.zero, Vector2.one,
                ini, Mathf.Max(12, Mathf.RoundToInt(size * 0.4f)), Color.white, TextAnchor.MiddleCenter);
            if (friend != null && !StudyStore.IsSelfAuthor(friend.code))
            {
                var tapBtn = go.AddComponent<Button>();
                tapBtn.targetGraphic = img;
                FriendEntry captured = friend;
                tapBtn.onClick.AddListener(() => OpenFriendProfile(captured));
            }
        }

        void DrawAvatar(RectTransform parent, bool openOnTap, float size, Vector2 anchor, Vector2 pos)
        {
            string border = StudyStore.EquippedCosmetic("border");
            if (!string.IsNullOrEmpty(border))
            {
                Color ring = border == "border_ink"
                    ? new Color(0.82f, 0.86f, 0.94f)
                    : border == "border_peach"
                        ? new Color(0.95f, 0.52f, 0.58f)
                        : RpgTheme.GoldHi;
                var ringGo = new GameObject("Ring");
                ringGo.transform.SetParent(parent, false);
                var ringRt = ringGo.AddComponent<RectTransform>();
                ringRt.anchorMin = anchor;
                ringRt.anchorMax = anchor;
                ringRt.pivot = new Vector2(0.5f, 0.5f);
                ringRt.sizeDelta = new Vector2(size + 14f, size + 14f);
                ringRt.anchoredPosition = pos;
                var ringImg = ringGo.AddComponent<Image>();
                Kit.MakeCircle(ringImg);
                ringImg.color = ring;
                ringImg.raycastTarget = false;
                if (LuxLayers(border) >= 2)
                {
                    var outer = new GameObject("RingOuter");
                    outer.transform.SetParent(parent, false);
                    var outerRt = outer.AddComponent<RectTransform>();
                    outerRt.anchorMin = anchor;
                    outerRt.anchorMax = anchor;
                    outerRt.pivot = new Vector2(0.5f, 0.5f);
                    outerRt.sizeDelta = new Vector2(size + 26f, size + 26f);
                    outerRt.anchoredPosition = pos;
                    var outerImg = outer.AddComponent<Image>();
                    Kit.MakeCircle(outerImg);
                    outerImg.color = LuxLayers(border) >= 3 ? RpgTheme.Zhu : ring;
                    outerImg.raycastTarget = false;
                    outer.transform.SetSiblingIndex(ringGo.transform.GetSiblingIndex());
                }
            }
            var go = new GameObject("Avatar");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;

            var maskImg = go.AddComponent<Image>();
            Kit.MakeCircle(maskImg);
            var photo = StudyStore.ProfileIconSprite();
            if (photo != null)
            {
                maskImg.color = Color.white;
                var mask = go.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                var photoGo = new GameObject("Photo");
                photoGo.transform.SetParent(go.transform, false);
                var prt = photoGo.AddComponent<RectTransform>();
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;
                var pimg = photoGo.AddComponent<Image>();
                pimg.sprite = photo;
                pimg.preserveAspect = false;
                pimg.color = Color.white;
                pimg.raycastTarget = false;
            }
            else
            {
                maskImg.color = StudyStore.ProfileColor();
                Kit.Label(rt, "Ini", Vector2.zero, Vector2.one,
                    StudyStore.ProfileInitial(), Mathf.Max(12, Mathf.RoundToInt(size * 0.4f)),
                    Color.white, TextAnchor.MiddleCenter);
            }

            if (!openOnTap) return;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = maskImg;
            btn.onClick.AddListener(OpenProfileDialog);
        }

        void RefreshProfilePreview()
        {
            if (profileAvatarHost == null) return;
            ClearSpawned(profileAvatarHost);
            DrawAvatar(profileAvatarHost, false, 64f, new Vector2(0.5f, 0.5f), Vector2.zero);
        }

        void RebuildReport()
        {
            ClearSpawned(reportPage.transform);
            var scroll = MakeScroll(reportPage.transform, "ReportScroll");
            var content = scroll.content;
            float y = -8f;

            y = DrawGoalCard(content, y);
            y -= 12f;

            KitPinnedLabel(content, "学習推移", 0.04f, y, 28, Color.white);
            y -= 42f;

            int today = StudyStore.MinutesOnDate(StudyStore.BusinessDate(StudyStore.Now));
            int month = StudyStore.MinutesThisMonth();
            int life = StudyStore.LifetimeMinutes();

            DrawStat(content, 0.04f, y, "今日", StudyStore.FormatMinutes(today));
            DrawStat(content, 0.36f, y, "今月", StudyStore.FormatMinutes(month));
            DrawStat(content, 0.68f, y, "累計", StudyStore.FormatMinutes(life));
            y -= 70f;

            DrawStat(content, 0.04f, y, "連続", StudyStore.CurrentStreakDays() + "日");
            DrawStat(content, 0.36f, y, "最長", StudyStore.BestStreakDays() + "日");
            DrawStat(content, 0.68f, y, "今日のBP", StudyStore.TodayBp.ToString());
            y -= 70f;

            DrawStat(content, 0.04f, y, "育成BP", StudyStore.GrowthBp.ToString());
            DrawStat(content, 0.36f, y, "総獲得BP", StudyStore.TotalEarnedBp.ToString());
            y -= 70f;

            var dia = Kit.Label(content, "DiaLine", new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                "ダイヤ  " + StudyStore.Diamonds + "\nブレイン残り\u00A0" + StudyStore.BrainSmashLeftToday() + "回",
                22, Accent, TextAnchor.MiddleLeft);
            Pin(dia.rectTransform, y, dia.fontSize * 2 + 12);
            y -= dia.fontSize * 2 + 16;

            KitPinnedLabel(content, "今週", 0.04f, y, 20, Muted);
            y -= 28f;
            reportRange = ReportRange.Week;
            reportCursor = StudyStore.Now.Date;
            y = DrawPeriodChart(content, y, true);

            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 40f);
        }

        float DrawGoalCard(RectTransform parent, float y)
        {
            int goal = StudyStore.WeeklyGoalMinutes();
            bool locked = StudyStore.HasLockedGoal();
            var weekStart = StudyStore.WeekStart(StudyStore.Now);
            int done = StudyStore.MinutesBetween(weekStart, weekStart.AddDays(7));
            float ratio = goal <= 0 ? 0f : Mathf.Clamp01(done / (float)goal);
            int pct = Mathf.RoundToInt(ratio * 100f);

            var card = Kit.Card(parent, "GoalCard", Vector2.zero, Vector2.one);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 210f);
            var tap = card.gameObject.AddComponent<Button>();
            tap.targetGraphic = card;
            tap.onClick.AddListener(() =>
            {
                if (StudyStore.HasLockedGoal()) ShowToast("今週は変更できません");
                else OpenGoalDialog();
            });

            var pieHost = Kit.Image(rt, "GoalPie", new Vector2(0.04f, 0.08f), new Vector2(0.42f, 0.92f), Color.clear);
            pieHost.raycastTarget = false;
            var bg = Kit.Image(pieHost.rectTransform, "GBg", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), RpgTheme.Navy);
            Kit.MakeCircle(bg);
            bg.raycastTarget = false;
            var slice = Kit.Image(pieHost.rectTransform, "GFill", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Accent);
            Kit.MakeCircle(slice);
            slice.type = UnityEngine.UI.Image.Type.Filled;
            slice.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            slice.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            slice.fillClockwise = true;
            slice.fillAmount = ratio;
            slice.raycastTarget = false;
            var hole = Kit.Image(pieHost.rectTransform, "GHole", new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.70f), RpgTheme.Navy);
            Kit.MakeCircle(hole);
            hole.raycastTarget = false;
            Kit.Label(pieHost.rectTransform, "GPct", Vector2.zero, Vector2.one, pct + "%", 22, Color.white, TextAnchor.MiddleCenter);

            Kit.Label(rt, "GTitle", new Vector2(0.44f, 0.68f), new Vector2(0.96f, 0.92f),
                "今週の目標", 18, Muted, TextAnchor.MiddleLeft);
            Kit.Label(rt, "GVal", new Vector2(0.44f, 0.18f), new Vector2(0.96f, 0.68f),
                locked
                    ? StudyStore.FormatMinutes(done) + "\n/ " + StudyStore.FormatMinutes(goal)
                    : "未設定", 22, Color.white, TextAnchor.MiddleLeft);
            return y - 222f;
        }

        float AppendGrowthHeroPreview(RectTransform content, float y)
        {
            const float heroSize = 480f;
            var card = Kit.Window(content, "HeroCard", Vector2.zero, Vector2.one);
            var crt = card.rectTransform;
            crt.anchorMin = new Vector2(0.5f, 1f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = new Vector2(0f, y);
            crt.sizeDelta = new Vector2(heroSize, heroSize);
            card.raycastTarget = false;

            var rawGo = new GameObject("HeroView");
            rawGo.transform.SetParent(crt, false);
            var rawRt = rawGo.AddComponent<RectTransform>();
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.offsetMin = new Vector2(8f, 8f);
            rawRt.offsetMax = new Vector2(-8f, -8f);
            growthHeroImage = rawGo.AddComponent<RawImage>();
            growthHeroImage.color = Color.white;
            growthHeroImage.raycastTarget = false;
            growthHeroImage.texture = growthHeroRt;
            var fitter = rawGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            growthHeroHost = crt;
            return y - (heroSize + 12f);
        }

        float AppendSkillPicker(RectTransform content, float y)
        {
            var head = Kit.Label(content, "SkillHead", Vector2.zero, Vector2.one,
                "技構成", 18, Muted, TextAnchor.MiddleLeft);
            var headRt = head.rectTransform;
            headRt.anchorMin = new Vector2(0.04f, 1f);
            headRt.anchorMax = new Vector2(0.96f, 1f);
            headRt.pivot = new Vector2(0.5f, 1f);
            headRt.anchoredPosition = new Vector2(0f, y);
            headRt.sizeDelta = new Vector2(0f, 36f);
            y -= 42f;

            var types = new[]
            {
                SkillCommandType.Skill1,
                SkillCommandType.Skill2,
                SkillCommandType.Skill3,
                SkillCommandType.Charge,
                SkillCommandType.BrainSmash
            };
            string[] titles = { "枠1", "枠2", "枠3", "枠4", "必殺" };
            for (int i = 0; i < types.Length; i++)
            {
                var def = UltimateCatalog.Get(StudyStore.SlotUltimateId(types[i]));
                string name = def != null
                    ? StudyStore.SkillTitle(def.displayName, StudyStore.UltimateLevel(def.id))
                    : "未装備";
                string need = def != null ? UltimateCatalog.GearNeedLabel(def.gear) : "";
                var captured = types[i];
                var btnSlot = Kit.Button(content, "Slot_" + types[i], Vector2.zero, Vector2.one,
                    titles[i] + "  " + name + "  " + need, 18, new Color(0.16f, 0.16f, 0.2f), Color.white, font);
                var srt = btnSlot.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.04f, 1f);
                srt.anchorMax = new Vector2(0.96f, 1f);
                srt.pivot = new Vector2(0.5f, 1f);
                srt.anchoredPosition = new Vector2(0f, y);
                srt.sizeDelta = new Vector2(0f, 72f);
                if (StudyStore.GrowthLocked())
                    btnSlot.interactable = false;
                else
                    btnSlot.onClick.AddListener(() => UltimateLoadoutScreen.Open(captured));
                y -= 80f;
            }
            return y - 12f;
        }

        IEnumerator RebuildGrowthNextFrame()
        {
            yield return null;
            if (growthLookMode) RebuildGrowth();
        }

        void KeepGrowthScroll(ScrollRect scroll)
        {
            growthScroll = scroll;
            float norm = growthScrollNorm;
            Canvas.ForceUpdateCanvases();
            if (scroll != null) scroll.verticalNormalizedPosition = norm;
            int token = ++growthScrollToken;
            StartCoroutine(RestoreGrowthScroll(scroll, norm, token));
        }

        IEnumerator RestoreGrowthScroll(ScrollRect scroll, float norm, int token)
        {
            yield return null;
            if (token != growthScrollToken || scroll == null) yield break;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = norm;
            yield return null;
            if (token != growthScrollToken || scroll == null) yield break;
            scroll.verticalNormalizedPosition = norm;
        }

        void ApplyGachaPull(GachaPullResult got, bool ten)
        {
            RefreshWallet();
            if (!got.ok)
            {
                ShowToast(got.message);
                return;
            }
            ShowGachaResult(got, ten);
        }

        void ShowGachaRates()
        {
            if (gachaRatePanel != null) gachaRatePanel.SetActive(true);
        }

        void HideGachaRates()
        {
            if (gachaRatePanel != null) gachaRatePanel.SetActive(false);
        }

        static Sprite gachaIconSprite;
        static Sprite gachaBannerSprite;

        static Sprite GachaBannerSprite()
        {
            if (gachaBannerSprite != null) return gachaBannerSprite;
            const int w = 512;
            const int h = 320;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                    px[y * w + x] = PaintGachaBanner((x + 0.5f) / w, (y + 0.5f) / h);
            }
            tex.SetPixels(px);
            tex.Apply();
            gachaBannerSprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            return gachaBannerSprite;
        }

        static Color PaintGachaBanner(float u, float v)
        {
            float dx = (u - 0.5f) * 1.15f;
            float dy = v - 0.40f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            Color night = new Color(0.07f, 0.03f, 0.16f, 1f);
            Color violet = new Color(0.42f, 0.10f, 0.52f, 1f);
            Color col = Color.Lerp(violet, night, Mathf.Clamp01(d * 1.35f));
            float ang = Mathf.Atan2(dy, dx);
            float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 6f)), 16f);
            if (d < 0.62f)
                col = Color.Lerp(col, new Color(1f, 0.78f, 0.32f, 1f), ray * (1f - d / 0.62f) * 0.5f);
            col = Color.Lerp(col, new Color(1f, 0.90f, 0.55f, 1f), Mathf.Clamp01(1f - d / 0.18f) * 0.7f);
            col = Color.Lerp(col, Color.white, BannerStar(u, v));
            if (v < 0.045f)
                col = Color.Lerp(new Color(0.45f, 0.24f, 0.05f, 1f), new Color(1f, 0.84f, 0.32f, 1f), v / 0.045f);
            return col;
        }

        static float BannerStar(float u, float v)
        {
            float s = 0f;
            s = Mathf.Max(s, StarDot(u, v, 0.10f, 0.82f, 0.010f));
            s = Mathf.Max(s, StarDot(u, v, 0.22f, 0.62f, 0.007f));
            s = Mathf.Max(s, StarDot(u, v, 0.84f, 0.78f, 0.011f));
            s = Mathf.Max(s, StarDot(u, v, 0.90f, 0.48f, 0.006f));
            s = Mathf.Max(s, StarDot(u, v, 0.16f, 0.28f, 0.008f));
            s = Mathf.Max(s, StarDot(u, v, 0.78f, 0.22f, 0.007f));
            s = Mathf.Max(s, StarDot(u, v, 0.50f, 0.86f, 0.006f));
            return s;
        }

        static float StarDot(float u, float v, float px, float py, float r)
        {
            float dx = u - px;
            float dy = v - py;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d >= r) return 0f;
            return 1f - d / r;
        }

        static Sprite GachaIconSprite()
        {
            if (gachaIconSprite != null) return gachaIconSprite;
            const int s = 384;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[s * s];
            float cx = (s - 1) * 0.5f;
            float cy = (s - 1) * 0.5f;
            float scale = s * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float nx = (x - cx) / scale;
                    float ny = (y - cy) / scale;
                    px[y * s + x] = PaintGachaIcon(nx, ny);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            gachaIconSprite = Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), s);
            return gachaIconSprite;
        }

        static Color PaintGachaIcon(float nx, float ny)
        {
            float d = Mathf.Sqrt(nx * nx + ny * ny);
            float ang = Mathf.Atan2(ny, nx);
            float spike = 0.90f + 0.09f * Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 4f)), 8f);
            if (d > spike) return new Color(0f, 0f, 0f, 0f);

            Color gold = new Color(1f, 0.84f, 0.32f, 1f);
            Color goldDeep = new Color(0.55f, 0.30f, 0.05f, 1f);
            Color ink = new Color(0.10f, 0.06f, 0.03f, 1f);
            float rimIn = spike - 0.07f;
            if (d > rimIn)
            {
                float t = Mathf.InverseLerp(spike, rimIn, d);
                return Color.Lerp(goldDeep, gold, t);
            }

            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                float gx = Mathf.Cos(a) * 0.80f;
                float gy = Mathf.Sin(a) * 0.80f;
                float star = StarMask(nx, ny, gx, gy, 0.055f);
                if (star > 0f) return Color.Lerp(gold, Color.white, star);
            }

            if (d > 0.70f) return ink;
            if (d > 0.64f) return gold;

            float light = Mathf.Clamp01(0.42f + ny * 0.45f - nx * 0.12f);
            Color body = Color.Lerp(new Color(0.42f, 0.16f, 0.03f), new Color(1f, 0.72f, 0.18f), light);
            if (Mathf.Abs(ny) < 0.085f)
                body = new Color(0.98f, 0.95f, 0.88f);
            else if (ny > 0.085f)
                body = Color.Lerp(new Color(0.78f, 0.48f, 0.08f), new Color(1f, 0.94f, 0.62f), light);

            float sx = nx + 0.20f;
            float sy = ny - 0.30f;
            float sd = Mathf.Sqrt(sx * sx + sy * sy);
            if (sd < 0.15f && ny > 0.08f)
                body = Color.Lerp(body, Color.white, 1f - sd / 0.15f);

            float gem = Mathf.Sqrt(nx * nx + ny * ny * 2.1f);
            if (gem < 0.11f && Mathf.Abs(ny) < 0.10f)
                body = Color.Lerp(new Color(0.85f, 0.12f, 0.16f), new Color(1f, 0.78f, 0.72f), 1f - gem / 0.11f);

            if (d > 0.56f)
                body = Color.Lerp(body, ink, (d - 0.56f) / 0.08f);
            return body;
        }

        static float StarMask(float nx, float ny, float px, float py, float size)
        {
            float dx = (nx - px) / size;
            float dy = (ny - py) / size;
            float ax = Mathf.Abs(dx);
            float ay = Mathf.Abs(dy);
            if (ax < 0.22f && ay < 1f) return 1f - Mathf.Max(ax / 0.22f, ay);
            if (ay < 0.22f && ax < 1f) return 1f - Mathf.Max(ay / 0.22f, ax);
            return 0f;
        }

        static string GachaDropMark(GachaDrop drop)
        {
            if (drop == null) return "";
            return StudyStore.VersionMark(drop.version);
        }

        static Color RarityColor(GachaRarity rarity)
        {
            switch (rarity)
            {
                case GachaRarity.Common: return new Color(0.82f, 0.84f, 0.88f);
                case GachaRarity.Uncommon: return new Color(0.45f, 0.82f, 1f);
                case GachaRarity.Rare: return new Color(0.78f, 0.48f, 1f);
                default: return new Color(1f, 0.82f, 0.28f);
            }
        }

        static float PlaceHonor(RectTransform parent, Text nameLabel, float left)
        {
            return PlaceHonorFor(parent, nameLabel, left, StudyStore.EquippedCosmetic("title"));
        }

        static float PlaceHonorFor(RectTransform parent, Text nameLabel, float left, string titleId)
        {
            string title = StudyStore.TitleLabel(titleId);
            if (string.IsNullOrEmpty(title) || nameLabel == null) return left;
            float width = Mathf.Max(40f, title.Length * 16f + 12f);
            var mark = Kit.Label(parent, "Honor", nameLabel.rectTransform.anchorMin, nameLabel.rectTransform.anchorMax,
                title, 14, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
            var mrt = mark.rectTransform;
            mrt.anchorMax = new Vector2(mrt.anchorMin.x, mrt.anchorMax.y);
            mrt.pivot = new Vector2(0f, 0.5f);
            mrt.offsetMin = Vector2.zero;
            mrt.offsetMax = Vector2.zero;
            mrt.anchoredPosition = new Vector2(left, 0f);
            mrt.sizeDelta = new Vector2(width, 0f);
            return left + width + 10f;
        }

        static int LuxLayers(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            if (id.IndexOf("god") >= 0 || id.IndexOf("triple") >= 0 || id.IndexOf("summer") >= 0) return 3;
            if (id.IndexOf("double") >= 0 || id.IndexOf("zhu") >= 0 || id.IndexOf("royal") >= 0 || id.IndexOf("gold") >= 0 && id.IndexOf("seal") >= 0) return 2;
            return 1;
        }

        static string SealMark(string id)
        {
            if (id == "seal_god") return "神判";
            if (id == "seal_night") return "夜判";
            if (id == "seal_gold") return "金判";
            if (id == "seal_jade") return "翠判";
            if (id == "seal_sea") return "蒼判";
            if (id == "seal_goal") return "判";
            return "";
        }

        static void DressOwnCard(Image card)
        {
            DressCard(card, StudyStore.EquippedCosmetic("frame"), StudyStore.EquippedCosmetic("corner"));
        }

        static void DressFriendCard(Image card, FriendEntry friend)
        {
            if (friend == null) return;
            string frame = StudyStore.ParseCosmeticFromCsv(friend.cosmeticsCsv, "frame");
            string corner = StudyStore.ParseCosmeticFromCsv(friend.cosmeticsCsv, "corner");
            DressCard(card, frame, corner);
        }

        static void DressCard(Image card, string frame, string corner)
        {
            if (card == null) return;
            int layers = LuxLayers(frame);
            for (int i = 0; i < layers; i++)
            {
                Color line = i == 1 ? RpgTheme.Zhu : FrameTint(frame);
                DressRim(card.transform, line, 3f, 2f + i * 6f);
            }
            int jewels = LuxLayers(corner);
            if (jewels > 0)
                DressCorners(card.transform, FrameTint(corner), jewels >= 3 ? 18f : jewels == 2 ? 14f : 10f, 2f + (jewels - 1) * 4f);
        }

        static Color FrameTint(string id)
        {
            if (id != null && id.IndexOf("ink") >= 0) return new Color(0.86f, 0.89f, 0.96f);
            if (id != null && id.IndexOf("peach") >= 0) return new Color(0.96f, 0.62f, 0.66f);
            if (id != null && id.IndexOf("jade") >= 0) return new Color(0.45f, 0.82f, 0.55f);
            if (id != null && id.IndexOf("sea") >= 0) return new Color(0.45f, 0.72f, 0.95f);
            if (id != null && id.IndexOf("violet") >= 0) return new Color(0.72f, 0.48f, 0.95f);
            if (id != null && id.IndexOf("silver") >= 0) return new Color(0.78f, 0.80f, 0.84f);
            if (id != null && id.IndexOf("zhu") >= 0) return RpgTheme.Zhu;
            if (id == "back_paper") return new Color(0.93f, 0.84f, 0.62f);
            if (id == "back_night") return new Color(0.55f, 0.62f, 0.95f);
            if (id == "back_royal") return new Color(0.95f, 0.82f, 0.35f);
            return RpgTheme.GoldHi;
        }

        static void DressPageRim(Transform page, string id)
        {
            int layers = LuxLayers(id);
            for (int i = 0; i < layers; i++)
            {
                Color line = i == 1 ? RpgTheme.Zhu : FrameTint(id);
                DressRim(page, line, 4f, 2f + i * 8f);
            }
        }

        static void DressRim(Transform parent, Color color, float thick, float outset)
        {
            Edge(parent, "RimT", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(outset, outset), new Vector2(-outset, outset + thick), color);
            Edge(parent, "RimB", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(outset, -(outset + thick)), new Vector2(-outset, -outset), color);
            Edge(parent, "RimL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-(outset + thick), outset), new Vector2(-outset, -outset), color);
            Edge(parent, "RimR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(outset, outset), new Vector2(outset + thick, -outset), color);
        }

        static void DressCorners(Transform parent, Color color, float size, float outset)
        {
            Corner(parent, "C0", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-outset, -size), new Vector2(size, outset), color);
            Corner(parent, "C1", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-size, -size), new Vector2(outset, outset), color);
            Corner(parent, "C2", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(-outset, -outset), new Vector2(size, size), color);
            Corner(parent, "C3", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-size, -outset), new Vector2(outset, size), color);
        }

        static void Edge(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
        {
            var img = Kit.Image(parent, name, min, max, color);
            img.raycastTarget = false;
            img.rectTransform.offsetMin = offMin;
            img.rectTransform.offsetMax = offMax;
        }

        static void Corner(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
        {
            Edge(parent, name, min, max, offMin, offMax, color);
        }

        void OpenExchange()
        {
            if (exchangePage == null) return;
            exchangePage.SetActive(true);
            exchangePage.transform.SetAsLastSibling();
            RebuildExchange();
        }

        void CloseExchange()
        {
            if (exchangePage != null) exchangePage.SetActive(false);
            if (tab == Tab.Timeline) RebuildTimeline();
            RefreshWallet();
            RefreshProfilePreview();
        }

        void RebuildExchange()
        {
            if (exchangePage == null) return;
            ClearSpawned(exchangePage.transform);
            var dim = Kit.Image(exchangePage.transform, "Dim", Vector2.zero, Vector2.one, RpgTheme.DimHeavy);
            dim.raycastTarget = true;
            var panel = Kit.Window(exchangePage.transform, "Shop", new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f));
            Kit.Label(panel.transform, "Title", new Vector2(0.18f, 0.92f), new Vector2(0.72f, 0.99f),
                "交換所", 28, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            Kit.Label(panel.transform, "Dia", new Vector2(0.55f, 0.92f), new Vector2(0.96f, 0.99f),
                "ダイヤ " + StudyStore.Diamonds, 20, Color.white, TextAnchor.MiddleRight);
            var back = Kit.Button(panel.transform, "Back", new Vector2(0.04f, 0.92f), new Vector2(0.16f, 0.99f),
                "戻る", 16, new Color(0.22f, 0.22f, 0.25f), Color.white, font);
            back.onClick.AddListener(CloseExchange);

            var scroll = MakeScroll(panel.transform, "ShopScroll");
            var srt = scroll.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.03f, 0.03f);
            srt.anchorMax = new Vector2(0.97f, 0.90f);
            var content = scroll.content;
            float y = -8f;
            y = DrawShopPreview(content, y);
            y = DrawShopSection(content, y, "カードの枠", "カードの外側。中の色は変えない", new[]
            {
                Offer("frame_gold", "frame", "金の細枠", 20),
                Offer("frame_ink", "frame", "墨の細枠", 20),
                Offer("frame_peach", "frame", "桃の細枠", 25),
                Offer("frame_jade", "frame", "翠の細枠", 30),
                Offer("frame_sea", "frame", "蒼の細枠", 30),
                Offer("frame_violet", "frame", "紫の細枠", 35),
                Offer("frame_double", "frame", "金の二重枠", 50),
                Offer("frame_zhu", "frame", "朱の二重枠", 70),
                Offer("frame_silver", "frame", "銀の二重枠", 80),
                Offer("frame_jade_double", "frame", "翠の二重枠", 90),
                Offer("frame_triple", "frame", "金の三重枠", 100),
                Offer("frame_god", "frame", "神枠", 160)
            });
            y = DrawShopSection(content, y, "名前の肩書き", "タイムラインの名前の横", new[]
            {
                Offer("title_study", "title", "勉強中", 15),
                Offer("title_night", "title", "夜学", 20),
                Offer("title_book", "title", "書生", 25),
                Offer("title_tower", "title", "塔の者", 30),
                Offer("title_ace", "title", "一番", 40),
                Offer("title_god", "title", "神", 50),
                Offer("title_moon", "title", "月下", 70),
                Offer("title_lord", "title", "塔の主", 90),
                Offer("title_void", "title", "虚の者", 110),
                Offer("title_hundred", "title", "百階", 120),
                Offer("title_summer", "title", "夏の覇者", 140)
            });
            y = DrawShopSection(content, y, "アイコンの縁", "プロフィールとタイムライン", new[]
            {
                Offer("border_gold", "border", "金の縁", 20),
                Offer("border_ink", "border", "墨の縁", 20),
                Offer("border_peach", "border", "桃の縁", 25),
                Offer("border_jade", "border", "翠の縁", 25),
                Offer("border_sea", "border", "蒼の縁", 25),
                Offer("border_violet", "border", "紫の縁", 30),
                Offer("border_silver", "border", "銀の縁", 55),
                Offer("border_double", "border", "二重の金縁", 60),
                Offer("border_god", "border", "神の光輪", 120)
            });
            y = DrawShopSection(content, y, "タイムラインの縁", "画面のふちだけ。中の色は変えない", new[]
            {
                Offer("back_paper", "backdrop", "紙の縁", 40),
                Offer("back_night", "backdrop", "夜の縁", 50),
                Offer("back_jade", "backdrop", "翠の縁", 55),
                Offer("back_sea", "backdrop", "蒼の縁", 55),
                Offer("back_peach", "backdrop", "桃の縁", 60),
                Offer("back_violet", "backdrop", "紫の縁", 70),
                Offer("back_silver", "backdrop", "銀の額", 90),
                Offer("back_royal", "backdrop", "金の額", 100),
                Offer("back_god", "backdrop", "神の額", 180)
            });
            y = DrawShopSection(content, y, "達成の印", "目標をクリアしたときだけ", new[]
            {
                Offer("seal_goal", "seal", "判子", 30),
                Offer("seal_jade", "seal", "翠判", 50),
                Offer("seal_sea", "seal", "蒼判", 50),
                Offer("seal_gold", "seal", "金判", 80),
                Offer("seal_night", "seal", "夜判", 100),
                Offer("seal_god", "seal", "神判", 140)
            });
            y = DrawShopSection(content, y, "カードの角", "四隅の外側。文字色はそのまま", new[]
            {
                Offer("corner_gold", "corner", "金の角", 20),
                Offer("corner_ink", "corner", "墨の角", 20),
                Offer("corner_jade", "corner", "翠の角", 25),
                Offer("corner_sea", "corner", "蒼の角", 25),
                Offer("corner_peach", "corner", "桃の角", 30),
                Offer("corner_violet", "corner", "紫の角", 35),
                Offer("corner_silver", "corner", "銀の角", 70),
                Offer("corner_double", "corner", "大粒の角", 80),
                Offer("corner_god", "corner", "神の角", 150)
            });
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 24f);
        }

        struct ShopOffer
        {
            public string id;
            public string slot;
            public string name;
            public int cost;
        }

        static ShopOffer Offer(string id, string slot, string name, int cost)
        {
            return new ShopOffer { id = id, slot = slot, name = name, cost = cost };
        }

        float DrawShopPreview(RectTransform content, float y)
        {
            var card = Kit.Card(content, "Preview", Vector2.zero, Vector2.one);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 96f);
            DressOwnCard(card);
            DrawAvatar(rt, false, 48f, new Vector2(0f, 0.5f), new Vector2(40f, 0f));
            var who = Kit.Label(rt, "Who", new Vector2(0.22f, 0.48f), new Vector2(0.96f, 0.90f),
                StudyStore.ProfileName(), 18, Color.white, TextAnchor.MiddleLeft);
            who.rectTransform.offsetMin = new Vector2(PlaceHonor(rt, who, 8f), 0f);
            string sample = "数学  25分";
            Kit.Label(rt, "Sample", new Vector2(0.22f, 0.08f), new Vector2(0.96f, 0.48f),
                sample, 16, StudySubjectColors.Color(StudySubject.Math), TextAnchor.MiddleLeft);
            return y - 108f;
        }

        float DrawShopSection(RectTransform content, float y, string heading, string note, ShopOffer[] offers)
        {
            var head = Kit.Label(content, "H", new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                heading, 20, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            var hrt = head.rectTransform;
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, y);
            hrt.sizeDelta = new Vector2(0f, 28f);
            y -= 28f;
            var sub = Kit.Label(content, "N", new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                note, 14, Muted, TextAnchor.MiddleLeft);
            var srt = sub.rectTransform;
            srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0f, y);
            srt.sizeDelta = new Vector2(0f, 22f);
            y -= 28f;
            for (int i = 0; i < offers.Length; i++)
                y = DrawShopRow(content, y, offers[i]);
            return y - 8f;
        }

        float DrawShopRow(RectTransform content, float y, ShopOffer offer)
        {
            bool owned = StudyStore.OwnsCosmetic(offer.id);
            bool on = StudyStore.EquippedCosmetic(offer.slot) == offer.id;
            var row = Kit.Window(content, "Row", Vector2.zero, Vector2.one);
            var rt = row.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 64f);
            Color chip = ShopChip(offer);
            var swatch = Kit.Image(rt, "Chip", new Vector2(0.04f, 0.22f), new Vector2(0.16f, 0.78f), chip);
            swatch.raycastTarget = false;
            Kit.Label(rt, "Name", new Vector2(0.20f, 0.15f), new Vector2(0.58f, 0.85f),
                offer.name, 18, Color.white, TextAnchor.MiddleLeft);
            string action = on ? "装備中" : owned ? "装備する" : offer.cost + "ダイヤ";
            var btn = Kit.Button(rt, "Buy", new Vector2(0.60f, 0.16f), new Vector2(0.96f, 0.84f),
                action, 16, on ? RpgTheme.Gold : RpgTheme.ButtonFill, on ? RpgTheme.Bg : Color.white, font);
            ShopOffer captured = offer;
            btn.onClick.AddListener(() =>
            {
                string err = StudyStore.TryBuyCosmetic(captured.id, captured.slot, captured.cost);
                if (err != null) ShowToast(err);
                RebuildExchange();
            });
            return y - 72f;
        }

        static int DyeIndex(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith("dye_")) return -1;
            int index;
            return int.TryParse(id.Substring(4), out index) ? index : -1;
        }

        static Color ShopChip(ShopOffer offer)
        {
            if (offer.slot == "dye")
            {
                int index = DyeIndex(offer.id);
                return index < 0 ? Color.white : StudyStore.SubjectDyeColor(index);
            }
            if (offer.id.IndexOf("gold") >= 0) return RpgTheme.GoldHi;
            if (offer.id.IndexOf("ink") >= 0) return new Color(0.75f, 0.78f, 0.88f);
            if (offer.id.IndexOf("jade") >= 0) return new Color(0.45f, 0.82f, 0.55f);
            if (offer.id.IndexOf("sea") >= 0) return new Color(0.45f, 0.72f, 0.95f);
            if (offer.id.IndexOf("violet") >= 0) return new Color(0.72f, 0.48f, 0.95f);
            if (offer.id.IndexOf("silver") >= 0) return new Color(0.78f, 0.80f, 0.84f);
            if (offer.id.IndexOf("peach") >= 0 || offer.id == "back_peach") return new Color(0.95f, 0.5f, 0.55f);
            if (offer.id == "back_paper") return new Color(0.93f, 0.86f, 0.72f);
            if (offer.id == "back_night") return new Color(0.12f, 0.1f, 0.22f);
            if (offer.id.IndexOf("zhu") >= 0 || offer.id.IndexOf("seal") >= 0) return RpgTheme.Zhu;
            if (offer.id.IndexOf("night") >= 0) return new Color(0.35f, 0.45f, 0.85f);
            return RpgTheme.GoldHi;
        }

        public void RefreshSortieIfActive()
        {
            if (tab == Tab.Battle) RebuildSortie();
        }

        public void RefreshRaidIfOpen()
        {
            if (raidScreen != null && raidScreen.IsOpen) raidScreen.Reload();
        }

        public void OpenRaid()
        {
            if (raidScreen == null) raidScreen = new RaidScreen();
            raidScreen.Open(this, root, font);
        }

        public void Toast(string message)
        {
            ShowToast(message);
        }

        public Coroutine Run(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public void ShowFriendProfile(FriendEntry friend)
        {
            OpenFriendProfile(friend);
            if (root == null) return;
            var dialog = root.Find("FriendProfile");
            if (dialog == null) return;
            if (raidScreen != null && raidScreen.IsOpen && raidScreen.Host != null)
            {
                dialog.SetParent(raidScreen.Host, false);
                var drt = dialog.GetComponent<RectTransform>();
                if (drt != null)
                {
                    drt.anchorMin = Vector2.zero;
                    drt.anchorMax = Vector2.one;
                    drt.offsetMin = Vector2.zero;
                    drt.offsetMax = Vector2.zero;
                }
            }
            dialog.SetAsLastSibling();
            var panel = dialog.Find("Panel");
            if (panel == null) return;
            var rt = panel.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.03f, 0.62f);
            rt.anchorMax = new Vector2(0.97f, 0.985f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void OpenSelfProfile()
        {
            OpenProfileDialog();
            if (profileDialog == null) return;
            profileDialog.transform.SetAsLastSibling();
            var panel = profileDialog.transform.Find("Panel");
            if (panel == null) return;
            var rt = panel.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.03f, 0.18f);
            rt.anchorMax = new Vector2(0.97f, 0.985f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void RebuildSortie()
        {
            if (battlePage == null) return;
            ClearSpawned(battlePage.transform);
            bool paused = StudyStore.HasPausedBattle();
            var host = battlePage.transform;

            var plate = Kit.Window(host, "Plate", new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.97f));
            Kit.Label(plate.transform, "Floor", new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.94f),
                StudyStore.MyClimbLine() + (StudyStore.CurrentFloorIsBoss() ? "  ボス" : ""),
                28, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            string skin = HeroAppearance.SkinTitle(StudyStore.LookSlot(0));
            Kit.Label(plate.transform, "Skin", new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.62f),
                skin, 20, Color.white, TextAnchor.MiddleLeft);
            string stats = "HP " + StudyStore.BattleMaxHp()
                + "   攻 " + StudyStore.BattleAttack()
                + "   防 " + StudyStore.BattleDefense()
                + "\n速 " + StudyStore.BattleSpeed()
                + "   運 " + StudyStore.BattleLuck()
                + (StudyStore.EquippedSkinIsGod() ? "    神 1.1倍" : "");
            Kit.Label(plate.transform, "Stats", new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.40f),
                stats, 16, Muted, TextAnchor.MiddleLeft);

            Kit.Label(host, "Wallet", new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.60f),
                "今日のBP " + StudyStore.TodayBp + "    ダイヤ " + StudyStore.Diamonds, 18, Color.white, TextAnchor.MiddleLeft);

            string goLabel = paused
                ? "続きから"
                : "出撃   " + StudyStore.BattleCostBp + "BP";
            var go = Kit.Button(host, "Go", new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.48f),
                goLabel, 26, RpgTheme.ButtonFill, Color.white, font);
            RpgTheme.AddZhuAccent(go.transform);
            go.onClick.AddListener(Depart);

            int gifts = RaidStore.UnclaimedCount();
            string raidLabel = RaidStore.HasPause() ? "レイドバトル   続き" : "レイドバトル";
            raidLabel += "\n今日 " + RaidStore.TurnsLeft() + "/" + RaidRules.DailyTurns;
            if (gifts > 0) raidLabel += "    贈り物 " + gifts;
            var raid = Kit.Button(host, "Raid", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.27f),
                raidLabel, 22, new Color(0.55f, 0.08f, 0.12f), Color.white, font);
            raid.onClick.AddListener(OpenRaid);
        }

        void Depart()
        {
            if (AppFlow.Instance == null) return;
            if (StudyStore.HasPausedBattle())
            {
                AppFlow.Instance.ShowBattle();
                return;
            }
            if (!StudyStore.CanEnterBattle())
            {
                ShowBpShortage();
                return;
            }
            if (!StudyStore.TrySpendForBattle())
            {
                ShowBpShortage();
                return;
            }
            AppFlow.Instance.ShowBattle();
        }

        void RebuildGacha()
        {
            if (gachaPage == null) return;
            ClearSpawned(gachaPage.transform);
            gachaRatePanel = null;

            var bannerGo = new GameObject("GachaBanner");
            bannerGo.transform.SetParent(gachaPage.transform, false);
            var brt = bannerGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0.50f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var banner = bannerGo.AddComponent<Image>();
            banner.sprite = GachaBannerSprite();
            banner.type = Image.Type.Simple;
            banner.preserveAspect = false;
            banner.raycastTarget = false;

            var bannerTitle = Kit.Label(gachaPage.transform, "GachaBannerTitle",
                new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.985f),
                "装備・技ガチャ", 34, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
            bannerTitle.raycastTarget = false;
            var titleOutline = bannerTitle.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.28f, 0.08f, 0.02f, 0.95f);
            titleOutline.effectDistance = new Vector2(2f, -2f);

            var iconGo = new GameObject("GachaIcon");
            iconGo.transform.SetParent(gachaPage.transform, false);
            var irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.18f, 0.54f);
            irt.anchorMax = new Vector2(0.82f, 0.88f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = GachaIconSprite();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var pull = Kit.Button(gachaPage.transform, "Pull", new Vector2(0.04f, 0.36f), new Vector2(0.49f, 0.48f),
                "1回  " + GachaCatalog.PullCost + "ダイヤ", 22, Accent, Color.black, font);
            pull.onClick.AddListener(() => ApplyGachaPull(GachaCatalog.Pull(), false));
            int tenCost = GachaCatalog.PullCost * GachaCatalog.PullTenCount;
            var pullTen = Kit.Button(gachaPage.transform, "PullTen", new Vector2(0.51f, 0.36f), new Vector2(0.96f, 0.48f),
                "10連  " + tenCost + "ダイヤ", 22, Accent, Color.black, font);
            pullTen.onClick.AddListener(() => ApplyGachaPull(GachaCatalog.PullMany(GachaCatalog.PullTenCount), true));

            var rateBtn = Kit.Button(gachaPage.transform, "Rates", new Vector2(0.18f, 0.24f), new Vector2(0.82f, 0.34f),
                "提供割合", 22, Card, Color.white, font);
            rateBtn.onClick.AddListener(ShowGachaRates);
            var shopFromGacha = Kit.Button(gachaPage.transform, "OpenShop", new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.22f),
                "交換所", 22, RpgTheme.Gold, RpgTheme.Bg, font);
            shopFromGacha.onClick.AddListener(OpenExchange);

            gachaRatePanel = new GameObject("GachaRates");
            gachaRatePanel.transform.SetParent(gachaPage.transform, false);
            var prt = gachaRatePanel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            var dim = gachaRatePanel.AddComponent<Image>();
            dim.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);
            var scroll = MakeScroll(gachaRatePanel.transform, "RateScroll");
            var srt = scroll.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.12f);
            srt.anchorMax = Vector2.one;
            var closeRates = Kit.Button(gachaRatePanel.transform, "CloseRates", new Vector2(0.22f, 0.02f), new Vector2(0.78f, 0.10f),
                "閉じる", 22, Accent, Color.black, font);
            closeRates.onClick.AddListener(HideGachaRates);
            var content = scroll.content;
            float y = -12f;
            var title = Kit.Label(content, "Title", Vector2.zero, Vector2.one,
                "提供割合", 28, Accent, TextAnchor.MiddleCenter);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0.04f, 1f);
            trt.anchorMax = new Vector2(0.96f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, y);
            trt.sizeDelta = new Vector2(0f, 48f);
            y -= 56f;
            GachaRarity shown = (GachaRarity)(-1);
            var rows = GachaCatalog.RateRows();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].rarity != shown)
                {
                    shown = rows[i].rarity;
                    float band = GachaCatalog.RarityTotalRate(shown);
                    var head = Kit.Label(content, "R" + (int)shown, Vector2.zero, Vector2.one,
                        GachaCatalog.RarityLabel(shown), 20, Accent, TextAnchor.MiddleLeft);
                    var hrt = head.rectTransform;
                    hrt.anchorMin = new Vector2(0.06f, 1f);
                    hrt.anchorMax = new Vector2(0.94f, 1f);
                    hrt.pivot = new Vector2(0.5f, 1f);
                    hrt.anchoredPosition = new Vector2(0f, y);
                    hrt.sizeDelta = new Vector2(0f, 36f);
                    y -= 40f;
                }
                var line = Kit.Label(content, "I" + i, Vector2.zero, Vector2.one,
                    rows[i].name + "   " + rows[i].percent.ToString("0.00") + "%", 16, Color.white, TextAnchor.MiddleLeft);
                var lrt = line.rectTransform;
                lrt.anchorMin = new Vector2(0.08f, 1f);
                lrt.anchorMax = new Vector2(0.94f, 1f);
                lrt.pivot = new Vector2(0.5f, 1f);
                lrt.anchoredPosition = new Vector2(0f, y);
                lrt.sizeDelta = new Vector2(0f, 28f);
                y -= 30f;
            }
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 24f);
            gachaRatePanel.SetActive(false);
        }

        void ShowGachaResult(GachaPullResult got, bool ten)
        {
            if (gachaPage == null) return;
            HideGachaRates();
            var old = gachaPage.transform.Find("GachaReveal");
            if (old != null) DestroyImmediate(old.gameObject);

            var panel = new GameObject("GachaReveal");
            panel.transform.SetParent(gachaPage.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.04f, 0.07f, 0.97f);

            string heading = ten ? "10連の結果" : "ガチャ結果";
            var title = Kit.Label(panel.transform, "Title", new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.98f),
                heading, 30, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
            title.raycastTarget = false;

            if (got.drops.Count <= 1)
                BuildSingleGachaCard(panel.transform, got);
            else
                BuildGachaCardList(panel.transform, got);

            var close = Kit.Button(panel.transform, "CloseReveal", new Vector2(0.22f, 0.02f), new Vector2(0.78f, 0.11f),
                "閉じる", 22, Accent, Color.black, font);
            close.onClick.AddListener(() =>
            {
                if (panel != null) Destroy(panel);
            });
        }

        void BuildSingleGachaCard(Transform panel, GachaPullResult got)
        {
            if (got.drops.Count == 0) return;
            var drop = got.drops[0];
            Color tint = RarityColor(drop.rarity);
            var card = Kit.Window(panel, "Card", new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.84f));
            var bar = Kit.Image(card.transform, "Bar", new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.84f), tint);
            bar.raycastTarget = false;
            Kit.Label(card.transform, "Rarity", new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.84f),
                GachaCatalog.RarityName(drop.rarity), 26, Color.black, TextAnchor.MiddleCenter);
            Kit.Label(card.transform, "Name", new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.68f),
                drop.name, 34, Color.white, TextAnchor.MiddleCenter);
            string extra = GachaDropMark(drop);
            if (got.refund > 0)
                extra = (extra.Length > 0 ? extra + "\n" : "") + "ダイヤ" + got.refund + "返還";
            if (extra.Length > 0)
                Kit.Label(card.transform, "Extra", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.28f),
                    extra, 22, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
        }

        void BuildGachaCardList(Transform panel, GachaPullResult got)
        {
            var scroll = MakeScroll(panel, "RevealScroll");
            var srt = scroll.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.13f);
            srt.anchorMax = new Vector2(1f, 0.86f);
            var content = scroll.content;
            float y = -8f;
            for (int i = 0; i < got.drops.Count; i++)
            {
                var drop = got.drops[i];
                Color tint = RarityColor(drop.rarity);
                var card = Kit.Window(content, "Card" + i, Vector2.zero, Vector2.one);
                var crt = card.rectTransform;
                crt.anchorMin = new Vector2(0.04f, 1f);
                crt.anchorMax = new Vector2(0.96f, 1f);
                crt.pivot = new Vector2(0.5f, 1f);
                crt.anchoredPosition = new Vector2(0f, y);
                crt.sizeDelta = new Vector2(0f, 78f);
                var bar = Kit.Image(card.transform, "Bar", new Vector2(0f, 0f), new Vector2(0.025f, 1f), tint);
                bar.raycastTarget = false;
                Kit.Label(card.transform, "Rarity", new Vector2(0.06f, 0.52f), new Vector2(0.96f, 0.95f),
                    GachaCatalog.RarityName(drop.rarity) + "   " + GachaDropMark(drop), 16, tint, TextAnchor.MiddleLeft);
                Kit.Label(card.transform, "Name", new Vector2(0.06f, 0.05f), new Vector2(0.96f, 0.55f),
                    drop.name, 22, Color.white, TextAnchor.MiddleLeft);
                y -= 88f;
            }
            if (got.refund > 0)
            {
                var refund = Kit.Label(content, "Refund", Vector2.zero, Vector2.one,
                    "ダイヤ" + got.refund + "返還", 20, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
                var rrt = refund.rectTransform;
                rrt.anchorMin = new Vector2(0.08f, 1f);
                rrt.anchorMax = new Vector2(0.92f, 1f);
                rrt.pivot = new Vector2(0.5f, 1f);
                rrt.anchoredPosition = new Vector2(0f, y);
                rrt.sizeDelta = new Vector2(0f, 40f);
                y -= 48f;
            }
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 16f);
        }

        void RebuildGrowth()
        {
            if (growthPage == null) return;
            if (growthStatHost == null) SetupGrowthLayout();
            if (growthScroll != null)
                growthScrollNorm = growthScroll.verticalNormalizedPosition;
            ClearSpawned(growthStatHost);
            var scroll = MakeScroll(growthStatHost, "GrowthScroll");
            var content = scroll.content;
            float y = -10f;
            bool locked = StudyStore.GrowthLocked();
            if (locked) StudyStore.RestorePausedGrowth(StudyStore.GetPausedBattle());
            if (growthLookMode)
                y = AppendGrowthHeroPreview(content, y);

            if (locked)
            {
                var lockLab = Kit.Label(content, "BattleLock", Vector2.zero, Vector2.one,
                    "バトル中は育成を変更できません", 18, RpgTheme.Zhu, TextAnchor.MiddleCenter);
                var lockRt = lockLab.rectTransform;
                lockRt.anchorMin = new Vector2(0.04f, 1f);
                lockRt.anchorMax = new Vector2(0.96f, 1f);
                lockRt.pivot = new Vector2(0.5f, 1f);
                lockRt.anchoredPosition = new Vector2(0f, y);
                lockRt.sizeDelta = new Vector2(0f, 48f);
                y -= 56f;
            }

            int lv = StudyStore.PlayerLevel();
            float ratio = StudyStore.LevelProgress();
            var head = Kit.Window(content, "LvCard", Vector2.zero, Vector2.one);
            var hrt = head.rectTransform;
            hrt.anchorMin = new Vector2(0.04f, 1f);
            hrt.anchorMax = new Vector2(0.96f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, y);
            hrt.sizeDelta = new Vector2(0f, 168f);
            Kit.Label(hrt, "Lv", new Vector2(0.05f, 0.55f), new Vector2(0.50f, 0.92f),
                "Lv." + lv, 40, Color.white, TextAnchor.MiddleLeft);
            Kit.Label(hrt, "GBp", new Vector2(0.50f, 0.55f), new Vector2(0.95f, 0.92f),
                StudyStore.WeekFloorText(), 22, RpgTheme.GoldHi, TextAnchor.MiddleRight);
            var barBg = Kit.Image(hrt, "BarBg", new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.50f), RpgTheme.Navy);
            barBg.raycastTarget = false;
            RpgTheme.Paint(barBg, RpgTheme.BarSprite() ?? barBg.sprite, Color.white, true);
            var bar = Kit.Image(hrt, "Bar", new Vector2(0.05f, 0.36f), new Vector2(0.05f + 0.90f * ratio, 0.50f), Accent);
            bar.raycastTarget = false;
            RpgTheme.Paint(bar, RpgTheme.FrameFillSprite() ?? RpgTheme.ButtonSprite(), RpgTheme.Gold, true);
            string next = "育成BP " + StudyStore.GrowthBp + "    "
                + (lv >= StudyStore.MaxLevel
                    ? "Lv." + StudyStore.MaxLevel
                    : StudyStore.TotalEarnedBp + " / " + StudyStore.BpForNextLevel());
            Kit.Label(hrt, "Next", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.34f),
                next, 18, Muted, TextAnchor.MiddleLeft);
            y -= 184f;

            bool canLook = LookOptionCount(0) > 1;
            if (canLook)
            {
                var statsBtn = Kit.Button(content, "ModeStats", Vector2.zero, Vector2.one,
                    "ステータス", 18, growthLookMode ? Card : Accent, growthLookMode ? Color.white : Color.black, font);
                var srt = statsBtn.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.04f, 1f);
                srt.anchorMax = new Vector2(0.49f, 1f);
                srt.pivot = new Vector2(0.5f, 1f);
                srt.anchoredPosition = new Vector2(0f, y);
                srt.sizeDelta = new Vector2(0f, 48f);
                statsBtn.onClick.AddListener(() => { growthLookMode = false; RebuildGrowth(); });

                var lookBtn = Kit.Button(content, "ModeLook", Vector2.zero, Vector2.one,
                    "見た目", 18, growthLookMode ? Accent : Card, growthLookMode ? Color.black : Color.white, font);
                var lrt = lookBtn.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.51f, 1f);
                lrt.anchorMax = new Vector2(0.96f, 1f);
                lrt.pivot = new Vector2(0.5f, 1f);
                lrt.anchoredPosition = new Vector2(0f, y);
                lrt.sizeDelta = new Vector2(0f, 48f);
                lookBtn.onClick.AddListener(() => { growthLookMode = true; RebuildGrowth(); });
                y -= 60f;
            }
            else
            {
                growthLookMode = false;
            }

            if (growthLookMode)
            {
                var hint = Kit.Label(content, "LookHint", Vector2.zero, Vector2.one,
                    "スキンは神まで重なる。装備は神まで重なり、段が高いほど防御が上がる", 16, Muted, TextAnchor.MiddleCenter);
                var hintRt = hint.rectTransform;
                hintRt.anchorMin = new Vector2(0.04f, 1f);
                hintRt.anchorMax = new Vector2(0.96f, 1f);
                hintRt.pivot = new Vector2(0.5f, 1f);
                hintRt.anchoredPosition = new Vector2(0f, y);
                hintRt.sizeDelta = new Vector2(0f, 28f);
                y -= 34f;
                int slotCount = LookUiSlotCount();
                for (int i = 0; i < slotCount; i++)
                {
                    int count = LookOptionCount(i);
                    if (count <= 0) continue;
                    int captured = i;
                    int pick = StudyStore.LookSlot(i) % count;
                    int extra = i - 1;
                    bool armorSlot = i > 0 && HeroAppearance.IsArmorExtra(extra);
                    var row = Kit.Window(content, "Look" + i, Vector2.zero, Vector2.one);
                    var rt = row.rectTransform;
                    rt.anchorMin = new Vector2(0.04f, 1f);
                    rt.anchorMax = new Vector2(0.96f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, y);
                    rt.sizeDelta = new Vector2(0f, 64f);
                    Kit.Label(rt, "N", new Vector2(0.04f, 0.15f), new Vector2(0.28f, 0.85f),
                        LookSlotLabel(i), 20, Muted, TextAnchor.MiddleLeft);
                    if (locked)
                    {
                        string lockedText = i == 0
                            ? HeroAppearance.SkinTitle(StudyStore.LookSlot(0))
                            : ArmorOptionText(i, pick, count);
                        Kit.Label(rt, "V", new Vector2(0.30f, 0.15f), new Vector2(0.96f, 0.85f),
                            lockedText, 18, Color.white, TextAnchor.MiddleCenter);
                    }
                    else
                    {
                    var prev = Kit.Button(rt, "Prev", new Vector2(0.30f, 0.12f), new Vector2(0.48f, 0.88f),
                        "＜", 22, new Color(0.22f, 0.22f, 0.25f), Color.white, font);
                    string opening = i == 0
                        ? HeroAppearance.SkinTitle(StudyStore.LookSlot(0))
                        : ArmorOptionText(i, pick, count);
                    var valueLabel = Kit.Label(rt, "V", new Vector2(0.48f, 0.08f), new Vector2(0.72f, 0.92f),
                        opening, 16, Color.white, TextAnchor.MiddleCenter);
                    valueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    valueLabel.verticalOverflow = VerticalWrapMode.Overflow;
                    var nextBtn = Kit.Button(rt, "Next", new Vector2(0.72f, 0.12f), new Vector2(0.96f, 0.88f),
                        "＞", 22, new Color(0.22f, 0.22f, 0.25f), Color.white, font);
                    Action<int> cycle = dir =>
                    {
                        if (StudyStore.GrowthLocked())
                        {
                            ShowToast("バトル中は見た目を変えられません");
                            return;
                        }
                        if (captured == 0)
                        {
                            int n = HeroAppearance.PlayerBodyCount();
                            if (n <= 0) return;
                            int ord = HeroAppearance.PlayerBodyOrdinal(StudyStore.LookSlot(0)) + dir;
                            ord %= n;
                            if (ord < 0) ord += n;
                            int slotsBefore = LookUiSlotCount();
                            StudyStore.SetLookSlot(0, HeroAppearance.PlayerBodyLook(ord));
                            RebuildGrowthHero();
                            if (growthHeroImage != null && growthHeroRt != null)
                                growthHeroImage.texture = growthHeroRt;
                            valueLabel.text = HeroAppearance.SkinTitle(StudyStore.LookSlot(0));
                            if (LookUiSlotCount() != slotsBefore)
                                StartCoroutine(RebuildGrowthNextFrame());
                            return;
                        }
                        int countNow = LookOptionCount(captured);
                        if (armorSlot)
                        {
                            int next = NextOwnedArmor(extra, pick, dir, countNow);
                            StudyStore.SetLookSlot(captured, next);
                            pick = next;
                        }
                        else
                        {
                            StudyStore.CycleLookSlot(captured, dir, countNow);
                        }
                        ApplyHeroLook(growthHeroRoot);
                        countNow = LookOptionCount(captured);
                        if (countNow <= 0) return;
                        int shown = StudyStore.LookSlot(captured) % countNow;
                        valueLabel.text = ArmorOptionText(captured, shown, countNow);
                    };
                    prev.onClick.AddListener(() => cycle(-1));
                    nextBtn.onClick.AddListener(() => cycle(1));
                    }
                    y -= 72f;
                }
                content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 24f);
                KeepGrowthScroll(scroll);
                return;
            }

            y = AppendSkillPicker(content, y);

            string[] names = { "HP", "攻撃", "防御", "素早さ", "運" };
            int[] vals =
            {
                StudyStore.BattleMaxHp(),
                StudyStore.BattleAttack(),
                StudyStore.BattleDefense(),
                StudyStore.BattleSpeed(),
                StudyStore.BattleLuck()
            };
            int[] steps =
            {
                StudyStore.HpSteps,
                StudyStore.AtkSteps,
                StudyStore.DefSteps,
                StudyStore.SpdSteps,
                StudyStore.LuckSteps
            };
            for (int i = 0; i < 5; i++)
            {
                var row = Kit.Window(content, "Stat" + i, Vector2.zero, Vector2.one);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0.04f, 1f);
                rt.anchorMax = new Vector2(0.96f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(0f, 72f);
                Kit.Label(rt, "N", new Vector2(0.04f, 0.15f), new Vector2(0.28f, 0.85f),
                    names[i], 20, Muted, TextAnchor.MiddleLeft);
                Kit.Label(rt, "V", new Vector2(0.28f, 0.15f), new Vector2(locked ? 0.96f : 0.62f, 0.85f),
                    vals[i] + "   +" + steps[i], 22, Color.white, TextAnchor.MiddleLeft);
                if (!locked)
                {
                var plus = Kit.Button(rt, "Plus", new Vector2(0.64f, 0.12f), new Vector2(0.96f, 0.88f),
                    "+  30", 18, Accent, Color.black, font);
                int captured = i;
                plus.onClick.AddListener(() =>
                {
                    if (StudyStore.GrowthLocked())
                    {
                        ShowToast("バトル中はステータスを変えられません");
                        return;
                    }
                    if (!StudyStore.TryAddStat(captured))
                    {
                        bool capped = (captured == 3 && StudyStore.SpdSteps >= StudyStore.MaxSpdLuckSteps)
                            || (captured == 4 && StudyStore.LuckSteps >= StudyStore.MaxSpdLuckSteps);
                        ShowToast(capped ? "200までです" : "育成BPが足りません");
                        return;
                    }
                    RebuildGrowth();
                });
                }
                y -= 82f;
            }

            if (!locked)
            {
            var respec = Kit.Button(content, "Respec", Vector2.zero, Vector2.one,
                StudyStore.GrowthRespecUsed
                    ? "振り直し  " + StudyStore.RespecDiamondCost + "ダイヤ"
                    : "振り直し  無料",
                20, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            var rrt = respec.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.04f, 1f);
            rrt.anchorMax = new Vector2(0.96f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, y);
            rrt.sizeDelta = new Vector2(0f, 56f);
            respec.onClick.AddListener(() =>
            {
                if (StudyStore.GrowthLocked())
                {
                    ShowToast("バトル中は振り直せません");
                    return;
                }
                if (!StudyStore.TryRespec())
                {
                    ShowToast("ダイヤが足りません");
                    return;
                }
                RebuildGrowth();
            });
            y -= 72f;
            }
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 24f);
            KeepGrowthScroll(scroll);
        }

        void DrawStat(RectTransform parent, float x, float y, string title, string value)
        {
            var t = Kit.Label(parent, title, new Vector2(x, 1f), new Vector2(x + 0.28f, 1f), title, 16, Muted, TextAnchor.MiddleLeft);
            Pin(t.rectTransform, y, t.fontSize + 8);
            var v = Kit.Label(parent, title + "V", new Vector2(x, 1f), new Vector2(x + 0.28f, 1f), value, 24, Color.white, TextAnchor.MiddleLeft);
            Pin(v.rectTransform, y - 26f, v.fontSize + 8);
        }

        float DrawRangeButtons(RectTransform parent, float y)
        {
            string[] labels = { "1週間", "1か月", "1年" };
            for (int i = 0; i < 3; i++)
            {
                bool on = (int)reportRange == i;
                var b = Kit.Button(parent, "Range" + i, Vector2.zero, Vector2.one,
                    labels[i], 18, on ? Accent : Card, on ? Color.black : Color.white, font);
                var rt = b.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.04f + i * 0.32f, 1f);
                rt.anchorMax = new Vector2(0.32f + i * 0.32f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(0f, 40f);
                int captured = i;
                b.onClick.AddListener(() =>
                {
                    reportRange = (ReportRange)captured;
                    RebuildReportDetail();
                });
            }
            return y - 48f;
        }

        float DrawPeriodChart(RectTransform parent, float y, bool openDetail)
        {
            int cols;
            int[,] grid;
            string[] labels;
            GetPeriodGrid(out cols, out grid, out labels);

            int max = 30;
            for (int d = 0; d < cols; d++)
            {
                int sum = 0;
                for (int s = 0; s < 6; s++) sum += grid[d, s];
                if (sum > max) max = sum;
            }

            var box = Kit.Card(parent, "ChartBox", Vector2.zero, Vector2.one);
            var brt = box.rectTransform;
            brt.anchorMin = new Vector2(0.04f, 1f);
            brt.anchorMax = new Vector2(0.96f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, y);
            brt.sizeDelta = new Vector2(0f, 210f);
            if (openDetail)
            {
                var tap = box.gameObject.AddComponent<Button>();
                tap.targetGraphic = box;
                tap.onClick.AddListener(OpenReportDetail);
            }

            float gap = cols <= 7 ? 0.035f : cols <= 12 ? 0.012f : 0.004f;
            float slot = (0.92f - gap * (cols - 1)) / cols;
            float x = 0.04f;
            for (int d = 0; d < cols; d++)
            {
                float x0 = x;
                float x1 = x + slot;
                float stacked = 0f;
                for (int s = 0; s < 6; s++)
                {
                    if (grid[d, s] <= 0) continue;
                    float h = 0.72f * grid[d, s] / max;
                    Kit.Image(brt, "S" + d + s, new Vector2(x0, 0.18f + stacked), new Vector2(x1, 0.18f + stacked + h),
                        StudySubjectColors.Color((StudySubject)s)).raycastTarget = false;
                    stacked += h;
                }
                if (!string.IsNullOrEmpty(labels[d]))
                {
                    Kit.Label(brt, "D" + d, new Vector2(x0, 0.02f), new Vector2(x1, 0.16f),
                        labels[d], cols > 12 ? 12 : 14, Color.white, TextAnchor.MiddleCenter);
                }
                x = x1 + gap;
            }
            return y - 220f;
        }

        void GetPeriodGrid(out int cols, out int[,] grid, out string[] labels)
        {
            if (reportRange == ReportRange.Year)
            {
                cols = 12;
                grid = StudyStore.MonthsOfYear(reportCursor.Year);
                labels = new string[12];
                for (int i = 0; i < 12; i++) labels[i] = (i + 1) + "月";
                return;
            }
            if (reportRange == ReportRange.Month)
            {
                var start = new DateTime(reportCursor.Year, reportCursor.Month, 1);
                cols = DateTime.DaysInMonth(start.Year, start.Month);
                grid = StudyStore.DaysFrom(start, cols);
                labels = new string[cols];
                for (int i = 0; i < cols; i++)
                    labels[i] = (i == 0 || (i + 1) % 5 == 0 || i == cols - 1) ? (i + 1).ToString() : "";
                return;
            }
            var weekStart = StudyStore.WeekStart(reportCursor);
            cols = 7;
            grid = StudyStore.DaysFrom(weekStart, 7);
            labels = new string[7];
            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                string week = "日月火水木金土".Substring((int)day.DayOfWeek, 1);
                labels[i] = day.Day + "\n" + week;
            }
        }

        string PeriodTitle()
        {
            if (reportRange == ReportRange.Year) return reportCursor.Year + "年";
            if (reportRange == ReportRange.Month) return reportCursor.Year + "年" + reportCursor.Month + "月";
            var a = StudyStore.WeekStart(reportCursor);
            var b = a.AddDays(6);
            if (a.Month == b.Month) return a.Month + "月" + a.Day + "日〜" + b.Day + "日";
            return a.Month + "月" + a.Day + "日〜" + b.Month + "月" + b.Day + "日";
        }

        float DrawPeriodNav(RectTransform parent, float y)
        {
            var prev = Kit.Button(parent, "PrevP", Vector2.zero, Vector2.one, "<", 24, Card, Color.white, font);
            var prt = prev.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.04f, 1f);
            prt.anchorMax = new Vector2(0.16f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, y);
            prt.sizeDelta = new Vector2(0f, 40f);
            prev.onClick.AddListener(() => ShiftReportCursor(-1));

            var next = Kit.Button(parent, "NextP", Vector2.zero, Vector2.one, ">", 24, Card, Color.white, font);
            var nrt = next.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.84f, 1f);
            nrt.anchorMax = new Vector2(0.96f, 1f);
            nrt.pivot = new Vector2(0.5f, 1f);
            nrt.anchoredPosition = new Vector2(0f, y);
            nrt.sizeDelta = new Vector2(0f, 40f);
            next.onClick.AddListener(() => ShiftReportCursor(1));

            var title = Kit.Label(parent, "PTitle", new Vector2(0.18f, 1f), new Vector2(0.82f, 1f),
                PeriodTitle(), 20, Color.white, TextAnchor.MiddleCenter);
            Pin(title.rectTransform, y, 40f);
            return y - 48f;
        }

        void ShiftReportCursor(int dir)
        {
            if (reportRange == ReportRange.Week) reportCursor = StudyStore.WeekStart(reportCursor).AddDays(7 * dir);
            else if (reportRange == ReportRange.Month)
                reportCursor = new DateTime(reportCursor.Year, reportCursor.Month, 1).AddMonths(dir);
            else reportCursor = new DateTime(reportCursor.Year + dir, 1, 1);

            var now = StudyStore.Now.Date;
            if (reportCursor > now) reportCursor = now;
            if (reportCursor.Year < now.Year - 8) reportCursor = new DateTime(now.Year - 8, 1, 1);
            RebuildReportDetail();
        }

        void OpenReportDetail()
        {
            reportCursor = StudyStore.Now.Date;
            if (reportDetail != null) reportDetail.SetActive(true);
            RebuildReportDetail();
        }

        void RebuildReportDetail()
        {
            if (reportDetailContent == null) return;
            ClearSpawned(reportDetailContent);
            float y = -8f;
            y = DrawRangeButtons(reportDetailContent, y);
            y -= 4f;
            y = DrawPeriodNav(reportDetailContent, y);
            y -= 8f;
            y = DrawPeriodChart(reportDetailContent, y, false);
            reportDetailContent.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 40f);
        }

        int[] RangeTotals()
        {
            DateTime from = StudyStore.Now.Date.AddDays(-6);
            if (reportRange == ReportRange.Month) from = StudyStore.Now.Date.AddDays(-29);
            if (reportRange == ReportRange.Year) from = new DateTime(StudyStore.Now.Year, StudyStore.Now.Month, 1).AddMonths(-11);
            return StudyStore.SubjectTotalsSince(from);
        }

        float DrawWeekChart(RectTransform parent, float y)
        {
            return DrawPeriodChart(parent, y, true);
        }

        float DrawPieChart(RectTransform parent, float y, int[] totals)
        {
            int sum = 0;
            for (int i = 0; i < totals.Length; i++) sum += totals[i];

            var host = Kit.Card(parent, "PieHost", Vector2.zero, Vector2.one);
            var hrt = host.rectTransform;
            hrt.anchorMin = new Vector2(0.22f, 1f);
            hrt.anchorMax = new Vector2(0.78f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, y);
            hrt.sizeDelta = new Vector2(0f, 220f);
            host.raycastTarget = false;

            var bg = Kit.Image(hrt, "PieBg", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), RpgTheme.Navy);
            Kit.MakeCircle(bg);
            bg.raycastTarget = false;

            if (sum > 0)
            {
                float startDeg = 0f;
                for (int i = 0; i < 6; i++)
                {
                    if (totals[i] <= 0) continue;
                    float portion = totals[i] / (float)sum;
                    var slice = Kit.Image(hrt, "Pie" + i, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f),
                        StudySubjectColors.Color((StudySubject)i));
                    Kit.MakeCircle(slice);
                    slice.type = UnityEngine.UI.Image.Type.Filled;
                    slice.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
                    slice.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
                    slice.fillClockwise = true;
                    slice.fillAmount = portion;
                    slice.rectTransform.localEulerAngles = new Vector3(0f, 0f, -startDeg);
                    slice.raycastTarget = false;
                    startDeg += portion * 360f;
                }
            }
            else
            {
                Kit.Label(hrt, "EmptyPie", Vector2.zero, Vector2.one, "記録なし", 20, Muted, TextAnchor.MiddleCenter);
            }

            var hole = Kit.Image(hrt, "Hole", new Vector2(0.34f, 0.34f), new Vector2(0.66f, 0.66f), RpgTheme.Navy);
            Kit.MakeCircle(hole);
            hole.raycastTarget = false;
            Kit.Label(hrt, "PieSum", new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.6f),
                StudyStore.FormatMinutes(sum), 16, Color.white, TextAnchor.MiddleCenter);
            return y - 232f;
        }

        float DrawDistribution(RectTransform parent, float y, int[] totals)
        {
            int sum = 0;
            for (int i = 0; i < 6; i++) sum += totals[i];
            float x = 0.04f;
            var bar = Kit.Window(parent, "PieBar", Vector2.zero, Vector2.one);
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0.04f, 1f);
            brt.anchorMax = new Vector2(0.96f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, y);
            brt.sizeDelta = new Vector2(0f, 22f);
            if (sum > 0)
            {
                float cursor = 0f;
                for (int i = 0; i < 6; i++)
                {
                    if (totals[i] <= 0) continue;
                    float w = totals[i] / (float)sum;
                    Kit.Image(brt, "P" + i, new Vector2(cursor, 0f), new Vector2(cursor + w, 1f),
                        StudySubjectColors.Color((StudySubject)i));
                    cursor += w;
                }
            }
            y -= 36f;
            for (int i = 0; i < 6; i++)
            {
                int pct = sum <= 0 ? 0 : Mathf.RoundToInt(100f * totals[i] / sum);
                string line = StudySubjectNames.Display((StudySubject)i) + "  " + StudyStore.FormatMinutes(totals[i]) + "  (" + pct + "%)";
                var row = Kit.Label(parent, "Leg" + i, new Vector2(0.08f, 1f), new Vector2(0.96f, 1f), line, 22,
                    StudySubjectColors.Color((StudySubject)i), TextAnchor.MiddleLeft);
                Pin(row.rectTransform, y, row.fontSize + 10);
                y -= 34f;
            }
            return y;
        }

        void KitPinnedLabel(RectTransform parent, string text, float x, float y, int size, Color color)
        {
            var label = Kit.Label(parent, text, new Vector2(x, 1f), new Vector2(0.96f, 1f), text, size, color, TextAnchor.MiddleLeft);
            Pin(label.rectTransform, y, label.fontSize + 12);
        }

        static void Pin(RectTransform rt, float y, float h)
        {
            rt.pivot = new Vector2(0f, 1f);
            rt.anchorMin = new Vector2(rt.anchorMin.x, 1f);
            rt.anchorMax = new Vector2(rt.anchorMax.x, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, h);
        }

        void BuildTimerOverlay()
        {
            timerOverlay = new GameObject("TimerOverlay");
            timerOverlay.transform.SetParent(root, false);
            var rt = timerOverlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            timerOverlay.AddComponent<Image>().color = RpgTheme.DimHeavy;
            overlayTitle = Kit.Label(rt, "OvTitle", new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.72f),
                "教材", 28, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
            overlayTime = Kit.Label(rt, "OvTime", new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.62f),
                "00:00", 84, RpgTheme.GoldHi, TextAnchor.MiddleCenter);
            overlayHint = Kit.Label(rt, "OvHint", new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.42f),
                "", 16, Muted, TextAnchor.MiddleCenter);
            var pause = Kit.Button(rt, "OvPause", new Vector2(0.18f, 0.26f), new Vector2(0.82f, 0.34f),
                "一時停止", 24, Accent, Color.black, font);
            overlayPauseLabel = pause.GetComponentInChildren<Text>();
            pause.onClick.AddListener(() =>
            {
                if (StudyTimer.Instance != null) StudyTimer.Instance.TogglePause();
                RefreshTimerWidgets();
            });
            var stop = Kit.Button(rt, "OvStop", new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.24f),
                "保存して終了", 26, RpgTheme.Gold, RpgTheme.Bg, font);
            RpgTheme.AddZhuAccent(stop.transform);
            stop.onClick.AddListener(() =>
            {
                if (StudyTimer.Instance != null) StudyTimer.Instance.StopTimer();
                timerOverlay.SetActive(false);
                ShowTab(Tab.Timeline);
            });
            var hide = Kit.Button(rt, "OvHide", new Vector2(0.18f, 0.07f), new Vector2(0.82f, 0.14f),
                "閉じる", 20, Card, Color.white, font);
            hide.onClick.AddListener(() =>
            {
                timerOverlay.SetActive(false);
                if (tab == Tab.Record) RebuildRecord();
            });
            timerOverlay.SetActive(false);
        }

        void BuildAddDialog()
        {
            addDialog = new GameObject("AddDialog");
            addDialog.transform.SetParent(root, false);
            var rt = addDialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            addDialog.AddComponent<Image>().color = RpgTheme.Dim;

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.84f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "AddTitle", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f),
                "教材を追加", 24, Color.white, TextAnchor.MiddleLeft);

            addInput = Kit.Input(prt, "AddInput", new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.86f), font);
            Kit.Label(prt, "CatLbl", new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.71f),
                "カテゴリ", 16, Muted, TextAnchor.MiddleLeft);

            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float x0 = 0.05f + col * 0.31f;
                float y1 = 0.62f - row * 0.16f;
                float y0 = y1 - 0.14f;
                var subject = (StudySubject)i;
                var b = Kit.Button(prt, "Subj" + i,
                    new Vector2(x0, y0), new Vector2(x0 + 0.29f, y1),
                    StudySubjectNames.Display(subject), 18, StudySubjectColors.Color(subject) * 0.45f, Color.white, font);
                var txt = b.GetComponentInChildren<Text>();
                if (txt != null) Kit.Fit(txt, 18);
                int captured = i;
                b.onClick.AddListener(() =>
                {
                    addSubject = (StudySubject)captured;
                    RefreshAddSubjectButtons();
                });
                addSubjectButtons[i] = b;
            }

            var save = Kit.Button(prt, "Save", new Vector2(0.52f, 0.04f), new Vector2(0.95f, 0.16f),
                "保存", 22, Accent, Color.black, font);
            save.onClick.AddListener(SaveMaterial);
            var cancel = Kit.Button(prt, "Cancel", new Vector2(0.05f, 0.04f), new Vector2(0.48f, 0.16f),
                "キャンセル", 22, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            cancel.onClick.AddListener(() => addDialog.SetActive(false));
            addDialog.SetActive(false);
        }

        void RefreshAddSubjectButtons()
        {
            for (int i = 0; i < addSubjectButtons.Length; i++)
            {
                var btn = addSubjectButtons[i];
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                if (img == null) continue;
                Color c = StudySubjectColors.Color((StudySubject)i);
                img.sprite = null;
                img.type = Image.Type.Simple;
                img.color = (int)addSubject == i ? c : c * 0.35f;
            }
        }

        void BuildProfileDialog()
        {
            profileDialog = new GameObject("ProfileDialog");
            profileDialog.transform.SetParent(root, false);
            var rt = profileDialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            profileDialog.AddComponent<Image>().color = RpgTheme.Dim;

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.94f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "Title", new Vector2(0.05f, 0.93f), new Vector2(0.42f, 0.99f),
                "プロフィール", 22, Color.white, TextAnchor.MiddleLeft);
            var settings = Kit.Button(prt, "Settings", new Vector2(0.42f, 0.935f), new Vector2(0.58f, 0.985f),
                "設定", 15, new Color(0.22f, 0.22f, 0.26f), Color.white, font);
            settings.onClick.AddListener(OpenSettingsDialog);
            var dress = Kit.Button(prt, "Dress", new Vector2(0.60f, 0.935f), new Vector2(0.76f, 0.985f),
                "装飾", 15, RpgTheme.Gold, RpgTheme.Bg, font);
            dress.onClick.AddListener(OpenWardrobe);

            var previewHost = new GameObject("PreviewHost");
            previewHost.transform.SetParent(prt, false);
            profileAvatarHost = previewHost.AddComponent<RectTransform>();
            profileAvatarHost.anchorMin = new Vector2(0.80f, 0.90f);
            profileAvatarHost.anchorMax = new Vector2(0.97f, 0.995f);
            profileAvatarHost.offsetMin = Vector2.zero;
            profileAvatarHost.offsetMax = Vector2.zero;

            profileClimbText = Kit.Label(prt, "Climb", new Vector2(0.05f, 0.87f), new Vector2(0.76f, 0.92f),
                "", 16, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            var pick = Kit.Button(prt, "PickPhoto", new Vector2(0.05f, 0.80f), new Vector2(0.40f, 0.865f),
                "写真を選ぶ", 16, Accent, Color.black, font);
            pick.onClick.AddListener(PickProfilePhoto);
            var clear = Kit.Button(prt, "ClearPhoto", new Vector2(0.42f, 0.80f), new Vector2(0.76f, 0.865f),
                "写真を消す", 15, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            clear.onClick.AddListener(ClearProfilePhoto);

            Kit.Label(prt, "GoalLbl", new Vector2(0.05f, 0.745f), new Vector2(0.95f, 0.79f),
                "達成目標", 15, Muted, TextAnchor.MiddleLeft);
            profileGoalInput = Kit.Input(prt, "GoalInput", new Vector2(0.05f, 0.675f), new Vector2(0.95f, 0.745f),
                font, "例：○○大学合格");
            Kit.Label(prt, "NameLbl", new Vector2(0.05f, 0.625f), new Vector2(0.95f, 0.665f),
                "表示名", 15, Muted, TextAnchor.MiddleLeft);
            profileNameInput = Kit.Input(prt, "NameInput", new Vector2(0.05f, 0.555f), new Vector2(0.95f, 0.625f),
                font, "プレイヤー");
            Kit.Label(prt, "SchoolLbl", new Vector2(0.05f, 0.505f), new Vector2(0.95f, 0.545f),
                "志望校", 15, Muted, TextAnchor.MiddleLeft);
            profileSchoolInput = Kit.Input(prt, "SchoolInput", new Vector2(0.05f, 0.435f), new Vector2(0.95f, 0.505f),
                font, "例：○○大学");
            Kit.Label(prt, "BioLbl", new Vector2(0.05f, 0.385f), new Vector2(0.95f, 0.425f),
                "自己紹介", 15, Muted, TextAnchor.MiddleLeft);
            profileBioInput = Kit.Input(prt, "BioInput", new Vector2(0.05f, 0.315f), new Vector2(0.95f, 0.385f),
                font, "勉強がんばる");
            Kit.Label(prt, "ColorLbl", new Vector2(0.05f, 0.255f), new Vector2(0.95f, 0.305f),
                "アイコンの色（写真なしのとき）", 15, Muted, TextAnchor.MiddleLeft);

            for (int i = 0; i < 6; i++)
            {
                int col = i % 6;
                float x0 = 0.05f + col * 0.155f;
                var b = Kit.Button(prt, "Color" + i,
                    new Vector2(x0, 0.16f), new Vector2(x0 + 0.13f, 0.25f),
                    "", 1, StudySubjectColors.Color((StudySubject)i), Color.white, font);
                int captured = i;
                b.onClick.AddListener(() =>
                {
                    profileColorIndex = captured;
                    RefreshProfileColorButtons();
                });
                profileColorButtons[i] = b;
                Kit.MakeCircle(b.GetComponent<Image>());
            }

            var save = Kit.Button(prt, "Save", new Vector2(0.52f, 0.04f), new Vector2(0.94f, 0.15f),
                "保存", 22, Accent, Color.black, font);
            save.onClick.AddListener(SaveProfileDialog);
            var cancel = Kit.Button(prt, "Cancel", new Vector2(0.06f, 0.04f), new Vector2(0.48f, 0.15f),
                "キャンセル", 22, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            cancel.onClick.AddListener(() => profileDialog.SetActive(false));
            profileDialog.SetActive(false);
        }

        void BuildFriendDialog()
        {
            friendDialog = new GameObject("FriendDialog");
            friendDialog.transform.SetParent(root, false);
            var rt = friendDialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            friendDialog.AddComponent<Image>().color = RpgTheme.Dim;

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "Title", new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.98f),
                "フレンド", 26, Color.white, TextAnchor.MiddleLeft);
            friendMyCodeText = Kit.Label(prt, "MyCode", new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.90f),
                "", 20, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            Kit.Label(prt, "CodeLbl", new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.81f),
                "フレンドのID", 16, Muted, TextAnchor.MiddleLeft);
            friendCodeInput = Kit.Input(prt, "CodeInput", new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.74f),
                font, "6文字");
            var add = Kit.Button(prt, "Add", new Vector2(0.52f, 0.56f), new Vector2(0.94f, 0.65f),
                "申請", 20, RpgTheme.Gold, RpgTheme.Bg, font);
            add.onClick.AddListener(SubmitFriend);
            friendStatusText = Kit.Label(prt, "Status", new Vector2(0.06f, 0.56f), new Vector2(0.50f, 0.65f),
                "", 16, RpgTheme.Danger, TextAnchor.MiddleLeft);

            var listHost = new GameObject("FriendList");
            listHost.transform.SetParent(prt, false);
            var lrt = listHost.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.04f, 0.16f);
            lrt.anchorMax = new Vector2(0.96f, 0.53f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var listScroll = MakeScroll(listHost.transform, "FriendScroll");
            friendListContent = listScroll.content;

            var close = Kit.Button(prt, "Close", new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.13f),
                "閉じる", 22, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            close.onClick.AddListener(() => friendDialog.SetActive(false));
            friendDialog.SetActive(false);
        }

        void OpenFriendRank()
        {
            if (root == null) return;
            var old = root.Find("FriendRank");
            if (old != null) Destroy(old.gameObject);
            var dialog = new GameObject("FriendRank");
            dialog.transform.SetParent(root, false);
            var rt = dialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            dialog.AddComponent<Image>().color = RpgTheme.DimHeavy;
            var panel = Kit.Window(rt, "Panel", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            Kit.Label(panel.transform, "Title", new Vector2(0.06f, 0.90f), new Vector2(0.55f, 0.98f),
                "フレンド", 26, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            var add = Kit.Button(panel.transform, "Add", new Vector2(0.56f, 0.91f), new Vector2(0.76f, 0.98f),
                "申請", 16, RpgTheme.Gold, RpgTheme.Bg, font);
            add.onClick.AddListener(() => OpenFriendDialog());
            var close = Kit.Button(panel.transform, "Close", new Vector2(0.78f, 0.91f), new Vector2(0.96f, 0.98f),
                "閉じる", 16, new Color(0.22f, 0.22f, 0.25f), Color.white, font);
            close.onClick.AddListener(() => Destroy(dialog));
            if (SupabaseSync.Ready && !rankRefreshing)
            {
                rankRefreshing = true;
                StartCoroutine(RefreshFriendRank());
            }
            var scroll = MakeScroll(panel.transform, "RankScroll");
            var srt = scroll.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.04f, 0.04f);
            srt.anchorMax = new Vector2(0.96f, 0.88f);
            var content = scroll.content;
            var rows = new List<FriendRankRow>();
            rows.Add(new FriendRankRow
            {
                name = StudyStore.ProfileName(),
                line = "Lv." + StudyStore.PlayerLevel() + "   " + StudyStore.MyClimbLine(),
                score = StudyStore.DungeonWeek * 1000 + StudyStore.DungeonFloor,
                self = true
            });
            var friends = StudyStore.FriendList;
            for (int i = 0; i < friends.Count; i++)
            {
                var friend = friends[i];
                if (friend == null) continue;
                int week = friend.week <= 0 ? 1 : friend.week;
                int floor = friend.floor <= 0 ? 1 : friend.floor;
                rows.Add(new FriendRankRow
                {
                    name = string.IsNullOrEmpty(friend.name) ? friend.code : friend.name,
                    line = "Lv." + Mathf.Max(1, friend.level) + "   " + StudyStore.ClimbLine(week, floor),
                    score = week * 1000 + floor,
                    self = false,
                    friend = friend
                });
            }
            rows.Sort((a, b) => b.score.CompareTo(a.score));
            float y = -8f;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = Kit.Card(content, "Rank", Vector2.zero, Vector2.one);
                var rrt = row.rectTransform;
                rrt.anchorMin = new Vector2(0.02f, 1f);
                rrt.anchorMax = new Vector2(0.98f, 1f);
                rrt.pivot = new Vector2(0.5f, 1f);
                rrt.anchoredPosition = new Vector2(0f, y);
                rrt.sizeDelta = new Vector2(0f, 72f);
                Kit.Label(rrt, "Place", new Vector2(0.04f, 0.15f), new Vector2(0.18f, 0.85f),
                    (i + 1) + "位", 20, i == 0 ? RpgTheme.GoldHi : Color.white, TextAnchor.MiddleLeft);
                Kit.Label(rrt, "Name", new Vector2(0.18f, 0.48f), new Vector2(0.96f, 0.92f),
                    rows[i].self ? rows[i].name + "  自分" : rows[i].name, 18, Color.white, TextAnchor.MiddleLeft);
                Kit.Label(rrt, "Line", new Vector2(0.18f, 0.08f), new Vector2(0.96f, 0.48f),
                    rows[i].line, 16, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
                if (!rows[i].self && rows[i].friend != null)
                {
                    var view = row.gameObject.AddComponent<Button>();
                    view.targetGraphic = row;
                    FriendEntry shown = rows[i].friend;
                    view.onClick.AddListener(() => OpenFriendProfile(shown));
                }
                y -= 80f;
            }
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 12f);
        }

        struct FriendRankRow
        {
            public string name;
            public string line;
            public int score;
            public bool self;
            public FriendEntry friend;
        }

        void AskDeleteLog(StudyLogEntry log)
        {
            if (log == null || confirmDialog == null) return;
            pendingDeleteId = null;
            pendingDeleteLogId = log.id;
            if (confirmText != null)
                confirmText.text = log.materialName + "  " + StudyStore.FormatDuration(log) + "\nを削除します。もらったBPは戻します。";
            confirmDialog.SetActive(true);
            confirmDialog.transform.SetAsLastSibling();
        }

        void OpenFriendDialog()
        {
            if (root != null)
            {
                var rank = root.Find("FriendRank");
                if (rank != null) Destroy(rank.gameObject);
            }
            rankRefreshing = false;
            if (friendCodeInput != null) friendCodeInput.text = "";
            RefreshFriendDialog("");
            if (friendDialog != null)
            {
                friendDialog.SetActive(true);
                friendDialog.transform.SetAsLastSibling();
            }
            if (SupabaseSync.Ready) StartCoroutine(RefreshFriendDialogFromServer());
        }

        void SubmitFriend()
        {
            string code = friendCodeInput != null ? friendCodeInput.text : "";
            StartCoroutine(SendFriendRequest(code));
        }

        IEnumerator SendFriendRequest(string code)
        {
            yield return SupabaseSync.RequestFriend(code, null);
            string msg = SupabaseSync.LastMessage;
            bool good = FriendStatusGood(msg);
            if (good && msg == "申請しました")
            {
                if (friendCodeInput != null) friendCodeInput.text = "";
            }
            RefreshFriendDialog(msg);
            if (tab == Tab.Timeline) RebuildTimeline();
        }

        IEnumerator RefreshFriendDialogFromServer()
        {
            yield return SupabaseSync.SyncSocial();
            if (friendDialog != null && friendDialog.activeSelf)
                RefreshFriendDialog(SupabaseSync.LastMessage == "サーバーの準備がまだです" ? SupabaseSync.LastMessage : "");
        }

        IEnumerator AnswerAndRefresh(string code, string name, bool accept)
        {
            yield return SupabaseSync.AnswerRequest(code, name, accept);
            RefreshFriendDialog(SupabaseSync.LastMessage);
            if (tab == Tab.Timeline) RebuildTimeline();
        }

        IEnumerator DropAndRefresh(string code)
        {
            StudyStore.RemoveFriend(code);
            yield return SupabaseSync.DropFriend(code);
            RefreshFriendDialog("");
            if (tab == Tab.Timeline) RebuildTimeline();
        }

        IEnumerator RefreshTimelineFeed()
        {
            yield return SupabaseSync.SyncSocial();
            yield return SupabaseSync.PushMyLogs();
            yield return SupabaseSync.PullFriendLogs();
            if (tab == Tab.Timeline) RebuildTimeline();
        }

        static bool FriendStatusGood(string status)
        {
            return status == "申請しました"
                || status == "フレンドになりました"
                || status == "すでにフレンドです"
                || status == "申請中です"
                || status == "相手から申請が来ています";
        }

        IEnumerator RefreshFriendRank()
        {
            yield return SupabaseSync.SyncSocial();
            if (root != null && root.Find("FriendRank") != null
                && (friendDialog == null || !friendDialog.activeSelf))
                OpenFriendRank();
            rankRefreshing = false;
        }

        void RefreshFriendDialog(string status)
        {
            if (friendMyCodeText != null)
                friendMyCodeText.text = "あなたのコード  " + StudyStore.MyFriendCode();
            if (friendStatusText != null)
            {
                friendStatusText.text = status ?? "";
                friendStatusText.color = FriendStatusGood(status) ? RpgTheme.GoldHi : RpgTheme.Danger;
            }
            if (friendListContent == null) return;
            ClearSpawned(friendListContent);
            float y = -8f;
            var incoming = SupabaseSync.Incoming;
            for (int i = 0; i < incoming.Count; i++)
            {
                var req = incoming[i];
                if (req == null) continue;
                var row = Kit.Card(friendListContent, "In", Vector2.zero, Vector2.one);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0.02f, 1f);
                rt.anchorMax = new Vector2(0.98f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(0f, 72f);
                var name = Kit.Label(rt, "Name", new Vector2(0.04f, 0.48f), new Vector2(0.48f, 0.92f),
                    req.fromName, 18, Color.white, TextAnchor.MiddleLeft);
                var code = Kit.Label(rt, "Code", new Vector2(0.04f, 0.08f), new Vector2(0.48f, 0.48f),
                    req.fromCode + "  申請", 15, Muted, TextAnchor.MiddleLeft);
                string capturedCode = req.fromCode;
                string capturedName = req.fromName;
                var yes = Kit.Button(rt, "Yes", new Vector2(0.50f, 0.18f), new Vector2(0.72f, 0.82f),
                    "受理", 16, RpgTheme.Gold, RpgTheme.Bg, font);
                yes.onClick.AddListener(() => StartCoroutine(AnswerAndRefresh(capturedCode, capturedName, true)));
                var no = Kit.Button(rt, "No", new Vector2(0.74f, 0.18f), new Vector2(0.96f, 0.82f),
                    "拒否", 16, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
                no.onClick.AddListener(() => StartCoroutine(AnswerAndRefresh(capturedCode, capturedName, false)));
                y -= 80f;
            }
            var outgoing = SupabaseSync.Outgoing;
            for (int i = 0; i < outgoing.Count; i++)
            {
                var req = outgoing[i];
                if (req == null) continue;
                var row = Kit.Card(friendListContent, "Out", Vector2.zero, Vector2.one);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0.02f, 1f);
                rt.anchorMax = new Vector2(0.98f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(0f, 64f);
                string label = req.toCode;
                if (!string.IsNullOrEmpty(req.fromName)
                    && req.fromName != StudyStore.ProfileName()
                    && req.fromName != req.fromCode)
                    label = req.fromName;
                Kit.Label(rt, "Name", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.88f),
                    label + "  申請中", 18, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
                y -= 72f;
            }
            var friends = StudyStore.FriendList;
            if (friends.Count == 0 && incoming.Count == 0 && outgoing.Count == 0)
            {
                var empty = Kit.Label(friendListContent, "None", new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                    "まだフレンドがいません", 18, Muted, TextAnchor.MiddleLeft);
                empty.rectTransform.pivot = new Vector2(0.5f, 1f);
                empty.rectTransform.anchoredPosition = new Vector2(0f, y);
                empty.rectTransform.sizeDelta = new Vector2(0f, 40f);
                friendListContent.sizeDelta = new Vector2(0f, 56f);
                return;
            }
            for (int i = 0; i < friends.Count; i++)
            {
                var friend = friends[i];
                if (friend == null) continue;
                var row = Kit.Card(friendListContent, "Row", Vector2.zero, Vector2.one);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0.02f, 1f);
                rt.anchorMax = new Vector2(0.98f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(0f, 72f);
                DrawFriendAvatar(rt, friend, 48f, new Vector2(0f, 0.5f), new Vector2(32f, 0f));
                var name = Kit.Label(rt, "Name", new Vector2(0f, 0.48f), new Vector2(0.62f, 0.92f),
                    friend.name, 18, Color.white, TextAnchor.MiddleLeft);
                name.rectTransform.offsetMin = new Vector2(64f, 0f);
                var code = Kit.Label(rt, "Code", new Vector2(0f, 0.08f), new Vector2(0.62f, 0.48f),
                    friend.code + "   " + StudyStore.ClimbLine(friend.week, friend.floor), 16, Muted, TextAnchor.MiddleLeft);
                code.rectTransform.offsetMin = new Vector2(64f, 0f);
                var drop = Kit.Button(rt, "Drop", new Vector2(0.66f, 0.18f), new Vector2(0.96f, 0.82f),
                    "外す", 16, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
                string captured = friend.code;
                drop.onClick.AddListener(() => StartCoroutine(DropAndRefresh(captured)));
                var view = row.gameObject.AddComponent<Button>();
                view.targetGraphic = row;
                FriendEntry shown = friend;
                view.onClick.AddListener(() => OpenFriendProfile(shown));
                y -= 80f;
            }
            friendListContent.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 12f);
        }

        void OpenFriendProfile(FriendEntry friend)
        {
            if (friend == null || root == null) return;
            var old = root.Find("FriendProfile");
            if (old != null) Destroy(old.gameObject);
            var dialog = new GameObject("FriendProfile");
            dialog.transform.SetParent(root, false);
            var rt = dialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = dialog.AddComponent<Image>();
            dim.color = RpgTheme.Dim;
            var dimBtn = dialog.AddComponent<Button>();
            dimBtn.targetGraphic = dim;
            dimBtn.onClick.AddListener(() => Destroy(dialog));

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.90f));
            var prt = panel.rectTransform;
            DrawFriendAvatar(prt, friend, 92f, new Vector2(0.20f, 0.86f), Vector2.zero);
            Kit.Label(prt, "Name", new Vector2(0.36f, 0.86f), new Vector2(0.96f, 0.96f),
                string.IsNullOrEmpty(friend.name) ? friend.code : friend.name, 26, Color.white, TextAnchor.MiddleLeft);
            Kit.Label(prt, "Meta", new Vector2(0.36f, 0.76f), new Vector2(0.96f, 0.86f),
                "Lv." + Mathf.Max(1, friend.level) + "    " + friend.code, 15, Muted, TextAnchor.MiddleLeft);

            string goal = string.IsNullOrEmpty(friend.goal) ? "目標はまだありません" : friend.goal;
            Kit.Label(prt, "GoalCap", new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.74f),
                "達成目標", 14, Muted, TextAnchor.LowerLeft);
            Kit.Label(prt, "Goal", new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.68f),
                goal, 28, RpgTheme.GoldHi, TextAnchor.UpperLeft);
            Kit.Label(prt, "Study", new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.56f),
                "今週 " + FormatStudyMinutes(friend.weekMinutes) + "    合計 " + FormatStudyMinutes(friend.lifetimeMinutes),
                18, Color.white, TextAnchor.MiddleLeft);
            Kit.Label(prt, "Climb", new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.46f),
                StudyStore.ClimbLine(friend.week, friend.floor), 16, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            string school = string.IsNullOrEmpty(friend.school) ? "未設定" : friend.school;
            string bio = string.IsNullOrEmpty(friend.bio) ? "未設定" : friend.bio;
            Kit.Label(prt, "Detail", new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.38f),
                "志望校  " + school + "\n自己紹介  " + bio
                + "\n1日 " + friend.dailyGoalMinutes + "分    今週の目標 " + friend.weeklyGoalMinutes + "分",
                18, Color.white, TextAnchor.UpperLeft);

            FriendEntry captured = friend;
            var bodyBtn = Kit.Button(prt, "Body3D", new Vector2(0.06f, 0.03f), new Vector2(0.32f, 0.11f),
                "3Dキャラ", 16, RpgTheme.Gold, RpgTheme.Bg, font);
            bodyBtn.onClick.AddListener(() => OpenFriendBody(captured));
            var loadoutBtn = Kit.Button(prt, "Loadout", new Vector2(0.34f, 0.03f), new Vector2(0.60f, 0.11f),
                "技構成", 16, RpgTheme.Gold, RpgTheme.Bg, font);
            loadoutBtn.onClick.AddListener(() => OpenFriendLoadout(captured));
            var close = Kit.Button(prt, "Close", new Vector2(0.62f, 0.03f), new Vector2(0.94f, 0.11f),
                "閉じる", 16, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            close.onClick.AddListener(() => Destroy(dialog));
        }

        void OpenFriendLoadout(FriendEntry friend)
        {
            if (friend == null || root == null) return;
            var old = root.Find("FriendLoadout");
            if (old != null) Destroy(old.gameObject);
            var dialog = new GameObject("FriendLoadout");
            dialog.transform.SetParent(root, false);
            var rt = dialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = dialog.AddComponent<Image>();
            dim.color = RpgTheme.Dim;
            var dimBtn = dialog.AddComponent<Button>();
            dimBtn.targetGraphic = dim;
            dimBtn.onClick.AddListener(() => Destroy(dialog));

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.80f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "Title", new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.98f),
                (string.IsNullOrEmpty(friend.name) ? friend.code : friend.name) + "の技構成",
                22, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            var ids = StudyStore.ParseSkillsCsv(friend.skillsCsv);
            float y = -8f;
            var scrollHost = new GameObject("LoadoutScroll");
            scrollHost.transform.SetParent(prt, false);
            var shrt = scrollHost.AddComponent<RectTransform>();
            shrt.anchorMin = new Vector2(0.04f, 0.14f);
            shrt.anchorMax = new Vector2(0.96f, 0.86f);
            shrt.offsetMin = Vector2.zero;
            shrt.offsetMax = Vector2.zero;
            var scroll = MakeScroll(scrollHost.transform, "Scroll");
            var content = scroll.content;
            string[] slotNames = { "枠1", "枠2", "枠3", "枠4", "必殺" };
            for (int i = 0; i < ids.Length; i++)
            {
                string name = SkillDisplayName(ids[i]);
                string slot = i < slotNames.Length ? slotNames[i] : (i + 1).ToString();
                var label = Kit.Label(content, "Slot" + i, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                    slot + "  " + (string.IsNullOrEmpty(name) ? "未設定" : name), 18, Color.white, TextAnchor.MiddleLeft);
                var lrt = label.rectTransform;
                lrt.pivot = new Vector2(0.5f, 1f);
                lrt.anchoredPosition = new Vector2(0f, y);
                lrt.sizeDelta = new Vector2(0f, 44f);
                y -= 48f;
            }
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 12f);
            var close = Kit.Button(prt, "Close", new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.12f),
                "閉じる", 20, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            close.onClick.AddListener(() => Destroy(dialog));
        }

        static string FormatStudyMinutes(int minutes)
        {
            minutes = Mathf.Max(0, minutes);
            int hours = minutes / 60;
            int rest = minutes % 60;
            if (hours <= 0) return rest + "分";
            if (rest == 0) return hours + "時間";
            return hours + "時間" + rest + "分";
        }

        static string SkillDisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            var ult = ShiftingMetropolis.Battle.UltimateCatalog.Get(id);
            if (ult != null) return ult.displayName;
            var basic = ShiftingMetropolis.Battle.SkillCatalog.Get(id);
            if (basic != null) return basic.displayName;
            return id;
        }

        GameObject friendBodyStudio;
        Camera friendBodyCam;
        RenderTexture friendBodyRt;
        RawImage friendBodyImage;

        void OpenFriendBody(FriendEntry friend)
        {
            if (friend == null || root == null) return;
            var old = root.Find("FriendBody");
            if (old != null) Destroy(old.gameObject);
            if (friendBodyStudio != null) Destroy(friendBodyStudio);
            if (friendBodyRt != null) { friendBodyRt.Release(); friendBodyRt = null; }

            var dialog = new GameObject("FriendBody");
            dialog.transform.SetParent(root, false);
            var rt = dialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = dialog.AddComponent<Image>();
            dim.color = RpgTheme.Dim;
            var dimBtn = dialog.AddComponent<Button>();
            dimBtn.targetGraphic = dim;
            dimBtn.onClick.AddListener(() => CloseFriendBody(dialog));

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.86f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "Title", new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.98f),
                (string.IsNullOrEmpty(friend.name) ? friend.code : friend.name) + "の3Dキャラ",
                20, RpgTheme.GoldHi, TextAnchor.MiddleLeft);

            var imgGo = new GameObject("Preview");
            imgGo.transform.SetParent(prt, false);
            var irt = imgGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.08f, 0.12f);
            irt.anchorMax = new Vector2(0.92f, 0.88f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            friendBodyImage = imgGo.AddComponent<RawImage>();

            var close = Kit.Button(prt, "Close", new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.10f),
                "閉じる", 18, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            close.onClick.AddListener(() => CloseFriendBody(dialog));

            BuildFriendBodyStudio(friend);
        }

        void CloseFriendBody(GameObject dialog)
        {
            if (dialog != null) Destroy(dialog);
            if (friendBodyStudio != null) Destroy(friendBodyStudio);
            friendBodyStudio = null;
            if (friendBodyRt != null) { friendBodyRt.Release(); friendBodyRt = null; }
        }

        void BuildFriendBodyStudio(FriendEntry friend)
        {
            var lookArr = StudyStore.ParseLookCsv(friend.lookCsv);
            bool hasLook = false;
            for (int i = 0; i < lookArr.Length; i++) if (lookArr[i] != 0) { hasLook = true; break; }

            friendBodyStudio = new GameObject("FriendBodyStudio");
            friendBodyStudio.transform.position = new Vector3(0f, -4500f, 0f);

            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(friendBodyStudio.transform, false);

            HeroAppearance.SkinKind heroKind = HeroAppearance.SkinKind.GanzSe;
            GameObject hero = hasLook
                ? HeroAppearance.SpawnLooked(pivot, Vector3.zero, Quaternion.Euler(0f, 180f, 0f),
                    i => (i >= 0 && i < lookArr.Length) ? lookArr[i] : 0, out heroKind)
                : null;
            if (hero == null)
            {
                Kit.Label(friendBodyImage.rectTransform, "None", Vector2.zero, Vector2.one,
                    "3Dデータがまだありません", 18, Muted, TextAnchor.MiddleCenter);
            }
            else
            {
                SetLayerRecursively(hero, 31);
                var fighter = hero.AddComponent<BattleFighter>();
                fighter.Bind(hero.transform, false, heroKind, hero.name);
            }

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(friendBodyStudio.transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(40f, 30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.cullingMask = 1 << 31;

            friendBodyRt = new RenderTexture(640, 640, 16, RenderTextureFormat.ARGB32);
            friendBodyRt.Create();
            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(friendBodyStudio.transform, false);
            friendBodyCam = camGo.AddComponent<Camera>();
            friendBodyCam.clearFlags = CameraClearFlags.SolidColor;
            friendBodyCam.backgroundColor = RpgTheme.Bg;
            friendBodyCam.fieldOfView = 32f;
            friendBodyCam.aspect = 1f;
            friendBodyCam.nearClipPlane = 0.05f;
            friendBodyCam.farClipPlane = 25f;
            friendBodyCam.targetTexture = friendBodyRt;
            friendBodyCam.allowHDR = false;
            friendBodyCam.cullingMask = 1 << 31;
            friendBodyCam.transform.position = friendBodyStudio.transform.position + new Vector3(0f, 1f, -3.2f);
            friendBodyCam.transform.LookAt(friendBodyStudio.transform.position + Vector3.up * 1.02f);
            if (friendBodyImage != null) friendBodyImage.texture = friendBodyRt;
        }

        void OpenSettingsDialog()
        {
            if (root == null) return;
            var old = root.Find("SettingsDialog");
            if (old != null) Destroy(old.gameObject);
            var dialog = new GameObject("SettingsDialog");
            dialog.transform.SetParent(root, false);
            var rt = dialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            dialog.AddComponent<Image>().color = RpgTheme.Dim;
            var panel = Kit.Frame(rt, "Panel", new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.72f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "Title", new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f),
                "設定", 28, Color.white, TextAnchor.MiddleLeft);
            Kit.Label(prt, "Hint", new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.76f),
                "この端末のセーブを消します。\n勉強記録、BP、ガチャ、フレンド一覧が消えます。", 18, Muted, TextAnchor.UpperLeft);
            var wipe = Kit.Button(prt, "Wipe", new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.42f),
                "データ削除", 22, RpgTheme.ButtonFill, RpgTheme.Zhu, font);
            wipe.onClick.AddListener(ConfirmErase);
            var close = Kit.Button(prt, "Close", new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.18f),
                "閉じる", 20, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            close.onClick.AddListener(() => Destroy(dialog));
        }

        void ConfirmErase()
        {
            if (root == null) return;
            var old = root.Find("EraseConfirm");
            if (old != null) Destroy(old.gameObject);
            var dialog = new GameObject("EraseConfirm");
            dialog.transform.SetParent(root, false);
            var rt = dialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            dialog.AddComponent<Image>().color = RpgTheme.DimHeavy;
            var panel = Kit.Frame(rt, "Panel", new Vector2(0.10f, 0.32f), new Vector2(0.90f, 0.68f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "Ask", new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.92f),
                "消しますか？\nこの操作は戻せません。", 26, Color.white, TextAnchor.MiddleCenter);
            var yes = Kit.Button(prt, "Yes", new Vector2(0.08f, 0.10f), new Vector2(0.46f, 0.34f),
                "消す", 22, RpgTheme.ButtonFill, RpgTheme.Zhu, font);
            yes.onClick.AddListener(() =>
            {
                StudyStore.EraseAllProgress();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
            var no = Kit.Button(prt, "No", new Vector2(0.54f, 0.10f), new Vector2(0.92f, 0.34f),
                "やめる", 22, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            no.onClick.AddListener(() => Destroy(dialog));
        }

        void OpenWardrobe()
        {
            if (root == null) return;
            var old = root.Find("Wardrobe");
            if (old != null) Destroy(old.gameObject);
            var dialog = new GameObject("Wardrobe");
            dialog.transform.SetParent(root, false);
            var rt = dialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            dialog.AddComponent<Image>().color = RpgTheme.DimHeavy;
            var panel = Kit.Window(rt, "Panel", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            Kit.Label(panel.transform, "Title", new Vector2(0.06f, 0.90f), new Vector2(0.70f, 0.98f),
                "装飾", 26, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            var close = Kit.Button(panel.transform, "Close", new Vector2(0.72f, 0.91f), new Vector2(0.96f, 0.98f),
                "閉じる", 16, new Color(0.22f, 0.22f, 0.25f), Color.white, font);
            close.onClick.AddListener(() =>
            {
                Destroy(dialog);
                RefreshProfilePreview();
                if (tab == Tab.Timeline) RebuildTimeline();
            });
            var scroll = MakeScroll(panel.transform, "WardrobeScroll");
            var srt = scroll.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.04f, 0.04f);
            srt.anchorMax = new Vector2(0.96f, 0.88f);
            var content = scroll.content;
            float y = -8f;
            y = DrawWardrobeSlot(content, y, "枠", "frame",
                new[] { "frame_gold", "frame_ink", "frame_peach", "frame_jade", "frame_sea", "frame_violet", "frame_double", "frame_zhu", "frame_silver", "frame_jade_double", "frame_triple", "frame_god" },
                new[] { "金の細枠", "墨の細枠", "桃の細枠", "翠", "蒼", "紫", "二重", "朱", "銀", "翠二重", "三重", "神枠" });
            y = DrawWardrobeSlot(content, y, "肩書き", "title",
                new[] { "title_study", "title_night", "title_book", "title_tower", "title_ace", "title_god", "title_moon", "title_lord", "title_void", "title_hundred", "title_summer" },
                new[] { "勉強中", "夜学", "書生", "塔の者", "一番", "神", "月下", "塔の主", "虚の者", "百階", "夏の覇者" });
            y = DrawWardrobeSlot(content, y, "縁", "border",
                new[] { "border_gold", "border_ink", "border_peach", "border_jade", "border_sea", "border_violet", "border_silver", "border_double", "border_god" },
                new[] { "金", "墨", "桃", "翠", "蒼", "紫", "銀", "二重", "光輪" });
            y = DrawWardrobeSlot(content, y, "タイムラインの縁", "backdrop",
                new[] { "back_paper", "back_night", "back_jade", "back_sea", "back_peach", "back_violet", "back_silver", "back_royal", "back_god" },
                new[] { "紙", "夜", "翠", "蒼", "桃", "紫", "銀の額", "金の額", "神の額" });
            y = DrawWardrobeSlot(content, y, "判子", "seal",
                new[] { "seal_goal", "seal_jade", "seal_sea", "seal_gold", "seal_night", "seal_god" },
                new[] { "判子", "翠判", "蒼判", "金判", "夜判", "神判" });
            y = DrawWardrobeSlot(content, y, "角", "corner",
                new[] { "corner_gold", "corner_ink", "corner_jade", "corner_sea", "corner_peach", "corner_violet", "corner_silver", "corner_double", "corner_god" },
                new[] { "金", "墨", "翠", "蒼", "桃", "紫", "銀", "大粒", "神" });
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 16f);
        }

        float DrawWardrobeSlot(RectTransform content, float y, string heading, string slot, string[] ids, string[] names)
        {
            var head = Kit.Label(content, "H", new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                heading, 18, RpgTheme.GoldHi, TextAnchor.MiddleLeft);
            var hrt = head.rectTransform;
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, y);
            hrt.sizeDelta = new Vector2(0f, 26f);
            y -= 30f;
            string equipped = StudyStore.EquippedCosmetic(slot);
            var off = Kit.Button(content, "Off", new Vector2(0.04f, 1f), new Vector2(0.28f, 1f),
                string.IsNullOrEmpty(equipped) ? "なし" : "外す", 15,
                string.IsNullOrEmpty(equipped) ? RpgTheme.Gold : new Color(0.22f, 0.22f, 0.25f),
                string.IsNullOrEmpty(equipped) ? RpgTheme.Bg : Color.white, font);
            var ort = off.GetComponent<RectTransform>();
            ort.pivot = new Vector2(0.5f, 1f);
            ort.anchoredPosition = new Vector2(0f, y);
            ort.sizeDelta = new Vector2(0f, 40f);
            string slotCaptured = slot;
            off.onClick.AddListener(() =>
            {
                StudyStore.ClearCosmetic(slotCaptured);
                OpenWardrobe();
            });
            float x = 0.30f;
            bool any = false;
            for (int i = 0; i < ids.Length; i++)
            {
                if (!StudyStore.OwnsCosmetic(ids[i])) continue;
                any = true;
                bool on = equipped == ids[i];
                string id = ids[i];
                string label = names[i];
                var btn = Kit.Button(content, "Opt", new Vector2(x, 1f), new Vector2(x + 0.22f, 1f),
                    label, 14, on ? RpgTheme.Gold : RpgTheme.ButtonFill, on ? RpgTheme.Bg : Color.white, font);
                var brt = btn.GetComponent<RectTransform>();
                brt.pivot = new Vector2(0f, 1f);
                brt.anchoredPosition = new Vector2(0f, y);
                brt.sizeDelta = new Vector2(0f, 40f);
                btn.onClick.AddListener(() =>
                {
                    if (StudyStore.EquippedCosmetic(slotCaptured) == id)
                        StudyStore.ClearCosmetic(slotCaptured);
                    else
                        StudyStore.TryBuyCosmetic(id, slotCaptured, 0);
                    OpenWardrobe();
                });
                x += 0.23f;
                if (x > 0.76f)
                {
                    x = 0.04f;
                    y -= 48f;
                }
            }
            if (!any)
            {
                var none = Kit.Label(content, "None", new Vector2(0.32f, 1f), new Vector2(0.96f, 1f),
                    "未所持", 15, Muted, TextAnchor.MiddleLeft);
                var nrt = none.rectTransform;
                nrt.pivot = new Vector2(0.5f, 1f);
                nrt.anchoredPosition = new Vector2(0f, y);
                nrt.sizeDelta = new Vector2(0f, 40f);
            }
            return y - 52f;
        }

        void OpenProfileDialog()
        {
            if (profileDialog != null)
            {
                var panel = profileDialog.transform.Find("Panel");
                var prt = panel != null ? panel.GetComponent<RectTransform>() : null;
                if (prt != null)
                {
                    prt.anchorMin = new Vector2(0.05f, 0.08f);
                    prt.anchorMax = new Vector2(0.95f, 0.94f);
                    prt.offsetMin = Vector2.zero;
                    prt.offsetMax = Vector2.zero;
                }
            }
            StudyStore.Load();
            profileColorIndex = StudyStore.Profile != null ? Mathf.Clamp(StudyStore.Profile.colorIndex, 0, 5) : 0;
            if (profileClimbText != null)
                profileClimbText.text = StudyStore.MyClimbLine();
            if (profileNameInput != null)
            {
                profileNameInput.text = StudyStore.Profile != null ? (StudyStore.Profile.displayName ?? "") : "";
            }
            if (profileBioInput != null)
            {
                profileBioInput.text = StudyStore.Profile != null ? (StudyStore.Profile.bio ?? "") : "";
            }
            if (profileSchoolInput != null)
            {
                profileSchoolInput.text = StudyStore.Profile != null ? (StudyStore.Profile.school ?? "") : "";
            }
            if (profileGoalInput != null)
            {
                profileGoalInput.text = StudyStore.Profile != null ? (StudyStore.Profile.goal ?? "") : "";
            }
            RefreshProfileColorButtons();
            RefreshProfilePreview();
            if (profileDialog != null) profileDialog.SetActive(true);
        }

        void PickProfilePhoto()
        {
            string path = null;
#if UNITY_EDITOR
            path = UnityEditor.EditorUtility.OpenFilePanel("アイコン画像を選ぶ", "", "png,jpg,jpeg");
#elif UNITY_STANDALONE_WIN
            path = WindowsImageDialog.Open();
#elif UNITY_WEBGL
            WebGlIme.PickPhoto(OnWebPhoto);
            return;
#else
            ShowToast("ブラウザでは色を選んでください");
            return;
#endif
            if (string.IsNullOrEmpty(path)) return;
            if (StudyStore.SaveProfileIconFromPath(path))
            {
                RefreshProfilePreview();
                if (tab == Tab.Timeline) RebuildTimeline();
            }
        }

        void OnWebPhoto(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return;
            byte[] bytes;
            try { bytes = System.Convert.FromBase64String(base64); }
            catch { ShowToast("画像を読み込めませんでした"); return; }
            if (StudyStore.SaveProfileIconFromBytes(bytes))
            {
                RefreshProfilePreview();
                if (tab == Tab.Timeline) RebuildTimeline();
            }
            else ShowToast("画像を読み込めませんでした");
        }

        void ClearProfilePhoto()
        {
            StudyStore.ClearProfileIcon();
            RefreshProfilePreview();
            if (tab == Tab.Timeline) RebuildTimeline();
        }

        void RefreshProfileColorButtons()
        {
            for (int i = 0; i < profileColorButtons.Length; i++)
            {
                if (profileColorButtons[i] == null) continue;
                var img = profileColorButtons[i].GetComponent<Image>();
                if (img == null) continue;
                Color c = StudySubjectColors.Color((StudySubject)i);
                img.color = i == profileColorIndex ? c : c * 0.45f;
            }
        }

        void SaveProfileDialog()
        {
            string name = profileNameInput != null ? profileNameInput.text : "";
            string bio = profileBioInput != null ? profileBioInput.text : "";
            string school = profileSchoolInput != null ? profileSchoolInput.text : "";
            string goal = profileGoalInput != null ? profileGoalInput.text : "";
            StudyStore.SaveProfile(name, bio, school, goal, profileColorIndex);
            if (SupabaseSync.Ready) StartCoroutine(SupabaseSync.PushSelf());
            if (profileDialog != null) profileDialog.SetActive(false);
            if (tab == Tab.Timeline) RebuildTimeline();
        }

        void BuildManualDialog()
        {
            manualDialog = new GameObject("ManualDialog");
            manualDialog.transform.SetParent(root, false);
            var rt = manualDialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            manualDialog.AddComponent<Image>().color = RpgTheme.Dim;

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.94f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "Title", new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.98f),
                "あとから記録", 26, Color.white, TextAnchor.MiddleLeft);
            Kit.Label(prt, "CatLbl", new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.91f),
                "カテゴリ", 16, Muted, TextAnchor.MiddleLeft);

            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float x0 = 0.05f + col * 0.31f;
                float y1 = 0.86f - row * 0.075f;
                float y0 = y1 - 0.068f;
                var subject = (StudySubject)i;
                var b = Kit.Button(prt, "ManCat" + i,
                    new Vector2(x0, y0), new Vector2(x0 + 0.29f, y1),
                    StudySubjectNames.Display(subject), 15, Card, Color.white, font);
                int captured = i;
                b.onClick.AddListener(() =>
                {
                    manualSubject = (StudySubject)captured;
                    manualMaterial = null;
                    RefreshManualPickers();
                });
                manualCatButtons[i] = b;
            }

            Kit.Label(prt, "MatLbl", new Vector2(0.05f, 0.67f), new Vector2(0.95f, 0.71f),
                "教材", 16, Muted, TextAnchor.MiddleLeft);
            logMaterialTitle = Kit.Label(prt, "MatName", new Vector2(0.05f, 0.63f), new Vector2(0.95f, 0.67f),
                "教材を選択", 18, Accent, TextAnchor.MiddleLeft);

            var hostGo = new GameObject("MaterialHost");
            hostGo.transform.SetParent(prt, false);
            manualMaterialHost = hostGo.AddComponent<RectTransform>();
            manualMaterialHost.anchorMin = new Vector2(0.04f, 0.42f);
            manualMaterialHost.anchorMax = new Vector2(0.96f, 0.63f);
            manualMaterialHost.offsetMin = Vector2.zero;
            manualMaterialHost.offsetMax = Vector2.zero;
            hostGo.AddComponent<Image>().color = RpgTheme.Navy;

            Kit.Label(prt, "DurLbl", new Vector2(0.05f, 0.37f), new Vector2(0.95f, 0.41f),
                "勉強した時間", 16, Muted, TextAnchor.MiddleLeft);
            manualDurHourLabel = AddChoiceField(prt, new Vector2(0.05f, 0.30f), new Vector2(0.48f, 0.37f),
                () => OpenIntPicker("時間", 0, 48, manualDurHours, v => v + "時間", v =>
                {
                    manualDurHours = v;
                    RefreshManualTimeLabels();
                }));
            manualDurMinLabel = AddChoiceField(prt, new Vector2(0.52f, 0.30f), new Vector2(0.95f, 0.37f),
                () => OpenIntPicker("分", 0, 59, manualDurMins, v => v + "分", v =>
                {
                    manualDurMins = v;
                    RefreshManualTimeLabels();
                }));

            Kit.Label(prt, "StartLbl", new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.29f),
                "いつからやったか", 16, Muted, TextAnchor.MiddleLeft);
            manualYearLabel = AddChoiceField(prt, new Vector2(0.05f, 0.18f), new Vector2(0.48f, 0.25f), OpenManualYearPicker);
            manualMonthLabel = AddChoiceField(prt, new Vector2(0.52f, 0.18f), new Vector2(0.95f, 0.25f), OpenManualMonthPicker);
            manualDayLabel = AddChoiceField(prt, new Vector2(0.05f, 0.11f), new Vector2(0.36f, 0.18f), OpenManualDayPicker);
            manualHourLabel = AddChoiceField(prt, new Vector2(0.38f, 0.11f), new Vector2(0.69f, 0.18f),
                () => OpenIntPicker("時", 0, 23, manualHour, v => v.ToString("00") + "時", v =>
                {
                    manualHour = v;
                    RefreshManualTimeLabels();
                }));
            manualMinuteLabel = AddChoiceField(prt, new Vector2(0.70f, 0.11f), new Vector2(0.95f, 0.18f),
                () => OpenIntPicker("分", 0, 59, manualMinute, v => v.ToString("00") + "分", v =>
                {
                    manualMinute = v;
                    RefreshManualTimeLabels();
                }));

            var save = Kit.Button(prt, "SaveLog", new Vector2(0.52f, 0.02f), new Vector2(0.95f, 0.09f),
                "保存", 20, Accent, Color.black, font);
            save.onClick.AddListener(SaveManualLog);
            var cancel = Kit.Button(prt, "CancelLog", new Vector2(0.05f, 0.02f), new Vector2(0.48f, 0.09f),
                "戻る", 18, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            cancel.onClick.AddListener(() => manualDialog.SetActive(false));
            manualDialog.SetActive(false);
        }

        void RefreshManualPickers()
        {
            for (int i = 0; i < 6; i++)
            {
                var btn = manualCatButtons[i];
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                bool on = (int)manualSubject == i;
                if (img != null)
                {
                    img.sprite = null;
                    img.type = Image.Type.Simple;
                    img.color = on ? StudySubjectColors.Color((StudySubject)i) * 0.7f : RpgTheme.Navy;
                }
            }

            if (manualMaterialHost != null)
            {
                var doomed = new List<GameObject>();
                foreach (Transform child in manualMaterialHost) doomed.Add(child.gameObject);
                for (int i = 0; i < doomed.Count; i++) DestroyImmediate(doomed[i]);
            }

            var list = StudyStore.MaterialsFor(manualSubject);
            if (manualMaterial != null && manualMaterial.subject != (int)manualSubject)
            {
                manualMaterial = null;
            }
            if (manualMaterial == null && list.Count > 0) manualMaterial = list[0];

            if (logMaterialTitle != null)
            {
                logMaterialTitle.text = manualMaterial != null ? manualMaterial.name : "教材なし";
            }

            if (manualMaterialHost == null) return;
            if (list.Count == 0)
            {
                Kit.Label(manualMaterialHost, "EmptyMats", new Vector2(0.04f, 0.3f), new Vector2(0.96f, 0.7f),
                    "教材なし", 16, Muted, TextAnchor.MiddleCenter);
                return;
            }

            var scroll = MakeScroll(manualMaterialHost, "ManMats");
            var content = scroll.content;
            float y = -6f;
            for (int i = 0; i < list.Count; i++)
            {
                var mat = list[i];
                bool selected = manualMaterial != null && manualMaterial.id == mat.id;
                var b = Kit.Button(content, "ManMat" + i, Vector2.zero, Vector2.one,
                    mat.name, 16, selected ? Accent : RpgTheme.Navy, selected ? RpgTheme.Bg : Color.white, font);
                var brt = b.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.02f, 1f);
                brt.anchorMax = new Vector2(0.98f, 1f);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, y);
                brt.sizeDelta = new Vector2(0f, 48f);
                var txt = b.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    txt.verticalOverflow = VerticalWrapMode.Truncate;
                }
                var captured = mat;
                b.onClick.AddListener(() =>
                {
                    manualMaterial = captured;
                    RefreshManualPickers();
                });
                y -= 52f;
            }
            content.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 8f);
        }

        void OpenManualForSelected()
        {
            OpenManualDialog(null);
        }

        void OpenLogEditor(StudyLogEntry log)
        {
            if (log == null || log.open)
            {
                ShowToast("計測中の記録は直せません");
                return;
            }
            editingLogId = log.id;
            DateTime dt;
            if (!DateTime.TryParse(log.startedAt, out dt)) dt = StudyStore.Now;
            manualYear = dt.Year;
            manualMonth = dt.Month;
            manualDay = dt.Day;
            manualHour = dt.Hour;
            manualMinute = dt.Minute;
            manualDurHours = Mathf.Clamp(log.minutes / 60, 0, 48);
            manualDurMins = Mathf.Clamp(log.minutes % 60, 0, 59);
            manualSubject = (StudySubject)Mathf.Clamp(log.subject, 0, 5);
            manualMaterial = null;
            var mats = StudyStore.AllMaterials;
            for (int i = 0; i < mats.Count; i++)
            {
                if (mats[i] != null && mats[i].id == log.materialId)
                {
                    manualMaterial = mats[i];
                    manualSubject = (StudySubject)mats[i].subject;
                    break;
                }
            }
            SetManualTitle("記録を直す");
            if (manualDialog != null) manualDialog.SetActive(true);
            RefreshManualPickers();
            RefreshManualTimeLabels();
        }

        void SetManualTitle(string text)
        {
            if (manualDialog == null) return;
            var title = manualDialog.transform.Find("Panel/Title");
            if (title == null) return;
            var label = title.GetComponent<Text>();
            if (label != null) label.text = text;
        }

        void OpenManualDialog(StudyMaterial material)
        {
            editingLogId = null;
            SetManualTitle("あとから記録");
            var now = StudyStore.Now;
            manualYear = now.Year;
            manualMonth = now.Month;
            manualDay = now.Day;
            manualHour = now.Hour;
            manualMinute = now.Minute;
            manualDurHours = 1;
            manualDurMins = 0;
            RefreshManualTimeLabels();

            if (material != null)
            {
                manualMaterial = material;
                manualSubject = (StudySubject)material.subject;
            }
            else
            {
                manualSubject = StudySubject.Math;
                var inCat = StudyStore.MaterialsFor(manualSubject);
                if (inCat.Count == 0)
                {
                    var all = StudyStore.AllMaterials;
                    if (all.Count > 0)
                    {
                        manualMaterial = all[all.Count - 1];
                        manualSubject = (StudySubject)manualMaterial.subject;
                    }
                    else
                    {
                        manualMaterial = null;
                    }
                }
                else
                {
                    manualMaterial = inCat[0];
                }
            }

            if (manualDialog != null) manualDialog.SetActive(true);
            RefreshManualPickers();
        }

        void SaveManualLog()
        {
            if (manualMaterial == null) return;
            int hours = manualDurHours;
            int minutes = manualDurMins;
            if (!string.IsNullOrEmpty(editingLogId))
            {
                var before = StudyStore.FindLog(editingLogId);
                if (before == null || before.open) return;
                int oldMinutes = before.minutes;
                int oldSubject = before.subject;
                DateTime oldAt;
                bool oldToday = DateTime.TryParse(before.startedAt, out oldAt) && oldAt.Date == StudyStore.Now.Date;
                var started = ManualStartedAt();
                if (!StudyStore.ReviseLog(editingLogId, manualMaterial, hours, minutes, started)) return;
                if (oldToday)
                    PlayerProgress.AddStudyMinutes((StudySubject)oldSubject, -oldMinutes);
                if (started.Date == StudyStore.Now.Date)
                    PlayerProgress.AddStudyMinutes((StudySubject)manualMaterial.subject, hours * 60 + minutes);
                editingLogId = null;
                manualDialog.SetActive(false);
                ShowTab(Tab.Timeline);
                return;
            }
            var log = StudyStore.AddManualLog(manualMaterial, hours, minutes, ManualStartedAt());
            if (log == null) return;
            PlayerProgress.AddStudyMinutes((StudySubject)manualMaterial.subject, log.minutes);
            manualDialog.SetActive(false);
            ShowTab(Tab.Timeline);
        }

        DateTime ManualStartedAt()
        {
            ClampManualDay();
            var now = StudyStore.Now;
            var dt = new DateTime(manualYear, manualMonth, manualDay, manualHour, manualMinute, 0);
            if (dt > now.AddMinutes(2)) dt = now;
            return dt;
        }

        void ClampManualDay()
        {
            int maxDay = DateTime.DaysInMonth(Mathf.Clamp(manualYear, 2000, 2100), Mathf.Clamp(manualMonth, 1, 12));
            if (manualDay > maxDay) manualDay = maxDay;
            if (manualDay < 1) manualDay = 1;
        }

        void OpenManualYearPicker()
        {
            int y = StudyStore.Now.Year;
            OpenIntPicker("年", y - 8, y, manualYear, v => v + "年", v =>
            {
                manualYear = v;
                ClampManualDay();
                RefreshManualTimeLabels();
            });
        }

        void OpenManualMonthPicker()
        {
            OpenIntPicker("月", 1, 12, manualMonth, v => v + "月", v =>
            {
                manualMonth = v;
                ClampManualDay();
                RefreshManualTimeLabels();
            });
        }

        void OpenManualDayPicker()
        {
            ClampManualDay();
            int maxDay = DateTime.DaysInMonth(manualYear, manualMonth);
            OpenIntPicker("日", 1, maxDay, manualDay, v => v + "日", v =>
            {
                manualDay = v;
                RefreshManualTimeLabels();
            });
        }

        void RefreshManualTimeLabels()
        {
            ClampManualDay();
            if (manualYearLabel != null) manualYearLabel.text = manualYear + "年";
            if (manualMonthLabel != null) manualMonthLabel.text = manualMonth + "月";
            if (manualDayLabel != null) manualDayLabel.text = manualDay + "日";
            if (manualHourLabel != null) manualHourLabel.text = manualHour.ToString("00") + "時";
            if (manualMinuteLabel != null) manualMinuteLabel.text = manualMinute.ToString("00") + "分";
            if (manualDurHourLabel != null) manualDurHourLabel.text = manualDurHours + "時間";
            if (manualDurMinLabel != null) manualDurMinLabel.text = manualDurMins + "分";
        }

        Text AddChoiceField(RectTransform parent, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction onTap)
        {
            var b = Kit.Button(parent, "Choice" + min.x + min.y, min, max, "", 18,
                new Color(0.18f, 0.18f, 0.2f), Color.white, font);
            b.onClick.AddListener(onTap);
            return b.GetComponentInChildren<Text>();
        }

        void BuildChoicePicker()
        {
            choicePicker = new GameObject("ChoicePicker");
            choicePicker.transform.SetParent(root, false);
            var rt = choicePicker.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = choicePicker.AddComponent<Image>();
            dim.color = RpgTheme.Dim;
            var panel = Kit.Frame(rt, "Panel", new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.88f));
            var prt = panel.rectTransform;
            choiceTitle = Kit.Label(prt, "Title", new Vector2(0.06f, 0.9f), new Vector2(0.94f, 0.98f),
                "選ぶ", 24, Color.white, TextAnchor.MiddleLeft);
            var host = new GameObject("ListHost");
            host.transform.SetParent(prt, false);
            var hrt = host.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.04f, 0.12f);
            hrt.anchorMax = new Vector2(0.96f, 0.88f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;
            var scroll = MakeScroll(host.transform, "ChoiceScroll");
            choiceList = scroll.content;
            var cancel = Kit.Button(prt, "Cancel", new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.1f),
                "戻る", 20, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            cancel.onClick.AddListener(() => choicePicker.SetActive(false));
            var tap = choicePicker.AddComponent<Button>();
            tap.targetGraphic = dim;
            tap.onClick.AddListener(() => choicePicker.SetActive(false));
            choicePicker.SetActive(false);
        }

        void OpenIntPicker(string title, int min, int max, int current, Func<int, string> format, Action<int> onPicked)
        {
            if (choicePicker == null || choiceList == null) return;
            choicePicked = onPicked;
            if (choiceTitle != null) choiceTitle.text = title;
            var doomed = new List<GameObject>();
            foreach (Transform child in choiceList) doomed.Add(child.gameObject);
            for (int i = 0; i < doomed.Count; i++) DestroyImmediate(doomed[i]);

            if (max < min) max = min;
            float y = -6f;
            for (int v = min; v <= max; v++)
            {
                int captured = v;
                bool on = v == current;
                var b = Kit.Button(choiceList, "C" + v, Vector2.zero, Vector2.one,
                    format(v), 20, on ? Accent : RpgTheme.Navy,
                    on ? Color.black : Color.white, font);
                var brt = b.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.04f, 1f);
                brt.anchorMax = new Vector2(0.96f, 1f);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, y);
                brt.sizeDelta = new Vector2(0f, 44f);
                b.onClick.AddListener(() =>
                {
                    if (choicePicked != null) choicePicked(captured);
                    if (choicePicker != null) choicePicker.SetActive(false);
                });
                y -= 48f;
            }
            choiceList.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 10f);
            choicePicker.SetActive(true);
            choicePicker.transform.SetAsLastSibling();
        }

        void BuildToast()
        {
            toastGo = new GameObject("Toast");
            toastGo.transform.SetParent(root, false);
            var rt = toastGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.42f);
            rt.anchorMax = new Vector2(0.9f, 0.58f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            RpgTheme.PaintGoldFrame(toastGo.AddComponent<Image>());
            toastText = Kit.Label(rt, "Msg", new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.9f),
                "", 22, Color.white, TextAnchor.MiddleCenter);
            toastGo.SetActive(false);
        }

        void ShowToast(string message)
        {
            if (toastGo == null || toastText == null) return;
            toastText.text = message;
            toastGo.SetActive(true);
            toastGo.transform.SetAsLastSibling();
            toastUntil = Time.unscaledTime + 2.2f;
        }

        void OpenAddDialog(StudySubject subject)
        {
            addSubject = subject;
            if (addInput != null) addInput.text = string.Empty;
            RefreshAddSubjectButtons();
            if (addDialog != null)
            {
                addDialog.SetActive(true);
                addDialog.transform.SetAsLastSibling();
            }
        }

        void SaveMaterial()
        {
            string name = addInput != null ? addInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return;
            StudyStore.AddMaterial(name, addSubject);
            addDialog.SetActive(false);
            ShowTab(Tab.Record);
        }

        void BuildConfirmDialog()
        {
            confirmDialog = new GameObject("ConfirmDialog");
            confirmDialog.transform.SetParent(root, false);
            var rt = confirmDialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            confirmDialog.AddComponent<Image>().color = RpgTheme.Dim;
            var panel = Kit.Frame(rt, "Panel", new Vector2(0.12f, 0.38f), new Vector2(0.88f, 0.62f));
            confirmText = Kit.Label(panel.rectTransform, "Msg", new Vector2(0.06f, 0.45f), new Vector2(0.94f, 0.9f),
                "削除しますか？", 22, Color.white, TextAnchor.MiddleCenter);
            var ok = Kit.Button(panel.rectTransform, "Ok", new Vector2(0.52f, 0.08f), new Vector2(0.94f, 0.36f),
                "削除", 22, RpgTheme.ButtonFill, RpgTheme.Zhu, font);
            confirmDeleteBtn = ok;
            ok.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(pendingDeleteLogId))
                {
                    StudyStore.DeleteLog(pendingDeleteLogId);
                    pendingDeleteLogId = null;
                    confirmDialog.SetActive(false);
                    if (tab == Tab.Timeline) RebuildTimeline();
                    RefreshWallet();
                    return;
                }
                if (!string.IsNullOrEmpty(pendingDeleteId))
                {
                    if (!StudyStore.DeleteMaterial(pendingDeleteId))
                    {
                        confirmText.text = "計測中の教材は削除できません。先に保存して終了してください。";
                        return;
                    }
                }
                pendingDeleteId = null;
                confirmDialog.SetActive(false);
                ShowTab(Tab.Record);
            });
            var no = Kit.Button(panel.rectTransform, "No", new Vector2(0.06f, 0.08f), new Vector2(0.48f, 0.36f),
                "キャンセル", 22, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            no.onClick.AddListener(() => confirmDialog.SetActive(false));
            confirmDialog.SetActive(false);
        }

        void BuildMaterialDetail()
        {
            materialDetail = new GameObject("MaterialDetail");
            materialDetail.transform.SetParent(root, false);
            var rt = materialDetail.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = materialDetail.AddComponent<Image>();
            dim.color = RpgTheme.Dim;

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f));
            var prt = panel.rectTransform;
            materialDetailBook = Kit.Image(prt, "Book", new Vector2(0.05f, 0.38f), new Vector2(0.30f, 0.94f), Color.white);
            materialDetailName = Kit.Label(prt, "Name", new Vector2(0.32f, 0.68f), new Vector2(0.94f, 0.92f),
                "", 28, Color.white, TextAnchor.MiddleLeft);
            materialDetailStats = Kit.Label(prt, "Stats", new Vector2(0.32f, 0.38f), new Vector2(0.94f, 0.68f),
                "", 22, Color.white, TextAnchor.UpperLeft);

            var start = Kit.Button(prt, "Start", new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.36f),
                "計測する", 22, Accent, Color.black, font);
            start.onClick.AddListener(StartMeasureFromDetail);
            var manual = Kit.Button(prt, "Manual", new Vector2(0.06f, 0.04f), new Vector2(0.48f, 0.18f),
                "手入力", 18, new Color(0.22f, 0.22f, 0.26f), Color.white, font);
            manual.onClick.AddListener(() =>
            {
                var mat = detailMaterial;
                if (materialDetail != null) materialDetail.SetActive(false);
                if (mat != null) OpenManualDialog(mat);
            });
            var close = Kit.Button(prt, "Close", new Vector2(0.52f, 0.04f), new Vector2(0.94f, 0.18f),
                "閉じる", 18, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            close.onClick.AddListener(() => materialDetail.SetActive(false));
            var tap = materialDetail.AddComponent<Button>();
            tap.targetGraphic = dim;
            tap.onClick.AddListener(() => materialDetail.SetActive(false));
            materialDetail.SetActive(false);
        }

        void OpenMaterialDetail(StudyMaterial material)
        {
            if (material == null) return;
            detailMaterial = material;
            int mins = StudyStore.MinutesForMaterial(material.id);
            int sessions = StudyStore.SessionCountForMaterial(material.id);
            if (materialDetailName != null) materialDetailName.text = material.name;
            if (materialDetailBook != null)
                RpgTheme.PaintBook(materialDetailBook, StudySubjectColors.Color((StudySubject)material.subject), RpgTheme.BookVariant(material.id));
            if (materialDetailStats != null)
            {
                materialDetailStats.text = StudySubjectNames.Display((StudySubject)material.subject)
                    + "\n累計  " + StudyStore.FormatMinutes(mins)
                    + "\n回数  " + sessions + "回";
            }
            if (materialDetail != null)
            {
                materialDetail.SetActive(true);
                materialDetail.transform.SetAsLastSibling();
            }
        }

        void StartMeasureFromDetail()
        {
            var material = detailMaterial;
            if (material == null || StudyTimer.Instance == null) return;
            if (materialDetail != null) materialDetail.SetActive(false);
            StudyTimer.Instance.OpenMaterial(material);
            timerOverlay.SetActive(true);
            RefreshTimerWidgets();
            if (tab == Tab.Record) RebuildRecord();
        }

        void BuildReportDetail()
        {
            reportDetail = new GameObject("ReportDetail");
            reportDetail.transform.SetParent(root, false);
            var rt = reportDetail.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            reportDetail.AddComponent<Image>().color = RpgTheme.Bg;

            Kit.Label(rt, "DetTitle", new Vector2(0.04f, 0.93f), new Vector2(0.7f, 0.99f),
                "学習レポート", 26, Color.white, TextAnchor.MiddleLeft);
            var close = Kit.Button(rt, "DetClose", new Vector2(0.72f, 0.935f), new Vector2(0.96f, 0.985f),
                "閉じる", 20, Card, Color.white, font);
            close.onClick.AddListener(() => reportDetail.SetActive(false));

            var host = new GameObject("DetHost");
            host.transform.SetParent(rt, false);
            var hrt = host.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(1f, 0.93f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;
            var scroll = MakeScroll(host.transform, "DetScroll");
            reportDetailContent = scroll.content;
            reportDetail.SetActive(false);
        }

        void BuildGoalDialog()
        {
            goalDialog = new GameObject("GoalDialog");
            goalDialog.transform.SetParent(root, false);
            var rt = goalDialog.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            goalDialog.AddComponent<Image>().color = RpgTheme.Dim;

            var panel = Kit.Frame(rt, "Panel", new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f));
            var prt = panel.rectTransform;
            Kit.Label(prt, "Title", new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.96f),
                "今週の目標勉強時間", 24, Color.white, TextAnchor.MiddleLeft);
            goalHourLabel = AddChoiceField(prt, new Vector2(0.06f, 0.48f), new Vector2(0.48f, 0.72f),
                () => OpenIntPicker("時間", 0, 167, goalHour, v => v + "時間", v =>
                {
                    goalHour = v;
                    if (goalHour == 167 && goalMinute > 59) goalMinute = 59;
                    RefreshGoalLabels();
                }));
            goalMinuteLabel = AddChoiceField(prt, new Vector2(0.52f, 0.48f), new Vector2(0.94f, 0.72f),
                () => OpenIntPicker("分", 0, 59, goalMinute, v => v + "分", v =>
                {
                    goalMinute = v;
                    RefreshGoalLabels();
                }));

            var save = Kit.Button(prt, "Save", new Vector2(0.52f, 0.08f), new Vector2(0.94f, 0.28f),
                "保存", 22, Accent, Color.black, font);
            save.onClick.AddListener(SaveGoalDialog);
            var cancel = Kit.Button(prt, "Cancel", new Vector2(0.06f, 0.08f), new Vector2(0.48f, 0.28f),
                "キャンセル", 22, new Color(0.25f, 0.25f, 0.28f), Color.white, font);
            cancel.onClick.AddListener(() => goalDialog.SetActive(false));
            goalDialog.SetActive(false);
        }

        void BuildBpWarn()
        {
            bpWarn = new GameObject("BpWarn");
            bpWarn.transform.SetParent(root, false);
            var rt = bpWarn.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = bpWarn.AddComponent<Image>();
            dim.color = RpgTheme.Dim;
            var panel = Kit.Frame(rt, "Panel", new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.66f));
            bpWarnText = Kit.Label(panel.rectTransform, "Msg", new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.92f),
                "BPが足りません", 28, Color.white, TextAnchor.MiddleCenter);
            var ok = Kit.Button(panel.rectTransform, "Ok", new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.26f),
                "OK", 24, Accent, Color.black, font);
            ok.onClick.AddListener(() => bpWarn.SetActive(false));
            var tap = bpWarn.AddComponent<Button>();
            tap.targetGraphic = dim;
            tap.onClick.AddListener(() => bpWarn.SetActive(false));
            bpWarn.SetActive(false);
        }

        void ShowBpShortage()
        {
            if (bpWarnText != null)
            {
                bpWarnText.text = "BPが足りません\nバトルには " + StudyStore.BattleCostBp + " BP 必要\nいま "
                    + StudyStore.TodayBp + " BP";
            }
            if (bpWarn != null) bpWarn.SetActive(true);
        }

        void OpenGoalDialog()
        {
            if (StudyStore.HasLockedGoal()) return;
            int m = StudyStore.WeeklyGoalMinutes();
            if (m < 30) m = 180;
            goalHour = m / 60;
            goalMinute = m % 60;
            RefreshGoalLabels();
            if (goalDialog != null) goalDialog.SetActive(true);
        }

        void RefreshGoalLabels()
        {
            if (goalHourLabel != null) goalHourLabel.text = goalHour + "時間";
            if (goalMinuteLabel != null) goalMinuteLabel.text = goalMinute + "分";
        }

        void SaveGoalDialog()
        {
            int minutes = goalHour * 60 + goalMinute;
            if (minutes < 30) minutes = 30;
            if (minutes > StudyStore.MaxWeeklyGoalMinutes) minutes = StudyStore.MaxWeeklyGoalMinutes;
            if (!StudyStore.SetWeeklyGoalMinutes(minutes))
            {
                if (goalDialog != null) goalDialog.SetActive(false);
                return;
            }
            if (goalDialog != null) goalDialog.SetActive(false);
            RebuildReport();
            RebuildTimeline();
        }

        void OpenDeleteConfirm(string id)
        {
            pendingDeleteId = id;
            var mat = StudyStore.FindMaterial(id);
            if (StudyStore.IsMaterialMeasuring(id))
            {
                confirmText.text = (mat != null ? mat.name : "この教材") + " は計測中のため削除できません。";
                if (confirmDeleteBtn != null) confirmDeleteBtn.interactable = false;
            }
            else
            {
                confirmText.text = (mat != null ? mat.name : "この教材") + " を削除しますか？";
                if (confirmDeleteBtn != null) confirmDeleteBtn.interactable = true;
            }
            confirmDialog.SetActive(true);
        }

        void RefreshTimerWidgets()
        {
            var timer = StudyTimer.Instance;
            if (timer == null) return;
            if (overlayTitle != null && timer.CurrentMaterial != null)
                overlayTitle.text = timer.CurrentMaterial.name;
            if (overlayTime != null)
                overlayTime.text = StudyTimer.FormatTime(timer.SessionSeconds);
            if (overlayPauseLabel != null)
            {
                overlayPauseLabel.text = timer.IsPaused ? "再開" : "一時停止";
            }
            if (overlayHint != null) overlayHint.text = string.Empty;
            string status = timer.IsPaused ? "一時停止" : "計測中";
            if (liveBannerLabel != null && timer.CurrentMaterial != null)
            {
                liveBannerLabel.text = timer.CurrentMaterial.name + "  " + status + "  " + StudyTimer.FormatTime(timer.SessionSeconds);
            }
            if (recordBanner != null && tab == Tab.Record)
            {
                if (timer.HasActiveSession && timer.CurrentMaterial != null)
                {
                    recordBanner.color = RpgTheme.Zhu;
                    recordBanner.text = timer.CurrentMaterial.name + "  " + status + "  " + StudyTimer.FormatTime(timer.SessionSeconds);
                }
            }
        }

        static class Kit
        {
            public static Image Image(Transform parent, string name, Vector2 min, Vector2 max, Color color)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = min;
                rt.anchorMax = max;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.color = color;
                return img;
            }

            public static Image Window(Transform parent, string name, Vector2 min, Vector2 max)
            {
                var img = Image(parent, name, min, max, Color.white);
                RpgTheme.PaintGoldFrame(img);
                img.raycastTarget = true;
                return img;
            }

            public static Image Card(Transform parent, string name, Vector2 min, Vector2 max)
            {
                var img = Image(parent, name, min, max, Color.white);
                RpgTheme.PaintNavyCard(img);
                img.raycastTarget = true;
                return img;
            }

            public static Image Frame(Transform parent, string name, Vector2 min, Vector2 max)
            {
                var img = Image(parent, name, min, max, Color.white);
                RpgTheme.PaintGoldFrame(img);
                img.raycastTarget = true;
                return img;
            }

            static Sprite circleSprite;

            public static void MakeCircle(UnityEngine.UI.Image img)
            {
                if (img == null) return;
                img.sprite = CircleSprite();
                img.type = UnityEngine.UI.Image.Type.Simple;
                img.preserveAspect = true;
            }

            static Sprite CircleSprite()
            {
                if (circleSprite != null) return circleSprite;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                float cx = (size - 1) * 0.5f;
                float radius = cx - 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - cx;
                        float dy = y - cx;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(radius + 0.5f - d);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply(false, true);
                circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                return circleSprite;
            }

            public static int TypeSize(int size)
            {
                if (size >= 48) return size;
                if (size <= 14) return size + 6;
                if (size <= 18) return size + 5;
                if (size <= 24) return size + 4;
                return size + 2;
            }

            public static void Fit(Text txt, int size)
            {
                if (txt == null) return;
                size = TypeSize(size);
                txt.fontSize = size;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                txt.resizeTextForBestFit = true;
                txt.resizeTextMinSize = Mathf.Max(12, size - 6);
                txt.resizeTextMaxSize = size;
            }

            public static Text Label(Transform parent, string name, Vector2 min, Vector2 max, string text, int size, Color color, TextAnchor align)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = min;
                rt.anchorMax = max;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var txt = go.AddComponent<Text>();
                txt.font = RpgTheme.UiFont();
                txt.color = color;
                txt.alignment = align;
                txt.text = text;
                Fit(txt, size);
                txt.raycastTarget = false;
                return txt;
            }

            public static Button Button(Transform parent, string name, Vector2 min, Vector2 max, string label, int size, Color bg, Color fg, Font font)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = min;
                rt.anchorMax = max;
                rt.offsetMin = new Vector2(4, 4);
                rt.offsetMax = new Vector2(-4, -4);
                var img = go.AddComponent<Image>();
                Color fill = bg.a < 0.08f ? RpgTheme.ButtonFill : bg;
                if (RpgTheme.IsDanger(fill)) fill = RpgTheme.ButtonFill;
                RpgTheme.PaintButton(img, fill);
                if (RpgTheme.IsDanger(bg)) RpgTheme.AddZhuAccent(go.transform);
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                RpgTheme.StyleButtonColors(btn);
                var tgo = new GameObject("Text");
                tgo.transform.SetParent(go.transform, false);
                var trt = tgo.AddComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(6f, 2f);
                trt.offsetMax = new Vector2(-6f, -2f);
                var txt = tgo.AddComponent<Text>();
                txt.font = RpgTheme.UiFont();
                txt.text = label;
                Fit(txt, size);
                float lum = fill.r * 0.3f + fill.g * 0.59f + fill.b * 0.11f;
                Color textCol = fg;
                if (RpgTheme.IsDanger(bg)) textCol = Color.white;
                else if (lum > 0.55f) textCol = RpgTheme.Bg;
                else if (fg.a < 0.08f) textCol = Color.white;
                txt.color = textCol;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.raycastTarget = false;
                return btn;
            }

            public static InputField Input(Transform parent, string name, Vector2 min, Vector2 max, Font font, string placeholder = "教材名を入力")
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = min;
                rt.anchorMax = max;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                RpgTheme.PaintInput(img);
                var input = go.AddComponent<InputField>();
                var text = Label(go.transform, "Text", Vector2.zero, Vector2.one, string.Empty, 22, RpgTheme.InputText, TextAnchor.MiddleCenter);
                text.raycastTarget = true;
                var ph = Label(go.transform, "Placeholder", Vector2.zero, Vector2.one, placeholder, 22, RpgTheme.InputHint, TextAnchor.MiddleCenter);
                input.textComponent = text;
                input.placeholder = ph;
                input.targetGraphic = img;
                input.customCaretColor = true;
                input.caretColor = RpgTheme.InputText;
                input.selectionColor = new Color(0.88f, 0.68f, 0.12f, 0.45f);
                WebGlIme.Bind(input);
                return input;
            }
        }
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    static class WindowsImageDialog
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        class OpenFileName
        {
            public int lStructSize;
            public System.IntPtr hwndOwner;
            public System.IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public System.IntPtr lCustData;
            public System.IntPtr lpfnHook;
            public string lpTemplateName;
            public System.IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [System.Runtime.InteropServices.DllImport("comdlg32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        static extern bool GetOpenFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName ofn);

        public static string Open()
        {
            var ofn = new OpenFileName();
            ofn.lStructSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);
            ofn.lpstrFilter = "画像\0*.png;*.jpg;*.jpeg\0PNG\0*.png\0JPEG\0*.jpg;*.jpeg\0\0";
            ofn.lpstrFile = new string('\0', 260);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrFileTitle = new string('\0', 260);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
            ofn.lpstrTitle = "アイコン画像を選ぶ";
            ofn.Flags = 0x00001000 | 0x00000800 | 0x00000008 | 0x00080000;
            ofn.lpstrDefExt = "png";
            if (!GetOpenFileName(ofn)) return null;
            return ofn.lpstrFile;
        }
    }
#endif
}
