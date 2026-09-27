using UnityEngine;
using UnityEngine.UI;
using ShiftingMetropolis.Progress;

namespace ShiftingMetropolis.App
{
    public class HomeScreen : MonoBehaviour
    {
        Text bpText;
        Text todayText;
        Text sessionText;
        Text statusText;
        Button startButton;
        Text startButtonLabel;
        readonly Button[] subjectButtons = new Button[6];
        readonly Text[] subjectLabels = new Text[6];

        void Start()
        {
            bpText = FindText("HomeBpText");
            todayText = FindText("HomeTodayText");
            sessionText = FindText("HomeSessionText");
            statusText = FindText("HomeStatusText");
            startButton = FindButton("HomeStartButton");
            if (startButton != null)
            {
                startButtonLabel = startButton.GetComponentInChildren<Text>();
                startButton.onClick.AddListener(OnToggleTimer);
            }

            var battleButton = FindButton("HomeBattleButton");
            if (battleButton != null)
            {
                battleButton.onClick.AddListener(() =>
                {
                    if (AppFlow.Instance != null) AppFlow.Instance.TryShowBattle();
                });
            }

            for (int i = 0; i < 6; i++)
            {
                var subject = (StudySubject)i;
                var button = FindButton("HomeSubject_" + i);
                subjectButtons[i] = button;
                if (button == null) continue;
                subjectLabels[i] = button.GetComponentInChildren<Text>();
                button.onClick.AddListener(() => OnSelectSubject(subject));
            }

            Refresh();
        }

        void Update()
        {
            Refresh();
        }

        void OnSelectSubject(StudySubject subject)
        {
            if (StudyTimer.Instance != null) StudyTimer.Instance.SetSubject(subject);
            Refresh();
        }

        void OnToggleTimer()
        {
            if (StudyTimer.Instance != null) StudyTimer.Instance.Toggle();
            Refresh();
        }

        void Refresh()
        {
            var timer = StudyTimer.Instance;
            if (timer == null) return;

            if (bpText != null) bpText.text = "BP " + PlayerProgress.TotalBp;
            if (todayText != null) todayText.text = "今日 " + PlayerProgress.TodayMinutes + "分";
            if (sessionText != null) sessionText.text = StudyTimer.FormatTime(timer.SessionSeconds);

            if (statusText != null)
            {
                string subject = StudySubjectNames.Display(timer.CurrentSubject);
                statusText.text = timer.IsRunning
                    ? subject + " 計測中"
                    : subject + " を選択中";
            }

            if (startButtonLabel != null)
            {
                startButtonLabel.text = timer.IsRunning ? "ストップ" : "スタート";
            }

            if (startButton != null)
            {
                var img = startButton.GetComponent<Image>();
                if (img != null)
                {
                    if (timer.IsRunning)
                    {
                        RpgTheme.PaintButton(img, RpgTheme.ButtonFill);
                        RpgTheme.AddZhuAccent(startButton.transform);
                    }
                    else RpgTheme.PaintButton(img, RpgTheme.Gold);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                var button = subjectButtons[i];
                if (button == null) continue;
                bool selected = (int)timer.CurrentSubject == i;
                var subject = (StudySubject)i;
                Color sub = StudySubjectColors.Color(subject);
                var img = button.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    img.color = selected ? sub : sub * 0.35f;
                }
                if (subjectLabels[i] != null)
                {
                    subjectLabels[i].text = StudySubjectNames.Display(subject)
                        + "\n" + PlayerProgress.MinutesFor(subject) + "分";
                    RpgTheme.StyleLabel(subjectLabels[i], Color.white);
                }
            }
        }

        static Text FindText(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Text>() : null;
        }

        static Button FindButton(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }
    }
}
