using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShiftingMetropolis.EditorTools
{
    /// <summary>
    /// 一時的なワンショット修正スクリプト。Assets/anime 内の全アニメ専用fbxに対して
    /// 単一の基準Avatar(Kicking.fbxから生成)をコピーして使わせることで、
    /// キャラごとに異なるAvatar生成による「地面にめり込む」「向きがおかしい」を解消する。
    /// 実行後は削除してよい。
    /// </summary>
    public static class OneShotFixAnimeAvatars
    {
        const string MasterPath = "Assets/anime/Mutant@Kicking.fbx";

        [MenuItem("Tools/OneShot/Fix Anime Avatars (Unify + Bake Root)")]
        public static void Run()
        {
            var masterImporter = AssetImporter.GetAtPath(MasterPath) as ModelImporter;
            if (masterImporter == null)
            {
                Debug.LogError("master importer not found: " + MasterPath);
                return;
            }
            // マスター側はそれ自身のモデルから作成し、Root位置/回転を焼き込む
            masterImporter.animationType = ModelImporterAnimationType.Human;
            masterImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            BakeRoot(masterImporter);
            masterImporter.SaveAndReimport();

            var masterAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(MasterPath);
            if (masterAvatar == null)
            {
                Debug.LogError("master avatar failed to load after reimport: " + MasterPath);
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/anime" });
            int ok = 0, skip = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == MasterPath) { skip++; continue; }
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) { skip++; continue; }
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = masterAvatar;
                BakeRoot(importer);
                importer.SaveAndReimport();
                ok++;
            }
            Debug.Log("OneShotFixAnimeAvatars done. ok=" + ok + " skip=" + skip);
        }

        static void BakeRoot(ModelImporter importer)
        {
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
                clips[i].lockRootPositionXZ = true;
                clips[i].keepOriginalOrientation = false;
                clips[i].keepOriginalPositionY = false;
                clips[i].keepOriginalPositionXZ = false;
                clips[i].heightFromFeet = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
