using System.Collections.Generic;
using UnityEngine;

namespace ShiftingMetropolis.App
{
    public class PlayerAssetTable : ScriptableObject
    {
        public string[] keys = System.Array.Empty<string>();
        public Object[] assets = System.Array.Empty<Object>();
        public AnimationClip[] clips = System.Array.Empty<AnimationClip>();
        public string[] extraSkinPaths = System.Array.Empty<string>();
        public string[] extraSkinLabels = System.Array.Empty<string>();
        public int[] extraSkinKinds = System.Array.Empty<int>();

        static PlayerAssetTable cached;
        Dictionary<string, int> byPath;
        Dictionary<string, int> byFile;

        public static PlayerAssetTable Load()
        {
            if (cached != null) return cached;
            cached = Resources.Load<PlayerAssetTable>("PlayerAssetTable");
            return cached;
        }

        public static T Find<T>(string path) where T : Object
        {
            var table = Load();
            if (table == null || string.IsNullOrEmpty(path)) return null;
            table.Ensure();
            if (table.byPath != null && table.byPath.TryGetValue(path, out int index))
                return table.Pick<T>(index);
            string file = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(file) && table.byFile != null && table.byFile.TryGetValue(file, out index))
                return table.Pick<T>(index);
            return null;
        }

        void Ensure()
        {
            if (byPath != null) return;
            byPath = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            byFile = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            if (keys == null) return;
            for (int i = 0; i < keys.Length; i++)
            {
                if (string.IsNullOrEmpty(keys[i])) continue;
                byPath[keys[i]] = i;
                string file = System.IO.Path.GetFileName(keys[i]);
                if (!string.IsNullOrEmpty(file) && !byFile.ContainsKey(file))
                    byFile[file] = i;
            }
        }

        T Pick<T>(int index) where T : Object
        {
            Object asset = assets != null && index < assets.Length ? assets[index] : null;
            AnimationClip clip = clips != null && index < clips.Length ? clips[index] : null;
            if (typeof(T) == typeof(AnimationClip))
                return (clip as T) ?? (asset as T);
            return (asset as T) ?? (clip as T);
        }
    }

    public static class PlayerAssets
    {
        public static T Load<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path)) return null;
#if UNITY_EDITOR
            var editor = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (editor != null) return editor;
#endif
            return PlayerAssetTable.Find<T>(path);
        }

        public static AnimationClip LoadClip(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
#if UNITY_EDITOR
            var direct = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (direct != null && !direct.name.StartsWith("__preview")) return direct;
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            AnimationClip clip = null;
            for (int i = 0; i < assets.Length; i++)
            {
                var c = assets[i] as AnimationClip;
                if (c != null && !c.name.StartsWith("__preview")) clip = c;
            }
            if (clip != null) return clip;
#endif
            return PlayerAssetTable.Find<AnimationClip>(path);
        }
    }
}
