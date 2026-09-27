using System;
using UnityEngine;

namespace ShiftingMetropolis.Progress
{
    public class StudyTimer : MonoBehaviour
    {
        public static StudyTimer Instance { get; private set; }

        public StudySubject CurrentSubject { get; private set; } = StudySubject.Math;
        public StudyMaterial CurrentMaterial { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public float SessionSeconds { get; private set; }
        public bool HasActiveSession
        {
            get { return CurrentMaterial != null && (IsRunning || IsPaused); }
        }

        StudyLogEntry openLog;
        float bankedSeconds;
        float lastRealtime;
        float saveAccum;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            StudyStore.Load(true);
            PlayerProgress.Load();
            RestoreFromSave();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Tick();
                PersistSession();
                return;
            }

            ApplySavedElapsed();
            lastRealtime = Time.realtimeSinceStartup;
            PersistSession();
        }

        void OnApplicationQuit()
        {
            PersistSession();
        }

        void OnDisable()
        {
            if (Instance == this) PersistSession();
        }

        void Update()
        {
            Tick();
        }

        void RestoreFromSave()
        {
            var state = StudyStore.Session;
            if (state == null || string.IsNullOrEmpty(state.materialId)) return;

            CurrentMaterial = StudyStore.FindMaterial(state.materialId);
            if (CurrentMaterial == null) return;

            CurrentSubject = (StudySubject)CurrentMaterial.subject;
            openLog = StudyStore.FindLog(state.logId) ?? StudyStore.FindOpenLog(CurrentMaterial.id);
            SessionSeconds = state.sessionSeconds;
            bankedSeconds = state.bankedSeconds;
            IsRunning = state.running && !state.paused;
            IsPaused = state.paused && CurrentMaterial != null;
            lastRealtime = Time.realtimeSinceStartup;

            ApplySavedElapsed();

            lastRealtime = Time.realtimeSinceStartup;
            PersistSession();
        }

        void ApplySavedElapsed()
        {
            if (!IsRunning || IsPaused || StudyStore.Session == null || StudyStore.Session.lastUtcTicks <= 0)
                return;

            double elapsed = (DateTime.UtcNow.Ticks - StudyStore.Session.lastUtcTicks) / (double)TimeSpan.TicksPerSecond;
            if (elapsed > 0 && elapsed < 365 * 24 * 3600)
                ApplyElapsed((float)elapsed);
        }

        void ApplyElapsed(float dt)
        {
            if (dt < 0f) dt = 0f;
            SessionSeconds += dt;
            bankedSeconds += dt;
            AwardBankedMinutes();
        }

        void Tick()
        {
            if (!IsRunning) return;

            float now = Time.realtimeSinceStartup;
            if (lastRealtime <= 0f) lastRealtime = now;
            float dt = now - lastRealtime;
            lastRealtime = now;
            if (dt < 0f) dt = 0f;

            SessionSeconds += dt;
            bankedSeconds += dt;
            AwardBankedMinutes();

            saveAccum += dt;
            if (saveAccum >= 1f)
            {
                saveAccum = 0f;
                PersistSession();
            }
        }

        void AwardBankedMinutes()
        {
            while (bankedSeconds >= 60f)
            {
                bankedSeconds -= 60f;
                StudyStore.AddLiveMinute(openLog);
                PlayerProgress.AddStudyMinute(CurrentSubject);
            }
        }

        void PersistSession()
        {
            StudyStore.SaveSession(HasActiveSession, IsPaused, CurrentMaterial, openLog, SessionSeconds, bankedSeconds);
        }

        public void SetSubject(StudySubject subject)
        {
            CurrentSubject = subject;
        }

        public void OpenMaterial(StudyMaterial material)
        {
            if (material == null) return;

            if (CurrentMaterial != null && CurrentMaterial.id == material.id)
            {
                if (!IsRunning) StartTimer();
                lastRealtime = Time.realtimeSinceStartup;
                PersistSession();
                return;
            }

            if (IsRunning || IsPaused) StopTimer();

            CurrentMaterial = material;
            CurrentSubject = (StudySubject)material.subject;
            openLog = StudyStore.FindOpenLog(material.id);
            if (openLog != null)
            {
                SessionSeconds = openLog.minutes * 60f + StudyStore.Session.bankedSeconds;
                bankedSeconds = StudyStore.Session.materialId == material.id
                    ? StudyStore.Session.bankedSeconds
                    : 0f;
                if (StudyStore.Session.materialId == material.id)
                {
                    SessionSeconds = StudyStore.Session.sessionSeconds;
                    bankedSeconds = StudyStore.Session.bankedSeconds;
                }
            }
            else
            {
                SessionSeconds = 0f;
                bankedSeconds = 0f;
                openLog = StudyStore.BeginSession(material);
            }

            lastRealtime = Time.realtimeSinceStartup;
            IsPaused = false;
            IsRunning = true;
            PersistSession();
        }

        public void StartTimer()
        {
            if (CurrentMaterial == null) return;
            if (IsRunning) return;
            lastRealtime = Time.realtimeSinceStartup;
            if (openLog == null) openLog = StudyStore.BeginSession(CurrentMaterial);
            openLog.open = true;
            IsPaused = false;
            IsRunning = true;
            PersistSession();
        }

        public void PauseTimer()
        {
            if (!IsRunning) return;
            AwardBankedMinutes();
            IsRunning = false;
            IsPaused = true;
            PersistSession();
        }

        public void ResumeTimer()
        {
            if (CurrentMaterial == null) return;
            lastRealtime = Time.realtimeSinceStartup;
            if (openLog == null) openLog = StudyStore.BeginSession(CurrentMaterial);
            openLog.open = true;
            IsPaused = false;
            IsRunning = true;
            PersistSession();
        }

        public void TogglePause()
        {
            if (IsPaused) ResumeTimer();
            else if (IsRunning) PauseTimer();
        }

        public void StopTimer()
        {
            if (!IsRunning && !IsPaused && openLog == null) return;
            AwardBankedMinutes();
            int already = openLog != null ? openLog.minutes : 0;
            int finalMinutes = Mathf.FloorToInt(SessionSeconds / 60f);
            int extra = finalMinutes - already;
            if (extra > 0) PlayerProgress.AddStudyMinutes(CurrentSubject, extra);
            StudyStore.CommitTimedSession(openLog, SessionSeconds);
            IsRunning = false;
            IsPaused = false;
            openLog = null;
            SessionSeconds = 0f;
            bankedSeconds = 0f;
            CurrentMaterial = null;
            PersistSession();
        }

        public void Toggle()
        {
            if (IsRunning) StopTimer();
            else StartTimer();
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;
            if (h > 0) return string.Format("{0}:{1:00}:{2:00}", h, m, s);
            return string.Format("{0:00}:{1:00}", m, s);
        }
    }
}
