using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ShiftingMetropolis.Battle;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    public class UltimateLoadoutScreen : MonoBehaviour
    {
        static UltimateLoadoutScreen instance;

        Font font;
        GameObject panel;
        Text equippedText;
        RectTransform catalogList;
        readonly List<GameObject> catalogRows = new List<GameObject>();
        SkillCommandType? pickingSlot;
        float catalogY;

        public static void Ensure()
        {
            if (instance != null) return;
            var home = GameObject.Find("HomeCanvas");
            if (home != null)
            {
                instance = home.GetComponent<UltimateLoadoutScreen>();
                if (instance == null) instance = home.AddComponent<UltimateLoadoutScreen>();
                return;
            }
            var go = new GameObject("UltimateLoadoutScreen");
            instance = go.AddComponent<UltimateLoadoutScreen>();
        }

        public static void Open(SkillCommandType? slot = null)
        {
            if (StudyStore.GrowthLocked()) return;
            Ensure();
            if (instance != null) instance.Show(slot);
        }

        public static void Close()
        {
            if (instance != null) instance.Hide();
        }

        void Awake()
        {
            if (instance == null) instance = this;
            font = RpgTheme.UiFont();
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            BuildUi();
        }

        void Show(SkillCommandType? slot = null)
        {
            if (panel == null) BuildUi();
            if (panel == null) return;
            pickingSlot = slot;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            RefreshAll();
        }

        void Hide()
        {
            pickingSlot = null;
            if (panel != null) panel.SetActive(false);
            var ui = Object.FindAnyObjectByType<StudyAppUI>();
            if (ui != null) ui.RefreshGrowthView();
        }

        void BuildUi()
        {
            if (panel != null) return;
            Transform parent = transform;
            var home = GameObject.Find("HomeCanvas");
            if (home != null) parent = home.transform;

            panel = new GameObject("UltimateLoadoutPanel");
            panel.transform.SetParent(parent, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = RpgTheme.DimHeavy;
            bg.raycastTarget = true;
            panel.SetActive(false);

            CreateText(panel.transform, "技構成", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(640f, 56f), 32, TextAnchor.MiddleCenter, RpgTheme.GoldHi);

            equippedText = CreateText(panel.transform, "", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(680f, 40f), 18, TextAnchor.MiddleCenter, RpgTheme.Gold);

            CreateText(panel.transform, "必殺はブレインストーム　その日30分で1回", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(640f, 32f), 16, TextAnchor.MiddleCenter, RpgTheme.Muted);

            var scrollGo = new GameObject("CatalogScroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.04f, 0.12f);
            scrollRt.anchorMax = new Vector2(0.96f, 0.78f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48f;
            scroll.inertia = true;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewportRt;

            var listGo = new GameObject("CatalogList");
            listGo.transform.SetParent(viewportGo.transform, false);
            catalogList = listGo.AddComponent<RectTransform>();
            catalogList.anchorMin = new Vector2(0f, 1f);
            catalogList.anchorMax = new Vector2(1f, 1f);
            catalogList.pivot = new Vector2(0.5f, 1f);
            catalogList.anchoredPosition = Vector2.zero;
            catalogList.sizeDelta = new Vector2(0f, 20f);
            scroll.content = catalogList;

            CreateButton(panel.transform, "閉じる", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(240f, 64f), RpgTheme.Danger, Hide);
        }

        void RefreshAll()
        {
            var s1 = UltimateCatalog.Get(StudyStore.SlotUltimateId(SkillCommandType.Skill1));
            var s2 = UltimateCatalog.Get(StudyStore.SlotUltimateId(SkillCommandType.Skill2));
            var s3 = UltimateCatalog.Get(StudyStore.SlotUltimateId(SkillCommandType.Skill3));
            var s4 = UltimateCatalog.Get(StudyStore.SlotUltimateId(SkillCommandType.Charge));
            var u = UltimateCatalog.Get(StudyStore.SlotUltimateId(SkillCommandType.BrainSmash));
            if (equippedText != null)
            {
                equippedText.text = SlotTitle(SkillCommandType.Skill1) + " " + NameOf(s1)
                    + "  /  " + SlotTitle(SkillCommandType.Skill2) + " " + NameOf(s2)
                    + "  /  " + SlotTitle(SkillCommandType.Skill3) + " " + NameOf(s3)
                    + "  /  " + SlotTitle(SkillCommandType.Charge) + " " + NameOf(s4)
                    + "  /  必殺 " + NameOf(u);
            }

            for (int i = 0; i < catalogRows.Count; i++)
                if (catalogRows[i] != null) Destroy(catalogRows[i]);
            catalogRows.Clear();
            catalogY = -6f;

            if (pickingSlot == null)
            {
                AddHeader("枠を選ぶ");
                AddSlotRow(SkillCommandType.Skill1);
                AddSlotRow(SkillCommandType.Skill2);
                AddSlotRow(SkillCommandType.Skill3);
                AddSlotRow(SkillCommandType.Charge);
                AddSlotRow(SkillCommandType.BrainSmash);
                FinishList();
                return;
            }

            var slot = pickingSlot.Value;
            AddHeader(SlotTitle(slot) + " に入れる");
            AddListButton("戻る", false, Hide);
            foreach (var gear in UltimateCatalog.GearOrder)
            {
                bool smash = slot == SkillCommandType.BrainSmash;
                if (smash && gear != UltimateGear.Brainstorm) continue;
                if (!smash && gear == UltimateGear.Brainstorm) continue;
                AddHeader(UltimateCatalog.GearLabel(gear));
                foreach (var def in UltimateCatalog.DefsIn(gear))
                {
                    if (smash && def.gear != UltimateGear.Brainstorm) continue;
                    if (!smash && def.gear == UltimateGear.Brainstorm) continue;
                    if (!StudyStore.IsUltimateOwned(def.id)) continue;
                    bool isOn = def.id == StudyStore.SlotUltimateId(slot);
                    string tag = BattleElementUtil.IsElementSkill(def.element)
                        ? "【" + BattleElementUtil.Label(def.element) + "】"
                        : "";
                    int ver = Mathf.Clamp(StudyStore.UltimateLevel(def.id), 1, StudyStore.SkillVersionMax);
                    string title = StudyStore.SkillTitle(def.displayName, ver);
                    string verText = ver <= 5 ? "  V" + ver : "";
                    string body = tag + title + verText + "  " + (isOn ? "装備中" : "入れる")
                        + "\n" + MotionLabel(def) + "  威力" + def.MultiplierAtLevel(ver).ToString("0.00");
                    string id = def.id;
                    var captured = slot;
                    if (isOn) AddListButton(body, true, null);
                    else AddListButton(body, false, () =>
                    {
                        if (StudyStore.GrowthLocked()) return;
                        StudyStore.EquipSlotUltimate(captured, id);
                        BattleAudio.UiClick();
                        Hide();
                    });
                }
            }
            FinishList();
        }

        void FinishList()
        {
            if (catalogList != null)
                catalogList.sizeDelta = new Vector2(0f, Mathf.Abs(catalogY) + 16f);
        }

        static string NameOf(UltimateSkillDefinition def)
        {
            return def != null ? def.displayName : "未装備";
        }

        static string SlotTitle(SkillCommandType type)
        {
            switch (type)
            {
                case SkillCommandType.Skill1: return "枠1";
                case SkillCommandType.Skill2: return "枠2";
                case SkillCommandType.Skill3: return "枠3";
                case SkillCommandType.Charge: return "枠4";
                case SkillCommandType.BrainSmash: return "必殺";
                default: return "枠";
            }
        }

        void AddSlotRow(SkillCommandType type)
        {
            var def = UltimateCatalog.Get(StudyStore.SlotUltimateId(type));
            string gear = def != null ? UltimateCatalog.GearLabel(def.gear) : "";
            string tag = def != null && BattleElementUtil.IsElementSkill(def.element)
                ? "【" + BattleElementUtil.Label(def.element) + "】"
                : "";
            var captured = type;
            int ver = def != null ? StudyStore.UltimateLevel(def.id) : 0;
            string titled = def != null ? StudyStore.SkillTitle(def.displayName, ver) : "未装備";
            AddListButton(SlotTitle(type) + "  " + tag + titled + "\n" + gear, def != null, () =>
            {
                pickingSlot = captured;
                BattleAudio.UiClick();
                RefreshAll();
            });
        }

        void AddHeader(string title)
        {
            var lab = CreateText(catalogList, title, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, catalogY), new Vector2(0f, 36f), 18, TextAnchor.MiddleLeft, RpgTheme.GoldHi);
            var rt = lab.rectTransform;
            rt.pivot = new Vector2(0.5f, 1f);
            catalogRows.Add(lab.gameObject);
            catalogY -= 40f;
        }

        void AddListButton(string body, bool on, System.Action click)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(catalogList, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 1f);
            rt.anchorMax = new Vector2(0.98f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, catalogY);
            rt.sizeDelta = new Vector2(0f, 72f);
            var img = go.AddComponent<Image>();
            RpgTheme.PaintGoldFrame(img);
            img.raycastTarget = true;
            if (on) RpgTheme.AddZhuAccent(go.transform);
            if (click != null)
            {
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                btn.onClick.AddListener(() => click.Invoke());
            }
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16f, 4f);
            trt.offsetMax = new Vector2(-16f, -4f);
            var text = textGo.AddComponent<Text>();
            text.font = RpgTheme.UiFont();
            text.text = body;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 18;
            text.color = on ? RpgTheme.GoldHi : Color.white;
            text.raycastTarget = false;
            catalogRows.Add(go);
            catalogY -= 80f;
        }

        static string MotionLabel(UltimateSkillDefinition def)
        {
            if (def != null && def.selfBuff) return "自己強化";
            if (def != null && !string.IsNullOrEmpty(def.waveKind)) return "波動・念力";
            switch (def != null ? def.motionType : UltimateMotionType.MeleeRush)
            {
                case UltimateMotionType.TelekinesisSlam: return "念力・武器叩きつけ";
                case UltimateMotionType.MagicBolt: return "魔弾";
                case UltimateMotionType.FieldNuke: return "全体大技";
                case UltimateMotionType.SwordVolley: return "弓・飛剣";
                default: return "近接乱舞";
            }
        }

        GameObject CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color, System.Action onClick)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            RpgTheme.PaintButton(img, color);
            if (RpgTheme.IsDanger(color))
            {
                RpgTheme.PaintButton(img, RpgTheme.ButtonFill);
                RpgTheme.AddZhuAccent(go.transform);
            }
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            RpgTheme.StyleButtonColors(btn);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = RpgTheme.UiFont();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 22;
            text.color = Color.white;
            text.raycastTarget = false;
            return go;
        }

        Text CreateText(Transform parent, string content, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = RpgTheme.UiFont();
            text.text = content;
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
