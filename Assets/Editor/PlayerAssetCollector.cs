using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ShiftingMetropolis.App;

namespace ShiftingMetropolis.EditorTools
{
    public class PlayerAssetCollector : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Collect();
        }

        [MenuItem("Tools/Collect Player Assets")]
        public static void Collect()
        {
            var script = new System.Text.StringBuilder();
            foreach (var file in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
                script.Append(File.ReadAllText(file));
            string blob = script.ToString();

            bag = new List<string>();
            bagSeen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var paths = bag;

            foreach (Match m in Regex.Matches(blob, "\"(Assets/[^\"]+)\""))
                Bag(m.Groups[1].Value);

            AddMentioned("Assets/anime", blob, true);
            string[] folders =
            {
                "Assets/back",
                "Assets/DoubleL/Demo",
                "Assets/Kevin Iglesias/Human Animations/Animations",
                "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs",
                "Assets/Polytope Studio/Lowpoly_Characters/Prefabs",
                "Assets/hotondo"
            };
            for (int i = 0; i < folders.Length; i++)
                AddMentioned(folders[i], blob, false);

            var keys = new List<string>();
            var assets = new List<Object>();
            var clips = new List<AnimationClip>();
            var skinPaths = new List<string>();
            var skinLabels = new List<string>();
            var skinKinds = new List<int>();

            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                var main = AssetDatabase.LoadMainAssetAtPath(path);
                if (main == null) continue;
                AnimationClip clip = null;
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int a = 0; a < all.Length; a++)
                {
                    var c = all[a] as AnimationClip;
                    if (c != null && !c.name.StartsWith("__preview")) { clip = c; break; }
                }
                keys.Add(path);
                assets.Add(main);
                clips.Add(clip);

                if (!IsPlayableSkin(path)) continue;
                string file = Path.GetFileNameWithoutExtension(path);
                skinPaths.Add(path);
                skinLabels.Add(file);
                skinKinds.Add(SkinKindFor(file));
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            var table = AssetDatabase.LoadAssetAtPath<PlayerAssetTable>("Assets/Resources/PlayerAssetTable.asset");
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<PlayerAssetTable>();
                AssetDatabase.CreateAsset(table, "Assets/Resources/PlayerAssetTable.asset");
            }
            table.keys = keys.ToArray();
            table.assets = assets.ToArray();
            table.clips = clips.ToArray();
            table.extraSkinPaths = skinPaths.ToArray();
            table.extraSkinLabels = skinLabels.ToArray();
            table.extraSkinKinds = skinKinds.ToArray();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log("Player assets collected: " + keys.Count + "  skins " + skinPaths.Count);
        }

        static void AddMentioned(string folder, string blob, bool everything)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            var guids = AssetDatabase.FindAssets("", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (!everything && !Mentioned(blob, path) && !IsPlayableSkin(path)) continue;
                // local function isn't visible; call through a captured list by re-parsing in Collect.
                // This method is nested? No. Use a static bag.
                Bag(path);
            }
        }

        static List<string> bag;
        static HashSet<string> bagSeen;

        static void Bag(string path)
        {
            if (bag == null) return;
            if (string.IsNullOrEmpty(path)) return;
            path = path.Replace('\\', '/');
            if (!bagSeen.Add(path)) return;
            if (AssetDatabase.IsValidFolder(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) == null) return;
            bag.Add(path);
        }

        static bool Mentioned(string blob, string path)
        {
            string file = Path.GetFileName(path);
            if (string.IsNullOrEmpty(file) || file.Length < 5) return false;
            if (blob.IndexOf(file, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string stem = Path.GetFileNameWithoutExtension(path);
            return stem.Length >= 8 && blob.IndexOf(stem, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsPlayableSkin(string path)
        {
            if (path == null || !path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) return false;
            if (path.IndexOf("/No-Rig/", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf("Extras_Rig", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf("/Extras/", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf("/Modular Parts/", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf("/knight/", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf("weaponsassetspack", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            string file = Path.GetFileNameWithoutExtension(path);
            if (file.StartsWith("Character_Killer", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (file.StartsWith("Character_Monster", System.StringComparison.OrdinalIgnoreCase)) return true;
            string[] keep =
            {
                "SHOGUN_Lowpoly", "DRIZZLE_Lowpoly", "GEKKOU_lowpoly", "KASA_Lowpoly", "KURENAI_lowpoly", "SAMIDALE_lowpoly",
                "Character_01", "Character_06", "Character_11", "Character_16", "Character_32",
                "Character_Female_02", "Character_Female_07", "Character_Female_12", "Character_Female_16", "Character_33_Female",
                "Character_Male_33", "Character_Male_34", "Character_Male_35", "Character_Male_36", "Character_Male_37",
                "Character_Male_38", "Character_Male_39", "Character_Male_40", "Character_Male_41", "Character_Male_42",
                "Character_Male_43", "Character_Male_44", "Character_Male_45",
                "Char_Ronin_01", "Modular_Warrior"
            };
            for (int i = 0; i < keep.Length; i++)
            {
                if (string.Equals(file, keep[i], System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static int SkinKindFor(string file)
        {
            if (file.IndexOf("SHOGUN", System.StringComparison.OrdinalIgnoreCase) >= 0
                || file.IndexOf("DRIZZLE", System.StringComparison.OrdinalIgnoreCase) >= 0
                || file.IndexOf("GEKKOU", System.StringComparison.OrdinalIgnoreCase) >= 0
                || file.IndexOf("KASA", System.StringComparison.OrdinalIgnoreCase) >= 0
                || file.IndexOf("KURENAI", System.StringComparison.OrdinalIgnoreCase) >= 0
                || file.IndexOf("SAMIDALE", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return 6;
            if (string.Equals(file, "Modular_Warrior", System.StringComparison.OrdinalIgnoreCase)) return 5;
            return 4;
        }
    }
}
