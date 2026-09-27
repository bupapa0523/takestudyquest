using UnityEngine;

namespace ShiftingMetropolis.Progress
{
    public enum StudySubject
    {
        Math,
        Programming,
        English,
        General,
        Specialized,
        Other
    }

    public static class StudySubjectColors
    {
        public static Color Color(StudySubject subject)
        {
            switch (subject)
            {
                case StudySubject.Math: return new UnityEngine.Color(0.35f, 0.82f, 0.95f);
                case StudySubject.Programming: return new UnityEngine.Color(0.35f, 0.82f, 0.45f);
                case StudySubject.English: return new UnityEngine.Color(0.95f, 0.78f, 0.22f);
                case StudySubject.General: return new UnityEngine.Color(0.72f, 0.72f, 0.76f);
                case StudySubject.Specialized: return new UnityEngine.Color(0.72f, 0.48f, 0.95f);
                default: return new UnityEngine.Color(0.95f, 0.55f, 0.28f);
            }
        }
    }

    public static class StudySubjectNames
    {
        public static string Display(StudySubject subject)
        {
            switch (subject)
            {
                case StudySubject.Math: return "数学";
                case StudySubject.Programming: return "プログラミング";
                case StudySubject.English: return "英語";
                case StudySubject.General: return "一般教科";
                case StudySubject.Specialized: return "専門教科";
                default: return "その他";
            }
        }
    }
}
