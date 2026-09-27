using System.Collections.Generic;
using UnityEngine;
using ShiftingMetropolis.App;
using ShiftingMetropolis.Battle;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftingMetropolis.Dungeon
{
    /// <summary>
    /// 10ステージ分の塔フロア。中央は空けて広く戦い、壁・ビル・木は外周と奥に置く。
    /// </summary>
    public static class DungeonRoomBuilder
    {
        const string Mini = "Assets/back/kenney_mini-dungeon/Models/FBX format/";
        const string Modular = "Assets/back/kenney_modular-dungeon-kit_1.0/Models/FBX format/";
        const string Town = "Assets/back/kenney_fantasy-town-kit_2.0/Models/FBX format/";
        const string City = "Assets/back/kenney_city-kit-commercial_2.1 (1)/Models/FBX format/";
        const string Arcade = "Assets/back/kenney_mini-arcade/Models/FBX format/";
        const string KayD = "Assets/hotondo/ステージ/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
        const string KayF = "Assets/hotondo/ステージ/KayKit_Forest_Nature_Pack_1.0_FREE/Assets/fbx(unity)/";
        const string KayH = "Assets/hotondo/ステージ/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/";
        const string KayC = "Assets/hotondo/ステージ/KayKit_City_Builder_Bits_1.0_FREE/Assets/fbx (unity)/";
        const string KayP = "Assets/hotondo/ステージ/KayKit_Platformer_Pack_1.0_FREE/Assets/fbx(unity)/";
        const string KayHex = "Assets/hotondo/ステージ/KayKit_Medieval_Hexagon_Pack_1.0_FREE/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/";
        const string Trailer = "Assets/hotondo/Trailer_Park/Models/Trailer_Park_Props.fbx";

        public static readonly Vector3 Arena = new Vector3(0.1f, 0f, 5.8f);
        public static int LayoutSalt;

        static GameObject room;
        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

        public static void Build(Transform parent, int floor, bool boss, GameObject fallbackFloor)
        {
            Clear();
            Sweep(parent);
            if (parent == null) return;

            room = new GameObject("DungeonRoom");
            room.transform.SetParent(parent, false);
            room.transform.localPosition = Arena;
            room.transform.localRotation = Quaternion.identity;

            int stage = DungeonRunner.StageIndex(floor);
            var rng = new System.Random(unchecked(floor * 7919 + (boss ? 17 : 0) + LayoutSalt));

            bool usePlane = stage == 2 || stage == 7;
            if (fallbackFloor != null)
            {
                var rend = fallbackFloor.GetComponent<Renderer>();
                if (rend != null) rend.enabled = usePlane;
            }

            switch (stage)
            {
                case 0: BuildStoneTower(rng); break;
                case 1: BuildCityBits(rng); break;
                case 2: BuildWildForest(rng); break;
                case 3: BuildGraveyard(rng); break;
                case 4: BuildToyCorridor(rng); break;
                case 5: BuildHexVillage(rng); break;
                case 6: BuildKayDungeon(rng); break;
                case 7: BuildTrailerYard(rng); break;
                case 8: BuildArcade(rng); break;
                default: BuildSummit(rng); break;
            }

            if (stage == 0 || stage == 6 || stage == 9)
                Place(Modular + "stairs-wide.fbx", new Vector3(-11.5f, 0f, 2.5f), 90f, 0.85f);

            if (boss) DressBoss(rng, stage);
            else AddPointLight(new Vector3(0f, 4.2f, 0.5f), new Color(1f, 0.92f, 0.82f), 1.6f, 16f, false);
        }

        public static void Clear()
        {
            if (room != null)
            {
                room.name = "DungeonRoom_Dying";
                Kill(room);
                room = null;
            }
        }

        static void Sweep(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null) continue;
                if (child.name == "DungeonRoom" || child.name.StartsWith("DungeonRoom_"))
                    Kill(child.gameObject);
            }
        }

        static void Kill(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(go);
                return;
            }
#endif
            Object.Destroy(go);
        }

        static void BuildStoneTower(System.Random rng)
        {
            TileFloors(Modular + "template-floor.fbx", Modular + "template-floor-detail-a.fbx", 4f, 2, 1f);
            BackWalls(Modular + "template-wall.fbx", 4f, 10.2f, 0f);
            LeftWalls(Modular + "template-wall.fbx", 4f, -10.2f, 0f);
            RightWalls(Modular + "template-wall.fbx", 4f, 10.2f, 0f);
            BackWalls(Modular + "template-wall.fbx", 4f, 10.2f, 4.15f);
            LeftWalls(Modular + "template-wall.fbx", 4f, -10.2f, 4.15f);
            RightWalls(Modular + "template-wall.fbx", 4f, 10.2f, 4.15f);
            BackWalls(Modular + "template-wall.fbx", 4f, 10.2f, 8.3f);
            LeftWalls(Modular + "template-wall.fbx", 4f, -10.2f, 8.3f);
            RightWalls(Modular + "template-wall.fbx", 4f, 10.2f, 8.3f);
            Place(Modular + "template-wall-corner.fbx", new Vector3(-10f, 0f, 10f), 0f, 1f);
            Place(Modular + "template-wall-corner.fbx", new Vector3(10f, 0f, 10f), -90f, 1f);
            CapCeiling(12.4f);
            Place(Mini + "banner.fbx", new Vector3(-3.2f, 0f, 9.1f), 0f, 4.4f);
            Place(Mini + "banner.fbx", new Vector3(3.2f, 0f, 9.1f), 0f, 4.4f);
            Place(Mini + "column.fbx", new Vector3(-5.2f, 0f, 7.4f), 0f, 4.2f);
            Place(Mini + "column.fbx", new Vector3(5.2f, 0f, 7.4f), 0f, 4.2f);
            Place(Mini + "column.fbx", new Vector3(-5.4f, 0f, -1.6f), 0f, 3.6f);
            ScatterEdge(rng, Mini + "barrel.fbx", 2.8f, 3);
            ScatterEdge(rng, Mini + "chest.fbx", 2.6f, 1);
        }

        static void BuildCityBits(System.Random rng)
        {
            TileFloors(KayC + "road_straight.fbx", KayC + "road_straight_crossing.fbx", 4f, 2, 2f);
            EncloseCity();
            Place(KayC + "streetlight.fbx", new Vector3(-6.4f, 0f, 7.2f), 0f, 4.2f, PlaceSit.Ground);
            Place(KayC + "streetlight.fbx", new Vector3(6.6f, 0f, 7.0f), 180f, 4.2f, PlaceSit.Ground);
            Place(KayC + "trafficlight_A.fbx", new Vector3(-5.8f, 0f, 8.6f), 0f, 4.0f, PlaceSit.Ground);
            Place(KayC + "trafficlight_B.fbx", new Vector3(6.0f, 0f, 8.4f), 180f, 4.0f, PlaceSit.Ground);
            Place(KayC + "car_taxi.fbx", new Vector3(-8.2f, 0.02f, 6.8f), 8f, 4.2f, PlaceSit.Ground);
            Place(KayC + "car_sedan.fbx", new Vector3(8.4f, 0.02f, 7.4f), 172f, 4.2f, PlaceSit.Ground);
            Place(KayC + "car_police.fbx", new Vector3(-8.4f, 0.02f, 3.2f), 6f, 4.1f, PlaceSit.Ground);
            Place(KayC + "dumpster.fbx", new Vector3(-8.0f, 0f, 8.0f), 20f, 3.0f, PlaceSit.Ground);
            Place(KayC + "bench.fbx", new Vector3(8.0f, 0f, 8.2f), -90f, 3.2f, PlaceSit.Ground);
            Place(KayC + "firehydrant.fbx", new Vector3(-6.8f, 0f, 5.6f), 0f, 3.0f, PlaceSit.Ground);
            ScatterEdge(rng, KayC + "trash_A.fbx", 3.0f, 2);
            AddPointLight(new Vector3(-6.4f, 4.4f, 7.2f), new Color(1f, 0.85f, 0.55f), 2.2f, 12f, false);
        }

        static void BuildWildForest(System.Random rng)
        {
            string[] trees =
            {
                KayF + "Tree_1_B_Color1.fbx", KayF + "Tree_1_C_Color1.fbx",
                KayF + "Tree_3_A_Color1.fbx", KayF + "Tree_4_A_Color1.fbx",
                KayF + "Tree_2_D_Color1.fbx"
            };
            Vector3[] spots =
            {
                new Vector3(-7.8f, 0f, 8.2f), new Vector3(-6.6f, 0f, 5.0f), new Vector3(-8.2f, 0f, 1.6f),
                new Vector3(-7.0f, 0f, -2.2f), new Vector3(-5.8f, 0f, 9.4f), new Vector3(-9.0f, 0f, 3.4f),
                new Vector3(7.2f, 0f, 8.0f), new Vector3(8.0f, 0f, 4.6f), new Vector3(6.8f, 0f, 1.0f),
                new Vector3(7.6f, 0f, -2.0f), new Vector3(5.8f, 0f, 9.2f), new Vector3(8.8f, 0f, 2.8f)
            };
            for (int i = 0; i < spots.Length; i++)
                Place(trees[rng.Next(trees.Length)], spots[i], rng.Next(360), 1.2f + (float)rng.NextDouble() * 0.4f);
            Place(KayF + "Tree_Bare_1_B_Color1.fbx", new Vector3(-8.6f, 0f, 8.8f), 15f, 1.4f);
            Place(KayF + "Bush_3_A_Color1.fbx", new Vector3(-5.4f, 0f, 6.4f), 10f, 2.3f);
            Place(KayF + "Bush_2_A_Color1.fbx", new Vector3(5.6f, 0f, 6.2f), 40f, 2.3f);
            Place(KayF + "Bush_4_A_Color1.fbx", new Vector3(-5.8f, 0f, -1.6f), 70f, 2.0f);
            Place(KayF + "Bush_1_A_Color1.fbx", new Vector3(5.4f, 0f, -1.4f), 20f, 2.0f);
            Place(KayF + "Rock_1_A_Color1.fbx", new Vector3(-5.8f, 0f, 4.8f), 0f, 3.4f);
            Place(KayF + "Rock_3_B_Color1.fbx", new Vector3(5.4f, 0f, 5.0f), 25f, 2.8f);
            Place(KayF + "Rock_2_A_Color1.fbx", new Vector3(-6.4f, 0f, 0.4f), 50f, 2.6f);
            Place(KayF + "Grass_1_A_Color1.fbx", new Vector3(-4.8f, 0f, 3.2f), 0f, 2.4f);
            Place(KayF + "Grass_2_A_Color1.fbx", new Vector3(4.6f, 0f, 2.8f), 40f, 2.4f);
            EncloseTower(KayD + "wall.fbx", 4f, 3, 1f, 4f);
            AddPointLight(new Vector3(0f, 5.2f, 3f), new Color(0.55f, 1f, 0.45f), 1.6f, 14f, false);
        }

        static void BuildGraveyard(System.Random rng)
        {
            TileFloors(KayH + "floor_dirt.fbx", KayH + "floor_dirt_grave.fbx", 4f, 2, 1f);
            EncloseTower(KayD + "wall.fbx", 4f, 3, 1f, 4f);
            Place(KayH + "crypt.fbx", new Vector3(0f, 0f, 9.6f), 180f, 0.55f, PlaceSit.Ground);
            Place(KayH + "arch_gate.fbx", new Vector3(0f, 0f, 9.0f), 0f, 1.05f, PlaceSit.Ground);
            Place(KayH + "tree_dead_large_decorated.fbx", new Vector3(-9.2f, 0f, 7.8f), 20f, 1.05f, PlaceSit.Ground);
            Place(KayH + "tree_dead_medium.fbx", new Vector3(9.0f, 0f, 7.6f), -15f, 1.1f, PlaceSit.Ground);
            Place(KayH + "tree_pine_orange_large.fbx", new Vector3(-11.2f, 0f, 2.4f), 0f, 0.7f, PlaceSit.Ground);
            Place(KayH + "tree_pine_yellow_large.fbx", new Vector3(11.2f, 0f, 2.2f), 30f, 0.7f, PlaceSit.Ground);
            Place(KayH + "grave_A.fbx", new Vector3(-7.6f, 0f, 5.2f), 10f, 1.05f, PlaceSit.Ground);
            Place(KayH + "grave_B.fbx", new Vector3(7.8f, 0f, 5.0f), -12f, 1.05f, PlaceSit.Ground);
            Place(KayH + "gravestone.fbx", new Vector3(-7.4f, 0f, 2.4f), 8f, 1.1f, PlaceSit.Ground);
            Place(KayH + "gravemarker_A.fbx", new Vector3(7.6f, 0f, 2.2f), -8f, 1.1f, PlaceSit.Ground);
            Place(KayH + "coffin.fbx", new Vector3(-8.4f, 0f, 8.4f), 80f, 1.1f, PlaceSit.Ground);
            Place(KayH + "shrine.fbx", new Vector3(8.2f, 0f, 8.6f), -20f, 1.15f, PlaceSit.Ground);
            Place(KayH + "post_skull.fbx", new Vector3(-6.4f, 0f, 8.0f), 0f, 1.3f, PlaceSit.Ground);
            Place(KayH + "ribcage.fbx", new Vector3(6.6f, 0f, 7.8f), 25f, 1.4f, PlaceSit.Ground);
            Place(KayH + "pumpkin_orange_jackolantern.fbx", new Vector3(-8.0f, 0f, 8.8f), 0f, 1.6f, PlaceSit.Ground);
            Place(KayH + "pumpkin_yellow_jackolantern.fbx", new Vector3(8.2f, 0f, 8.6f), 20f, 1.6f, PlaceSit.Ground);
            for (int i = -2; i <= 2; i++)
                Place(KayH + "fence.fbx", new Vector3(i * 4f, 0f, 9.4f), 0f, 1f, PlaceSit.Ground);
            Place(KayH + "fence.fbx", new Vector3(-8.8f, 0f, 5.0f), 90f, 1f, PlaceSit.Ground);
            Place(KayH + "fence_broken.fbx", new Vector3(8.8f, 0f, 5.0f), 90f, 1f, PlaceSit.Ground);
            Place(KayH + "lantern_standing.fbx", new Vector3(-5.0f, 0f, 7.2f), 0f, 1.5f, PlaceSit.Ground);
            Place(KayH + "post_lantern.fbx", new Vector3(5.0f, 0f, 7.2f), 0f, 1.5f, PlaceSit.Ground);
            AddPointLight(new Vector3(0f, 2.2f, 4f), new Color(1f, 0.42f, 0.08f), 2.8f, 12f, false);
        }

        static void BuildToyCorridor(System.Random rng)
        {
            TileFloors(KayP + "green/platform_4x4x1_green.fbx", KayP + "blue/platform_4x4x1_blue.fbx", 4f, 2, 1f, PlaceSit.FloorTop);
            Place(KayP + "red/arch_wide_red.fbx", new Vector3(0f, 0f, 4.85f), 0f, 1.15f, PlaceSit.Ground);
            Place(KayP + "yellow/arch_tall_yellow.fbx", new Vector3(-8.6f, 0f, 4.2f), 10f, 0.95f, PlaceSit.Ground);
            Place(KayP + "blue/arch_tall_blue.fbx", new Vector3(8.6f, 0f, 4.0f), -10f, 0.95f, PlaceSit.Ground);
            Place(KayP + "green/platform_4x2x2_green.fbx", new Vector3(-9.25f, 0f, 3.2f), 90f, 1f, PlaceSit.Ground);
            Place(KayP + "red/platform_4x2x2_red.fbx", new Vector3(9.25f, 0f, 3.0f), -90f, 1f, PlaceSit.Ground);
            Place(KayP + "yellow/platform_4x2x4_yellow.fbx", new Vector3(-9.35f, 2.05f, 2.2f), 0f, 1f);
            Place(KayP + "blue/platform_4x2x4_blue.fbx", new Vector3(9.35f, 2.05f, 2.0f), 0f, 1f);
            Place(KayP + "blue/pipe_straight_A_blue.fbx", new Vector3(-9.2f, 0f, 3.8f), 90f, 1.05f, PlaceSit.Ground);
            Place(KayP + "red/pipe_straight_A_red.fbx", new Vector3(9.2f, 0f, 3.6f), 90f, 1.05f, PlaceSit.Ground);
            Place(KayP + "yellow/cone_yellow.fbx", new Vector3(-8.8f, 0f, 2.8f), 0f, 1.3f, PlaceSit.Ground);
            Place(KayP + "blue/cone_blue.fbx", new Vector3(8.8f, 0f, 2.6f), 0f, 1.3f, PlaceSit.Ground);
            EncloseToy();
            DressToyWalls();
            AddPointLight(new Vector3(-3.2f, 2.8f, 2f), new Color(0.15f, 1f, 0.45f), 2.4f, 10f, false);
            AddPointLight(new Vector3(3.2f, 2.8f, 2f), new Color(1f, 0.28f, 0.18f), 2.4f, 10f, false);
            AddPointLight(new Vector3(0f, 5.2f, 2f), new Color(1f, 0.85f, 0.2f), 2.0f, 14f, false);
        }

        static void EncloseToy()
        {
            string[] blocks =
            {
                KayP + "red/platform_4x4x4_red.fbx",
                KayP + "blue/platform_4x4x4_blue.fbx",
                KayP + "green/platform_4x4x4_green.fbx",
                KayP + "yellow/platform_4x4x4_yellow.fbx"
            };
            const float side = 10f;
            const float back = 7.15f;
            const float spacing = 3.88f;
            for (int story = 0; story < 3; story++)
            {
                float y = story * 3.88f;
                for (int i = -3; i <= 3; i++)
                {
                    Place(blocks[Wrap(i + story * 2, blocks.Length)], new Vector3(i * spacing, y, back), 0f, 1f);
                    Place(blocks[Wrap(i + story * 2 + 1, blocks.Length)], new Vector3(-side, y, i * spacing), 90f, 1f);
                    Place(blocks[Wrap(i + story * 2 + 3, blocks.Length)], new Vector3(side, y, i * spacing), -90f, 1f);
                }
            }
            CapCeiling(11.4f);
            PaperToyWalls();
        }

        static GameObject PlaceEuler(string path, Vector3 localPos, Vector3 euler, float scale)
        {
            var go = Place(path, localPos, 0f, scale, PlaceSit.None);
            if (go != null) go.transform.localRotation = Quaternion.Euler(euler);
            return go;
        }

        static void PaperToyWalls()
        {
            string[] tiles =
            {
                KayP + "red/platform_4x4x1_red.fbx",
                KayP + "blue/platform_4x4x1_blue.fbx",
                KayP + "green/platform_4x4x1_green.fbx",
                KayP + "yellow/platform_4x4x1_yellow.fbx"
            };
            const float back = 4.72f;
            const float side = 9.42f;
            for (int story = 0; story < 3; story++)
            {
                float y = 2f + story * 4f;
                for (int i = -2; i <= 2; i++)
                {
                    float along = i * 4f;
                    PlaceEuler(tiles[Wrap(i + story, tiles.Length)], new Vector3(along, y, back), new Vector3(90f, 0f, 0f), 1f);
                    PlaceEuler(tiles[Wrap(i + story + 1, tiles.Length)], new Vector3(-side, y, along), new Vector3(0f, 0f, -90f), 1f);
                    PlaceEuler(tiles[Wrap(i + story + 2, tiles.Length)], new Vector3(side, y, along), new Vector3(0f, 0f, 90f), 1f);
                }
            }
        }

        static void DressToyWalls()
        {
            const float back = 4.78f;
            Place(KayP + "red/platform_decorative_2x2x2_red.fbx", new Vector3(-7.8f, 4.4f, back), 0f, 1f);
            Place(KayP + "blue/platform_decorative_2x2x2_blue.fbx", new Vector3(7.8f, 4.2f, back), 0f, 1f);
            Place(KayP + "green/platform_decorative_2x2x2_green.fbx", new Vector3(-5.0f, 6.6f, back), 0f, 1f);
            Place(KayP + "yellow/platform_decorative_2x2x2_yellow.fbx", new Vector3(5.0f, 6.4f, back), 0f, 1f);
            Place(KayP + "yellow/flag_B_yellow.fbx", new Vector3(-8.6f, 0f, 4.55f), 0f, 1.9f, PlaceSit.Ground);
            Place(KayP + "blue/flag_B_blue.fbx", new Vector3(8.6f, 0f, 4.55f), 0f, 1.9f, PlaceSit.Ground);
            Place(KayP + "red/flag_C_red.fbx", new Vector3(-8.4f, 0f, 3.7f), 0f, 1.7f, PlaceSit.Ground);
            Place(KayP + "green/flag_C_green.fbx", new Vector3(8.4f, 0f, 3.7f), 0f, 1.7f, PlaceSit.Ground);
            Place(KayP + "red/hoop_red.fbx", new Vector3(-7.2f, 3.6f, back), 0f, 1.45f);
            Place(KayP + "blue/hoop_blue.fbx", new Vector3(7.2f, 3.4f, back), 0f, 1.45f);
            Place(KayP + "green/hoop_green.fbx", new Vector3(0f, 6.8f, back), 0f, 1.5f);
            Place(KayP + "yellow/pipe_straight_A_yellow.fbx", new Vector3(-9.2f, 2.6f, 2.4f), 90f, 1.15f);
            Place(KayP + "green/pipe_straight_A_green.fbx", new Vector3(9.2f, 2.4f, 2.2f), 90f, 1.15f);
            Place(KayP + "red/pipe_90_B_red.fbx", new Vector3(-9.2f, 5.8f, 3.6f), 90f, 1.1f);
            Place(KayP + "blue/pipe_90_B_blue.fbx", new Vector3(9.2f, 5.6f, 3.4f), -90f, 1.1f);
            Place(KayP + "red/signage_arrow_wall_red.fbx", new Vector3(-8.4f, 2.8f, back), 0f, 1.6f);
            Place(KayP + "blue/signage_arrow_wall_blue.fbx", new Vector3(8.4f, 2.6f, back), 0f, 1.6f);
            Place(KayP + "yellow/signage_arrows_right_yellow.fbx", new Vector3(-6.4f, 1.8f, back), 0f, 1.45f);
            Place(KayP + "green/signage_arrows_left_green.fbx", new Vector3(6.4f, 1.6f, back), 0f, 1.45f);
            Place(KayP + "red/bracing_large_red.fbx", new Vector3(-9.2f, 0.2f, 4.4f), 90f, 1.15f);
            Place(KayP + "blue/bracing_large_blue.fbx", new Vector3(9.2f, 0.2f, 4.2f), -90f, 1.15f);
            Place(KayP + "yellow/bracing_medium_yellow.fbx", new Vector3(-9.2f, 3.8f, 1.4f), 90f, 1.1f);
            Place(KayP + "green/bracing_medium_green.fbx", new Vector3(9.2f, 3.6f, 1.2f), -90f, 1.1f);
            Place(KayP + "blue/diamond_blue.fbx", new Vector3(-8.4f, 6.6f, back), 0f, 1.1f);
            Place(KayP + "yellow/diamond_yellow.fbx", new Vector3(8.4f, 6.4f, back), 0f, 1.1f);
            Place(KayP + "green/diamond_green.fbx", new Vector3(-3.6f, 7.4f, back), 0f, 1.05f);
            Place(KayP + "red/diamond_red.fbx", new Vector3(3.6f, 7.2f, back), 0f, 1.05f);
            Place(KayP + "neutral/signage_finish_wide.fbx", new Vector3(0f, 3.6f, back), 0f, 1.35f);
            Place(KayP + "yellow/platform_decorative_1x1x1_yellow.fbx", new Vector3(-9.2f, 6.6f, 3.2f), 90f, 1.25f);
            Place(KayP + "red/platform_decorative_1x1x1_red.fbx", new Vector3(9.2f, 6.4f, 3.0f), -90f, 1.25f);
            Place(KayP + "blue/platform_decorative_2x2x2_blue.fbx", new Vector3(-9.2f, 1.5f, 3.6f), 90f, 1f);
            Place(KayP + "green/platform_decorative_2x2x2_green.fbx", new Vector3(9.2f, 1.3f, 3.4f), -90f, 1f);
            Place(KayP + "red/flag_A_red.fbx", new Vector3(-9.15f, 0f, 4.8f), 90f, 1.85f, PlaceSit.Ground);
            Place(KayP + "yellow/flag_A_yellow.fbx", new Vector3(9.15f, 0f, 4.6f), -90f, 1.85f, PlaceSit.Ground);
            Place(KayP + "yellow/hoop_yellow.fbx", new Vector3(-9.15f, 4.8f, 2.8f), 90f, 1.4f);
            Place(KayP + "red/hoop_angled_red.fbx", new Vector3(9.15f, 4.6f, 2.6f), -90f, 1.4f);
            Place(KayP + "green/signage_arrow_wall_green.fbx", new Vector3(-9.18f, 2.8f, 3.8f), 90f, 1.5f);
            Place(KayP + "yellow/signage_arrow_wall_yellow.fbx", new Vector3(9.18f, 2.6f, 3.6f), -90f, 1.5f);
            Place(KayP + "blue/platform_4x2x2_blue.fbx", new Vector3(-6.6f, 2.15f, back), 0f, 1f);
            Place(KayP + "red/platform_4x2x2_red.fbx", new Vector3(6.6f, 4.15f, back), 0f, 1f);
            Place(KayP + "yellow/platform_4x2x2_yellow.fbx", new Vector3(0f, 7.7f, back), 0f, 1f);
        }

        static void BuildHexVillage(System.Random rng)
        {
            TileFloors(KayHex + "tiles/roads/hex_road_C.fbx", KayHex + "tiles/roads/hex_road_H.fbx", 4f, 2, 2.2f);
            EncloseTower(Modular + "template-wall.fbx", 4f, 3, 1f, 4.15f);
            Place(KayHex + "buildings/red/building_castle_red.fbx", new Vector3(0f, 0f, 9.4f), 180f, 4.6f);
            Place(KayHex + "buildings/blue/building_tavern_blue.fbx", new Vector3(-7.6f, 0f, 7.6f), 25f, 6.8f);
            Place(KayHex + "buildings/yellow/building_tavern_yellow.fbx", new Vector3(7.8f, 0f, 7.4f), -20f, 6.8f);
            Place(KayHex + "buildings/green/building_windmill_green.fbx", new Vector3(-9.0f, 0f, 2.6f), 90f, 6.2f);
            Place(KayHex + "buildings/blue/building_church_blue.fbx", new Vector3(8.8f, 0f, 2.4f), -90f, 6.2f);
            Place(KayHex + "buildings/yellow/building_blacksmith_yellow.fbx", new Vector3(-7.4f, 0f, -2.2f), 12f, 6.4f);
            Place(KayHex + "buildings/green/building_well_green.fbx", new Vector3(6.6f, 0f, -2.0f), 0f, 5.8f);
            Place(KayHex + "buildings/neutral/fence_stone_straight_gate.fbx", new Vector3(0f, 0f, 9.0f), 0f, 3.6f);
            Place(KayHex + "buildings/neutral/wall_straight.fbx", new Vector3(-8.6f, 0f, 5.2f), 90f, 3.2f);
            Place(KayHex + "buildings/neutral/wall_straight.fbx", new Vector3(8.6f, 0f, 5.0f), 90f, 3.2f);
            Place(Mini + "banner.fbx", new Vector3(-3.0f, 0f, 8.4f), 0f, 4f);
            Place(Mini + "banner.fbx", new Vector3(3.0f, 0f, 8.4f), 0f, 4f);
            ScatterEdge(rng, Mini + "barrel.fbx", 2.6f, 2);
        }

        static void BuildKayDungeon(System.Random rng)
        {
            TileFloors(KayD + "floor_tile_large.fbx", KayD + "floor_tile_big_grate.fbx", 4f, 2, 1f);
            EncloseTower(KayD + "wall.fbx", 4f, 3, 1f, 4f);
            Place(KayD + "rubble_large.fbx", new Vector3(6.8f, 0f, 5.8f), -90f, 1.1f);
            Place(KayD + "wall_gated.fbx", new Vector3(0f, 0f, 9.6f), 0f, 1.2f);
            Place(KayD + "banner_green.fbx", new Vector3(-3.4f, 1.2f, 9.4f), 0f, 1.3f);
            Place(KayD + "banner_patternB_red.fbx", new Vector3(3.4f, 1.2f, 9.4f), 0f, 1.3f);
            Place(KayD + "pillar_decorated.fbx", new Vector3(-5.4f, 0f, 7.2f), 0f, 1.15f);
            Place(KayD + "pillar_decorated.fbx", new Vector3(5.4f, 0f, 7.2f), 0f, 1.15f);
            Place(KayD + "table_medium_broken.fbx", new Vector3(-6.6f, 0f, 4.4f), 20f, 1.1f);
            Place(KayD + "bed_floor.fbx", new Vector3(6.8f, 0f, 4.2f), -90f, 1.2f);
            Place(KayD + "crates_stacked.fbx", new Vector3(-7.0f, 0f, 1.4f), 15f, 1.15f);
            Place(KayD + "barrier.fbx", new Vector3(6.6f, 0f, 1.2f), 0f, 1.1f);
            Place(KayD + "rubble_large.fbx", new Vector3(-6.4f, 0f, -2.4f), 40f, 1.2f);
            Place(KayD + "barrel_large_decorated.fbx", new Vector3(6.2f, 0f, -2.2f), -10f, 1f);
            Place(KayD + "torch_mounted.fbx", new Vector3(-4.6f, 2.2f, 9.0f), 0f, 1.6f);
            Place(KayD + "torch_mounted.fbx", new Vector3(4.6f, 2.2f, 9.0f), 0f, 1.6f);
            Place(KayD + "torch_lit.fbx", new Vector3(-5.2f, 0f, 5.6f), 0f, 1.4f);
            ScatterEdge(rng, KayD + "trunk_small_A.fbx", 1.3f, 2);
            AddPointLight(new Vector3(0f, 2.6f, 5f), new Color(1f, 0.5f, 0.16f), 2.6f, 12f, false);
        }

        static void BuildTrailerYard(System.Random rng)
        {
            Place(KayF + "Tree_1_B_Color1.fbx", new Vector3(-7.6f, 0f, 7.6f), 20f, 1.25f);
            Place(KayF + "Tree_3_A_Color1.fbx", new Vector3(7.4f, 0f, 7.4f), -12f, 1.25f);
            Place(KayF + "Tree_4_A_Color1.fbx", new Vector3(-8.2f, 0f, 1.6f), 40f, 1.15f);
            Place(KayF + "Tree_2_D_Color1.fbx", new Vector3(8.0f, 0f, 1.2f), -30f, 1.15f);
            PlaceNamed(Trailer, "Fence", new Vector3(0f, 0f, 9.4f), 0f, 1.35f);
            PlaceNamed(Trailer, "lamppost", new Vector3(-5.4f, 0f, 6.4f), 0f, 1.05f);
            PlaceNamed(Trailer, "lamppost", new Vector3(5.6f, 0f, 6.2f), 15f, 1.05f);
            PlaceNamed(Trailer, "Couch", new Vector3(-6.8f, 0f, -2.6f), 25f, 2.2f);
            PlaceNamed(Trailer, "Armchair", new Vector3(-5.4f, 0f, -3.2f), 40f, 2.3f);
            PlaceNamed(Trailer, "TV", new Vector3(6.2f, 0f, -2.4f), -25f, 2.2f);
            PlaceNamed(Trailer, "Refrigerator", new Vector3(7.2f, 0f, 3.6f), -80f, 1.9f);
            PlaceNamed(Trailer, "Table", new Vector3(-6.4f, 0f, 3.8f), 15f, 2.0f);
            PlaceNamed(Trailer, "Chair", new Vector3(-5.2f, 0f, 4.4f), 40f, 2.0f);
            PlaceNamed(Trailer, "trash_can_metal", new Vector3(-5.8f, 0f, 2.2f), 0f, 2.2f);
            PlaceNamed(Trailer, "Garbage_bag", new Vector3(5.4f, 0f, 4.0f), 30f, 2.4f);
            PlaceNamed(Trailer, "Bed", new Vector3(7.2f, 0f, 0.4f), 70f, 1.8f);
            PlaceNamed(Trailer, "Plants_01", new Vector3(-6.2f, 0f, 0.6f), 0f, 1.6f);
            PlaceNamed(Trailer, "Flowers", new Vector3(5.6f, 0f, 1.6f), 0f, 1.8f);
            ScatterEdge(rng, Mini + "barrel.fbx", 2.4f, 2);
            EncloseTower(Modular + "template-wall.fbx", 4f, 3, 1f, 4.15f);
            AddPointLight(new Vector3(-5.4f, 3.6f, 6.4f), new Color(1f, 0.78f, 0.35f), 2.2f, 11f, false);
        }

        static void BuildArcade(System.Random rng)
        {
            TileFloors(Arcade + "floor.fbx", Arcade + "floor.fbx", 4f, 2, 4f);
            EncloseTower(Arcade + "wall.fbx", 4f, 3, 4f, 3.95f);
            string[] cabs =
            {
                Arcade + "arcade-machine.fbx", Arcade + "pinball.fbx", Arcade + "claw-machine.fbx",
                Arcade + "dance-machine.fbx", Arcade + "air-hockey.fbx", Arcade + "gambling-machine.fbx",
                Arcade + "basketball-game.fbx", Arcade + "vending-machine.fbx"
            };
            Vector3[] spots =
            {
                new Vector3(-6.4f, 0f, 7.2f), new Vector3(-7.2f, 0f, 4.6f), new Vector3(-6.8f, 0f, 1.6f),
                new Vector3(-6.2f, 0f, -1.6f), new Vector3(6.4f, 0f, 7.4f), new Vector3(7.0f, 0f, 4.4f),
                new Vector3(6.6f, 0f, 1.4f), new Vector3(6.2f, 0f, -1.4f)
            };
            for (int i = 0; i < spots.Length; i++)
                Place(cabs[i % cabs.Length], spots[i], i < 4 ? 90f : -90f, 2.7f);
            Place(Arcade + "column.fbx", new Vector3(-5.4f, 0f, 8.4f), 0f, 3.4f);
            Place(Arcade + "column.fbx", new Vector3(5.4f, 0f, 8.4f), 0f, 3.4f);
            Place(Arcade + "prize-wheel.fbx", new Vector3(0f, 0f, 9.0f), 180f, 2.6f);
            Place(Arcade + "ticket-machine.fbx", new Vector3(-3.4f, 0f, 8.4f), 180f, 2.4f);
            Place(Arcade + "prizes.fbx", new Vector3(3.4f, 0f, 8.4f), 180f, 2.4f);
            Place(KayP + "yellow/flag_A_yellow.fbx", new Vector3(-4.2f, 0f, 8.8f), 0f, 2f);
            Place(KayP + "red/flag_A_red.fbx", new Vector3(4.2f, 0f, 8.8f), 0f, 2f);
            Place(KayP + "blue/pipe_straight_A_blue.fbx", new Vector3(-8.0f, 2.2f, 8.2f), 0f, 1.1f);
            Place(KayP + "red/pipe_straight_A_red.fbx", new Vector3(8.0f, 2.2f, 8.2f), 0f, 1.1f);
            AddPointLight(new Vector3(-4f, 2.4f, 3f), new Color(0.95f, 0.15f, 1f), 2.6f, 10f, false);
            AddPointLight(new Vector3(4f, 2.4f, 3f), new Color(0.15f, 0.75f, 1f), 2.4f, 10f, false);
        }

        static void BuildSummit(System.Random rng)
        {
            TileFloors(Modular + "template-floor-detail.fbx", KayD + "floor_tile_large_rocks.fbx", 4f, 2, 1f);
            EncloseTower(KayD + "wall.fbx", 4f, 3, 1f, 4f);
            Place(KayH + "crypt.fbx", new Vector3(0f, 0f, 9.2f), 180f, 0.85f);
            Place(KayD + "wall_gated.fbx", new Vector3(0f, 0f, 9.2f), 0f, 1.3f);
            Place(KayH + "shrine_candles.fbx", new Vector3(-6.2f, 0f, 6.6f), 0f, 2.3f);
            Place(KayH + "shrine_candles.fbx", new Vector3(6.2f, 0f, 6.6f), 0f, 2.3f);
            Place(KayH + "shrine.fbx", new Vector3(0f, 0f, 7.6f), 180f, 1.8f);
            Place(Mini + "banner.fbx", new Vector3(-3.4f, 0f, 8.6f), 0f, 5.2f);
            Place(Mini + "banner.fbx", new Vector3(3.4f, 0f, 8.6f), 0f, 5.2f);
            Place(KayP + "neutral/pillar_1x1x8.fbx", new Vector3(-5.6f, 0f, 7.8f), 0f, 1.15f);
            Place(KayP + "neutral/pillar_1x1x8.fbx", new Vector3(5.6f, 0f, 7.8f), 0f, 1.15f);
            Place(Mini + "column.fbx", new Vector3(-8.2f, 0f, 3.2f), 0f, 4.4f);
            Place(Mini + "column.fbx", new Vector3(8.2f, 0f, 3.0f), 0f, 4.4f);
            ScatterEdge(rng, Mini + "chest.fbx", 2.8f, 2);
            AddPointLight(new Vector3(0f, 3.4f, 4f), new Color(1f, 0.45f, 0.28f), 2.8f, 14f, false);
        }

        static void DressBoss(System.Random rng, int stage)
        {
            switch (stage)
            {
                case 0:
                    Place(Mini + "column.fbx", new Vector3(-4.8f, 0f, 6.2f), 0f, 5.2f);
                    Place(Mini + "column.fbx", new Vector3(4.8f, 0f, 6.2f), 0f, 5.2f);
                    Place(Mini + "banner.fbx", new Vector3(-2.4f, 0f, 8.6f), 0f, 5.4f);
                    Place(Mini + "banner.fbx", new Vector3(2.4f, 0f, 8.6f), 0f, 5.4f);
                    Place(Modular + "gate-door.fbx", new Vector3(0f, 0f, 8.8f), 0f, 1.25f);
                    break;
                case 1:
                    Place(KayC + "car_police.fbx", new Vector3(-8.6f, 0.02f, 8.2f), 10f, 4.2f, PlaceSit.Ground);
                    Place(KayC + "car_sedan.fbx", new Vector3(8.6f, 0.02f, 8.4f), 170f, 4.2f, PlaceSit.Ground);
                    AddPointLight(new Vector3(-8.0f, 2.2f, 8.0f), new Color(1f, 0.12f, 0.12f), 3.2f, 8f, true);
                    AddPointLight(new Vector3(8.0f, 2.2f, 8.2f), new Color(0.2f, 0.35f, 1f), 2.8f, 8f, true);
                    break;
                case 2:
                    Place(KayF + "Tree_Bare_1_B_Color1.fbx", new Vector3(-5.2f, 0f, 7.4f), 30f, 1.7f);
                    Place(KayF + "Tree_Bare_1_B_Color1.fbx", new Vector3(5.0f, 0f, 7.2f), -25f, 1.7f);
                    Place(KayF + "Rock_1_A_Color1.fbx", new Vector3(0f, 0f, 8.4f), 0f, 4.2f);
                    break;
                case 3:
                    Place(KayH + "coffin_decorated.fbx", new Vector3(-7.8f, 0f, 8.2f), 15f, 1.1f, PlaceSit.Ground);
                    Place(KayH + "skull.fbx", new Vector3(0f, 0f, 8.4f), 0f, 1.3f, PlaceSit.Ground);
                    Place(KayH + "skull_candle.fbx", new Vector3(7.4f, 0f, 8.0f), -10f, 1.5f, PlaceSit.Ground);
                    break;
                case 4:
                    Place(KayP + "red/bomb_A_red.fbx", new Vector3(-8.8f, 0f, 3.8f), 0f, 1.35f, PlaceSit.Ground);
                    Place(KayP + "blue/bomb_A_blue.fbx", new Vector3(8.8f, 0f, 3.6f), 0f, 1.35f, PlaceSit.Ground);
                    Place(KayP + "yellow/flag_A_yellow.fbx", new Vector3(-8.7f, 0f, 4.55f), 0f, 1.9f, PlaceSit.Ground);
                    Place(KayP + "red/flag_A_red.fbx", new Vector3(8.7f, 0f, 4.55f), 0f, 1.9f, PlaceSit.Ground);
                    break;
                case 5:
                    Place(KayHex + "buildings/neutral/projectile_catapult.fbx", new Vector3(0f, 0f, 8.6f), 180f, 5.2f);
                    Place(Mini + "banner.fbx", new Vector3(-2.6f, 0f, 8.2f), 0f, 4.8f);
                    Place(Mini + "banner.fbx", new Vector3(2.6f, 0f, 8.2f), 0f, 4.8f);
                    break;
                case 6:
                    Place(KayD + "floor_tile_big_spikes.fbx", new Vector3(-4.8f, 0.02f, 6.4f), 0f, 1.1f);
                    Place(KayD + "floor_tile_big_spikes.fbx", new Vector3(4.8f, 0.02f, 6.4f), 0f, 1.1f);
                    Place(KayD + "sword_shield_gold.fbx", new Vector3(0f, 0f, 8.2f), 0f, 1.6f);
                    Place(KayD + "torch_lit.fbx", new Vector3(-3.8f, 0f, 7.4f), 0f, 1.6f);
                    Place(KayD + "torch_lit.fbx", new Vector3(3.8f, 0f, 7.4f), 0f, 1.6f);
                    break;
                case 7:
                    PlaceNamed(Trailer, "Garbage_bag_01", new Vector3(-4.6f, 0f, 5.2f), 20f, 2.6f);
                    PlaceNamed(Trailer, "Garbage_bag_02", new Vector3(4.6f, 0f, 5.0f), -15f, 2.6f);
                    PlaceNamed(Trailer, "trash_can_metal", new Vector3(0f, 0f, 8.2f), 0f, 2.4f);
                    break;
                case 8:
                    Place(Arcade + "gambling-machine.fbx", new Vector3(0f, 0f, 7.6f), 180f, 3.1f);
                    Place(KayP + "blue/flag_A_blue.fbx", new Vector3(-2.6f, 0f, 8.6f), 0f, 2.4f);
                    Place(KayP + "red/flag_A_red.fbx", new Vector3(2.6f, 0f, 8.6f), 0f, 2.4f);
                    break;
                default:
                    Place(Mini + "banner.fbx", new Vector3(-2.6f, 0f, 8.4f), 0f, 5.6f);
                    Place(Mini + "banner.fbx", new Vector3(2.6f, 0f, 8.4f), 0f, 5.6f);
                    Place(KayH + "shrine_candles.fbx", new Vector3(0f, 0f, 6.8f), 180f, 2.6f);
                    Place(Modular + "gate-door.fbx", new Vector3(0f, 0f, 8.6f), 0f, 1.2f);
                    break;
            }

            var theme = DungeonRunner.GetTheme(DungeonRunner.StageBossFloor(stage), true);
            AddPointLight(new Vector3(0f, 4.2f, 2.4f), new Color(1f, 0.22f, 0.18f), 9.5f, 24f, true);
            AddPointLight(new Vector3(-4.4f, 2.6f, 5.2f), new Color(1f, 0.25f, 0.20f), 5.5f, 15f, true);
            AddPointLight(new Vector3(4.4f, 2.6f, 5.2f), new Color(0.95f, 0.18f, 0.35f), 4.2f, 14f, false);
            AddPointLight(new Vector3(0.2f, 2.4f, 7.2f), new Color(1f, 0.40f, 0.30f), 3.8f, 12f, false);
            BattleVfx.BossAmbience(room.transform, new Color(1f, 0.20f, 0.15f, 0.95f), new Color(theme.fogColor.r, theme.fogColor.g, theme.fogColor.b, 0.35f));
        }

        enum PlaceSit { None, Ground, FloorTop }

        static void TileFloors(string a, string b, float spacing, int radius, float meshScale, PlaceSit sit = PlaceSit.None)
        {
            for (int ix = -radius; ix <= radius; ix++)
            {
                for (int iz = -radius; iz <= radius; iz++)
                    Place(((ix + iz) & 1) == 0 ? a : b, new Vector3(ix * spacing, 0.005f, iz * spacing), 0f, meshScale, sit);
            }
        }

        static void BackWalls(string wall, float size, float z, float y)
        {
            for (int ix = -2; ix <= 2; ix++)
                Place(wall, new Vector3(ix * size, y, z), 0f, 1f);
        }

        static void LeftWalls(string wall, float size, float x, float y)
        {
            for (int iz = -2; iz <= 2; iz++)
                Place(wall, new Vector3(x, y, iz * size), 90f, 1f);
        }

        static void RightWalls(string wall, float size, float x, float y)
        {
            for (int iz = -2; iz <= 2; iz++)
                Place(wall, new Vector3(x, y, iz * size), -90f, 1f);
        }

        static void EncloseTower(string wall, float size, int stories, float wallScale, float storyHeight)
        {
            if (stories < 3) stories = 3;
            const float dist = 10f;
            float spacing = size * 0.97f;
            for (int story = 0; story < stories; story++)
            {
                float y = story * (storyHeight * 0.98f);
                for (int i = -3; i <= 3; i++)
                {
                    Place(wall, new Vector3(i * spacing, y, dist), 0f, wallScale);
                    Place(wall, new Vector3(-dist, y, i * spacing), 90f, wallScale);
                    Place(wall, new Vector3(dist, y, i * spacing), -90f, wallScale);
                }
            }
            CapCeiling(stories * storyHeight - 0.2f);
        }

        static void EncloseCity()
        {
            string[] street =
            {
                KayC + "building_H.fbx", KayC + "building_C.fbx", KayC + "building_D.fbx",
                KayC + "building_G.fbx", KayC + "building_E.fbx", KayC + "building_F.fbx",
                KayC + "building_A.fbx"
            };
            string[] sky =
            {
                City + "building-skyscraper-c.fbx", City + "building-skyscraper-a.fbx",
                City + "building-skyscraper-d.fbx", City + "building-skyscraper-e.fbx",
                City + "building-skyscraper-b.fbx"
            };

            const float dist = 10.6f;
            const float space = 5.15f;
            const float scale = 2.75f;
            for (int i = -3; i <= 3; i++)
            {
                Place(street[Wrap(i + 3, street.Length)], new Vector3(i * space, 0f, dist), 180f, scale);
                Place(street[Wrap(i + 5, street.Length)], new Vector3(-dist, 0f, i * space), 90f, scale);
                Place(street[Wrap(i + 1, street.Length)], new Vector3(dist, 0f, i * space), -90f, scale);
            }

            const float dist2 = 13.4f;
            const float skySpace = 7.0f;
            const float skyScale = 6.4f;
            for (int i = -2; i <= 2; i++)
            {
                Place(sky[Wrap(i + 2, sky.Length)], new Vector3(i * skySpace, 0f, dist2), 0f, skyScale);
                Place(sky[Wrap(i + 3, sky.Length)], new Vector3(-dist2, 0f, i * skySpace), 90f, skyScale);
                Place(sky[Wrap(i + 1, sky.Length)], new Vector3(dist2, 0f, i * skySpace), -90f, skyScale);
            }

            CapCeiling(14.5f);
        }

        static int Wrap(int i, int n)
        {
            int r = i % n;
            return r < 0 ? r + n : r;
        }

        static void CapCeiling(float y)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "TowerCeiling";
            go.transform.SetParent(room.transform, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.transform.localScale = new Vector3(24f, 0.5f, 24f);
            var col = go.GetComponent<Collider>();
            if (col != null) Drop(col);
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            var mat = new Material(shader);
            var colr = new Color(0.12f, 0.11f, 0.13f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", colr);
            rend.sharedMaterial = mat;
        }

        static void ScatterEdge(System.Random rng, string path, float scale, int count)
        {
            Vector3[] corners =
            {
                new Vector3(-8.8f, 0f, 8.6f), new Vector3(8.6f, 0f, 8.4f),
                new Vector3(-8.6f, 0f, 7.2f), new Vector3(8.4f, 0f, 7.0f),
            };
            int n = Mathf.Min(count, corners.Length);
            for (int i = 0; i < n; i++)
            {
                Vector3 jitter = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.8f, 0f, ((float)rng.NextDouble() - 0.5f) * 0.8f);
                Place(path, corners[rng.Next(corners.Length)] + jitter, rng.Next(360), scale, PlaceSit.Ground);
            }
        }

        static GameObject Place(string path, Vector3 localPos, float yaw, float scale, PlaceSit sit = PlaceSit.None)
        {
            return Place(path, localPos, yaw, Vector3.one * scale, sit);
        }

        static GameObject Place(string path, Vector3 localPos, float yaw, Vector3 scale, PlaceSit sit = PlaceSit.None)
        {
            var prefab = Load(path);
            if (prefab == null || room == null) return null;
            var go = Object.Instantiate(prefab, room.transform);
            go.name = prefab.name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = scale;
            Sanitize(go);
            UpgradeMaterials(go);
            SitPlaced(go, sit);
            return go;
        }

        static void SitPlaced(GameObject go, PlaceSit sit)
        {
            if (go == null || sit == PlaceSit.None) return;
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0) return;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null) b.Encapsulate(rends[i].bounds);
            }
            float targetY = go.transform.position.y;
            float dy = sit == PlaceSit.Ground ? (targetY - b.min.y) : (targetY - b.max.y);
            if (Mathf.Abs(dy) < 0.0005f) return;
            go.transform.position += new Vector3(0f, dy, 0f);
        }

        static GameObject PlaceNamed(string path, string childName, Vector3 localPos, float yaw, float scale)
        {
            var prefab = Load(path);
            if (prefab == null || room == null) return null;
            Transform src = null;
            var all = prefab.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == childName)
                {
                    src = all[i];
                    break;
                }
            }
            if (src == null) return null;

            var wrapper = new GameObject(childName);
            wrapper.transform.SetParent(room.transform, false);
            var go = Object.Instantiate(src.gameObject, wrapper.transform);
            go.name = childName + "_Mesh";
            go.transform.localScale = src.localScale;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            Sanitize(go);
            UpgradeMaterials(go);

            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                Vector3 origin = wrapper.transform.position;
                go.transform.position += new Vector3(origin.x - b.center.x, origin.y - b.min.y, origin.z - b.center.z);
            }

            wrapper.transform.localPosition = localPos;
            wrapper.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            wrapper.transform.localScale = Vector3.one * scale;
            return wrapper;
        }

        static void Drop(Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            Object.Destroy(obj);
        }

        static void Sanitize(GameObject go)
        {
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) Drop(cols[i]);
            var lights = go.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++) Drop(lights[i]);
            var cams = go.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cams.Length; i++) Drop(cams[i]);
            var anims = go.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < anims.Length; i++) anims[i].enabled = false;
        }

        static void AddPointLight(Vector3 localPos, Color color, float intensity, float range, bool pulse)
        {
            var go = new GameObject(pulse ? "BossPulseLight" : "FillLight");
            go.transform.SetParent(room.transform, false);
            go.transform.localPosition = localPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            if (!pulse) return;
            var p = go.AddComponent<DungeonRoomPulse>();
            p.baseIntensity = intensity;
            p.amp = intensity * 0.5f;
            p.speed = 2.8f;
        }

        static void AddMist(Color color, bool embers)
        {
            var go = new GameObject(embers ? "BossEmbers" : "BossMist");
            go.transform.SetParent(room.transform, false);
            go.transform.localPosition = embers ? new Vector3(0f, 0.2f, 2.4f) : new Vector3(0f, 0.45f, 1.6f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = embers ? 1.6f : 5.5f;
            main.startSpeed = embers ? 1.8f : 0.12f;
            main.startSize = embers ? 0.12f : 2.1f;
            main.startColor = color;
            main.maxParticles = embers ? 36 : 40;
            main.gravityModifier = embers ? -0.35f : 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.rateOverTime = embers ? 14f : 6f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = embers ? new Vector3(8f, 0.2f, 8f) : new Vector3(10f, 0.6f, 10f);
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                rend.material = mat;
            }
            ps.Play();
        }

        static void UpgradeMaterials(GameObject go)
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null) return;
            var rends = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                var srcs = rends[i].sharedMaterials;
                if (srcs == null || srcs.Length == 0) continue;
                var copies = new Material[srcs.Length];
                bool changed = false;
                for (int m = 0; m < srcs.Length; m++)
                {
                    var src = srcs[m];
                    if (src == null) continue;
                    if (src.shader != null && src.shader.name.IndexOf("Universal", System.StringComparison.Ordinal) >= 0)
                    {
                        copies[m] = src;
                        continue;
                    }
                    var copy = new Material(urp);
                    Texture tex = null;
                    if (src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                    if (tex == null && src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
                    Color col = Color.white;
                    if (src.HasProperty("_Color")) col = src.GetColor("_Color");
                    if (src.HasProperty("_BaseColor")) col = src.GetColor("_BaseColor");
                    if (copy.HasProperty("_BaseMap") && tex != null) copy.SetTexture("_BaseMap", tex);
                    if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", col);
                    bool cutout = src.shader != null && (
                        src.shader.name.IndexOf("Cutout", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || src.shader.name.IndexOf("Transparent", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || src.HasProperty("_Cutoff"));
#if UNITY_EDITOR
                    if (!cutout && tex != null && tex is Texture2D t2 && t2.alphaIsTransparency) cutout = true;
#endif
                    if (cutout)
                    {
                        if (copy.HasProperty("_AlphaClip")) copy.SetFloat("_AlphaClip", 1f);
                        if (copy.HasProperty("_Cutoff")) copy.SetFloat("_Cutoff", 0.35f);
                        copy.EnableKeyword("_ALPHATEST_ON");
                    }
                    copies[m] = copy;
                    changed = true;
                }
                if (changed) rends[i].sharedMaterials = copies;
            }
        }

        static GameObject Load(string path)
        {
            if (cache.TryGetValue(path, out var cached)) return cached;
            GameObject prefab = PlayerAssets.Load<GameObject>(path);
            cache[path] = prefab;
            return prefab;
        }
    }
}
