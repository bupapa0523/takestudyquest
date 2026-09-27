using UnityEngine;
using UnityEngine.UI;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    /// <summary>
    /// 追加したUIパックの枠・ボタン・帯を使う。金枠はパネル、朱はアクセント。教科色は触らない。
    /// </summary>
    public static class RpgTheme
    {
        public static readonly Color Bg = new Color(0.05f, 0.08f, 0.22f, 1f);
        public static readonly Color Navy = new Color(0.06f, 0.10f, 0.32f, 1f);
        public static readonly Color CardFill = new Color(0.08f, 0.13f, 0.36f, 1f);
        public static readonly Color ButtonFill = new Color(0.12f, 0.18f, 0.42f, 1f);
        public static readonly Color Gold = new Color(0.88f, 0.68f, 0.12f, 1f);
        public static readonly Color GoldHi = new Color(1f, 0.84f, 0.28f, 1f);
        public static readonly Color GoldLo = new Color(0.42f, 0.26f, 0.04f, 1f);
        public static readonly Color Text = Color.white;
        public static readonly Color Muted = new Color(0.72f, 0.74f, 0.80f, 1f);
        public static readonly Color Zhu = new Color(0.86f, 0.27f, 0.14f, 1f);
        public static readonly Color Danger = Zhu;
        public static readonly Color Shadow = new Color(0.02f, 0.02f, 0.06f, 0.85f);
        public static readonly Color Dim = new Color(0.01f, 0.02f, 0.06f, 0.72f);
        public static readonly Color DimHeavy = new Color(0.01f, 0.02f, 0.06f, 0.90f);

        const string PackDir = "Assets/UI/Complete_UI_Essential_Pack_Free/Complete_UI_Essential_Pack_Free/01_Flat_Theme/Sprites/";
        static Sprite window;
        static Sprite windowThin;
        static Font rpgFont;
        static readonly System.Collections.Generic.Dictionary<string, Sprite> cache = new System.Collections.Generic.Dictionary<string, Sprite>();
        static readonly System.Collections.Generic.Dictionary<int, Sprite> books = new System.Collections.Generic.Dictionary<int, Sprite>();

        public static Font UiFont()
        {
            if (rpgFont != null) return rpgFont;
            rpgFont = Resources.Load<Font>("Fonts/DotGothic16-Regular");
            if (rpgFont == null)
                rpgFont = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "YuGothic", "MS Gothic", "Hiragino Sans" }, 24);
            if (rpgFont == null)
                rpgFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return rpgFont;
        }

        public static Sprite PackSprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            Sprite sp;
            if (cache.TryGetValue(fileName, out sp) && sp != null) return sp;
            sp = Resources.Load<Sprite>("UiPack/" + fileName);
#if UNITY_EDITOR
            if (sp == null)
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(PackDir + fileName + ".png");
#endif
            if (sp != null) cache[fileName] = sp;
            return sp;
        }

        public static Sprite FrameGoldSprite() { return PackSprite("UI_Flat_Frame03a") ?? WindowSprite(); }
        public static Sprite FrameNavySprite() { return PackSprite("UI_Flat_Frame02a") ?? PackSprite("UI_Flat_Frame01a"); }
        public static Sprite FrameFillSprite() { return PackSprite("UI_Flat_Frame01a"); }
        public static Sprite ButtonSprite() { return PackSprite("UI_Flat_Button01a_1"); }
        public static Sprite BannerSprite() { return PackSprite("UI_Flat_Banner02a") ?? PackSprite("UI_Flat_Banner01a"); }
        public static Sprite SlotSprite() { return PackSprite("UI_Flat_FrameSlot02a") ?? PackSprite("UI_Flat_FrameSlot01a"); }
        public static Sprite InputSprite() { return PackSprite("UI_Flat_InputField01a"); }
        public static Sprite PlusSprite() { return PackSprite("UI_Flat_ButtonPlus01a"); }
        public static Sprite SelectSprite() { return PackSprite("UI_Flat_Select01a_1"); }
        public static Sprite BarSprite() { return PackSprite("UI_Flat_Bar03a"); }
        public static Sprite ZhuBarSprite() { return PackSprite("UI_Flat_BarFill01c"); }
        public static Sprite GoldBarSprite() { return PackSprite("UI_Flat_BarFill01a"); }

        public static Sprite WindowSprite()
        {
            if (window == null) window = MakeLuxWindow(72, 4);
            return window;
        }

        public static Sprite ThinWindowSprite()
        {
            if (windowThin == null) windowThin = MakeLuxWindow(56, 3);
            return windowThin;
        }

        public static void Paint(Image img, Sprite sp, Color color, bool sliced = true)
        {
            if (img == null) return;
            if (sp == null)
            {
                PaintFill(img, color);
                return;
            }
            img.sprite = sp;
            img.overrideSprite = sp;
            img.material = null;
            img.color = color;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = false;
            img.useSpriteMesh = false;
            img.pixelsPerUnitMultiplier = 1f;
        }

        public static void PaintWindow(Image img, bool thin = false)
        {
            PaintGoldFrame(img, thin);
        }

        public static void PaintGoldFrame(Image img, bool thin = false)
        {
            if (img == null) return;
            var frame = thin ? ThinWindowSprite() : WindowSprite();
            Paint(img, frame, Color.white, true);
            EnsureInner(img, thin ? 8f : 12f, CardFill);
        }

        public static void PaintNavyCard(Image img)
        {
            if (img == null) return;
            var t = img.transform.Find("FrameFill");
            if (t != null) t.gameObject.SetActive(false);
            var sp = FrameFillSprite() ?? FrameNavySprite();
            Paint(img, sp, new Color(0.16f, 0.22f, 0.50f, 1f), true);
        }

        public static void PaintButton(Image img, Color tint)
        {
            if (img == null) return;
            if (tint.a < 0.08f)
            {
                PaintFill(img, tint);
                return;
            }
            var sp = ButtonSprite();
            if (sp == null)
            {
                PaintFill(img, tint);
                return;
            }
            Paint(img, sp, tint, true);
        }

        public static void PaintBanner(Image img, Color tint)
        {
            Paint(img, BannerSprite(), tint, true);
        }

        public static void PaintSlot(Image img)
        {
            var sp = SlotSprite();
            Paint(img, sp, sp != null ? Color.white : CardFill, true);
        }

        public static readonly Color InputFill = new Color(1f, 0.96f, 0.88f, 1f);
        public static readonly Color InputText = new Color(0.10f, 0.08f, 0.06f, 1f);
        public static readonly Color InputHint = new Color(0.42f, 0.34f, 0.22f, 1f);

        public static void PaintInput(Image img)
        {
            PaintFill(img, InputFill);
        }

        public static void PaintPlus(Image img)
        {
            var sp = PlusSprite();
            Paint(img, sp, sp != null ? Zhu : Zhu, false);
            img.preserveAspect = true;
        }

        public static void PaintSelect(Image img, Color tint)
        {
            Paint(img, SelectSprite(), tint, true);
        }

        public static void AddZhuAccent(Transform parent)
        {
            if (parent == null) return;
            var existing = parent.Find("ZhuAccent");
            RectTransform rt;
            Image img;
            if (existing == null)
            {
                var go = new GameObject("ZhuAccent");
                go.transform.SetParent(parent, false);
                rt = go.AddComponent<RectTransform>();
                img = go.AddComponent<Image>();
                img.raycastTarget = false;
            }
            else
            {
                rt = existing.GetComponent<RectTransform>();
                img = existing.GetComponent<Image>();
            }
            SetZhuAccentRatio(parent, 1f);
            if (img != null) Paint(img, ZhuBarSprite() ?? BannerSprite(), Zhu, true);
        }

        public static void SetZhuAccentRatio(Transform parent, float ratio)
        {
            if (parent == null) return;
            var t = parent.Find("ZhuAccent");
            if (t == null) return;
            var rt = t.GetComponent<RectTransform>();
            if (rt == null) return;
            ratio = Mathf.Clamp01(ratio);
            rt.anchorMin = new Vector2(0.08f, 0.82f);
            rt.anchorMax = new Vector2(0.08f + 0.84f * ratio, 0.94f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            t.gameObject.SetActive(ratio > 0.001f);
        }

        public static void PaintFill(Image img, Color color)
        {
            if (img == null) return;
            img.sprite = null;
            img.overrideSprite = null;
            img.material = null;
            img.type = Image.Type.Simple;
            img.color = color;
        }

        public static int BookVariant(string id)
        {
            int v = 17;
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < id.Length; i++) v = v * 31 + id[i];
            }
            return Mathf.Abs(v);
        }

        public static void PaintBook(Image img, Color cover, int variant)
        {
            if (img == null) return;
            var sp = BookSprite(cover, variant);
            Paint(img, sp, Color.white, false);
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        public static Sprite BookSprite(Color cover, int variant)
        {
            int key = (Mathf.RoundToInt(cover.r * 63f) << 20)
                ^ (Mathf.RoundToInt(cover.g * 63f) << 12)
                ^ (Mathf.RoundToInt(cover.b * 63f) << 4)
                ^ (variant & 7);
            Sprite sp;
            if (books.TryGetValue(key, out sp) && sp != null) return sp;
            sp = MakeBook(cover, variant & 3);
            books[key] = sp;
            return sp;
        }

        static void EnsureInner(Image img, float pad, Color fill)
        {
            var t = img.transform.Find("FrameFill");
            RectTransform rt;
            Image inner;
            if (t == null)
            {
                var go = new GameObject("FrameFill");
                go.transform.SetParent(img.transform, false);
                go.transform.SetAsFirstSibling();
                rt = go.AddComponent<RectTransform>();
                inner = go.AddComponent<Image>();
            }
            else
            {
                rt = t.GetComponent<RectTransform>();
                inner = t.GetComponent<Image>();
                if (inner == null) inner = t.gameObject.AddComponent<Image>();
            }
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
            inner.raycastTarget = false;
            inner.gameObject.SetActive(true);
            PaintFill(inner, fill);
        }

        public static void StyleLabel(Text text, Color color)
        {
            if (text == null) return;
            var font = UiFont();
            if (font != null) text.font = font;
            text.color = color;
        }

        public static void StyleButtonColors(Button btn)
        {
            if (btn == null) return;
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = new Color(0.92f, 0.90f, 0.78f, 1f);
            cols.pressedColor = new Color(0.78f, 0.72f, 0.55f, 1f);
            cols.disabledColor = new Color(0.55f, 0.55f, 0.7f, 0.7f);
            cols.selectedColor = Color.white;
            cols.colorMultiplier = 1f;
            btn.colors = cols;
        }

        public static void Outline(GameObject go)
        {
            if (go == null) return;
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.enabled = true;
            outline.effectColor = new Color(0.02f, 0.03f, 0.08f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        public static void GoldRim(GameObject go)
        {
            if (go == null) return;
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.enabled = true;
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        public static bool IsSubjectTint(Color c)
        {
            for (int i = 0; i < 6; i++)
            {
                var s = StudySubjectColors.Color((StudySubject)i);
                if (Near(c, s) || Near(c, s * 0.7f))
                    return true;
            }
            return false;
        }

        public static bool IsDanger(Color c)
        {
            return c.a > 0.4f && c.r > 0.45f && c.g < 0.45f && c.b < 0.38f && c.r > c.g + 0.15f;
        }

        static bool Near(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db < 0.0025f;
        }

        static Sprite MakeBook(Color cover, int variant)
        {
            const int w = 80;
            const int h = 104;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "RpgBook";
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, Color.clear);

            Color spine = cover * 0.42f;
            spine.a = 1f;
            Color mid = cover * 0.82f;
            mid.a = 1f;
            Color hi = Color.Lerp(cover, Color.white, 0.22f);
            hi.a = 1f;
            Color dark = cover * 0.55f;
            dark.a = 1f;
            Color page = new Color(0.94f, 0.90f, 0.78f, 1f);
            Color pageLine = new Color(0.80f, 0.76f, 0.64f, 1f);
            Color shade = new Color(0.02f, 0.03f, 0.08f, 0.40f);

            int x0 = 12;
            int x1 = 68;
            int y0 = 8;
            int y1 = 96;
            FillTex(tex, x0 + 4, y0 - 3, x1 + 5, y1 - 4, shade);
            FillTex(tex, x1 - 7, y0 + 2, x1 + 2, y1 - 2, page);
            for (int i = 0; i < 7; i++)
                FillTex(tex, x1 - 6, y0 + 8 + i * 11, x1 + 1, y0 + 9 + i * 11, pageLine);
            FillTex(tex, x0, y0, x0 + 11, y1, spine);
            FillTex(tex, x0 + 10, y0, x0 + 12, y1, GoldLo);
            FillTex(tex, x0 + 12, y0, x1 - 7, y1, mid);
            FillTex(tex, x0 + 13, y1 - 7, x1 - 10, y1 - 2, hi);
            OutlineTex(tex, x0 + 12, y0, x1 - 7, y1, Gold);
            FillTex(tex, x0 + 18, y1 - 30, x1 - 14, y1 - 18, dark);
            OutlineTex(tex, x0 + 18, y1 - 30, x1 - 14, y1 - 18, GoldHi);

            int cx = (x0 + 12 + x1 - 7) / 2;
            int cy = y0 + 36;
            if (variant == 0)
            {
                FillTex(tex, cx - 8, cy, cx + 8, cy + 2, Gold);
                FillTex(tex, cx, cy - 8, cx + 2, cy + 8, Gold);
            }
            else if (variant == 1)
            {
                FillTex(tex, x0 + 18, cy + 6, x1 - 14, cy + 8, Gold);
                FillTex(tex, x0 + 18, cy - 4, x1 - 14, cy - 2, Gold);
            }
            else if (variant == 2)
            {
                FillCircle(tex, cx, cy, 8, Gold);
                FillCircle(tex, cx, cy, 4, dark);
            }
            else
            {
                OutlineTex(tex, cx - 8, cy - 8, cx + 8, cy + 8, Gold);
                FillTex(tex, cx - 2, cy - 2, cx + 2, cy + 2, GoldHi);
            }

            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 48f);
        }

        static void FillTex(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            int xmin = Mathf.Max(0, Mathf.Min(x0, x1));
            int xmax = Mathf.Min(tex.width - 1, Mathf.Max(x0, x1));
            int ymin = Mathf.Max(0, Mathf.Min(y0, y1));
            int ymax = Mathf.Min(tex.height - 1, Mathf.Max(y0, y1));
            for (int y = ymin; y <= ymax; y++)
                for (int x = xmin; x <= xmax; x++)
                    tex.SetPixel(x, y, c);
        }

        static void OutlineTex(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            FillTex(tex, x0, y0, x1, y0 + 1, c);
            FillTex(tex, x0, y1 - 1, x1, y1, c);
            FillTex(tex, x0, y0, x0 + 1, y1, c);
            FillTex(tex, x1 - 1, y0, x1, y1, c);
        }

        static void FillCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= r2) FillTex(tex, x, y, x, y, c);
                }
            }
        }

        static Sprite MakeLuxWindow(int size, int outer)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "RpgGoldWindow";
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
            tex.Apply(false, false);
            float pad = padPx + 1;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect, new Vector4(pad, pad, pad, pad));
        }
    }
}
