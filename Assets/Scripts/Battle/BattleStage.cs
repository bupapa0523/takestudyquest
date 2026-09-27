using System;
using UnityEngine;
using UnityEngine.UI;
using ShiftingMetropolis.App;
using ShiftingMetropolis.Progress;
using ShiftingMetropolis.Dungeon;

namespace ShiftingMetropolis.Battle
{
    public static class BattleStage
    {
        public static BattleFighter PlayerFighter;
        public static BattleFighter EnemyFighter;
        public static string PlayerLabel = "プレイヤー";
        public static string EnemyLabel = "拳士";
        public static EnemyKit.Kind EnemyKind = EnemyKit.Kind.Killer;

        static Transform root;
        static GameObject floor;
        static GameObject playerGo;
        static GameObject enemyGo;

        static Vector3 savedCamPos;
        static Quaternion savedCamRot;
        static bool savedCam;
        static bool savedCamBg;
        static Color savedCamBgColor;
        static CameraClearFlags savedCamClearFlags;

        static bool savedFog;
        static Color savedFogColor;
        static FogMode savedFogMode;
        static float savedFogDensity;
        static Color savedAmbient;
        static UnityEngine.Rendering.AmbientMode savedAmbientMode;
        static Light savedDirLight;
        static Color savedDirColor;
        static float savedDirIntensity;
        static GameObject floorLabelGo;

        public static int Floor;
        public static bool IsBoss;
        public static bool RaidMode;
        public static int RaidFloor = 1;
        public static int RaidWeek = 1;
        public static int RaidSkin;
        public static string RaidSkinPath = "";
        public static string RaidEnemyName = "";
        public static float RaidScale = 2.88f;
        static float savedFov;
        static bool savedFovSet;

        public static void Rebuild(Transform battleRoot)
        {
            Clear();
            root = battleRoot;
            if (RaidMode)
            {
                Floor = Mathf.Clamp(RaidFloor <= 0 ? 1 : RaidFloor, 1, StudyStore.DungeonMaxFloor);
                IsBoss = true;
                RaidScale = RaidRules.BodyScale(Floor);
            }
            else
            {
                Floor = StudyStore.DungeonFloor;
                IsBoss = StudyStore.IsBossFloor(Floor);
            }
            HidePlaceholder("PlayerVisual");
            HidePlaceholder("EnemyVisual");
            SoftenBackground();
            EnsureFloor();
            ApplyTheme();
            int roomFloor = Floor;
            if (RaidMode)
            {
                roomFloor = UnityEngine.Random.Range(1, StudyStore.DungeonMaxFloor + 1);
                DungeonRoomBuilder.LayoutSalt = UnityEngine.Random.Range(1, 999983);
            }
            else DungeonRoomBuilder.LayoutSalt = 0;
            DungeonRoomBuilder.Build(root, roomFloor, IsBoss, floor);
            ShowFloorBanner();
            BattleHudSkin.Apply();

            var cam = Camera.main;
            if (cam != null && !savedCam)
            {
                savedCamPos = cam.transform.position;
                savedCamRot = cam.transform.rotation;
                savedCam = true;
            }

            Vector3 playerPos = new Vector3(1.35f, 0f, 2.55f);
            Vector3 enemyPos = new Vector3(-1.15f, 0f, 8.45f);
            Quaternion playerRot = Quaternion.LookRotation(Flat(enemyPos - playerPos), Vector3.up);
            Quaternion enemyRot = Quaternion.LookRotation(Flat(playerPos - enemyPos), Vector3.up);

            int playerSkin = StudyStore.LookSlot(HeroAppearance.SlotSkin);
            playerGo = HeroAppearance.SpawnLooked(root, playerPos, playerRot, StudyStore.LookSlot, out var playerKind);
            if (playerGo != null)
            {
                string visualName = playerGo.name;
                PlayerFighter = playerGo.AddComponent<BattleFighter>();
                PlayerFighter.Bind(playerGo.transform, true, playerKind, visualName);
                playerGo.name = "BattleAvatar_Player";
            }

            string enemyVisual = null;
            HeroAppearance.SkinKind enemyKind = HeroAppearance.SkinKind.GanzSe;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                int enemySkin = RaidMode
                    ? ResolveRaidSkin(playerSkin)
                    : HeroAppearance.PickBattleOpponent(Floor, IsBoss, playerSkin, attempt);
                int[] enemyLook = BuildEnemyLook(enemySkin, IsBoss);
                var candidate = HeroAppearance.SpawnLooked(root, enemyPos, enemyRot, i =>
                {
                    if (i < 0 || i >= enemyLook.Length) return 0;
                    return enemyLook[i];
                }, out var candidateKind);
                if (candidate == null) continue;
                var candidateFighter = candidate.AddComponent<BattleFighter>();
                candidateFighter.Bind(candidate.transform, false, candidateKind, candidate.name);
                if (candidateFighter.IsPosedProperly() || attempt == 3 || RaidMode)
                {
                    if (!RaidMode && attempt == 3 && !candidateFighter.IsPosedProperly())
                    {
                        UnityEngine.Object.Destroy(candidate);
                        candidate = HeroAppearance.SpawnLooked(root, enemyPos, enemyRot, _ => 0, out candidateKind);
                        if (candidate != null)
                        {
                            candidateFighter = candidate.AddComponent<BattleFighter>();
                            candidateFighter.Bind(candidate.transform, false, candidateKind, candidate.name);
                        }
                    }
                    enemyGo = candidate;
                    if (enemyGo != null)
                    {
                        if (RaidMode) enemyGo.transform.localScale = Vector3.one * RaidScale;
                        else if (IsBoss) enemyGo.transform.localScale = Vector3.one * (Floor >= 100 ? 1.52f : 1.38f);
                        EnemyFighter = candidateFighter;
                        enemyVisual = enemyGo.name;
                        enemyKind = candidateKind;
                        enemyGo.name = "BattleAvatar_Enemy";
                    }
                    break;
                }
                UnityEngine.Object.Destroy(candidate);
            }

            HidePlaceholder("EnemyVisual");
            HidePlaceholder("PlayerVisual");

            PlayerLabel = PlayerDisplayName();
            EnemyKind = KindOf(enemyVisual, enemyKind);
            EnemyLabel = RaidMode && !string.IsNullOrEmpty(RaidEnemyName)
                ? RaidEnemyName
                : EnemyDisplayName(EnemyKind, IsBoss);

            if (cam != null)
            {
                // 少し下を見てキャラを画面上寄りに置き、下のコマンド枠と被らないようにする。
                Vector3 look = (playerPos + enemyPos) * 0.5f + Vector3.up * (IsBoss ? 1.45f : 1.28f);
                Vector3 offset = IsBoss
                    ? new Vector3(6.35f, 0.55f, -7.85f)
                    : new Vector3(6.05f, 0.48f, -7.45f);
                cam.transform.position = look + offset;
                cam.transform.LookAt(look);
                BattleCam.CaptureRest();
            }
        }

        public static void Clear()
        {
            if (playerGo != null)
            {
                playerGo.SetActive(false);
                UnityEngine.Object.Destroy(playerGo);
            }
            if (enemyGo != null)
            {
                enemyGo.SetActive(false);
                UnityEngine.Object.Destroy(enemyGo);
            }
            playerGo = null;
            enemyGo = null;
            PlayerFighter = null;
            EnemyFighter = null;
            BattleCam.Release();
            if (savedCam)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.transform.position = savedCamPos;
                    cam.transform.rotation = savedCamRot;
                    if (savedFovSet)
                    {
                        cam.fieldOfView = savedFov;
                        savedFovSet = false;
                    }
                    if (savedCamBg)
                    {
                        cam.backgroundColor = savedCamBgColor;
                        cam.clearFlags = savedCamClearFlags;
                        savedCamBg = false;
                    }
                }
                savedCam = false;
            }
            if (savedFog)
            {
                RenderSettings.fog = false;
                RenderSettings.fogColor = savedFogColor;
                RenderSettings.fogMode = savedFogMode;
                RenderSettings.fogDensity = savedFogDensity;
                RenderSettings.ambientMode = savedAmbientMode;
                RenderSettings.ambientLight = savedAmbient;
                if (savedDirLight != null)
                {
                    savedDirLight.color = savedDirColor;
                    savedDirLight.intensity = savedDirIntensity;
                    savedDirLight = null;
                }
                savedFog = false;
            }
            if (floorLabelGo != null)
            {
                floorLabelGo.SetActive(false);
                UnityEngine.Object.Destroy(floorLabelGo);
                floorLabelGo = null;
            }
            if (floor != null)
            {
                var rend = floor.GetComponent<Renderer>();
                if (rend != null) rend.enabled = true;
            }
            DungeonRoomBuilder.Clear();
        }

        static void ApplyTheme()
        {
            var theme = DungeonRunner.GetTheme(Floor, IsBoss);
            if (!savedFog)
            {
                savedFogColor = RenderSettings.fogColor;
                savedFogMode = RenderSettings.fogMode;
                savedFogDensity = RenderSettings.fogDensity;
                savedAmbient = RenderSettings.ambientLight;
                savedAmbientMode = RenderSettings.ambientMode;
                savedFog = true;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = IsBoss ? Mathf.Max(theme.fogDensity, 0.022f) : theme.fogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(theme.lightTint, Color.white, IsBoss ? 0.28f : 0.08f) * (IsBoss ? 0.72f : 0.42f);

            var cam = Camera.main;
            if (cam != null)
            {
                if (!savedCamBg)
                {
                    savedCamBgColor = cam.backgroundColor;
                    savedCamClearFlags = cam.clearFlags;
                    savedCamBg = true;
                }
                if (IsBoss)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.40f, 0.03f, 0.04f); // 禍々しく深紅に染まった空
                }
            }

            var light = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                if (savedDirLight == null)
                {
                    savedDirLight = light;
                    savedDirColor = light.color;
                    savedDirIntensity = light.intensity;
                }
                light.color = IsBoss ? Color.Lerp(theme.lightTint, new Color(1f, 0.28f, 0.22f), 0.65f) : theme.lightTint;
                light.intensity = IsBoss ? 1.45f : 1.05f;
            }

            if (floor != null)
            {
                var rend = floor.GetComponent<Renderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    var mat = rend.sharedMaterial;
                    Color tone = Color.Lerp(theme.fogColor, theme.lightTint, IsBoss ? 0.35f : 0.12f);
                    tone = Color.Lerp(tone, Color.black, IsBoss ? 0.18f : 0.55f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tone);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", tone);
                }
            }
        }

        static void ShowFloorBanner()
        {
            var canvas = GameObject.Find("BattleCanvas");
            if (canvas == null) return;
            var existing = canvas.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].name == "FloorBanner" && existing[i].gameObject != canvas)
                {
                    existing[i].gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(existing[i].gameObject);
                }
            }
            floorLabelGo = new GameObject("FloorBanner");
            floorLabelGo.transform.SetParent(canvas.transform, false);
            var rt = floorLabelGo.AddComponent<RectTransform>();
            rt.anchorMin = RaidMode ? new Vector2(0.22f, 0.848f) : new Vector2(0.24f, 1f);
            rt.anchorMax = RaidMode ? new Vector2(0.78f, 0.898f) : new Vector2(0.76f, 1f);
            rt.pivot = new Vector2(0.5f, RaidMode ? 0.5f : 1f);
            rt.anchoredPosition = RaidMode ? Vector2.zero : new Vector2(0f, -118f);
            rt.sizeDelta = RaidMode ? Vector2.zero : new Vector2(0f, 36f);
            floorLabelGo.AddComponent<CanvasGroup>().blocksRaycasts = false;

            var text = floorLabelGo.AddComponent<Text>();
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 18;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 18;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = IsBoss ? new Color(1f, 0.35f, 0.35f) : Color.white;
            text.text = RaidMode
                ? Mathf.Max(1, RaidWeek) + "周目  " + Floor + "F  レイド"
                : DungeonRunner.FloorLabel(Floor, IsBoss);

            var home = canvas.transform.Find("Button_BattleHome");
            if (home != null) home.SetAsLastSibling();
        }

        static int ResolveRaidSkin(int avoidLook)
        {
            if (!string.IsNullOrEmpty(RaidSkinPath))
            {
                int fromPath = HeroAppearance.LookIndexForPath(RaidSkinPath);
                if (fromPath >= 0) return fromPath;
            }
            if (RaidSkin >= 0) return RaidSkin;
            return HeroAppearance.PickBattleOpponent(Floor, true, avoidLook);
        }

        static int PickEnemySkin(int playerSkin)
        {
            return HeroAppearance.PickBattleOpponent(Floor, IsBoss, playerSkin);
        }

        static int[] BuildEnemyLook(int skin, bool boss)
        {
            var look = new int[16];
            look[0] = skin;
            look[1] = 2;
            for (int i = 7; i <= 12; i++) look[i] = boss ? 2 : 1;
            return look;
        }

        static string PlayerDisplayName()
        {
            return StudyStore.ProfileName();
        }

        static readonly string[] BruiserNames = { "拳士", "刺客", "傭兵", "浪人", "闘技者", "門番", "武人", "番犬" };
        static readonly string[] MageNames = { "術師", "呪術師", "炎使い", "氷使い", "召喚士", "影法師", "魔導士" };
        static readonly string[] KnightNames = { "武士", "剣豪", "鎧武者", "近衛", "守護者", "大将", "侍" };

        static EnemyKit.Kind KindOf(string visualName, HeroAppearance.SkinKind kind)
        {
            if (kind == HeroAppearance.SkinKind.HotondoShogun) return EnemyKit.Kind.Shogun;
            if (!string.IsNullOrEmpty(visualName)
                && visualName.IndexOf("Monster", StringComparison.OrdinalIgnoreCase) >= 0)
                return EnemyKit.Kind.Monster;
            return EnemyKit.Kind.Killer;
        }

        static string EnemyDisplayName(EnemyKit.Kind kind, bool boss)
        {
            string[] pool = kind == EnemyKit.Kind.Monster ? MageNames
                : kind == EnemyKit.Kind.Shogun ? KnightNames
                : BruiserNames;
            int i = Mathf.Abs(Floor * 17 + (boss ? 3 : 1)) % pool.Length;
            string name = pool[i];
            if (!boss) return name;
            string titled = "主・" + name;
            return titled.Length <= 7 ? titled : "階層主";
        }

        static void EnsureFloor()
        {
            if (root == null) return;
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "BattleFloor";
                floor.transform.SetParent(root, false);
                var col = floor.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.Destroy(col);
                var rend = floor.GetComponent<Renderer>();
                if (rend != null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    if (shader != null)
                    {
                        var mat = new Material(shader);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.12f, 0.13f, 0.16f));
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.12f, 0.13f, 0.16f));
                        rend.sharedMaterial = mat;
                    }
                }
            }
            floor.transform.localPosition = DungeonRoomBuilder.Arena + new Vector3(0f, -0.02f, 0f);
            floor.transform.localScale = new Vector3(2.4f, 1f, 2.4f);
        }

        static void HidePlaceholder(string name)
        {
            var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name)
                {
                    all[i].gameObject.SetActive(false);
                    var rends = all[i].GetComponentsInChildren<Renderer>(true);
                    for (int r = 0; r < rends.Length; r++)
                    {
                        if (rends[r] != null) rends[r].enabled = false;
                    }
                    var cols = all[i].GetComponentsInChildren<Collider>(true);
                    for (int c = 0; c < cols.Length; c++)
                    {
                        if (cols[c] != null) cols[c].enabled = false;
                    }
                }
            }
        }

        static void SoftenBackground()
        {
            var canvas = GameObject.Find("BattleCanvas");
            if (canvas == null) return;
            var images = canvas.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null || images[i].gameObject.name != "Background") continue;
                if (images[i].transform.parent != null && images[i].transform.parent.GetComponent<Slider>() != null)
                    continue;
                var rt = images[i].rectTransform;
                if (rt.anchorMin.x > 0.02f || rt.anchorMin.y > 0.02f || rt.anchorMax.x < 0.98f || rt.anchorMax.y < 0.98f)
                    continue;
                var c = images[i].color;
                c.a = IsBoss ? 0.10f : 0.16f;
                images[i].color = c;
                images[i].raycastTarget = false;
            }
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            if (v.sqrMagnitude < 0.0001f) return Vector3.forward;
            return v.normalized;
        }
    }
}
