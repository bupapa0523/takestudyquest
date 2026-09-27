using ShiftingMetropolis.Progress;
using ShiftingMetropolis.Battle;
using UnityEngine;

namespace ShiftingMetropolis.App
{
    public class AppFlow : MonoBehaviour
    {
        public static AppFlow Instance { get; private set; }

        GameObject homeCanvas;
        GameObject battleRoot;

        void Awake()
        {
            Instance = this;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t.name == "HomeCanvas") homeCanvas = t.gameObject;
                if (t.name == "BattleRoot") battleRoot = t.gameObject;
            }
            ShowHome();
        }

        public void ShowHome()
        {
            if (homeCanvas != null) homeCanvas.SetActive(true);
            if (battleRoot != null) battleRoot.SetActive(false);
            ShiftingMetropolis.Battle.BattleStage.Clear();
            if (homeCanvas != null)
            {
                var ui = homeCanvas.GetComponentInChildren<StudyAppUI>(true);
                if (ui != null)
                {
                    ui.RefreshSortieIfActive();
                    ui.RefreshRaidIfOpen();
                }
            }
        }

        public bool TryShowBattle()
        {
            if (StudyStore.HasPausedBattle())
            {
                ShowBattle();
                return true;
            }
            if (!StudyStore.CanEnterBattle()) return false;
            if (!StudyStore.TrySpendForBattle()) return false;
            ShowBattle();
            return true;
        }

        public void ShowBattle()
        {
            BattleStage.RaidMode = false;
            BattleManager.MarkSessionEntered();
            if (homeCanvas != null) homeCanvas.SetActive(false);
            if (battleRoot != null) battleRoot.SetActive(true);
        }

        public void ShowRaidBattle()
        {
            BattleStage.RaidMode = true;
            BattleManager.MarkSessionEntered();
            if (homeCanvas != null) homeCanvas.SetActive(false);
            if (battleRoot != null) battleRoot.SetActive(true);
        }
    }
}
