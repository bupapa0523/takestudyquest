using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// 自分の技は肩越しヒーローショット。敵とボスは据え置き。
    /// 空の側はカメラの後ろに隠す。
    /// </summary>
    public class BattleCam : MonoBehaviour
    {
        enum Mode { Off, Windup, Hold, Punch, Recover }

        static BattleCam inst;

        Camera cam;
        Vector3 restPos;
        Quaternion restRot;
        float restFov = 60f;
        bool haveRest;
        bool big;

        Mode mode;
        float t;
        float dur;
        Vector3 fromPos;
        Vector3 toPos;
        Quaternion fromRot;
        Quaternion toRot;
        float fromFov;
        float toFov;
        float shake;
        Vector3 punchDir;
        CanvasGroup hud;
        Image voidWash;
        float voidWashTarget;
        float voidWashAlpha;
        bool senseInvert;
        float senseBlend;
        float shotPull = 1f;
        bool poseLock;
        bool fullBody;

        public static BattleCam Ensure()
        {
            var main = Camera.main;
            if (main == null) return null;
            inst = main.GetComponent<BattleCam>();
            if (inst == null) inst = main.gameObject.AddComponent<BattleCam>();
            inst.cam = main;
            return inst;
        }

        public static void CaptureRest()
        {
            var rig = Ensure();
            if (rig == null || rig.cam == null) return;
            rig.restPos = rig.cam.transform.position;
            rig.restRot = rig.cam.transform.rotation;
            rig.restFov = rig.cam.fieldOfView;
            if (rig.restFov < 10f) rig.restFov = 60f;
            rig.haveRest = true;
        }

        public static void Release()
        {
            if (inst == null) return;
            inst.senseInvert = false;
            inst.senseBlend = 0f;
            inst.voidWashTarget = 0f;
            inst.voidWashAlpha = 0f;
            BattleFx.SetVoidBattleField(false, Vector3.zero);
            inst.RestoreNow();
            inst.mode = Mode.Off;
            inst.FadeHud(1f);
        }

        public static IEnumerator Windup(Transform me, Transform foe, bool ultimate, bool bossFront, float pull = 1f, bool poseLock = false, bool fullBody = false)
        {
            var rig = Ensure();
            if (rig == null) yield break;
            if (!rig.haveRest) CaptureRest();
            rig.BeginWindup(me, foe, ultimate, bossFront, pull, poseLock, fullBody);
            while (rig != null && rig.mode == Mode.Windup)
                yield return null;
        }

        public static void Punch()
        {
            if (inst != null) inst.BeginPunch();
        }

        public static void SetSenseInvert(bool on)
        {
            var rig = Ensure();
            if (rig == null) return;
            rig.senseInvert = on;
        }

        public static void SetVoidWash(float alpha)
        {
            var rig = Ensure();
            if (rig == null) return;
            rig.EnsureVoidWash();
            rig.voidWashTarget = Mathf.Clamp01(alpha);
        }

        public static void PulseVoidWash(float peak, float holdSeconds = 0.12f)
        {
            var rig = Ensure();
            if (rig == null) return;
            rig.EnsureVoidWash();
            rig.StartCoroutine(rig.VoidWashPulse(peak, holdSeconds));
        }

        IEnumerator VoidWashPulse(float peak, float holdSeconds)
        {
            float prev = voidWashTarget;
            voidWashTarget = Mathf.Clamp01(peak);
            float t = 0f;
            while (t < holdSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            voidWashTarget = prev;
        }

        void EnsureVoidWash()
        {
            if (voidWash != null) return;
            var go = new GameObject("VoidWashOverlay");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 7500;
            go.AddComponent<CanvasScaler>();
            var imgGo = new GameObject("Fill");
            imgGo.transform.SetParent(go.transform, false);
            var rt = imgGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            voidWash = imgGo.AddComponent<Image>();
            voidWash.color = new Color(1f, 1f, 1f, 0f);
            voidWash.raycastTarget = false;
        }

        public static IEnumerator Recover()
        {
            var rig = Ensure();
            if (rig == null) yield break;
            rig.BeginRecover();
            while (rig != null && rig.mode == Mode.Recover)
                yield return null;
            if (rig != null) rig.RestoreNow();
        }

        void BeginWindup(Transform me, Transform foe, bool ultimate, bool bossFront, float pull, bool lockPose, bool bodyShot)
        {
            big = ultimate;
            poseLock = lockPose;
            fullBody = bodyShot;
            shotPull = pull < 0.8f ? 1f : pull;
            if (me == null) me = foe;
            fromPos = haveRest ? restPos : transform.position;
            fromRot = haveRest ? restRot : transform.rotation;
            fromFov = haveRest ? restFov : (cam != null ? cam.fieldOfView : restFov);
            shake = 0f;
            t = 0f;
            dur = big ? 0.46f : 0.26f;
            if (poseLock) SetupPoseLockShot();
            else if (fullBody) SetupFullBodyShot(me, foe);
            else if (bossFront) SetupBossShot(me, foe);
            else SetupHeroShot(me, foe);
            mode = Mode.Windup;
            FadeHud(0f);
        }

        void SetupFullBodyShot(Transform me, Transform foe)
        {
            Vector3 feet = me != null ? me.position : Vector3.zero;
            Vector3 mid = feet + Vector3.up * 0.95f;
            Vector3 fwd = me != null ? Flat(me.forward) : Vector3.forward;
            if (fwd.sqrMagnitude < 0.01f)
            {
                Vector3 toFoe = foe != null ? Flat(foe.position - feet) : Vector3.forward;
                fwd = toFoe.sqrMagnitude > 0.01f ? toFoe.normalized : Vector3.forward;
            }
            else fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            right.Normalize();

            float pull = shotPull > 0.8f ? shotPull : 1f;
            toPos = feet - fwd * (6.35f * pull) + right * 1.15f + Vector3.up * 1.35f;
            if (toPos.y < 0.7f) toPos.y = 0.7f;
            toRot = Quaternion.LookRotation(mid - toPos, Vector3.up);
            punchDir = Vector3.zero;
            toFov = Mathf.Clamp(restFov + 8f, 58f, 70f);
        }

        void SetupPoseLockShot()
        {
            toPos = haveRest ? restPos : transform.position;
            toRot = haveRest ? restRot : transform.rotation;
            punchDir = Vector3.zero;
            toFov = Mathf.Max(46f, restFov - (big ? 8f : 5f));
        }

        void SetupHeroShot(Transform me, Transform foe)
        {
            Vector3 hero = Chest(me, 1.22f);
            Vector3 other = foe != null ? Chest(foe, 1.25f) : hero + me.forward * 4f;
            Vector3 fwd = Flat(other - hero);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            right.Normalize();
            if (Vector3.Dot(right, restPos - hero) < 0f) right = -right;

            float pull = shotPull > 0.8f ? shotPull : 1f;
            float back = (big ? 3.85f : 4.35f) * pull;
            float side = (big ? 2.35f : 2.55f) * pull;
            float lift = (big ? 1.58f : 1.72f) * Mathf.Lerp(1f, pull, 0.35f);
            Vector3 look = Vector3.Lerp(hero, other, 0.34f) + Vector3.up * 0.04f;
            toPos = me.position - fwd * back + right * side + Vector3.up * lift;
            if (toPos.y < 1.2f) toPos.y = 1.2f;
            toRot = Quaternion.LookRotation(look - toPos, Vector3.up);
            punchDir = (look - toPos).normalized;
            toFov = Mathf.Clamp(restFov + (big ? 2f : 5f), 54f, 68f);
        }

        void SetupBossShot(Transform me, Transform boss)
        {
            if (boss == null) boss = me;
            Vector3 face = Chest(boss, 1.85f);
            Vector3 toHero = me != null ? Flat(me.position - boss.position) : Vector3.back;
            if (toHero.sqrMagnitude < 0.01f) toHero = Vector3.back;
            toHero.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, toHero);
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            right.Normalize();
            if (Vector3.Dot(right, restPos - face) < 0f) right = -right;

            toPos = boss.position + toHero * 5.15f + right * 0.7f + Vector3.up * 1.2f;
            if (toPos.y < 0.9f) toPos.y = 0.9f;
            toRot = Quaternion.LookRotation(face - toPos, Vector3.up);
            punchDir = (face - toPos).normalized;
            toFov = Mathf.Clamp(restFov + 2f, 52f, 64f);
        }

        void BeginPunch()
        {
            fromPos = toPos;
            fromRot = toRot;
            fromFov = cam != null ? cam.fieldOfView : restFov;
            if (poseLock || fullBody)
            {
                toPos = fromPos;
                toRot = fromRot;
                toFov = fullBody ? fromFov : Mathf.Max(50f, fromFov - (big ? 4f : 2f));
            }
            else
            {
                Vector3 dir = punchDir.sqrMagnitude > 0.01f ? punchDir : transform.forward;
                float punchIn = (big ? 0.32f : 0.14f) / Mathf.Max(1f, shotPull);
                toPos = fromPos + dir * punchIn;
                if (toPos.y < 0.9f) toPos.y = 0.9f;
                toRot = fromRot;
                toFov = Mathf.Max(50f, fromFov - (big ? 3f : 2f));
            }
            shake = big ? 0.10f : 0.045f;
            t = 0f;
            dur = big ? 0.12f : 0.08f;
            mode = Mode.Punch;
        }

        void BeginRecover()
        {
            fromPos = toPos;
            fromRot = toRot;
            fromFov = cam != null ? cam.fieldOfView : restFov;
            toPos = restPos;
            toRot = restRot;
            toFov = restFov;
            shake = 0f;
            t = 0f;
            dur = big ? 0.36f : 0.20f;
            mode = Mode.Recover;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.05f) dt = 0.05f;
            t += dt;
            voidWashAlpha = Mathf.MoveTowards(voidWashAlpha, voidWashTarget, dt / 0.22f);
            if (voidWash != null)
            {
                var c = voidWash.color;
                c.a = voidWashAlpha;
                voidWash.color = c;
            }
            float invertTarget = senseInvert ? 1f : 0f;
            senseBlend = Mathf.MoveTowards(senseBlend, invertTarget, dt / 0.16f);

            if (mode == Mode.Off)
            {
                if (haveRest) Apply(restPos, restRot, restFov, 0f);
                return;
            }

            if (mode == Mode.Hold)
            {
                Apply(toPos, toRot, toFov, 0f);
                return;
            }

            float u = dur <= 0.001f ? 1f : Mathf.Clamp01(t / dur);
            u = u * u * (3f - 2f * u);
            Vector3 pos = Vector3.Lerp(fromPos, toPos, u);
            Quaternion rot = Quaternion.Slerp(fromRot, toRot, u);
            float fov = Mathf.Lerp(fromFov, toFov, u);
            float mag = 0f;
            if (mode == Mode.Punch)
                mag = shake * (1f - u) * Mathf.Sin(t * 62f);
            Apply(pos, rot, fov, mag);

            if (u < 1f) return;
            if (mode == Mode.Windup || mode == Mode.Punch) mode = Mode.Hold;
            else if (mode == Mode.Recover)
            {
                mode = Mode.Off;
                FadeHud(1f);
            }
        }

        void Apply(Vector3 pos, Quaternion rot, float fov, float mag)
        {
            if (mag != 0f)
                pos += transform.right * mag + transform.up * (mag * 0.35f);
            if (senseBlend > 0.001f)
                rot = rot * Quaternion.Euler(0f, 0f, 180f * senseBlend);
            transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = fov;
        }

        void RestoreNow()
        {
            if (!haveRest || cam == null) return;
            mode = Mode.Off;
            if (!senseInvert) senseBlend = 0f;
            Apply(restPos, restRot, restFov, 0f);
            FadeHud(1f);
        }

        static Vector3 Chest(Transform t, float y)
        {
            return t != null ? t.position + Vector3.up * y : Vector3.up;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        void FadeHud(float alpha)
        {
            if (hud == null)
            {
                var canvas = GameObject.Find("BattleCanvas");
                if (canvas != null)
                {
                    hud = canvas.GetComponent<CanvasGroup>();
                    if (hud == null) hud = canvas.AddComponent<CanvasGroup>();
                }
            }
            if (hud == null) return;
            hud.alpha = alpha;
            hud.interactable = alpha > 0.9f;
            hud.blocksRaycasts = alpha > 0.9f;
        }
    }
}
