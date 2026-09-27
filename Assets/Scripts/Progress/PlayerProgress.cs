using System;
using UnityEngine;

namespace ShiftingMetropolis.Progress
{
    /// <summary>
    /// 今日の分の内訳。累計BPは StudyStore が正とする。
    /// </summary>
    public static class PlayerProgress
    {
        const string KeyTodayDate = "sm_today_date";
        const string KeyTodayMinutes = "sm_today_minutes";

        public static int TotalBp => StudyStore.CurrentBp;
        public static int TodayMinutes { get; private set; }

        static readonly int[] TodayBySubject = new int[6];
        static bool loaded;

        public static void Load()
        {
            StudyStore.Load();
            string savedDate = PlayerPrefs.GetString(KeyTodayDate, string.Empty);
            string today = DateKey(StudyStore.Now);
            loaded = true;
            if (savedDate != today)
            {
                TodayMinutes = StudyStore.MinutesOnDate(StudyStore.Now);
                for (int i = 0; i < TodayBySubject.Length; i++) TodayBySubject[i] = 0;
                var logs = StudyStore.AllLogs;
                for (int i = 0; i < logs.Count; i++)
                {
                    DateTime dt;
                    if (!DateTime.TryParse(logs[i].startedAt, out dt)) continue;
                    if (dt.Date != StudyStore.Now.Date) continue;
                    int s = logs[i].subject;
                    if (s >= 0 && s < 6) TodayBySubject[s] += logs[i].minutes;
                }
                PlayerPrefs.SetString(KeyTodayDate, today);
                PlayerPrefs.SetInt(KeyTodayMinutes, TodayMinutes);
                SaveTodayBreakdown();
                PlayerPrefs.Save();
            }
            else
            {
                TodayMinutes = PlayerPrefs.GetInt(KeyTodayMinutes, StudyStore.MinutesOnDate(StudyStore.Now));
                for (int i = 0; i < TodayBySubject.Length; i++)
                {
                    TodayBySubject[i] = PlayerPrefs.GetInt(SubjectKey((StudySubject)i), 0);
                }
            }
        }

        public static int MinutesFor(StudySubject subject)
        {
            LoadIfNeeded();
            return TodayBySubject[(int)subject];
        }

        public static void AddStudyMinute(StudySubject subject)
        {
            AddStudyMinutes(subject, 1);
        }

        public static void AddStudyMinutes(StudySubject subject, int minutes)
        {
            if (minutes == 0) return;
            LoadIfNeeded();
            TodayMinutes = Mathf.Max(0, TodayMinutes + minutes);
            int subjectIndex = (int)subject;
            if (subjectIndex >= 0 && subjectIndex < TodayBySubject.Length)
                TodayBySubject[subjectIndex] = Mathf.Max(0, TodayBySubject[subjectIndex] + minutes);
            PlayerPrefs.SetInt(KeyTodayMinutes, TodayMinutes);
            PlayerPrefs.SetString(KeyTodayDate, DateKey(StudyStore.Now));
            PlayerPrefs.SetInt(SubjectKey(subject), TodayBySubject[(int)subject]);
            PlayerPrefs.Save();
        }

        static void LoadIfNeeded()
        {
            if (loaded) return;
            Load();
        }

        static void SaveTodayBreakdown()
        {
            for (int i = 0; i < TodayBySubject.Length; i++)
            {
                PlayerPrefs.SetInt(SubjectKey((StudySubject)i), TodayBySubject[i]);
            }
        }

        static string SubjectKey(StudySubject subject) => "sm_today_min_" + (int)subject;
        static string DateKey(DateTime dt) => dt.ToString("yyyy-MM-dd");
    }
}
