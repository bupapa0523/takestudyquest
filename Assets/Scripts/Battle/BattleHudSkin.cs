using UnityEngine;
using UnityEngine.UI;
using ShiftingMetropolis.App;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// ドラクエ風の金枠ウィンドウ。会話とコマンドを同じ枠で切り替える。
    /// </summary>
    public static class BattleHudSkin
    {
        static Sprite window;
        static Sprite hpWindow;
        static Sprite barWhite;
        static Sprite hpTrack;
        static Sprite dimSprite;
        static Font rpgFont;
        static bool commandMode;

        static readonly Color TextCol = Color.white;
        static readonly Color Gold = new Color(0.88f, 0.68f, 0.12f, 1f);
        static readonly Color GoldHi = new Color(1f, 0.84f, 0.28f, 1f);
        static readonly Color GoldLo = new Color(0.42f, 0.26f, 0.04f, 1f);
        static readonly Color Navy = new Color(0.06f, 0.10f, 0.32f, 1f);
        static readonly Color GearWait = new Color(0.72f, 0.74f, 0.80f, 1f);
        static readonly Color HpGreen = new Color(0.18f, 0.92f, 0.36f, 1f);
        static readonly Color HpYellow = new Color(0.98f, 0.84f, 0.16f, 1f);
        static readonly Color HpRed = new Color(0.95f, 0.16f, 0.18f, 1f);
        static readonly Color EnemyHp = new Color(0.98f, 0.20f, 0.22f, 1f);

        static Text talkName;
        static Text talkLine;
        static Text resultTitle;
        static Text resultSub;
        static Text playerHpNum;
        static Text enemyHpNum;
        static Slider playerHpSlider;
        static Slider enemyHpSlider;
        static GameObject resultRoot;
        static readonly GameObject[] CmdButtons = new GameObject[5];
        static readonly string[] CmdNames =
        {
            "Button_Skill1", "Button_Skill2", "Button_Skill3",
            "Button_Charge", "Button_BrainSmash"
        };
        static GameObject gearRoot;
        static Button gearButton;
        static Text gearValue;
        static readonly Image[] gearPips = new Image[4];
        static Text gearHint;
        static System.Action gearUp;
        static GameObject raidRoot;
        static Image raidFill;
        static Image raidCore;
        static Text raidName;
        static Text raidNums;
        static Text raidTurns;
        static int lastRaidHp = -1;
        static int lastRaidMax = -1;
        static Transform battleCanvasRoot;

        public static void Apply()
        {
            var canvas = GameObject.Find("BattleCanvas");
            if (canvas == null) return;
            battleCanvasRoot = canvas.transform;
            EnsureSprites();
            var c = canvas.GetComponent<Canvas>();
            if (c != null) c.pixelPerfect = false;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var root = canvas.transform;

            HideFullBackdrop(root);
            Hide(FindGo(root, "HudTalkPanel"));
            Hide(FindGo(root, "HudCmdPanel"));
            Hide(FindGo(root, "HudPanelCommands"));
            Hide(FindGo(root, "HudPanelEnemy"));
            Hide(FindGo(root, "HudPanelPlayer"));
            var log = FindGo(root, "LogText");
            if (log != null) log.SetActive(false);
            var rawResult = FindGo(root, "ResultText");
            if (rawResult != null) rawResult.SetActive(false);

            var main = EnsurePanel(root, "HudMainWindow", window, 0.03f, 0.012f, 0.97f, 0.286f, true);
            if (main != null && main.GetComponent<RectMask2D>() == null)
                main.gameObject.AddComponent<RectMask2D>();
            var hpE = EnsurePanel(root, "HudEnemyPlate", hpWindow, 0.54f, 0.878f, 0.96f, 0.978f, true);
            var hpP = EnsurePanel(root, "HudPlayerPlate", hpWindow, 0.50f, 0.338f, 0.96f, 0.438f, true);
            var floorPlate = EnsurePanel(root, "HudFloorPlate", hpWindow, 0.18f, 0.918f, 0.50f, 0.982f, true);
            if (hpE != null && hpE.GetComponent<RectMask2D>() == null)
                hpE.gameObject.AddComponent<RectMask2D>();
            if (hpP != null && hpP.GetComponent<RectMask2D>() == null)
                hpP.gameObject.AddComponent<RectMask2D>();
            if (main != null) main.SetSiblingIndex(0);
            if (hpE != null) hpE.SetSiblingIndex(1);
            if (hpP != null) hpP.SetSiblingIndex(2);
            if (floorPlate != null) floorPlate.SetSiblingIndex(3);

            NestInside(root, hpE, "EnemyNameLabel", 0.52f, 0.90f, 10f, 0.06f, 0.52f);
            NestInside(root, hpE, "EnemyHPSlider", 0.12f, 0.46f, 10f, 0.04f, 0.96f);
            NestInside(root, hpP, "PlayerNameLabel", 0.52f, 0.90f, 10f, 0.06f, 0.52f);
            NestInside(root, hpP, "PlayerHPSlider", 0.12f, 0.46f, 10f, 0.04f, 0.96f);
            enemyHpNum = EnsureText(hpE, "HudEnemyHpNum", 0.54f, 0.52f, 0.94f, 0.90f, 14, EnemyHp, TextAnchor.MiddleRight);
            playerHpNum = EnsureText(hpP, "HudPlayerHpNum", 0.54f, 0.52f, 0.94f, 0.90f, 14, HpGreen, TextAnchor.MiddleRight);

            Nest(root, main, "Button_Skill1", 0.04f, 0.66f, 0.29f, 0.94f);
            Nest(root, main, "Button_Skill2", 0.31f, 0.66f, 0.56f, 0.94f);
            Nest(root, main, "Button_Skill3", 0.04f, 0.35f, 0.29f, 0.63f);
            Nest(root, main, "Button_Charge", 0.31f, 0.35f, 0.56f, 0.63f);
            Nest(root, main, "Button_BrainSmash", 0.04f, 0.05f, 0.56f, 0.32f);
            BuildGearPanel(root, main);

            for (int i = 0; i < CmdNames.Length; i++)
            {
                CmdButtons[i] = FindGo(root, CmdNames[i]);
                StyleButton(CmdButtons[i], true);
            }

            Place(FindRt(root, "Button_BattleHome"), 0.02f, 0.905f, 0.16f, 0.985f);
            StyleButton(FindGo(root, "Button_BattleHome"), true);

            StyleHpName(FindGo(root, "EnemyNameLabel"));
            StyleHpName(FindGo(root, "PlayerNameLabel"));
            playerHpSlider = StyleSlider(FindGo(root, "PlayerHPSlider"), HpGreen);
            enemyHpSlider = StyleSlider(FindGo(root, "EnemyHPSlider"), EnemyHp);

            talkName = EnsureText(main != null ? main : root, "HudTalkName", 0.06f, 0.72f, 0.94f, 0.90f, 18, GoldHi, TextAnchor.MiddleLeft);
            if (talkName != null) talkName.gameObject.SetActive(false);
            talkLine = EnsureText(main != null ? main : root, "HudTalkLine", 0.06f, 0.08f, 0.94f, 0.92f, 22, TextCol, TextAnchor.UpperLeft);
            StyleTalk(talkLine, 22, TextCol, TextAnchor.UpperLeft, true);

            BuildResult(root);

            if (floorPlate != null)
            {
                var oldBanner = FindGo(root, "FloorBanner");
                string floorText = "";
                if (oldBanner != null)
                {
                    var ot = oldBanner.GetComponent<Text>();
                    if (ot != null) floorText = ot.text ?? "";
                    oldBanner.SetActive(false);
                }
                var label = EnsureText(floorPlate, "HudFloorLabel", 0.06f, 0.10f, 0.94f, 0.90f, 18, TextCol, TextAnchor.MiddleCenter);
                if (label != null)
                {
                    if (string.IsNullOrEmpty(floorText))
                        floorText = BattleStage.Floor + "F";
                    bool boss = BattleStage.IsBoss;
                    if (!floorText.StartsWith("◆") && !floorText.StartsWith("◇"))
                        floorText = (boss ? "◆ " : "◇ ") + floorText;
                    label.text = floorText;
                    label.color = boss ? new Color(1f, 0.62f, 0.38f) : TextCol;
                    label.alignment = TextAnchor.MiddleCenter;
                    Fit(label, 16);
                }
            }

            var hudTexts = canvas.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < hudTexts.Length; i++)
                UseFont(hudTexts[i]);

            RaiseHome(root);
            HideResult();
            RefreshMode();
            ApplyRaidChrome(root);
        }

        public static void Talk(string speaker, string line)
        {
            if (talkLine == null || talkName == null) Apply();
            commandMode = false;
            if (talkName != null)
            {
                talkName.text = "";
                talkName.gameObject.SetActive(false);
            }
            if (talkLine != null) talkLine.text = line ?? "";
            RefreshMode();
        }

        public static void ShowCommands()
        {
            if (talkLine == null) Apply();
            commandMode = true;
            RefreshMode();
        }

        public static void HideResult()
        {
            if (resultRoot != null) resultRoot.SetActive(false);
        }

        public static void ShowResult(bool won, string title, string sub)
        {
            if (resultRoot == null) Apply();
            if (resultRoot == null) return;
            resultRoot.SetActive(true);
            if (resultTitle != null)
            {
                resultTitle.text = title ?? "";
                resultTitle.color = won ? GoldHi : new Color(1f, 0.55f, 0.55f);
            }
            if (resultSub != null) resultSub.text = sub ?? "";
            commandMode = false;
            RefreshMode();
            if (battleCanvasRoot != null) RaiseHome(battleCanvasRoot);
        }

        static void RaiseHome(Transform root)
        {
            if (resultRoot != null) resultRoot.transform.SetAsLastSibling();
            var home = root.Find("Button_BattleHome");
            if (home != null) home.SetAsLastSibling();
        }

        static void GoHome()
        {
            HideResult();
            var flow = AppFlow.Instance;
            if (flow == null) flow = UnityEngine.Object.FindAnyObjectByType<AppFlow>();
            if (flow != null) flow.ShowHome();
        }

        public static void SetHp(bool isPlayer, float ratio, int hp, int max)
        {
            ratio = Mathf.Clamp01(ratio);
            if (!isPlayer && BattleStage.RaidMode)
            {
                SetRaidHp(ratio, hp, max);
                return;
            }
            var slider = isPlayer ? playerHpSlider : enemyHpSlider;
            var num = isPlayer ? playerHpNum : enemyHpNum;
            Color col = isPlayer ? PlayerHpColor(ratio) : EnemyHp;
            ApplyBar(slider, ratio, col);
            if (num != null)
            {
                num.text = string.Format("{0}/{1}", hp, max);
                num.color = col;
            }
        }

        static Color PlayerHpColor(float ratio)
        {
            if (ratio > 0.5f) return HpGreen;
            if (ratio > 0.25f) return HpYellow;
            return HpRed;
        }

        static void RefreshMode()
        {
            bool showingResult = resultRoot != null && resultRoot.activeSelf;
            if (talkName != null) talkName.gameObject.SetActive(false);
            if (talkLine != null) talkLine.gameObject.SetActive(!commandMode && !showingResult);
            for (int i = 0; i < CmdButtons.Length; i++)
            {
                var go = CmdButtons[i];
                if (go == null) continue;
                var cg = go.GetComponent<CanvasGroup>();
                if (cg == null) cg = go.AddComponent<CanvasGroup>();
                cg.alpha = commandMode && !showingResult ? 1f : 0f;
                cg.interactable = commandMode && !showingResult;
                cg.blocksRaycasts = commandMode && !showingResult;
            }
            if (gearRoot != null)
            {
                var cg = gearRoot.GetComponent<CanvasGroup>();
                if (cg == null) cg = gearRoot.AddComponent<CanvasGroup>();
                cg.alpha = commandMode && !showingResult ? 1f : 0f;
                cg.interactable = commandMode && !showingResult;
                cg.blocksRaycasts = commandMode && !showingResult;
            }
        }

        static void BuildResult(Transform root)
        {
            resultRoot = FindGo(root, "HudResultRoot");
            if (resultRoot == null)
            {
                resultRoot = new GameObject("HudResultRoot", typeof(RectTransform));
                resultRoot.transform.SetParent(root, false);
            }
            var rt = resultRoot.GetComponent<RectTransform>();
            Place(rt, 0f, 0f, 1f, 1f);
            var dim = EnsurePanel(resultRoot.transform, "HudResultDim", dimSprite, 0f, 0f, 1f, 1f, false);
            if (dim != null)
            {
                var img = dim.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.01f, 0.02f, 0.08f, 0.72f);
                    img.raycastTarget = true;
                }
            }
            var win = EnsurePanel(resultRoot.transform, "HudResultWindow", window, 0.08f, 0.30f, 0.92f, 0.74f, true);
            resultTitle = EnsureText(win != null ? win : resultRoot.transform, "HudResultTitle", 0.08f, 0.58f, 0.92f, 0.90f, 34, GoldHi, TextAnchor.MiddleCenter);
            var line = EnsurePanel(win != null ? win : resultRoot.transform, "HudResultLine", barWhite, 0.18f, 0.52f, 0.82f, 0.56f, false);
            if (line != null)
            {
                var img = line.GetComponent<Image>();
                if (img != null) img.color = Gold;
            }
            resultSub = EnsureText(win != null ? win : resultRoot.transform, "HudResultSub", 0.08f, 0.32f, 0.92f, 0.50f, 20, TextCol, TextAnchor.MiddleCenter);
            StyleTalk(resultSub, 20, TextCol, TextAnchor.MiddleCenter, true);
            if (resultTitle != null)
                Fit(resultTitle, 32);
            WireGoHome(dim != null ? dim.gameObject : null);
            EnsureResultHome(win != null ? win : resultRoot.transform);
        }

        static void EnsureResultHome(Transform win)
        {
            if (win == null) return;
            var t = win.Find("HudResultHome");
            GameObject go;
            if (t == null)
            {
                go = new GameObject("HudResultHome", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(win, false);
            }
            else go = t.gameObject;
            go.SetActive(true);
            Place(go.GetComponent<RectTransform>(), 0.22f, 0.08f, 0.78f, 0.28f);
            var img = go.GetComponent<Image>();
            Paint(img, window, true);
            if (img != null) img.raycastTarget = true;
            var label = EnsureText(go.transform, "HudResultHomeLabel", 0.06f, 0.10f, 0.94f, 0.90f, 22, TextCol, TextAnchor.MiddleCenter);
            if (label != null)
            {
                label.text = "ホームへ";
                label.resizeTextForBestFit = false;
                label.fontSize = 22;
            }
            WireGoHome(go);
        }

        static void WireGoHome(GameObject go)
        {
            if (go == null) return;
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
                btn.targetGraphic = img;
            }
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = GoldHi;
            cols.pressedColor = Gold;
            cols.colorMultiplier = 1f;
            btn.colors = cols;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(GoHome);
        }

        static void EnsureSprites()
        {
            window = MakeLuxWindow(72, 4);
            hpWindow = MakeLuxWindow(56, 3);
            barWhite = MakeSolid(Color.white);
            hpTrack = MakeHpTrack(24);
            dimSprite = MakeSolid(Color.white);
        }

        internal static Font GetFont()
        {
            if (rpgFont != null) return rpgFont;
            rpgFont = Resources.Load<Font>("Fonts/DotGothic16-Regular");
            if (rpgFont == null)
                rpgFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "YuGothic", "MS Gothic", "Hiragino Sans" }, 24);
            if (rpgFont == null)
                rpgFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return rpgFont;
        }

        static void UseFont(Text text)
        {
            if (text == null) return;
            var font = GetFont();
            if (font != null) text.font = font;
        }

        static Sprite MakeLuxWindow(int size, int outer)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "DqGoldWindow";
            int padPx = outer + 3;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dist = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    Color col = Navy;
                    if (dist < outer) col = Gold;
                    else if (dist < outer + 1) col = GoldLo;
                    else if (dist < outer + 2) col = GoldHi;
                    else if (dist < padPx) col = Gold;
                    tex.SetPixel(x, y, col);
                }
            }
            StampCorner(tex, size, padPx);
            tex.Apply(false, false);
            float pad = padPx + 1;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect, new Vector4(pad, pad, pad, pad));
        }

        static Sprite MakeHpTrack(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "HpTrack";
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool gold = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    bool inner = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    Color col = new Color(0.04f, 0.05f, 0.10f, 1f);
                    if (gold) col = Gold;
                    else if (inner) col = GoldLo;
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
        }

        static void StampCorner(Texture2D tex, int size, int inset)
        {
            int[] ox = { inset, inset, size - 1 - inset, size - 1 - inset };
            int[] oy = { inset, size - 1 - inset, inset, size - 1 - inset };
            for (int i = 0; i < 4; i++)
            {
                int cx = ox[i];
                int cy = oy[i];
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = cx + dx;
                        int y = cy + dy;
                        if (x < 0 || y < 0 || x >= size || y >= size) continue;
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 1)
                            tex.SetPixel(x, y, GoldHi);
                    }
            }
        }

        static Sprite MakeSolid(Color color)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    tex.SetPixel(x, y, color);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f, 0, SpriteMeshType.FullRect);
        }

        static void Nest(Transform searchRoot, Transform panel, string name, float x0, float y0, float x1, float y1)
        {
            if (panel == null) return;
            var rt = FindRt(searchRoot, name);
            if (rt == null) return;
            rt.SetParent(panel, false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            Place(rt, x0, y0, x1, y1);
        }

        static void NestInside(Transform searchRoot, Transform panel, string name, float y0, float y1, float pad, float x0, float x1)
        {
            if (panel == null) return;
            var rt = FindRt(searchRoot, name);
            if (rt == null) return;
            rt.SetParent(panel, false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = new Vector2(pad, 2f);
            rt.offsetMax = new Vector2(-pad, -2f);
        }

        static Transform EnsurePanel(Transform root, string name, Sprite sprite, float x0, float y0, float x1, float y1, bool sliced)
        {
            var t = root.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(root, false);
                t = go.transform;
            }
            else go = t.gameObject;
            go.SetActive(true);
            Place(go.GetComponent<RectTransform>(), x0, y0, x1, y1);
            Paint(go.GetComponent<Image>(), sprite, sliced);
            var img = go.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
            return t;
        }

        static Text EnsureText(Transform root, string name, float x0, float y0, float x1, float y1, int size, Color color, TextAnchor align)
        {
            if (root == null) return null;
            var t = root.Find(name);
            if (t == null) t = FindDeep(root, name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(root, false);
            }
            else
            {
                go = t.gameObject;
                go.transform.SetParent(root, false);
            }
            Place(go.GetComponent<RectTransform>(), x0, y0, x1, y1);
            var text = go.GetComponent<Text>();
            UseFont(text);
            text.raycastTarget = false;
            StyleLabel(go, size, color, align);
            return text;
        }

        static void HideFullBackdrop(Transform root)
        {
            var images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null || images[i].gameObject.name != "Background") continue;
                if (images[i].transform.parent != null && images[i].transform.parent.GetComponent<Slider>() != null)
                    continue;
                var rt = images[i].rectTransform;
                if (rt.anchorMin.x > 0.02f || rt.anchorMin.y > 0.02f || rt.anchorMax.x < 0.98f || rt.anchorMax.y < 0.98f)
                    continue;
                images[i].color = new Color(1f, 1f, 1f, 0f);
                images[i].raycastTarget = false;
            }
        }

        static void Hide(GameObject go)
        {
            if (go != null) go.SetActive(false);
        }

        static void StyleTalk(Text text, int size, Color color, TextAnchor align, bool wrap)
        {
            if (text == null) return;
            UseFont(text);
            Fit(text, size);
            text.color = color;
            text.alignment = align;
            text.lineSpacing = 1f;
            InkOutline(text.gameObject);
        }

        static void StyleButton(GameObject go, bool framed)
        {
            if (go == null) return;
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;
            var mask = go.GetComponent<RectMask2D>();
            if (mask != null) mask.enabled = false;
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                if (framed) Paint(img, hpWindow, true);
                else
                {
                    img.sprite = null;
                    img.color = new Color(1f, 1f, 1f, 0.01f);
                    img.raycastTarget = true;
                }
                img.raycastTarget = true;
            }
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                var cols = btn.colors;
                cols.normalColor = Color.white;
                cols.highlightedColor = GoldHi;
                cols.pressedColor = Gold;
                cols.disabledColor = new Color(0.55f, 0.55f, 0.7f, 0.7f);
                cols.colorMultiplier = 1f;
                btn.colors = cols;
                btn.targetGraphic = img;
            }
            var labels = go.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
                ClipLabel(labels[i], framed);
        }

        public static void ClipLabel(Text label, bool framed)
        {
            if (label == null) return;
            if (label.gameObject.name == "HudGearNeed")
            {
                Fit(label, 14);
                InkOutline(label.gameObject);
                return;
            }
            UseFont(label);
            label.color = TextCol;
            Fit(label, 16);
            label.alignment = framed ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            var rt = label.rectTransform;
            rt.localScale = Vector3.one;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(10f, 6f);
            rt.offsetMax = new Vector2(-10f, -6f);
            InkOutline(label.gameObject);
        }

        public static void SetCommandLabels(GameObject btn, string skillName, string gearText, bool gearReady)
        {
            if (btn == null) return;
            Text nameLabel = null;
            var texts = btn.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null) continue;
                if (texts[i].gameObject.name == "HudGearNeed") continue;
                nameLabel = texts[i];
                break;
            }
            if (nameLabel != null)
            {
                UseFont(nameLabel);
                nameLabel.text = skillName ?? "";
                nameLabel.color = TextCol;
                nameLabel.alignment = TextAnchor.MiddleCenter;
                Fit(nameLabel, 16);
                nameLabel.raycastTarget = false;
                var nrt = nameLabel.rectTransform;
                nrt.anchorMin = new Vector2(0f, 0.46f);
                nrt.anchorMax = Vector2.one;
                nrt.pivot = new Vector2(0.5f, 0.5f);
                nrt.offsetMin = new Vector2(10f, 2f);
                nrt.offsetMax = new Vector2(-10f, -8f);
                InkOutline(nameLabel.gameObject);
            }
            var gear = EnsureText(btn.transform, "HudGearNeed", 0.08f, 0.12f, 0.92f, 0.44f, 14,
                gearReady ? TextCol : GearWait, TextAnchor.MiddleCenter);
            if (gear == null) return;
            gear.text = gearText ?? "";
            gear.color = gearReady ? TextCol : GearWait;
            gear.alignment = TextAnchor.MiddleCenter;
            Fit(gear, 14);
            InkOutline(gear.gameObject);
        }

        public static void BindGearUp(System.Action action)
        {
            gearUp = action;
            if (gearButton == null) return;
            gearButton.onClick.RemoveAllListeners();
            if (gearUp != null) gearButton.onClick.AddListener(() => gearUp());
        }

        public static void SetGear(int current, int max, bool canUp)
        {
            if (gearValue != null)
                gearValue.text = current.ToString();
            for (int i = 0; i < gearPips.Length; i++)
            {
                if (gearPips[i] == null) continue;
                bool on = current > i;
                gearPips[i].color = on ? Gold : new Color(0.12f, 0.14f, 0.28f, 1f);
            }
            if (gearHint != null)
                gearHint.text = current >= max ? "MAX" : "タップでギアアップ";
            if (gearButton != null) gearButton.interactable = canUp;
        }

        public static void SetCommandGear(GameObject btn, string text, bool ready)
        {
            if (btn == null) return;
            var label = EnsureText(btn.transform, "HudGearNeed", 0.08f, 0.12f, 0.92f, 0.44f, 14,
                ready ? TextCol : GearWait, TextAnchor.MiddleCenter);
            if (label == null) return;
            label.text = text ?? "";
            label.color = ready ? TextCol : GearWait;
            label.alignment = TextAnchor.MiddleCenter;
            Fit(label, 14);
            InkOutline(label.gameObject);
        }

        static void BuildGearPanel(Transform canvasRoot, Transform main)
        {
            if (canvasRoot == null) canvasRoot = main;
            if (canvasRoot == null) return;
            Transform existing = canvasRoot.Find("HudGearPanel");
            if (existing == null && main != null)
                existing = main.Find("HudGearPanel");
            if (existing == null)
                existing = FindDeep(canvasRoot, "HudGearPanel");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            float x0 = 0.58f;
            float y0 = 0.012f;
            float x1 = 0.97f;
            float y1 = 0.286f;
            if (main != null)
            {
                var mrt = main as RectTransform ?? main.GetComponent<RectTransform>();
                if (mrt != null)
                {
                    x1 = mrt.anchorMax.x;
                    y0 = mrt.anchorMin.y;
                    y1 = mrt.anchorMax.y;
                }
            }
            var panel = EnsurePanel(canvasRoot, "HudGearPanel", window, x0, y0, x1, y1, true);
            gearRoot = panel != null ? panel.gameObject : null;
            if (gearRoot == null) return;
            if (main != null)
            {
                var mrt = main as RectTransform ?? main.GetComponent<RectTransform>();
                var grt = gearRoot.GetComponent<RectTransform>();
                if (mrt != null && grt != null)
                {
                    grt.offsetMin = new Vector2(0f, mrt.offsetMin.y);
                    grt.offsetMax = new Vector2(0f, mrt.offsetMax.y);
                }
            }
            if (gearRoot.GetComponent<RectMask2D>() == null)
                gearRoot.AddComponent<RectMask2D>();
            var img = gearRoot.GetComponent<Image>();
            if (img != null)
            {
                Paint(img, window, true);
                img.raycastTarget = true;
            }
            gearButton = gearRoot.GetComponent<Button>();
            if (gearButton == null) gearButton = gearRoot.AddComponent<Button>();
            gearButton.targetGraphic = img;
            var cols = gearButton.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = GoldHi;
            cols.pressedColor = Gold;
            cols.disabledColor = new Color(0.55f, 0.55f, 0.7f, 0.85f);
            cols.colorMultiplier = 1f;
            gearButton.colors = cols;
            BindGearUp(gearUp);
            gearRoot.transform.SetAsLastSibling();

            var title = EnsureText(gearRoot.transform, "HudGearTitle", 0.10f, 0.76f, 0.90f, 0.92f, 18, TextCol, TextAnchor.MiddleCenter);
            if (title != null)
            {
                title.text = "ギア";
                title.color = TextCol;
                Fit(title, 18);
            }
            gearValue = EnsureText(gearRoot.transform, "HudGearValue", 0.10f, 0.34f, 0.90f, 0.68f, 44, TextCol, TextAnchor.MiddleCenter);
            if (gearValue != null)
            {
                gearValue.text = "1";
                Fit(gearValue, 44);
                gearValue.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            string[] pip = { "1", "2", "3" };
            for (int i = 0; i < 4; i++)
            {
                var old = gearRoot.transform.Find("HudGearPip" + i);
                if (old != null) old.gameObject.SetActive(i < 3);
            }
            for (int i = 0; i < 3; i++)
            {
                float px = 0.08f + i * 0.30f;
                var pipGo = EnsurePanel(gearRoot.transform, "HudGearPip" + i, hpWindow, px, 0.20f, px + 0.26f, 0.38f, true);
                if (pipGo != null)
                {
                    gearPips[i] = pipGo.GetComponent<Image>();
                    var pt = EnsureText(pipGo, "HudGearPipLabel" + i, 0.05f, 0.05f, 0.95f, 0.95f, 16, TextCol, TextAnchor.MiddleCenter);
                    if (pt != null) pt.text = pip[i];
                }
            }
            gearPips[3] = null;
            gearHint = EnsureText(gearRoot.transform, "HudGearHint", 0.08f, 0.04f, 0.92f, 0.18f, 14, TextCol, TextAnchor.MiddleCenter);
            if (gearHint != null)
            {
                gearHint.text = "タップでギアアップ";
                Fit(gearHint, 14);
            }
            SetGear(1, 3, false);
        }

        static void StyleLabel(GameObject go, int size, Color color, TextAnchor align)
        {
            if (go == null) return;
            var text = go.GetComponent<Text>();
            if (text == null) return;
            UseFont(text);
            Fit(text, size);
            text.color = color;
            text.alignment = align;
            InkOutline(go);
        }

        static void StyleHpName(GameObject go)
        {
            if (go == null) return;
            var text = go.GetComponent<Text>();
            if (text == null) return;
            UseFont(text);
            InkOutline(go);
            Fit(text, 16);
            text.color = TextCol;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.localScale = Vector3.one;
        }

        static Slider StyleSlider(GameObject go, Color fill)
        {
            if (go == null) return null;
            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                slider.transition = Selectable.Transition.None;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.direction = Slider.Direction.LeftToRight;
                slider.interactable = false;
            }
            var images = go.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                string n = images[i].gameObject.name;
                if (n == "Handle" || n == "Fill" || n == "Background")
                {
                    images[i].enabled = false;
                    images[i].raycastTarget = false;
                }
            }
            var track = EnsurePanel(go.transform, "HpTrack", hpTrack, 0f, 0f, 1f, 1f, true);
            var fillPanel = EnsurePanel(go.transform, "HpFill", barWhite, 0f, 0f, 1f, 1f, false);
            if (track != null) track.SetSiblingIndex(0);
            if (fillPanel != null) fillPanel.SetSiblingIndex(1);
            if (fillPanel != null)
            {
                var img = fillPanel.GetComponent<Image>();
                if (img != null)
                {
                    Paint(img, barWhite, false);
                    img.color = fill;
                }
            }
            ApplyBar(slider, slider != null ? slider.value : 1f, fill);
            return slider;
        }

        static void ApplyBar(Slider slider, float ratio, Color fill)
        {
            if (slider == null) return;
            ratio = Mathf.Clamp01(ratio);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            Transform fillT = slider.transform.Find("HpFill");
            if (fillT == null)
            {
                var deep = slider.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < deep.Length; i++)
                {
                    if (deep[i] != null && deep[i].name == "HpFill")
                    {
                        fillT = deep[i];
                        break;
                    }
                }
            }
            if (fillT == null) return;
            var rt = fillT.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.offsetMin = new Vector2(2f, 2f);
                rt.offsetMax = new Vector2(-2f, -2f);
            }
            var img = fillT.GetComponent<Image>();
            if (img != null)
            {
                img.enabled = true;
                bool enemy = slider == enemyHpSlider;
                Sprite sp = enemy ? (RpgTheme.ZhuBarSprite() ?? barWhite) : barWhite;
                Paint(img, sp, false);
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
                img.fillAmount = ratio;
                img.color = enemy ? Color.white : fill;
                img.preserveAspect = false;
            }
            fillT.gameObject.SetActive(ratio > 0.004f);
            slider.fillRect = rt;
            slider.SetValueWithoutNotify(ratio);
            if (img != null) img.fillAmount = ratio;
        }

        static void Paint(Image img, Sprite sprite, bool sliced)
        {
            if (img == null) return;
            img.sprite = sprite;
            img.overrideSprite = sprite;
            img.material = null;
            img.color = Color.white;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = false;
            img.useSpriteMesh = false;
            img.pixelsPerUnitMultiplier = 1f;
        }

        static void Fit(Text text, int size)
        {
            if (text == null) return;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(11, size - 6);
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        static void InkOutline(GameObject go)
        {
            if (go == null) return;
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.enabled = true;
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            if (rt == null) return;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector3.zero;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static RectTransform FindRt(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<RectTransform>() : null;
        }

        static GameObject FindGo(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.gameObject : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        public static void SetRaidTurns(int left)
        {
            if (raidTurns == null) return;
            raidTurns.text = "残 " + Mathf.Max(0, left) + " / " + RaidRules.DailyTurns;
        }

        public static void ClearRaid()
        {
            if (raidRoot != null)
            {
                raidRoot.SetActive(false);
                UnityEngine.Object.Destroy(raidRoot);
            }
            raidRoot = null;
            raidFill = null;
            raidCore = null;
            raidName = null;
            raidNums = null;
            raidTurns = null;
            battleCanvasRoot = null;
            lastRaidHp = -1;
            lastRaidMax = -1;
        }

        static void ApplyRaidChrome(Transform root)
        {
            var enemyPlate = FindGo(root, "HudEnemyPlate");
            var floorPlate = FindGo(root, "HudFloorPlate");
            if (!BattleStage.RaidMode)
            {
                if (enemyPlate != null) enemyPlate.SetActive(true);
                if (floorPlate != null) floorPlate.SetActive(true);
                ClearRaid();
                return;
            }
            if (enemyPlate != null) enemyPlate.SetActive(false);
            if (floorPlate != null) floorPlate.SetActive(false);
            if (raidRoot == null)
            {
                raidRoot = new GameObject("RaidDynamaxBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                raidRoot.transform.SetParent(root, false);
                var rt = raidRoot.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.18f, 0.918f);
                rt.anchorMax = new Vector2(0.985f, 0.978f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                raidRoot.GetComponent<Image>().color = new Color(0.20f, 0.02f, 0.04f, 0.94f);
                var track = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                track.transform.SetParent(raidRoot.transform, false);
                var tr = track.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0.015f, 0.16f);
                tr.anchorMax = new Vector2(0.985f, 0.42f);
                tr.offsetMin = Vector2.zero;
                tr.offsetMax = Vector2.zero;
                track.GetComponent<Image>().color = new Color(0.05f, 0.01f, 0.02f, 1f);
                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillGo.transform.SetParent(track.transform, false);
                var fr = fillGo.GetComponent<RectTransform>();
                fr.anchorMin = Vector2.zero;
                fr.anchorMax = Vector2.one;
                fr.offsetMin = Vector2.zero;
                fr.offsetMax = Vector2.zero;
                raidFill = fillGo.GetComponent<Image>();
                raidFill.color = new Color(1f, 0.22f, 0.26f, 1f);
                var coreGo = new GameObject("Core", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                coreGo.transform.SetParent(fillGo.transform, false);
                var cr = coreGo.GetComponent<RectTransform>();
                cr.anchorMin = new Vector2(0f, 0.55f);
                cr.anchorMax = new Vector2(1f, 0.82f);
                cr.offsetMin = Vector2.zero;
                cr.offsetMax = Vector2.zero;
                raidCore = coreGo.GetComponent<Image>();
                raidCore.color = new Color(1f, 0.95f, 0.8f, 0.85f);
                raidName = EnsureText(raidRoot.transform, "RaidName", 0.03f, 0.42f, 0.46f, 0.96f, 15, Color.white, TextAnchor.MiddleLeft);
                raidTurns = EnsureText(raidRoot.transform, "RaidTurns", 0.46f, 0.42f, 0.62f, 0.96f, 12, new Color(1f, 0.82f, 0.55f), TextAnchor.MiddleCenter);
                raidNums = EnsureText(raidRoot.transform, "RaidNums", 0.62f, 0.42f, 0.97f, 0.96f, 13, new Color(1f, 0.86f, 0.86f), TextAnchor.MiddleRight);
            }
            if (raidRoot != null)
            {
                var barRt = raidRoot.GetComponent<RectTransform>();
                barRt.anchorMin = new Vector2(0.18f, 0.918f);
                barRt.anchorMax = new Vector2(0.985f, 0.978f);
                barRt.offsetMin = Vector2.zero;
                barRt.offsetMax = Vector2.zero;
            }
            raidRoot.SetActive(true);
            raidRoot.transform.SetAsLastSibling();
            var home = root.Find("Button_BattleHome");
            if (home != null) home.SetAsLastSibling();
        }

        static void SetRaidHp(float ratio, int hp, int max)
        {
            if (hp == lastRaidHp && max == lastRaidMax) return;
            lastRaidHp = hp;
            lastRaidMax = max;
            if (raidRoot == null)
            {
                if (battleCanvasRoot != null) ApplyRaidChrome(battleCanvasRoot);
            }
            if (raidName != null)
                raidName.text = string.IsNullOrEmpty(BattleStage.EnemyLabel) ? "巨影" : BattleStage.EnemyLabel;
            if (raidFill != null)
            {
                var rt = raidFill.rectTransform;
                rt.anchorMax = new Vector2(Mathf.Max(0.02f, ratio), 1f);
                raidFill.color = ratio > 0.5f
                    ? new Color(1f, 0.24f, 0.28f, 1f)
                    : ratio > 0.25f
                        ? new Color(0.95f, 0.40f, 0.10f, 1f)
                        : new Color(0.55f, 0.04f, 0.07f, 1f);
            }
            if (raidNums != null)
                raidNums.text = string.Format("{0}  /  {1}", RaidRules.FormatHp(hp), RaidRules.FormatHp(max));
        }
    }

}
