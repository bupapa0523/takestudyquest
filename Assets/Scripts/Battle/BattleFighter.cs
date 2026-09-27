using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif
using ShiftingMetropolis.App;

namespace ShiftingMetropolis.Battle
{
    public class BattleFighter : MonoBehaviour
    {
        public Transform Root { get; private set; }
        public Animator Anim { get; private set; }

        Vector3 homePos;
        Quaternion homeRot;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable idlePlayable;
        AnimationClipPlayable actionPlayable;
        bool graphReady;
        bool actionConnected;
        int actionGen;
        readonly List<Renderer> rends = new List<Renderer>();
        Coroutine flashCo;
        int hitReactSerial;
        ParticleSystem burnFx;
        ParticleSystem stunFx;
        bool stunWobble;
        bool invertHold;
        AnimationClip idleClip;
        AnimationClip hitClip;
        AnimationClip downClip;
        AnimationClip chargeClip;
        AnimationClip ultimateHitClip;
        readonly Dictionary<SkillCommandType, AnimationClip> skillClips = new Dictionary<SkillCommandType, AnimationClip>();
        static readonly Dictionary<string, AnimationClip> animeClipCache = new Dictionary<string, AnimationClip>();
        HeroAppearance.SkinKind skinKind;
        string visualName;
        bool boundIsPlayer;
        bool pinFeet;
        float idleFootY;
        float floorY;
        bool floorReady;
        Transform faceLockTarget;
        float faceLockYaw;

        public void Bind(Transform hero, bool isPlayer, HeroAppearance.SkinKind kind = HeroAppearance.SkinKind.Synty, string visual = null)
        {
            Root = hero;
            skinKind = kind;
            visualName = visual;
            boundIsPlayer = isPlayer;
            if (string.IsNullOrEmpty(visualName) && hero != null) visualName = hero.name;
            floorY = hero.position.y;
            floorReady = true;
            homePos = hero.position;
            homeRot = hero.rotation;
            Anim = hero.GetComponentInChildren<Animator>();
            CacheRenderers(hero);
            LoadClips(isPlayer);
            StartGraph();
            PlayIdleImmediate();
            if (graphReady) graph.Evaluate();
            else if (Anim != null && Anim.gameObject.activeInHierarchy) Anim.Update(0f);
            PlantToFloor();
            CaptureHome();
            CaptureIdleFoot();
            EnsureAilmentFx();
        }

        public bool IsPosedProperly()
        {
            if (!graphReady || Anim == null || Anim.avatar == null || !Anim.avatar.isValid || !Anim.avatar.isHuman)
                return false;
            var head = Anim.GetBoneTransform(HumanBodyBones.Head);
            var hips = Anim.GetBoneTransform(HumanBodyBones.Hips);
            if (head != null && hips != null)
            {
                if (head.position.y < hips.position.y + 0.15f) return false;
                if (head.position.y < 0.65f) return false;
            }
            return true;
        }

        public bool TransformToShogun()
        {
            if (Root == null) return false;
            if (skinKind == HeroAppearance.SkinKind.HotondoShogun) return true;
            var prefab = HeroAppearance.LoadFirstShogunPrefab(out var kind);
            if (prefab == null) return false;

            ClearFlash();
            if (graphReady && graph.IsValid()) graph.Destroy();
            graphReady = false;
            actionConnected = false;

            for (int i = 0; i < rends.Count; i++)
                if (rends[i] != null) rends[i].enabled = false;
            var oldAnims = Root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < oldAnims.Length; i++)
                if (oldAnims[i] != null) oldAnims[i].enabled = false;

            var go = UnityEngine.Object.Instantiate(prefab, Root);
            go.name = prefab.name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            HeroAppearance.Apply(go.transform, kind, _ => 0);
            var anim = go.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.runtimeAnimatorController = null;
            }

            skinKind = kind;
            visualName = go.name;
            Anim = anim;
            CacheRenderers(go.transform);
            LoadClips(boundIsPlayer);
            StartGraph();
            PlayIdleImmediate();
            if (graphReady) graph.Evaluate();
            PlantToFloor();
            CaptureHome();
            CaptureIdleFoot();
            EnsureAilmentFx();
            return true;
        }

        public void SetAilments(bool burning, bool stunned)
        {
            EnsureAilmentFx();
            SetLoop(burnFx, burning);
            SetLoop(stunFx, stunned);
            stunWobble = stunned;
            if (!stunned && Root != null) Root.rotation = homeRot;
        }

        void LateUpdate()
        {
            if (graphReady && graph.IsValid() && graph.IsPlaying())
            {
                float dt = Time.deltaTime;
                if (dt < 0f) dt = 0f;
                graph.Evaluate(dt);
            }
            if (invertHold) return;
            if (faceLockTarget != null) AlignBodyToward(faceLockTarget, faceLockYaw);
            if (Root != null && Root.position.y <= homePos.y + 0.08f)
                PlantToFloor();
            else if (pinFeet) PinFeetToGround();
            if (!stunWobble || Root == null) return;
            float wobble = Mathf.Sin(Time.time * 10f) * 8f;
            Root.rotation = homeRot * Quaternion.Euler(0f, wobble, wobble * 0.25f);
        }

        void CaptureIdleFoot()
        {
            idleFootY = floorReady ? floorY : homePos.y;
            float minY = LowestContactY();
            if (minY < 900f) idleFootY = minY;
        }

        void PlantToFloor()
        {
            if (Root == null || !floorReady) return;
            float minY = LowestContactY();
            if (minY > 900f) return;
            float ground = floorReady ? floorY : homePos.y;
            float lift = ground - minY;
            if (lift > 0.012f && lift < 1.5f)
                Root.position += Vector3.up * lift;
        }

        float LowestContactY()
        {
            float feet = LowestFootY();
            if (feet < 900f) return feet;
            return MeshBottomY();
        }

        float MeshBottomY()
        {
            float minY = 9999f;
            for (int i = 0; i < rends.Count; i++)
            {
                var rend = rends[i];
                if (rend == null || !rend.enabled) continue;
                if (rend is ParticleSystemRenderer) continue;
                var b = rend.bounds;
                if (b.size.sqrMagnitude < 0.0001f) continue;
                if (b.min.y < minY) minY = b.min.y;
            }
            return minY;
        }

        float LowestFootY()
        {
            if (Anim == null) return 9999f;
            float minY = 9999f;
            var lf = Anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rf = Anim.GetBoneTransform(HumanBodyBones.RightFoot);
            if (lf != null) minY = Mathf.Min(minY, lf.position.y);
            if (rf != null) minY = Mathf.Min(minY, rf.position.y);
            var lt = Anim.GetBoneTransform(HumanBodyBones.LeftToes);
            var rt = Anim.GetBoneTransform(HumanBodyBones.RightToes);
            if (lt != null) minY = Mathf.Min(minY, lt.position.y);
            if (rt != null) minY = Mathf.Min(minY, rt.position.y);
            if (minY < 900f) minY -= 0.07f;
            return minY;
        }

        void PinFeetToGround()
        {
            PlantToFloor();
        }

        void AlignBodyToward(Transform target, float extraYaw = 0f)
        {
            if (Root == null || target == null) return;
            Vector3 to = target.position - Root.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            Root.rotation = Quaternion.LookRotation(to.normalized, Vector3.up) * Quaternion.Euler(0f, extraYaw, 0f);
        }

        public void BeginInvert(Vector3 toward)
        {
            invertHold = true;
            pinFeet = false;
            if (Root == null) return;
            Vector3 axis = toward;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.0001f) axis = Vector3.forward;
            Vector3 pivot = Root.position + Vector3.up * 1.08f;
            Root.RotateAround(pivot, axis.normalized, 180f);
        }

        public void EndInvert()
        {
            invertHold = false;
            if (Root == null) return;
            Root.position = homePos;
            Root.rotation = homeRot;
        }

        float MeasureStrikeReach(Transform target, bool useHands, bool useFeet)
        {
            if (Anim == null || Root == null) return 0f;
            Vector3 origin = Root.position + Vector3.up * 0.9f;
            var hips = Anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null) origin = hips.position;
            Vector3 face = Root.forward;
            if (target != null)
            {
                face = target.position - origin;
                face.y = 0f;
            }
            if (face.sqrMagnitude < 0.0001f) face = Vector3.forward;
            face.Normalize();
            float best = -999f;
            void Consider(HumanBodyBones bone)
            {
                var tr = Anim.GetBoneTransform(bone);
                if (tr == null) return;
                float along = Vector3.Dot(tr.position - origin, face);
                if (along > best) best = along;
            }
            if (useHands)
            {
                Consider(HumanBodyBones.RightHand);
                Consider(HumanBodyBones.LeftHand);
                Consider(HumanBodyBones.RightLowerArm);
                Consider(HumanBodyBones.LeftLowerArm);
            }
            if (useFeet)
            {
                Consider(HumanBodyBones.RightFoot);
                Consider(HumanBodyBones.LeftFoot);
                Consider(HumanBodyBones.RightLowerLeg);
                Consider(HumanBodyBones.LeftLowerLeg);
            }
            return best;
        }

        Vector3 HandOrChest()
        {
            if (Anim != null)
            {
                var hand = Anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand == null) hand = Anim.GetBoneTransform(HumanBodyBones.LeftHand);
                if (hand != null) return hand.position;
            }
            return Root != null ? Root.position + Vector3.up * 1.15f : Vector3.up;
        }

        public Vector3 HandsCenter()
        {
            if (Anim != null)
            {
                var l = Anim.GetBoneTransform(HumanBodyBones.LeftHand);
                var r = Anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (l != null && r != null) return (l.position + r.position) * 0.5f;
                if (r != null) return r.position;
                if (l != null) return l.position;
            }
            return Root != null ? Root.position + Vector3.up * 1.18f : Vector3.up;
        }

        public Vector3 RightHandPos()
        {
            if (Anim != null)
            {
                var h = Anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (h != null) return h.position;
            }
            return HandsCenter();
        }

        public Vector3 LeftHandPos()
        {
            if (Anim != null)
            {
                var h = Anim.GetBoneTransform(HumanBodyBones.LeftHand);
                if (h != null) return h.position;
            }
            return HandsCenter();
        }

        public Vector3 PalmPos(Vector3 toward)
        {
            return PalmPosOnHand(toward, false);
        }

        public Vector3 LeftPalmPos(Vector3 toward)
        {
            return PalmPosOnHand(toward, true);
        }

        Vector3 PalmPosOnHand(Vector3 toward, bool left)
        {
            if (Anim == null) return left ? LeftHandPos() : RightHandPos();
            var hand = Anim.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (hand == null) return left ? LeftHandPos() : RightHandPos();
            var mid = Anim.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            Vector3 p = hand.position;
            if (mid != null) p = Vector3.Lerp(hand.position, mid.position, 0.62f);
            Vector3 n = toward - p;
            n.y = 0f;
            if (n.sqrMagnitude < 0.0001f) n = Vector3.forward;
            n.Normalize();
            p += n * 0.09f - Vector3.up * 0.02f;
            p -= hand.up * 0.05f;
            return p;
        }

        Vector3 BonePos(HumanBodyBones bone, Vector3 fallback)
        {
            if (Anim == null) return fallback;
            var tr = Anim.GetBoneTransform(bone);
            return tr != null ? tr.position : fallback;
        }

        Vector3 StrikeLimb(bool kick)
        {
            if (kick)
            {
                var f = BonePos(HumanBodyBones.RightFoot, Vector3.zero);
                if (f.sqrMagnitude > 0.0001f) return f;
                return Root != null ? Root.position + Vector3.up * 0.35f + Root.forward * 0.4f : Vector3.up;
            }
            return HandOrChest();
        }

        Transform BoneTr(HumanBodyBones bone)
        {
            return Anim != null ? Anim.GetBoneTransform(bone) : null;
        }

        IEnumerator MeleeGlam(Color color, bool kick, float seconds)
        {
            Color glow = Color.Lerp(color, Color.white, 0.08f);
            var trails = new System.Collections.Generic.List<GameObject>();
            void Tip(Transform bone)
            {
                if (bone == null) return;
                trails.Add(BattleVfx.MakeAfterglowTrail(bone, glow, 0.07f, 0.18f));
            }
            if (kick)
            {
                Tip(BoneTr(HumanBodyBones.RightToes) ?? BoneTr(HumanBodyBones.RightFoot));
                Tip(BoneTr(HumanBodyBones.LeftToes) ?? BoneTr(HumanBodyBones.LeftFoot));
            }
            else
            {
                Tip(BoneTr(HumanBodyBones.RightIndexDistal)
                    ?? BoneTr(HumanBodyBones.RightIndexProximal)
                    ?? BoneTr(HumanBodyBones.RightHand));
                Tip(BoneTr(HumanBodyBones.LeftIndexDistal)
                    ?? BoneTr(HumanBodyBones.LeftIndexProximal)
                    ?? BoneTr(HumanBodyBones.LeftHand));
            }
            float t = 0f;
            seconds = Mathf.Max(0.3f, seconds);
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
            for (int i = 0; i < trails.Count; i++)
            {
                if (trails[i] == null) continue;
                var tr = trails[i].GetComponent<TrailRenderer>();
                if (tr != null) tr.emitting = false;
            }
            float fade = 0f;
            while (fade < 0.12f)
            {
                fade += Time.deltaTime;
                yield return null;
            }
            for (int i = 0; i < trails.Count; i++)
                if (trails[i] != null) UnityEngine.Object.Destroy(trails[i]);
        }

        public Vector3 MouthPos()
        {
            if (Anim != null)
            {
                var h = Anim.GetBoneTransform(HumanBodyBones.Head);
                if (h != null) return h.position + h.forward * 0.14f - h.up * 0.04f;
            }
            return Root != null ? Root.position + Vector3.up * 1.56f + Root.forward * 0.18f : Vector3.up * 1.56f;
        }

        public Vector3 KiFront(Vector3 to, float push, bool mouth)
        {
            Vector3 bone = mouth ? MouthPos() : HandsCenter();
            Vector3 origin = Root != null ? Root.position : bone;
            Vector3 aim = to - origin;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            return bone + aim.normalized * push;
        }

        public Vector3 ForeheadPos()
        {
            if (Anim != null)
            {
                var h = Anim.GetBoneTransform(HumanBodyBones.Head);
                if (h != null) return h.position + h.up * 0.07f + h.forward * 0.12f;
            }
            return Root != null ? Root.position + Vector3.up * 1.66f + Root.forward * 0.16f : Vector3.up * 1.66f;
        }

        public void CaptureHome()
        {
            if (Root == null) return;
            homePos = Root.position;
            homeRot = Root.rotation;
        }

        void LoadClips(bool isPlayer)
        {
            bool female = !string.IsNullOrEmpty(visualName)
                && visualName.IndexOf("Female", StringComparison.OrdinalIgnoreCase) >= 0;
            string sex = female ? "Female" : "Male";
            string tag = female ? "HumanF" : "HumanM";
            string combat = "Assets/Kevin Iglesias/Human Animations/Animations/" + sex + "/Combat/";
            idleClip = LoadFbxClip(combat + tag + "@CombatIdle01.fbx");
            if (idleClip == null)
                idleClip = LoadClip("Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim");
            hitClip = LoadFbxClip(combat + tag + "@CombatDamage01.fbx");
            if (hitClip == null)
                hitClip = LoadClip("Assets/DoubleL/Demo/Anim/Hit_F_1_InPlace.anim");
            downClip = LoadFbxClip(combat + tag + "@Death01.fbx");
            if (downClip == null)
                downClip = LoadClip("Assets/DoubleL/Demo/Anim/Hit_F_2_InPlace.anim");

            const string anime = "Assets/anime/";
            chargeClip = LoadFbxClip(anime + "Mutant@Sword And Shield Power Up.fbx");
            if (chargeClip == null) chargeClip = idleClip;
            skillClips[SkillCommandType.Charge] = chargeClip;

            ultimateHitClip = LoadFbxClip(anime + "Mutant@Sword And Shield Death.fbx");
            var jab = LoadFbxClip(combat + "1H/" + tag + "@Attack1H01_R.fbx");
            var kick = LoadClip("Assets/DoubleL/Demo/Anim/OneHand_Up_Jump_B_InPlace.anim");
            var cast = LoadClip("Assets/DoubleL/Demo/Anim/Action_A_5_2.anim");
            if (cast == null) cast = LoadClip("Assets/DoubleL/Demo/Anim/OneHand_Up_Shield_Block_Idle.anim");
            var smash = LoadFbxClip(combat + "2H/" + tag + "@Attack2H01.fbx");
            if (jab == null) jab = LoadClip("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim");
            if (kick == null) kick = LoadClip("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_1_InPlace.anim");
            if (smash == null) smash = haymakerFallback();
            skillClips[SkillCommandType.Skill1] = jab;
            skillClips[SkillCommandType.Skill2] = kick;
            skillClips[SkillCommandType.Skill3] = cast;
            skillClips[SkillCommandType.BrainSmash] = cast != null ? cast : smash;
        }

        static AnimationClip LoadAnimeClip(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            if (animeClipCache.TryGetValue(fileName, out var cached) && cached != null) return cached;
            var clip = LoadFbxClip("Assets/anime/Mutant@" + fileName);
            animeClipCache[fileName] = clip;
            return clip;
        }

        static AnimationClip haymakerFallback()
        {
            return LoadClip("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_3_InPlace.anim");
        }

        static AnimationClip LoadFbxClip(string path)
        {
            return PlayerAssets.LoadClip(path);
        }

        static AnimationClip LoadClip(string path)
        {
            return PlayerAssets.LoadClip(path);
        }

        void StartGraph()
        {
            if (Anim == null || idleClip == null) return;
            Anim.applyRootMotion = false;
            Anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Anim.runtimeAnimatorController = null;
            graph = PlayableGraph.Create("BattleFighter_" + name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            var output = AnimationPlayableOutput.Create(graph, "Anim", Anim);
            output.SetSourcePlayable(mixer);
            idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
            idlePlayable.SetDuration(double.MaxValue);
            mixer.ConnectInput(0, idlePlayable, 0, 1f);
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
            graph.Play();
            graphReady = true;
        }

        void Update()
        {
            if (!graphReady || idleClip == null || !idlePlayable.IsValid()) return;
            if (mixer.GetInputWeight(0) < 0.5f) return;
            double len = idleClip.length;
            if (len <= 0.01) return;
            double t = idlePlayable.GetTime();
            if (t >= len) idlePlayable.SetTime(t % len);
        }

        void PlayIdleImmediate()
        {
            pinFeet = false;
            faceLockTarget = null;
            if (!graphReady) return;
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
            idlePlayable.SetTime(0);
        }

        void FreezeAction()
        {
            if (actionPlayable.IsValid()) actionPlayable.SetSpeed(0f);
        }

        public void HoldPunchPose()
        {
            FreezeAction();
        }

        public void ClearMoveLocks()
        {
            pinFeet = false;
            faceLockTarget = null;
        }

        public void AlignToward(Transform target)
        {
            AlignBodyToward(target, faceLockYaw);
        }

        public void PlaySkillClip(string fileName, float speed = 1f)
        {
            var clip = LoadAnimeClip(fileName);
            if (clip != null && graphReady) PlayAction(clip, speed);
        }

        void PlayAction(AnimationClip clip, float speed = 1f)
        {
            if (!graphReady || clip == null || !graph.IsValid()) return;
            actionGen++;
            if (actionConnected && mixer.IsValid() && mixer.GetInput(1).IsValid())
                mixer.DisconnectInput(1);
            actionConnected = false;
            if (actionPlayable.IsValid())
                actionPlayable.Destroy();
            actionPlayable = AnimationClipPlayable.Create(graph, clip);
            double len = clip.length > 0.05f ? clip.length : 0.05;
            actionPlayable.SetDuration(len);
            actionPlayable.SetTime(0);
            actionPlayable.SetDone(false);
            actionPlayable.SetSpeed(Mathf.Max(0.05f, speed));
            mixer.ConnectInput(1, actionPlayable, 0);
            mixer.SetInputWeight(0, 0f);
            mixer.SetInputWeight(1, 1f);
            actionConnected = true;
            if (!graph.IsPlaying()) graph.Play();
            graph.Evaluate(0);
        }

        public IEnumerator PlaySkill(SkillCommandType type, Transform target, Action onImpact, UltimateSkillDefinition ultimate = null, BasicSkillDefinition basic = null)
        {
            AnimationClip clip = null;
            if (type == SkillCommandType.Charge) clip = chargeClip;
            if (clip == null && ultimate != null)
                clip = LoadAnimeClip(ultimate.animeClip);
            if (clip == null && basic != null)
            {
                SkillCommandType clipKey = basic.motion == BasicMotion.Kick
                    ? SkillCommandType.Skill2
                    : basic.motion == BasicMotion.Bolt
                        ? SkillCommandType.Skill3
                        : SkillCommandType.Skill1;
                skillClips.TryGetValue(clipKey, out clip);
            }
            if (clip == null) skillClips.TryGetValue(type, out clip);

            bool hasUlt = ultimate != null;
            bool telekinesis = hasUlt
                && (ultimate.motionType == UltimateMotionType.TelekinesisSlam || ultimate.motionType == UltimateMotionType.FieldNuke);
            bool rainDown = telekinesis && ultimate.telekinesis != null && ultimate.telekinesis.rainFromAbove;
            bool iceRain = telekinesis && ultimate.telekinesis != null && ultimate.telekinesis.kind == FallingObjectKind.IceChunk;
            bool domainBurst = telekinesis && ultimate.telekinesis != null && ultimate.telekinesis.domainExpand;
            bool bigBolt = hasUlt && ultimate.boltScale > 1.01f;
            bool pelletVolley = hasUlt && (ultimate.boltCount > 1 || ultimate.asteroid || ultimate.asteroidFull);
            bool waveSkill = hasUlt && !string.IsNullOrEmpty(ultimate.waveKind);
            bool swordVolley = hasUlt && ultimate.motionType == UltimateMotionType.SwordVolley;
            bool ultimateMagicBolt = hasUlt && ultimate.motionType == UltimateMotionType.MagicBolt;
            bool magic = !telekinesis && !swordVolley && (
                (basic != null && basic.motion == BasicMotion.Bolt)
                || (basic == null && (type == SkillCommandType.Skill3 || ((type == SkillCommandType.BrainSmash || hasUlt) && (ultimate == null || ultimateMagicBolt)))));
            bool kick = basic != null ? basic.motion == BasicMotion.Kick : type == SkillCommandType.Skill2;
            bool meleeUltimate = hasUlt && ultimate.motionType == UltimateMotionType.MeleeRush;
            if (meleeUltimate)
            {
                string n = ultimate.animeClip != null ? ultimate.animeClip : "";
                kick = n.IndexOf("Kick", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Chapa", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Capoeira", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Armada", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Queshada", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Jump", StringComparison.OrdinalIgnoreCase) >= 0
                    || (ultimate != null && ultimate.id == "shoulder_throw");
            }
            bool lunge = type != SkillCommandType.Charge && !telekinesis && !swordVolley
                && (!magic || (ultimate != null && ultimate.lungeMeters > 0.01f));
            bool useHands = magic || telekinesis || !kick;
            bool useFeet = kick || meleeUltimate;

            bool isUltimateMotion = hasUlt;
            bool rasSkill = ultimate != null && ultimate.id == "rasengan";
            const float rasCharge = 1.5f;
            float rasAnimDur = 0.85f;
            float durationCap = isUltimateMotion ? 2.8f : 2.2f;
            if (meleeUltimate && !rasSkill) durationCap = 2.2f;
            float duration = 0.7f;
            float playSpeed = 1f;
            if (clip != null)
            {
                duration = Mathf.Max(clip.length, 0.2f);
                bool customSpeed = ultimate != null && ultimate.animSpeed > 1.01f;
                if (rasSkill)
                {
                    playSpeed = customSpeed ? ultimate.animSpeed : 2.3f;
                    rasAnimDur = clip.length / Mathf.Max(0.05f, playSpeed);
                    duration = rasCharge + rasAnimDur + 0.1f;
                }
                else if (customSpeed)
                {
                    playSpeed = ultimate.animSpeed;
                    duration = clip.length / playSpeed;
                }
                if (!rasSkill && !customSpeed && duration > durationCap)
                {
                    playSpeed = clip.length / durationCap;
                    duration = durationCap;
                }
                else if (!rasSkill && !customSpeed && isUltimateMotion && duration < 0.7f)
                {
                    playSpeed = clip.length / 0.7f;
                    duration = 0.7f;
                }
            }
            if (magic) duration = Mathf.Max(duration, 0.7f);
            if (telekinesis || swordVolley) duration = Mathf.Max(duration, 0.85f);

            float impactFraction = type == SkillCommandType.Charge
                ? 0.4f
                : (ultimate != null ? ultimate.impactFraction : (kick ? 0.52f : 0.42f));
            impactFraction = Mathf.Clamp(impactFraction, 0.12f, 0.88f);
            float impactAt = rasSkill ? rasCharge + rasAnimDur * impactFraction : duration * impactFraction;
            bool elementUlt = ultimate != null && BattleElementUtil.IsElementSkill(ultimate.element);
            if (elementUlt && clip != null)
            {
                const float elementAnimCap = 1.15f;
                if (duration > elementAnimCap)
                {
                    playSpeed = clip.length / elementAnimCap;
                    duration = elementAnimCap;
                }
                impactAt = duration * impactFraction;
            }
            float extraYaw = ultimate != null ? ultimate.facingOffsetY : 0f;
            faceLockTarget = type == SkillCommandType.Charge ? null : target;
            faceLockYaw = extraYaw;

            pinFeet = type != SkillCommandType.Charge;
            int myGen = actionGen;
            try
            {
                if (clip != null && graphReady) PlayAction(clip, playSpeed);
                else if (Anim != null && Anim.gameObject.activeInHierarchy) Anim.Update(0f);
                myGen = actionGen;
                AlignBodyToward(target, extraYaw);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("BattleFighter PlayAction: " + ex.Message);
            }

            if (type == SkillCommandType.Charge)
                BattleVfx.PlayCharge(Root, duration);

            Color magColor = hasUlt
                ? ultimate.vfxColor
                : (basic != null ? basic.vfxColor : new Color(0.3f, 0.75f, 1f));

            if (meleeUltimate && (ultimate == null || (ultimate.id != "ryuken" && ultimate.id != "rasengan")))
                StartCoroutine(MeleeGlam(magColor, kick, duration));

            Vector3 lungePos = homePos;
            if (lunge && target != null)
            {
                Vector3 to = target.position - homePos;
                to.y = 0f;
                float dist = kick ? 0.7f : 0.95f;
                bool closeIn = ultimate != null && ultimate.lungeMeters > 0.01f;
                if (closeIn) dist = ultimate.lungeMeters;
                if (to.sqrMagnitude > 0.01f)
                {
                    float stop = 0.95f;
                    if (ultimate != null && Mathf.Abs(ultimate.lungeStop) > 0.001f) stop = ultimate.lungeStop;
                    float travel = closeIn
                        ? Mathf.Max(0f, to.magnitude - stop)
                        : to.magnitude * 0.42f;
                    lungePos = homePos + to.normalized * Mathf.Min(dist, travel);
                }
                if (BattleStage.RaidMode && to.sqrMagnitude > 0.01f)
                {
                    float attacker = Root != null ? Mathf.Max(1f, Root.lossyScale.x) : 1f;
                    float victim = Mathf.Max(1f, target.lossyScale.x);
                    float raidStop = 1.05f + (attacker - 1f) * 0.9f + (victim - 1f) * 0.58f;
                    float raidTravel = Mathf.Max(0.35f, to.magnitude - raidStop);
                    lungePos = homePos + to.normalized * raidTravel;
                }
            }

            bool impacted = false;
            Action fire = () =>
            {
                if (impacted) return;
                impacted = true;
                if (onImpact != null) onImpact();
            };

            var rasVfx = rasSkill ? BattleFx.SpawnRasenganVfx() : default;

            float startReach = MeasureStrikeReach(target, useHands, useFeet);
            float peakReach = startReach;
            bool armed = false;
            float retreatT = 0f;
            float t = 0f;
            bool launched = false;
            bool isMultiHit = (ultimate != null && (ultimate.id == "fist_fight" || ultimate.id == "knee_combo" || ultimate.id == "hurricane_kick" || ultimate.id == "butterfly_twirl" || ultimate.id == "kick_2"));
            int multiHitDone = 0;

            while (t < duration)
            {
                float dt = Time.deltaTime;
                if (dt < 0.0005f) dt = 0.016f;
                t += dt;

                if (isMultiHit && !launched && target != null)
                {
                    if (multiHitDone == 0 && t >= impactAt * 0.38f)
                    {
                        multiHitDone++;
                        Vector3 swingPos = BattleVfx.AtBody(target) + UnityEngine.Random.insideUnitSphere * 0.22f;
                        BattleVfx.PlaySwing(swingPos, magColor, kick);
                        BattleFx.Bloom(swingPos, magColor, 1.15f);
                    }
                    else if (multiHitDone == 1 && t >= impactAt * 0.70f)
                    {
                        multiHitDone++;
                        Vector3 swingPos = BattleVfx.AtBody(target) + UnityEngine.Random.insideUnitSphere * 0.22f;
                        BattleVfx.PlaySwing(swingPos, magColor, kick);
                        BattleFx.Bloom(swingPos, magColor, 1.25f);
                    }
                }

                if (rasSkill && graphReady && actionPlayable.IsValid() && actionGen == myGen && clip != null && t < rasCharge)
                {
                    actionPlayable.SetSpeed(0f);
                    actionPlayable.SetDone(false);
                    actionPlayable.SetTime(0);
                }
                else if (rasSkill && graphReady && actionPlayable.IsValid() && actionGen == myGen && t >= rasCharge && actionPlayable.GetSpeed() < 0.01)
                {
                    actionPlayable.SetDone(false);
                    actionPlayable.SetTime(0);
                    actionPlayable.SetSpeed(Mathf.Max(0.05f, playSpeed));
                }

                if (rasSkill && !launched && Root != null && t < rasCharge)
                {
                    Vector3 stay = homePos;
                    stay.y = homePos.y;
                    Root.position = stay;
                }
                else if (Root != null && (lunge || (ultimate != null && ultimate.jumpBoost > 0.01f)) && !(rasSkill && t < rasCharge))
                {
                    Vector3 pos = Root.position;
                    if (lunge)
                    {
                        if (!impacted)
                        {
                            float lungeT = rasSkill ? Mathf.Max(0f, t - rasCharge) : t;
                            float approach = Mathf.Max(0.22f, impactAt * 0.45f);
                            if (rasSkill)
                                approach = Mathf.Max(0.34f, (impactAt - rasCharge) * 0.96f);
                            else if (ultimate != null && ultimate.lungeMeters > 0.01f)
                            {
                                approach = Mathf.Max(0.18f, impactAt * 0.72f);
                                if (ultimate.lungeStop > 0.01f)
                                    approach = Mathf.Max(0.12f, impactAt);
                            }
                            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lungeT / approach));
                            pos = Vector3.Lerp(homePos, lungePos, u);
                        }
                        else
                        {
                            float retreatStart = Mathf.Max(impactAt, duration - 0.22f);
                            if (t < retreatStart)
                            {
                                pos = lungePos;
                            }
                            else
                            {
                                float back = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((t - retreatStart) / 0.22f));
                                pos = Vector3.Lerp(homePos, lungePos, back);
                            }
                        }
                    }
                    if (ultimate != null && ultimate.jumpBoost > 0.01f)
                    {
                        float u = Mathf.Clamp01(t / Mathf.Max(0.05f, duration));
                        float air;
                        if (u < 0.16f) air = Mathf.SmoothStep(0f, 1f, u / 0.16f);
                        else if (u < 0.78f) air = 1f;
                        else air = Mathf.SmoothStep(0f, 1f, (1f - u) / 0.22f);
                        pos.y = homePos.y + ultimate.jumpBoost * air;
                    }
                    Root.position = pos;
                }

                if (rasSkill && !launched && rasVfx.core != null)
                {
                    Vector3 body = target != null ? BattleVfx.AtBody(target) : (Root != null ? Root.position + Root.forward * 2f + Vector3.up * 1.05f : Vector3.up);
                    float u = Mathf.Clamp01(t / rasCharge);
                    float orbSize = t < rasCharge ? Mathf.Lerp(0.55f, 1.72f, u) : 1.72f;
                    BattleFx.UpdateRasenganVfx(rasVfx, LeftPalmPos(body), body, orbSize, t * 14f);
                }

                if (!launched)
                {
                    bool timeHit = t >= impactAt;
                    if (rasSkill && t < rasCharge)
                        timeHit = false;
                    else if (rasSkill && target != null && t >= rasCharge)
                    {
                        Vector3 body = BattleVfx.AtBody(target);
                        float palmDist = Vector3.Distance(LeftPalmPos(body), body);
                        timeHit = t >= impactAt || palmDist < 0.58f;
                    }
                    bool watchReach = !isUltimateMotion && !magic && !telekinesis && !swordVolley && type != SkillCommandType.Charge;
                    if (watchReach)
                    {
                        float reach = MeasureStrikeReach(target, useHands, useFeet);
                        if (t > Mathf.Min(0.14f, duration * 0.12f))
                        {
                            if (reach > peakReach + 0.012f)
                            {
                                peakReach = reach;
                                armed = peakReach > startReach + 0.06f;
                            }
                            else if (armed && reach < peakReach - 0.045f)
                                timeHit = true;
                        }
                        if (t < duration * 0.18f) timeHit = false;
                    }

                    if (timeHit)
                    {
                        launched = true;
                        AlignBodyToward(target, extraYaw);
                        if (telekinesis)
                        {
                            Vector3 from = HandOrChest();
                            Vector3 toChest = target != null ? target.position + Vector3.up * 1.05f : from + Vector3.forward;
                            if (domainBurst)
                                yield return BattleFx.DomainBurst(toChest, magColor, fire);
                            else if (rainDown || iceRain)
                                yield return BattleFx.TelekinesisSlam(Root, from, toChest, ultimate != null ? ultimate.telekinesis : null, fire);
                            else
                                yield return BattleFx.TelekinesisSlam(Root, from, toChest, ultimate != null ? ultimate.telekinesis : null, fire);
                        }
                        else if (ultimate != null && ultimate.id == "ryuken")
                        {
                            faceLockTarget = null;
                            AlignBodyToward(target, extraYaw);
                            Vector3 toChest = target != null ? target.position + Vector3.up * 1.05f : HandOrChest();
                            yield return BattleFx.Ryuken(this, toChest, fire);
                        }
                        else if (ultimate != null && ultimate.id == "rasengan")
                        {
                            Vector3 hitPos = target != null ? BattleVfx.AtBody(target) : HandOrChest();
                            BattleFx.RasenganHit(hitPos, fire, ref rasVfx);
                        }
                        else if (magic || swordVolley)
                        {
                            Vector3 from = HandOrChest();
                            Vector3 toChest = target != null ? target.position + Vector3.up * 1.05f : from + Vector3.forward;
                            if (Root != null)
                            {
                                Vector3 aim = toChest - Root.position;
                                aim.y = 0f;
                                if (aim.sqrMagnitude > 0.01f)
                                    from = Root.position + Vector3.up * 1.2f + aim.normalized * 0.45f;
                            }
                            if (swordVolley)
                                yield return BattleFx.SwordVolley(Root, toChest, ultimate != null ? ultimate.telekinesis : null, fire);
                            else if (waveSkill)
                            {
                                yield return BattleFx.AnimeWave(ultimate.waveKind, Root, target, magColor, fire);
                            }
                            else if (bigBolt)
                            {
                                BattleVfx.PlayMagicCast(from, magColor);
                                if (Root != null)
                                {
                                    Vector3 aim2 = toChest - Root.position;
                                    aim2.y = 0f;
                                    if (aim2.sqrMagnitude > 0.01f)
                                        from = Root.position + Vector3.up * 1.35f + aim2.normalized * 1.15f;
                                }
                                yield return BattleFx.BigBolt(from, toChest, magColor, ultimate.boltScale, 0.52f, fire);
                            }
                            else if (pelletVolley)
                            {
                                if (Root != null)
                                {
                                    Vector3 aim2 = toChest - Root.position;
                                    aim2.y = 0f;
                                    if (aim2.sqrMagnitude > 0.01f)
                                        from = Root.position + Vector3.up * 1.25f + aim2.normalized * 0.85f;
                                }
                                if (ultimate.asteroidFull)
                                    yield return BattleFx.AsteroidFullAttack(Root, toChest, magColor, fire);
                                else if (ultimate.asteroid)
                                    yield return BattleFx.AsteroidVolley(from, toChest, magColor, fire);
                                else
                                    yield return BattleFx.PelletVolley(from, toChest, magColor, ultimate.boltCount, fire, false);
                            }
                            else if (basic != null && basic.id == "shogun_bolt")
                                yield return BattleFx.Kurodama(from, toChest, fire);
                            else
                            {
                                BattleVfx.PlayMagicCast(from, magColor);
                                yield return BattleFx.Bolt(from, toChest, magColor, 0.12f, fire);
                            }
                        }
                        else
                        {
                            Color swing = ultimate != null
                                ? ultimate.vfxColor
                                : basic != null
                                    ? basic.vfxColor
                                    : kick
                                        ? new Color(1f, 0.5f, 0.15f)
                                        : new Color(1f, 0.9f, 0.45f);
                            Vector3 swingPos = target != null ? BattleVfx.AtBody(target) : HandOrChest();
                            BattleVfx.PlaySwing(swingPos, swing, kick);
                            if (meleeUltimate)
                            {
                                BattleFx.Bloom(swingPos, swing, kick ? 1.85f : 1.55f);
                                Vector3 across = Root != null ? Vector3.Cross(Vector3.up, Root.forward) : Vector3.right;
                                if (across.sqrMagnitude < 0.0001f) across = Vector3.right;
                                across.Normalize();
                                var cut = BattleVfx.MakeEnergyBeam(Color.Lerp(swing, Color.white, 0.45f), swing, 0.04f, 0.11f);
                                BattleVfx.PlaceBeam(cut, swingPos - across * 1.45f - Vector3.up * 0.15f,
                                    swingPos + across * 1.45f + Vector3.up * 0.35f, 1.25f);
                                UnityEngine.Object.Destroy(cut, 0.2f);
                                if (kick)
                                {
                                    var cut2 = BattleVfx.MakeEnergyBeam(swing, Color.Lerp(swing, Color.white, 0.3f), 0.03f, 0.08f);
                                    BattleVfx.PlaceBeam(cut2, swingPos + Vector3.up * 0.9f, swingPos - Vector3.up * 0.7f, 1.1f);
                                    UnityEngine.Object.Destroy(cut2, 0.18f);
                                    var ring = BattleVfx.MakeWaveRing(swing, true);
                                    ring.transform.position = swingPos;
                                    ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                                    ring.transform.localScale = new Vector3(1.8f, 0.08f, 1.8f);
                                    UnityEngine.Object.Destroy(ring, 0.22f);
                                }
                            }
                            fire();
                        }
                    }
                }

                if (launched && type != SkillCommandType.Charge)
                {
                    bool isMelee = meleeUltimate || kick || (basic != null && (basic.motion == BasicMotion.Punch || basic.motion == BasicMotion.Kick));
                    if (!isMelee)
                    {
                        break;
                    }
                }
                yield return null;
            }

            if (rainDown || iceRain || domainBurst || bigBolt || pelletVolley || waveSkill)
            {
                float wait = 0f;
                while (!impacted && wait < 4.5f)
                {
                    wait += Mathf.Max(Time.deltaTime, 0.0005f);
                    yield return null;
                }
            }
            if (rasVfx.core != null)
                BattleFx.DestroyRasenganVfx(ref rasVfx);
            if (!impacted && onImpact != null) onImpact();
            pinFeet = false;
            faceLockTarget = null;
            if (Root != null)
            {
                Root.position = homePos;
                Root.rotation = homeRot;
            }
            try
            {
                if (actionGen == myGen) PlayIdleImmediate();
            }
            catch (Exception ex) { Debug.LogWarning(ex.Message); }
        }

        public void ReplayHit(bool ultimate = true)
        {
            StartCoroutine(PlayHit(ultimate));
        }

        public IEnumerator PlayHit(bool ultimate = false)
        {
            int serial = ++hitReactSerial;
            AnimationClip use = (ultimate && ultimateHitClip != null) ? ultimateHitClip : hitClip;
            float maxLen = ultimate ? 1.3f : 0.9f;
            float duration = use != null ? Mathf.Clamp(use.length, 0.28f, maxLen) : 0.35f;
            if (use != null && graphReady) PlayAction(use);
            int myGen = actionGen;
            float waited = 0f;
            float hold = duration * 0.85f;
            while (waited < hold)
            {
                if (serial != hitReactSerial || actionGen != myGen) yield break;
                waited += Time.deltaTime;
                yield return null;
            }
            if (serial != hitReactSerial || actionGen != myGen) yield break;
            PlayIdleImmediate();
        }

        public IEnumerator PlayDown()
        {
            if (downClip != null && graphReady) PlayAction(downClip);
            float duration = downClip != null ? downClip.length : 0.6f;
            yield return new WaitForSeconds(duration);
            if (graphReady) mixer.SetInputWeight(0, 0f);
        }

        public void Flash(Color color)
        {
            ClearFlash();
            flashCo = StartCoroutine(FlashRoutine(color));
        }

        void ClearFlash()
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = null;
            for (int i = 0; i < rends.Count; i++)
            {
                if (rends[i] == null) continue;
                int slots = rends[i].sharedMaterials != null ? rends[i].sharedMaterials.Length : 1;
                for (int m = 0; m < slots; m++)
                    rends[i].SetPropertyBlock(null, m);
            }
        }

        IEnumerator FlashRoutine(Color color)
        {
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < rends.Count; i++)
            {
                if (rends[i] == null) continue;
                int slots = rends[i].sharedMaterials != null ? rends[i].sharedMaterials.Length : 1;
                for (int m = 0; m < slots; m++)
                {
                    var mat = m < rends[i].sharedMaterials.Length ? rends[i].sharedMaterials[m] : rends[i].sharedMaterial;
                    rends[i].GetPropertyBlock(block, m);
                    if (mat != null && mat.HasProperty("_BaseColor")) block.SetColor("_BaseColor", color);
                    if (mat != null && mat.HasProperty("_Color")) block.SetColor("_Color", color);
                    rends[i].SetPropertyBlock(block, m);
                }
            }
            yield return new WaitForSeconds(0.12f);
            for (int i = 0; i < rends.Count; i++)
            {
                if (rends[i] == null) continue;
                int slots = rends[i].sharedMaterials != null ? rends[i].sharedMaterials.Length : 1;
                for (int m = 0; m < slots; m++)
                    rends[i].SetPropertyBlock(null, m);
            }
            flashCo = null;
        }

        public void Shake(float duration = 0.22f, float mag = 0.12f)
        {
            StartCoroutine(ShakeRoutine(duration, mag));
        }

        IEnumerator ShakeRoutine(float duration, float mag)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (Root != null)
                    Root.position = homePos + new Vector3(UnityEngine.Random.Range(-1f, 1f) * mag, 0f, UnityEngine.Random.Range(-1f, 1f) * mag);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (Root != null) Root.position = homePos;
        }

        void CacheRenderers(Transform hero)
        {
            rends.Clear();
            var found = hero.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] == null) continue;
                if (!found[i].gameObject.activeInHierarchy) continue;
                rends.Add(found[i]);
            }
        }

        void EnsureAilmentFx()
        {
            if (Root == null) return;
            if (burnFx == null) burnFx = MakeLoopFx("BurnFx", new Color(1f, 0.4f, 0.08f), new Vector3(0f, 0.55f, 0f), 10f, true);
            if (stunFx == null) stunFx = MakeLoopFx("StunFx", new Color(1f, 0.92f, 0.35f), new Vector3(0f, 1.7f, 0f), 14f, false);
        }

        ParticleSystem MakeLoopFx(string name, Color color, Vector3 localPos, float rate, bool rise)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = rise ? 0.55f : 0.4f;
            main.startSpeed = rise ? 1.1f : 0.35f;
            main.startSize = rise ? 0.18f : 0.12f;
            main.startColor = color;
            main.gravityModifier = rise ? -0.55f : 0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = rise ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Circle;
            shape.radius = rise ? 0.22f : 0.28f;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = BattleVfx.LoopMat(rise ? "flame_03_a.png" : "star_04_a.png", color);
            go.SetActive(false);
            return ps;
        }

        static void SetLoop(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            if (on)
            {
                if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
                if (!ps.isPlaying) ps.Play();
            }
            else if (ps.gameObject.activeSelf)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            ClearFlash();
            if (graphReady && graph.IsValid()) graph.Destroy();
            graphReady = false;
        }
    }

    public static class BattleFx
    {
        public struct RasenganVfx
        {
            public GameObject core;
            public GameObject shell;
            public GameObject ringA;
            public GameObject ringB;
            public GameObject[] bits;
        }

        public static RasenganVfx SpawnRasenganVfx()
        {
            Color aqua = new Color(0.22f, 0.78f, 1f);
            Color coreC = new Color(0.82f, 0.95f, 1f);
            const int bitsN = 12;
            var bits = new GameObject[bitsN];
            for (int i = 0; i < bitsN; i++)
                bits[i] = BattleVfx.MakeEnergyOrb(coreC, aqua, 0.04f);
            return new RasenganVfx
            {
                core = BattleVfx.MakeEnergyOrb(coreC, aqua, 0.18f),
                shell = BattleVfx.MakeEnergyOrb(aqua, coreC, 0.32f),
                ringA = BattleVfx.MakeWaveRing(aqua, true),
                ringB = BattleVfx.MakeWaveRing(coreC, true),
                bits = bits
            };
        }

        public static void UpdateRasenganVfx(RasenganVfx vfx, Vector3 palm, Vector3 to, float size, float spin)
        {
            PutRasengan(palm, to, vfx.core, vfx.shell, vfx.bits, vfx.ringA, vfx.ringB, size, spin);
        }

        public static void RasenganHit(Vector3 to, Action onHit, ref RasenganVfx vfx)
        {
            Color aqua = new Color(0.22f, 0.78f, 1f);
            Color coreC = new Color(0.82f, 0.95f, 1f);
            Burst(to, aqua, 0.34f, 22);
            Burst(to, coreC, 0.22f, 12);
            Bloom(to, aqua, 0.68f);
            DestroyRasenganVfx(ref vfx);
            if (onHit != null) onHit();
        }

        public static void DestroyRasenganVfx(ref RasenganVfx vfx)
        {
            const int bitsN = 12;
            DestroyRasengan(vfx.core, vfx.shell, vfx.bits, bitsN, vfx.ringA, vfx.ringB);
            vfx.core = null;
            vfx.shell = null;
            vfx.ringA = null;
            vfx.ringB = null;
            vfx.bits = null;
        }

        public static void Burst(Vector3 pos, Color color, float size, int count)
        {
            BattleVfx.Burst(pos, color, Mathf.Max(0.18f, size), Mathf.Max(count, 10), 3.8f + size * 4f);
        }

        public static void Bloom(Vector3 pos, Color color, float size)
        {
            Burst(pos, color, size * 0.7f, 26);
            Burst(pos, Color.Lerp(color, Color.white, 0.5f), size * 0.45f, 16);
            var ring = BattleVfx.MakeWaveRing(Color.Lerp(color, Color.white, 0.35f), true);
            ring.transform.position = pos;
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(size * 2.1f, 0.11f, size * 2.1f);
            UnityEngine.Object.Destroy(ring, 0.28f);
            var orb = BattleVfx.MakeEnergyOrb(Color.Lerp(color, Color.white, 0.4f), color, size * 0.42f);
            orb.transform.position = pos;
            UnityEngine.Object.Destroy(orb, 0.28f);
        }

        /// <summary>
        /// 念力で武器(複数可)を浮かせ、スピンさせながら対象へ叩きつける演出。
        /// キャラクターの手には持たせず、独立オブジェクトとして飛ばす。
        /// </summary>
        public static IEnumerator TelekinesisSlam(Transform caster, Vector3 from, Vector3 to, TelekinesisWeaponData data, Action onHit)
        {
            if (data == null)
            {
                yield return Bolt(from, to, new Color(0.6f, 0.85f, 1f), 0.3f, onHit);
                yield break;
            }

            int count = Mathf.Clamp(data.weaponCount, 1, 4);
            var weapons = new List<GameObject>();
            var startPos = new List<Vector3>();
            var ctrlPos = new List<Vector3>();
            var landPos = new List<Vector3>();
            GameObject prefab = null;
            if (data.kind == FallingObjectKind.Sword && !string.IsNullOrEmpty(data.weaponPrefabPath))
                prefab = PlayerAssets.Load<GameObject>(data.weaponPrefabPath);
            // 対象へ向かう方向を軸に、斜め上・後方から弧を描いて降ってくる軌道を作る。
            Vector3 approachDir = to - (caster != null ? caster.position : from);
            approachDir.y = 0f;
            if (approachDir.sqrMagnitude < 0.01f) approachDir = Vector3.forward;
            approachDir.Normalize();
            Vector3 sideDir = Vector3.Cross(Vector3.up, approachDir);

            for (int i = 0; i < count; i++)
            {
                GameObject w = data.kind == FallingObjectKind.IceChunk
                    ? BattleVfx.MakeIceChunk(data.auraColor)
                    : (prefab != null ? UnityEngine.Object.Instantiate(prefab) : BattleVfx.MakeBoltBody(data.auraColor, false));
                w.name = data.kind == FallingObjectKind.IceChunk ? "VfxFallingIce" : "VfxFallingSword";
                float lane = count > 1 ? (i - (count - 1) * 0.5f) : 0f;
                Vector3 lateral = sideDir * (lane * data.floatRadius);
                Vector3 spawn = data.rainFromAbove
                    ? to + lateral + Vector3.up * 5.8f
                    : to + lateral - approachDir * 2.2f + Vector3.up * 3.2f;
                w.transform.position = spawn;
                weapons.Add(w);
                startPos.Add(spawn);
                ctrlPos.Add(data.rainFromAbove
                    ? to + lateral * 0.45f + Vector3.up * 2.6f
                    : to + lateral - approachDir * 0.8f + Vector3.up * 2.2f);
                landPos.Add(to + lateral * 0.35f);
                Aura(w.transform, data.auraColor, 1.0f);
                Burst(spawn, data.auraColor, 0.16f, 10);
                if (data.rainFromAbove)
                {
                    Vector3 tip = landPos[i] - spawn;
                    if (tip.sqrMagnitude > 0.0001f)
                        w.transform.rotation = Quaternion.LookRotation(tip.normalized, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                }
            }

            // 一瞬だけ浮かせて溜め(降ってくる予兆)。
            float hover = data.rainFromAbove ? 0.04f : 0.2f;
            float t = 0f;
            while (t < hover)
            {
                t += Time.deltaTime;
                for (int i = 0; i < weapons.Count; i++)
                {
                    if (weapons[i] == null) continue;
                    weapons[i].transform.position = startPos[i] + Vector3.up * (Mathf.Sin(t * 10f + i) * 0.08f);
                    if (data.rainFromAbove)
                    {
                        Vector3 tip = landPos[i] - weapons[i].transform.position;
                        if (tip.sqrMagnitude > 0.0001f)
                            weapons[i].transform.rotation = Quaternion.LookRotation(tip.normalized, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                    }
                }
                yield return null;
            }

            // 弧を描いて急降下(重い処理を避けるため2次ベジェで位置補間するだけ)。
            float diveTime = data.rainFromAbove ? 0.2f : 0.3f;
            float stagger = weapons.Count > 1 ? (data.rainFromAbove ? 0.03f : 0.05f) : 0f;
            var landed = new bool[weapons.Count];
            bool hitFired = false;
            float total = diveTime + stagger * Mathf.Max(0, weapons.Count - 1);
            t = 0f;
            while (t < total)
            {
                t += Time.deltaTime;
                for (int i = 0; i < weapons.Count; i++)
                {
                    if (weapons[i] == null || landed[i]) continue;
                    float localT = t - stagger * i;
                    if (localT <= 0f) continue;
                    float u = Mathf.Clamp01(localT / diveTime);
                    Vector3 a = Vector3.Lerp(startPos[i], ctrlPos[i], u);
                    Vector3 b = Vector3.Lerp(ctrlPos[i], landPos[i], u);
                    Vector3 pos = Vector3.Lerp(a, b, u);
                    Vector3 dir = (landPos[i] - pos).sqrMagnitude > 0.0001f
                        ? (landPos[i] - pos).normalized
                        : ((b - a).sqrMagnitude > 0.0001f ? (b - a).normalized : Vector3.down);
                    weapons[i].transform.position = pos;
                    weapons[i].transform.rotation = data.rainFromAbove
                        ? Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f)
                        : Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(75f, 0f, data.spinSpeed * t);
                    if (u >= 1f)
                    {
                        landed[i] = true;
                        Burst(landPos[i], data.auraColor, 0.28f, 16);
                        Bloom(landPos[i], data.auraColor, 0.95f);
                        if (data.rainFromAbove && !hitFired && onHit != null)
                        {
                            hitFired = true;
                            onHit();
                        }
                    }
                }
                yield return null;
            }

            Burst(to, data.auraColor, 0.4f, 24);
            Bloom(to, data.auraColor, 1.15f);
            if (!hitFired && onHit != null) onHit();

            yield return new WaitForSecondsRealtime(0.15f);
            for (int i = 0; i < weapons.Count; i++)
                if (weapons[i] != null) UnityEngine.Object.Destroy(weapons[i]);
        }

        /// <summary>
        /// 弓を放った位置から剣を3本、対象へ飛ばす。アバターのメッシュの外から出す。
        /// </summary>
        public static IEnumerator SwordVolley(Transform caster, Vector3 to, TelekinesisWeaponData data, Action onHit)
        {
            Color color = data != null ? data.auraColor : new Color(0.95f, 0.75f, 0.35f);
            if (data == null)
            {
                Vector3 fallback = caster != null ? caster.position + Vector3.up * 1.2f : to;
                yield return Bolt(fallback, to, color, 0.18f, onHit);
                yield break;
            }

            int count = 3;
            GameObject prefab = null;
            if (!string.IsNullOrEmpty(data.weaponPrefabPath))
                prefab = PlayerAssets.Load<GameObject>(data.weaponPrefabPath);
            Vector3 origin = caster != null ? caster.position + Vector3.up * 1.15f : to + Vector3.back;
            Vector3 aim = to - origin;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = caster != null ? caster.forward : Vector3.forward;
            aim.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, aim);
            float clearance = 1.15f;
            if (caster != null)
            {
                var rends = caster.GetComponentsInChildren<Renderer>();
                for (int r = 0; r < rends.Length; r++)
                {
                    if (rends[r] == null || rends[r] is ParticleSystemRenderer) continue;
                    if (!rends[r].enabled || !rends[r].gameObject.activeInHierarchy) continue;
                    var b = rends[r].bounds;
                    float along = Vector3.Dot(b.center - origin, aim);
                    float rad = new Vector2(b.extents.x, b.extents.z).magnitude + 0.35f;
                    clearance = Mathf.Max(clearance, along + rad + 0.55f);
                }
            }

            Vector3 muzzle = origin + aim * clearance;
            var weapons = new List<GameObject>();
            var startPos = new List<Vector3>();
            var endPos = new List<Vector3>();
            float[] height = { 0.08f, 0.28f, -0.04f };
            for (int i = 0; i < count; i++)
            {
                GameObject w = prefab != null
                    ? UnityEngine.Object.Instantiate(prefab)
                    : BattleVfx.MakeBoltBody(color, false);
                w.name = "VfxFlyingSword";
                var cols = w.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < cols.Length; c++)
                    if (cols[c] != null) UnityEngine.Object.Destroy(cols[c]);
                var rbs = w.GetComponentsInChildren<Rigidbody>(true);
                for (int c = 0; c < rbs.Length; c++)
                    if (rbs[c] != null) UnityEngine.Object.Destroy(rbs[c]);
                w.transform.localScale = Vector3.one * 1.65f;
                float lane = i - 1f;
                Vector3 spawn = muzzle + side * (lane * 0.72f) + Vector3.up * height[i];
                Vector3 land = to + side * (lane * 0.45f) + Vector3.up * (height[i] * 0.35f);
                w.transform.position = spawn;
                Vector3 fly = land - spawn;
                if (fly.sqrMagnitude > 0.0001f)
                    w.transform.rotation = Quaternion.LookRotation(fly.normalized, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                weapons.Add(w);
                startPos.Add(spawn);
                endPos.Add(land);
                var grip = BattleVfx.MakeEnergyOrb(Color.Lerp(color, Color.white, 0.45f), color, 0.07f);
                grip.name = "VfxSwordGrip";
                grip.transform.SetParent(w.transform, false);
                grip.transform.localPosition = Vector3.zero;
                grip.transform.localScale = Vector3.one * 0.09f;
            }

            float flyTime = 0.26f;
            float stagger = 0.07f;
            var landed = new bool[count];
            bool hit = false;
            float total = flyTime + stagger * (count - 1);
            float t = 0f;
            while (t < total)
            {
                t += Time.deltaTime;
                for (int i = 0; i < weapons.Count; i++)
                {
                    if (weapons[i] == null || landed[i]) continue;
                    float localT = t - stagger * i;
                    if (localT <= 0f) continue;
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(localT / flyTime));
                    Vector3 pos = Vector3.Lerp(startPos[i], endPos[i], u);
                    Vector3 fly = endPos[i] - startPos[i];
                    weapons[i].transform.position = pos;
                    if (fly.sqrMagnitude > 0.0001f)
                        weapons[i].transform.rotation = Quaternion.LookRotation(fly.normalized, Vector3.up)
                            * Quaternion.Euler(-90f, 0f, data.spinSpeed * t * 0.12f);
                    if (u >= 1f)
                    {
                        landed[i] = true;
                        Burst(endPos[i], color, 0.32f, 18);
                        Bloom(endPos[i], color, 1.25f);
                        if (!hit)
                        {
                            hit = true;
                            if (onHit != null) onHit();
                        }
                    }
                }
                yield return null;
            }

            if (!hit && onHit != null) onHit();
            Burst(to, color, 0.48f, 24);
            Bloom(to, color, 1.45f);
            yield return new WaitForSecondsRealtime(0.12f);
            for (int i = 0; i < weapons.Count; i++)
                if (weapons[i] != null) UnityEngine.Object.Destroy(weapons[i]);
        }

        public static IEnumerator Bolt(Vector3 from, Vector3 to, Color color, float seconds, Action onHit)
        {
            bool fire = color.r > color.b + 0.18f;
            var go = BattleVfx.MakeBoltBody(color, fire);
            go.name = "BattleBolt";
            go.transform.position = from;
            Burst(from, color, 0.18f, 12);
            Bloom(from, color, 0.55f);
            BattleVfx.AttachBoltTrail(go.transform, color);
            var sheath = BattleVfx.MakeEnergyBeam(Color.Lerp(color, Color.white, 0.4f), color, 0.035f, 0.09f);
            float t = 0f;
            seconds = Mathf.Max(0.18f, seconds);
            while (t < 1f)
            {
                t += Time.deltaTime / seconds;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                go.transform.position = Vector3.Lerp(from, to, u);
                float s = 0.55f + Mathf.Sin(u * Mathf.PI) * 0.22f;
                go.transform.localScale = new Vector3(s, s, 1f);
                BattleVfx.PlaceBeam(sheath, from, go.transform.position, 0.9f);
                yield return null;
            }
            UnityEngine.Object.Destroy(go);
            if (sheath != null) UnityEngine.Object.Destroy(sheath);
            Burst(to, color, 0.32f, 26);
            Bloom(to, color, 0.95f);
            if (onHit != null) onHit();
        }

        public static IEnumerator Kurodama(Vector3 from, Vector3 to, Action onHit)
        {
            Color black = new Color(0.03f, 0.01f, 0.04f);
            Color rim = new Color(0.22f, 0.03f, 0.06f);
            var ball = BattleVfx.MakeDarkSphere(black, 1f);
            ball.name = "VfxKurodama";
            ball.transform.position = from;
            ball.transform.localScale = Vector3.one * 0.12f;
            Burst(from, rim, 0.2f, 10);
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.22f);
                ball.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.9f, u);
                yield return null;
            }
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
                ball.transform.position = Vector3.Lerp(from, to, u);
                yield return null;
            }
            t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.45f);
                ball.transform.position = to;
                ball.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 3.4f, u);
                yield return null;
            }
            Bloom(to, rim, 1.5f);
            Burst(to, black, 0.55f, 22);
            if (onHit != null) onHit();
            t = 0f;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                if (ball != null) ball.transform.localScale = Vector3.one * Mathf.Lerp(3.4f, 0.04f, t / 0.28f);
                yield return null;
            }
            if (ball != null) UnityEngine.Object.Destroy(ball);
        }

        public static IEnumerator BigBolt(Vector3 from, Vector3 to, Color color, float scale, float seconds, Action onHit)
        {
            var go = BattleVfx.MakeBigBolt(color, scale);
            go.transform.position = from;
            Burst(from, color, 0.42f, 18);
            Bloom(from, color, 0.85f);
            BattleVfx.AttachBoltTrail(go.transform, color);
            var sheath = BattleVfx.MakeEnergyBeam(Color.Lerp(color, Color.white, 0.35f), color, 0.08f, 0.2f);
            const int sparks = 6;
            var bits = new GameObject[sparks];
            for (int i = 0; i < sparks; i++)
                bits[i] = BattleVfx.MakeEnergyOrb(color, 0.08f);
            float t = 0f;
            seconds = Mathf.Max(0.35f, seconds);
            while (t < 1f)
            {
                t += Time.deltaTime / seconds;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                go.transform.position = Vector3.Lerp(from, to, u);
                float pulse = 1f + Mathf.Sin(u * Mathf.PI) * 0.12f;
                go.transform.localScale = Vector3.one * scale * pulse;
                BattleVfx.PlaceBeam(sheath, from, go.transform.position, 1.05f);
                for (int i = 0; i < sparks; i++)
                {
                    if (bits[i] == null) continue;
                    float ang = t * 16f + i * (Mathf.PI * 2f / sparks);
                    float r = 0.28f * scale;
                    bits[i].transform.position = go.transform.position
                        + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang * 1.3f) * r * 0.4f, Mathf.Sin(ang) * r);
                }
                yield return null;
            }
            UnityEngine.Object.Destroy(go);
            if (sheath != null) UnityEngine.Object.Destroy(sheath);
            for (int i = 0; i < sparks; i++)
                if (bits[i] != null) UnityEngine.Object.Destroy(bits[i]);
            Burst(to, color, 0.55f, 32);
            Bloom(to, color, 1.45f);
            if (onHit != null) onHit();
        }

        public static IEnumerator DomainBurst(Vector3 to, Color color, Action onHit)
        {
            Vector3 center = to;
            var root = new GameObject("VfxDomain");
            root.transform.position = center;
            var dome = BattleVfx.MakeDomainShell(color, false);
            dome.transform.SetParent(root.transform, false);
            var ring = BattleVfx.MakeDomainShell(color, true);
            ring.transform.SetParent(root.transform, false);
            var core = BattleVfx.MakeEnergyOrb(Color.Lerp(color, Color.white, 0.35f), color, 0.35f);
            core.transform.SetParent(root.transform, false);
            var ring2 = BattleVfx.MakeDomainShell(Color.Lerp(color, Color.white, 0.25f), true);
            ring2.transform.SetParent(root.transform, false);
            Burst(center, color, 0.28f, 12);
            Bloom(center, color, 0.8f);
            float expand = 0.55f;
            float t = 0f;
            while (t < expand)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / expand));
                float r = Mathf.Lerp(0.22f, 4.0f, u);
                dome.transform.localScale = new Vector3(r, r * 0.82f, r);
                ring.transform.localScale = new Vector3(r * 1.05f, 0.07f, r * 1.05f);
                if (core != null) core.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 2.4f, u);
                if (ring2 != null) ring2.transform.localScale = new Vector3(r * 0.72f, 0.09f, r * 0.72f);
                yield return null;
            }
            Burst(to, color, 0.72f, 36);
            Bloom(to, color, 2.1f);
            BattleVfx.PlayHit(to, color, VfxHitKind.Ultimate);
            UnityEngine.Object.Destroy(root);
            if (onHit != null) onHit();
        }

        public static IEnumerator PelletVolley(Vector3 from, Vector3 to, Color color, int count, Action onHit, bool asteroid = false)
        {
            count = Mathf.Clamp(count, 4, asteroid ? 28 : 20);
            Vector3 aim = to - from;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, aim.normalized);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            var balls = new GameObject[count];
            var start = new Vector3[count];
            var end = new Vector3[count];
            var landed = new bool[count];
            bool hit = false;
            float stagger = asteroid ? 0.028f : 0.045f;
            float fly = asteroid ? 0.18f : 0.15f;
            float spread = asteroid ? 0.42f : 0.12f;
            float total = stagger * (count - 1) + fly;
            float t = 0f;
            while (t < total)
            {
                t += Time.deltaTime;
                for (int i = 0; i < count; i++)
                {
                    float localT = t - stagger * i;
                    if (localT < 0f || landed[i]) continue;
                    if (balls[i] == null)
                    {
                        float lane = (i % 7) - 3f;
                        float lift = ((i % 5) - 2f);
                        start[i] = from + side * (lane * 0.03f) + Vector3.up * (lift * 0.02f);
                        end[i] = to + side * (lane * spread * 0.22f) + Vector3.up * (lift * spread * 0.12f);
                        balls[i] = asteroid ? BattleVfx.MakeTrionCube(color) : BattleVfx.MakePellet(color);
                        balls[i].transform.position = start[i];
                        Burst(start[i], color, asteroid ? 0.08f : 0.12f, asteroid ? 4 : 6);
                    }
                    float u = Mathf.Clamp01(localT / fly);
                    balls[i].transform.position = Vector3.Lerp(start[i], end[i], u);
                    if (asteroid && balls[i] != null)
                        balls[i].transform.rotation = Quaternion.Euler(t * 420f + i * 40f, t * 310f, t * 180f);
                    if (u >= 1f)
                    {
                        landed[i] = true;
                        Burst(end[i], color, asteroid ? 0.12f : 0.16f, asteroid ? 6 : 8);
                        UnityEngine.Object.Destroy(balls[i]);
                        balls[i] = null;
                        if (!hit)
                        {
                            hit = true;
                            Bloom(to, color, 1.05f);
                            if (onHit != null) onHit();
                        }
                    }
                }
                yield return null;
            }
            if (!hit && onHit != null) onHit();
            for (int i = 0; i < count; i++)
                if (balls[i] != null) UnityEngine.Object.Destroy(balls[i]);
        }

        public static IEnumerator AsteroidVolley(Vector3 from, Vector3 to, Color color, Action onHit)
        {
            Vector3 aim = to - from;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, aim);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            Vector3 magPos = from + aim * 0.45f + Vector3.up * 0.08f;
            const int split = 27;
            var bits = new GameObject[split];
            var packedPos = new Vector3[split];
            var spreadPos = new Vector3[split];
            yield return FormSplit27(magPos, aim, color, 0.92f, 0.34f, bits, packedPos, spreadPos);
            yield return FireStraightFromBits(bits, spreadPos, to, side, color, 0.016f, onHit);
        }

        public static IEnumerator AsteroidFullAttack(Transform caster, Vector3 to, Color color, Action onHit)
        {
            Vector3 origin = caster != null ? caster.position : Vector3.zero;
            Vector3 aim = to - origin;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, aim);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            Vector3 posR = origin + Vector3.up * 1.25f + aim * 0.85f + side * 0.72f;
            Vector3 posL = origin + Vector3.up * 1.25f + aim * 0.85f - side * 0.72f;
            var magR = BattleVfx.MakeTrionCube(color, 0.92f);
            var magL = BattleVfx.MakeTrionCube(color, 0.92f);
            magR.name = "VfxFullAttackMagR";
            magL.name = "VfxFullAttackMagL";
            magR.transform.position = posR;
            magL.transform.position = posL;
            Quaternion magRot = Quaternion.Euler(18f, 28f, 12f);
            magR.transform.rotation = magRot;
            magL.transform.rotation = magRot;
            Burst(posR, color, 0.18f, 8);
            Burst(posL, color, 0.18f, 8);
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.2f));
                magR.transform.localScale = Vector3.one * u;
                magL.transform.localScale = Vector3.one * u;
                magR.transform.rotation = Quaternion.Euler(18f, 28f + u * 20f, 12f);
                magL.transform.rotation = Quaternion.Euler(18f, 28f + u * 20f, 12f);
                yield return null;
            }
            magRot = magR.transform.rotation;
            magR.transform.localScale = Vector3.one;
            magL.transform.localScale = Vector3.one;

            const int per = 27;
            var bits = new GameObject[per * 2];
            var packedPos = new Vector3[per * 2];
            var spreadPos = new Vector3[per * 2];
            FillBits27(posR, magRot, aim, color, 0.34f, bits, packedPos, spreadPos, 0);
            FillBits27(posL, magRot, aim, color, 0.34f, bits, packedPos, spreadPos, per);
            if (magR != null) UnityEngine.Object.Destroy(magR);
            if (magL != null) UnityEngine.Object.Destroy(magL);
            Burst(posR, color, 0.16f, 10);
            Burst(posL, color, 0.16f, 10);
            t = 0f;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.28f));
                for (int i = 0; i < bits.Length; i++)
                {
                    if (bits[i] == null) continue;
                    bits[i].transform.position = Vector3.Lerp(packedPos[i], spreadPos[i], u);
                    bits[i].transform.rotation = magRot * Quaternion.Euler(u * 18f, u * 24f, 0f);
                }
                yield return null;
            }
            yield return FireStraightFromBits(bits, spreadPos, to, side, color, 0.016f, onHit, 1.15f, 27, 0.52f, 0.55f);
        }

        static void FillBits27(Vector3 magPos, Quaternion magRot, Vector3 aim, Color color, float bitSize, GameObject[] bits, Vector3[] packedPos, Vector3[] spreadPos, int offset)
        {
            const float packed = 0.3f;
            const float spaced = 0.62f;
            Vector3 cluster = magPos + aim * 0.55f;
            int n = offset;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3 local = new Vector3(x, y, z);
                        packedPos[n] = magPos + magRot * (local * packed);
                        spreadPos[n] = cluster + magRot * (local * spaced);
                        bits[n] = BattleVfx.MakeTrionCube(color, bitSize);
                        bits[n].name = "VfxAsteroidBit";
                        bits[n].transform.position = packedPos[n];
                        bits[n].transform.rotation = magRot;
                        n++;
                    }
                }
            }
        }

        static Vector3 HandWorld(Transform caster, bool right, Vector3 fallback)
        {
            var anim = caster != null ? caster.GetComponentInChildren<Animator>() : null;
            if (anim != null)
            {
                var tr = anim.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                if (tr != null) return tr.position;
            }
            return fallback;
        }

        static IEnumerator FormSplit27(Vector3 magPos, Vector3 aim, Color color, float magSize, float bitSize, GameObject[] bits, Vector3[] packedPos, Vector3[] spreadPos, float spaced = 0.62f)
        {
            var mag = BattleVfx.MakeTrionCube(color, magSize);
            mag.name = "VfxTrionMagazine";
            mag.transform.position = magPos;
            Quaternion magRot = Quaternion.Euler(18f, 28f, 12f);
            mag.transform.rotation = magRot;
            Burst(magPos, color, 0.22f, 10);
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.2f));
                mag.transform.localScale = Vector3.one * u;
                mag.transform.rotation = Quaternion.Euler(18f, 28f + u * 20f, 12f);
                yield return null;
            }
            magRot = mag.transform.rotation;
            mag.transform.localScale = Vector3.one;
            const float packed = 0.3f;
            Vector3 cluster = magPos + aim * 0.55f;
            int n = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3 local = new Vector3(x, y, z);
                        packedPos[n] = magPos + magRot * (local * packed);
                        spreadPos[n] = cluster + magRot * (local * spaced);
                        bits[n] = BattleVfx.MakeTrionCube(color, bitSize);
                        bits[n].name = "VfxAsteroidBit";
                        bits[n].transform.position = packedPos[n];
                        bits[n].transform.rotation = magRot;
                        n++;
                    }
                }
            }
            if (mag != null) UnityEngine.Object.Destroy(mag);
            Burst(magPos, color, 0.18f, 12);
            t = 0f;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.28f));
                for (int i = 0; i < bits.Length; i++)
                {
                    if (bits[i] == null) continue;
                    bits[i].transform.position = Vector3.Lerp(packedPos[i], spreadPos[i], u);
                    bits[i].transform.rotation = magRot * Quaternion.Euler(u * 18f, u * 24f, 0f);
                }
                yield return null;
            }
        }

        static IEnumerator FormNinomiyaWall(Vector3 magPos, Vector3 aim, Vector3 side, Color color, float magSize, float bitSize, GameObject[] bits, Vector3[] packedPos, Vector3[] spreadPos, int offset)
        {
            var mag = BattleVfx.MakeTrionCube(color, magSize);
            mag.name = "VfxFullAttackMag";
            mag.transform.position = magPos;
            Quaternion face = Quaternion.LookRotation(aim, Vector3.up);
            mag.transform.rotation = face;
            Burst(magPos, color, 0.16f, 8);
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                mag.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.16f));
                yield return null;
            }
            mag.transform.localScale = Vector3.one;
            const float packed = 0.16f;
            const float wall = 0.28f;
            int n = offset;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3 local = side * x + Vector3.up * y;
                    packedPos[n] = magPos + local * packed;
                    spreadPos[n] = magPos + aim * 0.55f + local * wall;
                    bits[n] = BattleVfx.MakeTrionCube(color, bitSize);
                    bits[n].name = "VfxFullAttackBit";
                    bits[n].transform.position = packedPos[n];
                    bits[n].transform.rotation = face;
                    n++;
                }
            }
            if (mag != null) UnityEngine.Object.Destroy(mag);
            Burst(magPos, color, 0.14f, 8);
            t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.22f));
                for (int i = offset; i < offset + 9; i++)
                {
                    if (bits[i] == null) continue;
                    bits[i].transform.position = Vector3.Lerp(packedPos[i], spreadPos[i], u);
                }
                yield return null;
            }
        }

        static IEnumerator FireStraightFromBits(GameObject[] bits, Vector3[] spreadPos, Vector3 to, Vector3 side, Color color, float stagger, Action onHit, float bloom = 1.15f, int staggerGroup = 0, float life = 0.12f, float hold = 0f)
        {
            int split = bits.Length;
            int group = staggerGroup > 0 ? staggerGroup : split;
            var beams = new GameObject[split];
            var dests = new Vector3[split];
            var fired = new bool[split];
            bool hit = false;
            float total = stagger * Mathf.Max(0, group - 1) + life + 0.06f;
            float t = 0f;
            while (t < total)
            {
                t += Time.deltaTime;
                for (int i = 0; i < split; i++)
                {
                    float localT = t - stagger * (i % group);
                    if (localT < 0f || fired[i]) continue;
                    Vector3 muzzle = bits[i] != null ? bits[i].transform.position : spreadPos[i];
                    float lane = ((i % 9) - 4f) * 0.08f;
                    dests[i] = to + side * lane + Vector3.up * (((i / 9) - 1f) * 0.12f);
                    if (beams[i] == null)
                    {
                        beams[i] = BattleVfx.MakeEnergyBeam(Color.Lerp(color, Color.white, 0.4f), color, 0.024f, 0.06f);
                        Burst(muzzle, color, 0.05f, 3);
                    }
                    BattleVfx.PlaceBeam(beams[i], muzzle, dests[i], 0.8f);
                    if (localT >= life)
                    {
                        fired[i] = true;
                        Burst(dests[i], color, 0.1f, 4);
                        if (hold <= 0f)
                        {
                            if (beams[i] != null) UnityEngine.Object.Destroy(beams[i]);
                            beams[i] = null;
                        }
                        if (!hit)
                        {
                            hit = true;
                            Bloom(to, color, bloom);
                            if (onHit != null) onHit();
                        }
                    }
                }
                yield return null;
            }
            t = 0f;
            while (t < hold)
            {
                t += Time.deltaTime;
                for (int i = 0; i < split; i++)
                {
                    if (beams[i] == null) continue;
                    Vector3 muzzle = bits[i] != null ? bits[i].transform.position : spreadPos[i];
                    BattleVfx.PlaceBeam(beams[i], muzzle, dests[i], 0.8f);
                }
                yield return null;
            }
            float fade = 0f;
            while (fade < 0.12f)
            {
                fade += Time.deltaTime;
                float u = 1f - fade / 0.12f;
                for (int i = 0; i < split; i++)
                    if (bits[i] != null) bits[i].transform.localScale = Vector3.one * u;
                yield return null;
            }
            if (!hit && onHit != null) onHit();
            for (int i = 0; i < split; i++)
            {
                if (bits[i] != null) UnityEngine.Object.Destroy(bits[i]);
                if (beams[i] != null) UnityEngine.Object.Destroy(beams[i]);
            }
        }

        public static IEnumerator AnimeWave(string kind, Transform caster, Transform target, Color color, Action onHit)
        {
            var self = caster != null ? caster.GetComponent<BattleFighter>() : null;
            Vector3 origin = caster != null ? caster.position : Vector3.zero;
            Vector3 to = target != null ? target.position + Vector3.up * 1.05f : origin + Vector3.forward * 3f;
            Vector3 aim = to - origin;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();
            if (kind == "kamehameha")
                yield return Kamehameha(self, to, color, onHit);
            else if (kind == "cero")
                yield return Cero(self, to, color, onHit);
            else if (kind == "gran_rey_cero")
                yield return GranReyCero(self, to, color, onHit);
            else if (kind == "sakanade")
                yield return Sakanade(onHit);
            else if (kind == "getsuga")
                yield return Getsuga(origin + Vector3.up * 1.15f + aim * 0.4f, to, color, onHit);
            else if (kind == "spirit_gun")
                yield return SpiritGun(origin + Vector3.up * 1.22f + aim * 0.88f, to, color, onHit);
            else if (kind == "shinra_tensei")
                yield return ShinraTensei(origin + Vector3.up * 1.05f, to, color, onHit);
            else if (kind == "makankosappo")
                yield return Makankosappo(self, to, color, onHit);
            else if (kind == "genkidama")
                yield return Genkidama(origin, to, color, onHit);
            else if (kind == "hound")
                yield return Hound(origin + Vector3.up * 1.2f + aim * 1.4f, to, target, color, onHit);
            else if (kind == "meteora")
                yield return Meteora(origin + Vector3.up * 1.2f + aim * 1.4f, to, color, onHit);
            else if (kind == "kurohitsugi")
                yield return Kurohitsugi(to, target, color, onHit);
            else if (kind == "kyoshiki_murasaki")
                yield return KyoshikiMurasaki(self, caster, to, onHit);
            else if (kind == "ryuken")
                yield return Ryuken(self, to, onHit);
            else if (kind == "hakai")
                yield return Hakai(self, target, to, onHit);
            else if (kind == "rasengan")
            {
                var vfx = SpawnRasenganVfx();
                RasenganHit(to, onHit, ref vfx);
                yield return null;
            }
            else if (kind == "kaioken")
                yield return KaiokenKi(caster, color, onHit);
            else if (kind == "reiatsu")
                yield return ReiatsuRelease(caster, color, onHit);
            else if (kind == "shogun_henshin")
                yield return ShogunHenshin(caster, to, color, onHit);
            else if (kind == "element_fire")
                yield return ElementFireball(self, to, onHit);
            else if (kind == "element_fire_burn")
                yield return ElementFireBurn(self, target, to, onHit);
            else if (kind == "element_water")
                yield return ElementWaterRainCuts(self, target, to, onHit);
            else if (kind == "element_water_drown")
                yield return ElementWaterDrown(self, target, to, onHit);
            else if (kind == "element_light")
                yield return ElementLightFinalExplosion(self, target, to, onHit);
            else if (kind == "element_light_punch")
                yield return ElementLightBlitzPunch(self, target, to, onHit);
            else if (kind == "element_nature")
                yield return ElementNatureCrush(self, target, to, onHit);
            else if (kind == "element_nature_pierce")
                yield return ElementNatureBindPierce(self, target, to, onHit);
            else if (kind == "element_dark")
                yield return ElementDarkBlackHole(self, target, to, onHit);
            else if (kind == "element_dark_swallow")
                yield return ElementDarkSwallow(self, target, to, onHit);
            else if (kind == "element_mu")
                yield return ElementMuVoid(self, caster, onHit);
            else if (kind == "element_mu_bind")
                yield return ElementMuPillarBind(self, target, to, onHit);
            else if (kind == "element_fire_core")
                yield return ElementFireCore(self, target, to, onHit);
            else if (kind == "element_water_spear")
                yield return ElementWaterSpear(self, target, to, onHit);
            else if (kind == "element_light_cross")
                yield return ElementLightCross(self, target, to, onHit);
            else if (kind == "element_nature_jaw")
                yield return ElementNatureJaw(self, target, to, onHit);
            else if (kind == "element_dark_hex")
                yield return ElementDarkHex(self, target, to, onHit);
            else if (kind == "element_mu_press")
                yield return ElementMuPress(self, target, to, onHit);
            else
                yield return ShinraTensei(origin + Vector3.up * 1.05f, to, color, onHit);
        }

        static GameObject LoadLongswordPrefab()
        {
            return PlayerAssets.Load<GameObject>(
                "Assets/Polytope Studio/Lowpoly_Weapons/Prefabs/PT_Longsword_01_a.prefab");
        }

        static GameObject SpawnVfxSword(Color color)
        {
            var prefab = LoadLongswordPrefab();
            GameObject w = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : BattleVfx.MakeBoltBody(color, false);
            w.name = "VfxElementSword";
            var cols = w.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] != null) UnityEngine.Object.Destroy(cols[i]);
            w.transform.localScale = Vector3.one * 1.55f;
            return w;
        }

        static GameObject SpawnWoodSword()
        {
            Color bark = new Color(0.62f, 0.42f, 0.22f);
            var prefab = LoadLongswordPrefab();
            GameObject w;
            if (prefab != null)
            {
                w = UnityEngine.Object.Instantiate(prefab);
                w.name = "VfxWoodSword";
                var cols = w.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cols.Length; i++)
                    if (cols[i] != null) UnityEngine.Object.Destroy(cols[i]);
                BattleVfx.PaintWood(w, bark);
                w.transform.localScale = Vector3.one * 2.35f;
                return w;
            }
            w = new GameObject("VfxWoodSword");
            var blade = BattleVfx.MakeSolidBeam(bark, 0.08f, 1f);
            blade.transform.SetParent(w.transform, false);
            BattleVfx.PlaceBeam(blade, new Vector3(0f, 0.2f, 0f), new Vector3(0f, 2.6f, 0f), 0.28f);
            BattleVfx.PaintWood(blade, bark);
            var guard = BattleVfx.MakePropCube(new Color(0.32f, 0.2f, 0.1f), 1f);
            guard.transform.SetParent(w.transform, false);
            guard.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            guard.transform.localScale = new Vector3(0.7f, 0.12f, 0.2f);
            BattleVfx.PaintWood(guard, new Color(0.4f, 0.26f, 0.12f));
            var handle = BattleVfx.MakePropCube(bark, 1f);
            handle.transform.SetParent(w.transform, false);
            handle.transform.localPosition = new Vector3(0f, -0.22f, 0f);
            handle.transform.localScale = new Vector3(0.14f, 0.5f, 0.14f);
            BattleVfx.PaintWood(handle, bark);
            w.transform.localScale = Vector3.one * 2.1f;
            return w;
        }

        static void FlatBasis(BattleFighter self, Vector3 center, out Vector3 fwd, out Vector3 right)
        {
            fwd = Vector3.forward;
            if (self != null && self.Root != null)
            {
                fwd = center - self.Root.position;
                fwd.y = 0f;
            }
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            right = Vector3.Cross(Vector3.up, fwd);
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            right.Normalize();
        }

        static GameObject OpaqueDisc(Color color, float radius, float thick)
        {
            var d = BattleVfx.MakeOpaqueBeam(color, radius);
            d.transform.localScale = new Vector3(radius * 2f, Mathf.Max(0.02f, thick) * 0.5f, radius * 2f);
            return d;
        }

        static void ElementGroundRing(Vector3 center, Color color, float radius, float alphaMul = 1f)
        {
            var ring = BattleVfx.MakeWaveRing(Color.Lerp(color, Color.white, 0.3f), true);
            ring.transform.position = center + Vector3.up * 0.06f;
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(radius, 0.1f * alphaMul, radius);
            UnityEngine.Object.Destroy(ring, 0.75f);
        }

        static int impactFxFrame = -1;

        public static void MarkImpactFxPlayed()
        {
            impactFxFrame = Time.frameCount;
        }

        static void ElementFlinch(Transform foe, bool heavy = true)
        {
            if (foe == null) return;
            var fx = foe.GetComponent<BattleFighter>();
            if (fx == null) fx = foe.GetComponentInParent<BattleFighter>();
            if (fx == null) return;
            if (impactFxFrame != Time.frameCount)
            {
                impactFxFrame = Time.frameCount;
                Vector3 pos = BattleVfx.AtBody(foe);
                Color hit = heavy ? new Color(1f, 0.72f, 0.28f) : new Color(1f, 0.92f, 0.62f);
                BattleVfx.PlayHit(pos, hit, VfxHitKind.Ultimate);
                if (heavy) BattleAudio.ExplosionSmall();
                else BattleAudio.Crunch();
            }
            fx.ReplayHit(true);
            fx.Flash(new Color(1f, 0.32f, 0.18f));
        }

        static IEnumerator ElementHitBeat(bool heavy = false, Transform foe = null)
        {
            ElementFlinch(foe, heavy);
            BattleCam.Punch();
            yield return HitStop(heavy ? 0.065f : 0.04f);
        }

        static IEnumerator ElementLinger(float sec)
        {
            if (sec > 0.3f) sec = 0.3f;
            float t = 0f;
            while (t < sec)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        static void DestroyAll(GameObject[] arr)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] != null) UnityEngine.Object.Destroy(arr[i]);
        }

        static GameObject voidBattleField;

        public static void SetVoidBattleField(bool active, Vector3 center)
        {
            if (!active)
            {
                if (voidBattleField != null)
                {
                    UnityEngine.Object.Destroy(voidBattleField);
                    voidBattleField = null;
                }
                return;
            }
            if (voidBattleField == null)
            {
                voidBattleField = BattleVfx.MakeMistSphere(new Color(0.82f, 0.84f, 0.9f), 0.28f);
                voidBattleField.name = "VoidBattleField";
            }
            voidBattleField.transform.position = new Vector3(center.x, center.y + 0.35f, center.z);
            voidBattleField.transform.localScale = Vector3.one * 8.5f;
        }

        public static IEnumerator ElementFireball(BattleFighter self, Vector3 to, Action onHit)
        {
            Color hot = new Color(0.92f, 0.38f, 0.08f);
            Color coal = new Color(0.55f, 0.18f, 0.05f);
            Vector3 from = self != null ? self.RightHandPos() : to;
            var ball = BattleVfx.MakePropSphere(hot, 0.92f, 0.22f);
            ball.transform.position = from;
            var glow = BattleVfx.MakeEnergyOrb(Color.Lerp(hot, Color.white, 0.35f), hot, 0.1f);
            glow.transform.position = from;
            float t = 0f;
            while (t < 0.38f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.38f);
                from = self != null ? self.RightHandPos() : from;
                if (ball != null)
                {
                    ball.transform.position = from;
                    ball.transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 0.58f, u);
                }
                if (glow != null)
                {
                    glow.transform.position = from;
                    glow.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.42f, u);
                }
                if (Mathf.Repeat(t, 0.11f) < 0.025f)
                    Burst(from + Vector3.up * 0.05f, coal, 0.06f, 3);
                yield return null;
            }
            if (glow != null) UnityEngine.Object.Destroy(glow);
            Vector3 launch = from;
            var shot = BattleVfx.MakePropSphere(hot, 0.95f, 0.38f);
            shot.transform.position = launch;
            BattleVfx.AttachBoltTrail(shot.transform, hot);
            t = 0f;
            bool hit = false;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.45f);
                Vector3 p = Vector3.Lerp(launch, to, u * u * (3f - 2f * u));
                if (shot != null) shot.transform.position = p;
                if (!hit && u > 0.82f)
                {
                    hit = true;
                    Burst(to, hot, 0.35f, 14);
                    ElementGroundRing(to, coal, 1.8f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }
            if (shot != null)
            {
                shot.transform.position = to;
                shot.transform.localScale = Vector3.one * 0.52f;
            }
            var ember = BattleVfx.MakePropSphere(coal, 0.9f, 0.7f);
            ember.transform.position = to;
            yield return ElementLinger(0.42f);
            if (ember != null) UnityEngine.Object.Destroy(ember);
            if (shot != null) UnityEngine.Object.Destroy(shot);
            if (ball != null) UnityEngine.Object.Destroy(ball);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementWaterRainCuts(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color water = new Color(0.32f, 0.62f, 0.82f);
            Color foam = new Color(0.78f, 0.9f, 0.95f);
            Vector3 center = to;
            var puddle = BattleVfx.MakePropCube(water, 0.94f);
            puddle.transform.position = center + Vector3.up * 0.05f;
            puddle.transform.localScale = new Vector3(2.4f, 0.08f, 2.4f);
            const int cuts = 10;
            var blades = new GameObject[cuts];
            var starts = new Vector3[cuts];
            var ends = new Vector3[cuts];
            for (int i = 0; i < cuts; i++)
            {
                float yaw = i * (360f / cuts) + (i % 2) * 11f;
                float pitch = -12f + (i % 5) * 9f;
                Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
                float dist = 3.6f + (i % 4) * 0.4f;
                starts[i] = center + dir * dist + Vector3.up * ((i % 3) * 0.25f);
                ends[i] = center + Vector3.up * 0.15f;
                blades[i] = BattleVfx.MakePropCube(water, 0.96f);
            }
            float t = 0f;
            bool hit = false;
            while (t < 0.58f)
            {
                t += Time.deltaTime;
                float wave = Mathf.Clamp01((t - 0.04f) / 0.42f);
                for (int i = 0; i < cuts; i++)
                {
                    if (blades[i] == null) continue;
                    float delay = i / (float)cuts * 0.38f;
                    float u = Mathf.Clamp01((wave - delay) * 1.35f);
                    Vector3 tip = Vector3.Lerp(starts[i], ends[i], Mathf.SmoothStep(0f, 1f, u));
                    BattleVfx.PlaceBladeCube(blades[i], starts[i], tip, 0.14f + u * 0.08f, 0.04f);
                }
                if (!hit && wave > 0.58f)
                {
                    hit = true;
                    Burst(center, foam, 0.28f, 12);
                    ElementGroundRing(center, foam, 2f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }
            yield return ElementLinger(0.4f);
            if (puddle != null) UnityEngine.Object.Destroy(puddle);
            DestroyAll(blades);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementLightFinalExplosion(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color warm = new Color(1f, 0.84f, 0.48f);
            Color coreC = new Color(1f, 0.96f, 0.75f);
            Vector3 selfHome = self != null && self.Root != null ? self.Root.position : Vector3.zero;
            Quaternion selfRot = self != null && self.Root != null ? self.Root.rotation : Quaternion.identity;
            Vector3 chest = foe != null ? foe.position + Vector3.up * 1.05f : to;
            if (self != null && self.Root != null && foe != null)
            {
                Vector3 flat = foe.position - selfHome;
                flat.y = 0f;
                if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
                flat.Normalize();
                Vector3 spot = foe.position - flat * 1.15f;
                spot.y = selfHome.y;
                self.Root.position = spot;
                self.Root.rotation = Quaternion.LookRotation(flat, Vector3.up);
                chest = foe.position + Vector3.up * 1.05f;
            }
            Vector3 chargeCenter = self != null
                ? Vector3.Lerp(self.LeftPalmPos(chest), self.RightHandPos(), 0.5f)
                : chest + Vector3.up * 0.2f;
            var coreSolid = BattleVfx.MakePropSphere(coreC, 0.94f, 0.28f);
            coreSolid.transform.position = chargeCenter;
            float t = 0f;
            while (t < 0.38f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.38f);
                if (self != null)
                    chargeCenter = Vector3.Lerp(self.LeftPalmPos(chest), self.RightHandPos(), 0.5f);
                float s = Mathf.Lerp(0.22f, 1.12f, u * u);
                if (coreSolid != null)
                {
                    coreSolid.transform.position = chargeCenter;
                    coreSolid.transform.localScale = Vector3.one * s;
                }
                yield return null;
            }
            Vector3 blastAt = self != null && self.Root != null
                ? self.Root.position + Vector3.up * 1.05f
                : chargeCenter;
            if (coreSolid != null) UnityEngine.Object.Destroy(coreSolid);
            var blast = BattleVfx.MakePropSphere(coreC, 0.92f, 0.55f);
            blast.transform.position = blastAt;
            t = 0f;
            bool hit = false;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.22f);
                float scale = Mathf.Lerp(0.4f, 5.6f, u * u);
                if (blast != null)
                {
                    blast.transform.position = blastAt;
                    blast.transform.localScale = Vector3.one * scale;
                }
                if (!hit && u > 0.18f)
                {
                    hit = true;
                    Burst(blastAt, warm, 0.48f, 18);
                    ElementGroundRing(blastAt, warm, 3.4f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }
            t = 0f;
            while (t < 0.42f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.42f);
                if (blast != null)
                    blast.transform.localScale = Vector3.one * Mathf.Lerp(5.6f, 2.4f, u);
                yield return null;
            }
            if (blast != null) UnityEngine.Object.Destroy(blast);
            if (self != null && self.Root != null)
            {
                self.Root.position = selfHome;
                self.Root.rotation = selfRot;
            }
            if (!hit && onHit != null) onHit();
        }

        static void PlaceWoodRing(GameObject[] rings, int layer, int segs, Vector3 center, float y, float radius, float thick)
        {
            int off = layer * segs;
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * (Mathf.PI * 2f / segs);
                float a1 = (i + 1) * (Mathf.PI * 2f / segs);
                Vector3 p0 = center + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
                Vector3 p1 = center + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
                p0.y = y;
                p1.y = y;
                if (rings[off + i] != null)
                    BattleVfx.PlaceBeam(rings[off + i], p0, p1, thick);
            }
        }

        public static IEnumerator ElementNatureCrush(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color bark = new Color(0.4f, 0.29f, 0.17f);
            Color barkDark = new Color(0.32f, 0.22f, 0.12f);
            Color leaf = new Color(0.22f, 0.48f, 0.2f);
            Vector3 center = foe != null ? foe.position : to;
            float ground = center.y;
            Vector3 body = center + Vector3.up * 1.05f;
            const int pillarN = 8;
            const int ringH = 5;
            const float cageR = 1.72f;
            const float wrapR = 0.78f;
            const float pillarH = 3.55f;
            var pillars = new GameObject[pillarN];
            var roots = new Vector3[pillarN];
            for (int i = 0; i < pillarN; i++)
            {
                float ang = i * (Mathf.PI * 2f / pillarN);
                roots[i] = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * cageR;
                roots[i].y = ground;
                pillars[i] = BattleVfx.MakeSolidBeam(bark, 0.24f, 1f);
            }

            var floor = BattleVfx.MakePropCube(barkDark, 1f);
            floor.transform.position = center + Vector3.up * 0.04f;
            floor.transform.localScale = new Vector3(cageR * 2.2f, 0.1f, cageR * 2.2f);

            var rings = new GameObject[ringH * pillarN];
            for (int i = 0; i < rings.Length; i++)
                rings[i] = BattleVfx.MakeSolidBeam(i % 2 == 0 ? bark : barkDark, 0.14f, 1f);

            const int vineN = 16;
            var vines = new GameObject[vineN];
            for (int i = 0; i < vineN; i++)
                vines[i] = BattleVfx.MakeSolidBeam(i % 2 == 0 ? bark : barkDark, 0.15f, 1f);

            const int roofN = 8;
            var roof = new GameObject[roofN];
            for (int i = 0; i < roofN; i++)
            {
                roof[i] = BattleVfx.MakePropCube(leaf, 1f);
                roof[i].transform.localScale = new Vector3(1.35f, 0.28f, 0.95f);
            }

            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
                float h = Mathf.Lerp(0.18f, pillarH, u);
                for (int i = 0; i < pillarN; i++)
                    BattleVfx.PlaceBeam(pillars[i], roots[i], roots[i] + Vector3.up * h, 0.5f + u * 0.22f);
                if (floor != null)
                    floor.transform.localScale = new Vector3(
                        Mathf.Lerp(0.4f, cageR * 2.2f, u), 0.1f, Mathf.Lerp(0.4f, cageR * 2.2f, u));
                yield return null;
            }

            t = 0f;
            while (t < 0.48f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.48f));
                for (int layer = 0; layer < ringH; layer++)
                {
                    float y = ground + 0.28f + layer * 0.62f;
                    float r = Mathf.Lerp(cageR, wrapR + layer * 0.04f, u);
                    PlaceWoodRing(rings, layer, pillarN, center, y, r, 0.36f + u * 0.12f);
                }
                for (int i = 0; i < vineN; i++)
                {
                    int p = i % pillarN;
                    float turns = 1.15f + (i % 3) * 0.25f;
                    float h0 = 0.2f + (i % 4) * 0.18f;
                    float h1 = 1.85f + (i % 5) * 0.22f;
                    float a0 = p * (Mathf.PI * 2f / pillarN);
                    float a1 = a0 + turns * Mathf.PI * u;
                    Vector3 start = roots[p] + Vector3.up * Mathf.Lerp(0.15f, h0, u);
                    Vector3 end = center + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * Mathf.Lerp(cageR, wrapR, u);
                    end.y = ground + Mathf.Lerp(h0, h1, u);
                    if (vines[i] != null)
                        BattleVfx.PlaceBeam(vines[i], start, end, 0.4f);
                }
                for (int i = 0; i < roofN; i++)
                {
                    if (roof[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / roofN);
                    float rad = Mathf.Lerp(cageR * 0.95f, 0.55f, u);
                    Vector3 pos = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * rad;
                    pos.y = ground + Mathf.Lerp(pillarH + 0.45f, pillarH - 0.15f, u);
                    roof[i].transform.position = pos;
                    roof[i].transform.rotation = Quaternion.Euler(Mathf.Lerp(28f, 8f, u), ang * Mathf.Rad2Deg, 0f);
                }
                yield return null;
            }

            t = 0f;
            bool hit = false;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.32f));
                float tight = Mathf.Lerp(wrapR, 0.58f, u);
                for (int layer = 0; layer < ringH; layer++)
                {
                    float y = ground + 0.28f + layer * 0.62f;
                    PlaceWoodRing(rings, layer, pillarN, center, y, tight + layer * 0.03f, 0.5f);
                }
                for (int i = 0; i < vineN; i++)
                {
                    int p = i % pillarN;
                    float turns = 1.35f + (i % 3) * 0.28f;
                    float a0 = p * (Mathf.PI * 2f / pillarN);
                    float a1 = a0 + turns * Mathf.PI;
                    Vector3 start = roots[p] + Vector3.up * (0.25f + (i % 4) * 0.2f);
                    Vector3 end = center + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * tight;
                    end.y = ground + 0.55f + (i % 5) * 0.38f;
                    if (vines[i] != null)
                        BattleVfx.PlaceBeam(vines[i], start, end, 0.46f);
                }
                for (int i = 0; i < roofN; i++)
                {
                    if (roof[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / roofN);
                    Vector3 pos = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Mathf.Lerp(0.55f, 0.32f, u);
                    pos.y = ground + Mathf.Lerp(pillarH - 0.15f, 2.25f, u);
                    roof[i].transform.position = pos;
                }
                if (!hit && u > 0.55f)
                {
                    hit = true;
                    Burst(body, leaf, 0.22f, 10);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }

            t = 0f;
            while (t < 0.62f)
            {
                t += Time.deltaTime;
                float pulse = 0.58f + Mathf.Sin(t * 7f) * 0.03f;
                for (int layer = 0; layer < ringH; layer++)
                    PlaceWoodRing(rings, layer, pillarN, center, ground + 0.28f + layer * 0.62f, pulse + layer * 0.03f, 0.5f);
                yield return null;
            }

            if (floor != null) UnityEngine.Object.Destroy(floor);
            DestroyAll(pillars);
            DestroyAll(rings);
            DestroyAll(vines);
            DestroyAll(roof);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementDarkBlackHole(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color ink = new Color(0.03f, 0.01f, 0.05f);
            Color violet = new Color(0.55f, 0.12f, 0.78f);
            Vector3 pit = to;
            pit.y = foe != null ? foe.position.y + 0.2f : pit.y;
            var hole = BattleVfx.MakeOpaqueSphere(ink, 0.55f);
            var core = BattleVfx.MakeOpaqueSphere(new Color(0.08f, 0.03f, 0.1f), 0.35f);
            hole.transform.position = pit + Vector3.up * 0.35f;
            core.transform.position = pit + Vector3.up * 0.45f;
            const int diskN = 2;
            var disks = new GameObject[diskN];
            for (int i = 0; i < diskN; i++)
            {
                disks[i] = BattleVfx.MakeOpaqueSphere(new Color(0.06f, 0.02f, 0.08f), 0.25f);
                disks[i].transform.position = pit + Vector3.up * (0.2f + i * 0.12f);
            }
            const int swordN = 8;
            var swords = new GameObject[swordN];
            var trails = new GameObject[swordN];
            for (int i = 0; i < swordN; i++)
            {
                swords[i] = SpawnVfxSword(new Color(0.35f, 0.32f, 0.38f));
                trails[i] = null;
                float a = i * (Mathf.PI * 2f / swordN);
                swords[i].transform.position = pit + new Vector3(Mathf.Cos(a), 0.55f, Mathf.Sin(a)) * 3.1f;
            }
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.5f);
                if (hole != null)
                    hole.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 2.15f, u);
                for (int i = 0; i < diskN; i++)
                {
                    if (disks[i] == null) continue;
                    float r = Mathf.Lerp(0.25f, 1.8f - i * 0.2f, u);
                    disks[i].transform.localScale = new Vector3(r, 0.05f, r);
                }
                for (int i = 0; i < swordN; i++)
                {
                    if (swords[i] == null) continue;
                    float a = i * (Mathf.PI * 2f / swordN) + u * 3.1f;
                    float rad = Mathf.Lerp(3.1f, 1.25f, u);
                    Vector3 pos = pit + new Vector3(Mathf.Cos(a), 0.62f + Mathf.Sin(u * 5f + i) * 0.1f, Mathf.Sin(a)) * rad;
                    swords[i].transform.position = pos;
                    Vector3 aim = pit + Vector3.up * 0.95f - pos;
                    if (aim.sqrMagnitude > 0.0001f)
                        swords[i].transform.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                }
                if (core != null)
                    core.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 0.55f, u);
                yield return null;
            }
            t = 0f;
            bool hit = false;
            while (t < 0.24f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.24f);
                float ease = u * u * (3f - 2f * u);
                for (int i = 0; i < swordN; i++)
                {
                    if (swords[i] == null) continue;
                    Vector3 end = pit + Vector3.up * 0.95f;
                    swords[i].transform.position = Vector3.Lerp(swords[i].transform.position, end, ease);
                }
                if (!hit && u > 0.48f)
                {
                    hit = true;
                    Burst(pit, violet, 0.42f, 16);
                    ElementGroundRing(pit, ink, 2.2f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }
            yield return ElementLinger(0.45f);
            if (hole != null) UnityEngine.Object.Destroy(hole);
            if (core != null) UnityEngine.Object.Destroy(core);
            DestroyAll(disks);
            DestroyAll(swords);
            DestroyAll(trails);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementMuVoid(BattleFighter self, Transform caster, Action onHit)
        {
            Vector3 feet = caster != null ? caster.position : Vector3.zero;
            Vector3 center = feet + Vector3.up * 0.25f;
            Color mist = new Color(0.82f, 0.84f, 0.9f);
            var pulse = BattleVfx.MakePropSphere(mist, 0.28f, 0.45f);
            pulse.transform.position = center;
            var ground = BattleVfx.MakeMistSphere(mist, 0.22f);
            ground.transform.position = feet + Vector3.up * 0.04f;
            ground.transform.localScale = new Vector3(1.2f, 0.08f, 1.2f);
            float t = 0f;
            while (t < 0.34f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.34f);
                if (pulse != null)
                {
                    pulse.transform.position = center + Vector3.up * (u * 0.2f);
                    pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.45f, 0.95f, u);
                }
                if (ground != null)
                    ground.transform.localScale = new Vector3(Mathf.Lerp(1.2f, 4f, u), 0.08f, Mathf.Lerp(1.2f, 4f, u));
                yield return null;
            }
            if (pulse != null) UnityEngine.Object.Destroy(pulse);
            if (ground != null) UnityEngine.Object.Destroy(ground);
            Burst(feet, mist, 0.22f, 10);
            SetVoidBattleField(true, feet);
            if (onHit != null) onHit();
            yield return ElementLinger(0.35f);
        }

        public static IEnumerator ElementFireBurn(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color ember = new Color(1f, 0.38f, 0.05f);
            Color tip = new Color(1f, 0.78f, 0.18f);
            Color whiteHot = new Color(1f, 0.95f, 0.65f);
            Vector3 feet = foe != null ? foe.position : to;
            Vector3 body = feet + Vector3.up * 1.05f;
            const int flameN = 18;
            var tongues = new GameObject[flameN];
            var cores = new GameObject[flameN];
            var baseAng = new float[flameN];
            var baseH = new float[flameN];
            var baseR = new float[flameN];
            for (int i = 0; i < flameN; i++)
            {
                baseAng[i] = i * (Mathf.PI * 2f / flameN) + (i % 3) * 0.17f;
                baseR[i] = 0.28f + (i % 4) * 0.14f;
                baseH[i] = 0.45f + (i % 5) * 0.22f;
                tongues[i] = BattleVfx.MakeFlameTongue(i % 2 == 0 ? ember : tip, baseH[i]);
                cores[i] = BattleVfx.MakeEnergyOrb(whiteHot, ember, 0.08f);
            }
            float t = 0f;
            bool hit = false;
            while (t < 0.58f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.58f));
                for (int i = 0; i < flameN; i++)
                {
                    float flick = 0.78f + 0.28f * Mathf.PerlinNoise(i * 1.7f, t * 9f);
                    float ang = baseAng[i] + t * (1.6f + (i % 3) * 0.4f);
                    float rise = Mathf.Lerp(0.08f, 0.2f + (i % 6) * 0.28f, u) + Mathf.Sin(t * 14f + i) * 0.05f;
                    Vector3 p = feet + new Vector3(Mathf.Cos(ang) * baseR[i], rise, Mathf.Sin(ang) * baseR[i]);
                    if (tongues[i] != null)
                    {
                        tongues[i].transform.position = p + Vector3.up * (baseH[i] * 0.45f * flick);
                        tongues[i].transform.localScale = new Vector3(0.18f + u * 0.08f, baseH[i] * flick * Mathf.Lerp(0.45f, 1.35f, u), 0.12f);
                        tongues[i].transform.rotation = Quaternion.Euler(-12f + Mathf.Sin(t * 11f + i) * 8f, ang * Mathf.Rad2Deg, 0f);
                    }
                    if (cores[i] != null)
                    {
                        cores[i].transform.position = p + Vector3.up * 0.12f;
                        cores[i].transform.localScale = Vector3.one * (0.1f * flick * Mathf.Lerp(0.6f, 1.2f, u));
                    }
                }
                if (Mathf.Repeat(t, 0.09f) < 0.02f)
                    Burst(body + UnityEngine.Random.insideUnitSphere * 0.25f, tip, 0.08f, 4);
                if (!hit && u > 0.58f)
                {
                    hit = true;
                    Burst(body, ember, 0.42f, 18);
                    Burst(feet + Vector3.up * 0.2f, tip, 0.28f, 12);
                    ElementGroundRing(feet, ember, 2.1f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }
            t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < flameN; i++)
                {
                    float flick = 0.82f + 0.3f * Mathf.PerlinNoise(i * 2.1f, t * 11f);
                    float ang = baseAng[i] + t * 2.4f;
                    Vector3 p = feet + new Vector3(Mathf.Cos(ang) * baseR[i], 0.18f + (i % 6) * 0.28f, Mathf.Sin(ang) * baseR[i]);
                    if (tongues[i] != null)
                    {
                        tongues[i].transform.position = p + Vector3.up * (baseH[i] * 0.4f * flick);
                        tongues[i].transform.localScale = new Vector3(0.2f, baseH[i] * flick, 0.12f);
                    }
                    if (cores[i] != null) cores[i].transform.position = p;
                }
                yield return null;
            }
            DestroyAll(tongues);
            DestroyAll(cores);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementWaterDrown(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color water = new Color(0.22f, 0.55f, 0.86f);
            Color deep = new Color(0.12f, 0.34f, 0.62f);
            Vector3 body = foe != null ? foe.position + Vector3.up * 1.05f : to;
            Vector3 sky = body + Vector3.up * 8.2f;
            var ball = BattleVfx.MakePropSphere(water, 0.88f, 0.4f);
            ball.transform.position = sky;
            var core = BattleVfx.MakePropSphere(deep, 0.9f, 0.22f);
            core.transform.position = sky;
            float t = 0f;
            while (t < 0.48f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.48f));
                float s = Mathf.Lerp(0.45f, 7.2f, u);
                if (ball != null)
                {
                    ball.transform.position = sky;
                    ball.transform.localScale = Vector3.one * s;
                }
                if (core != null)
                {
                    core.transform.position = sky;
                    core.transform.localScale = Vector3.one * (s * 0.7f);
                }
                yield return null;
            }
            t = 0f;
            bool hit = false;
            while (t < 0.78f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.78f);
                float ease = u * u * (3f - 2f * u);
                Vector3 p = Vector3.Lerp(sky, body, ease);
                if (ball != null) ball.transform.position = p;
                if (core != null) core.transform.position = p;
                if (!hit && u > 0.82f)
                {
                    hit = true;
                    Burst(body, water, 0.55f, 20);
                    ElementGroundRing(body, water, 4.4f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }
            var puddle = BattleVfx.MakePropCube(deep, 0.9f);
            puddle.transform.position = (foe != null ? foe.position : body) + Vector3.up * 0.05f;
            puddle.transform.localScale = new Vector3(5.2f, 0.12f, 5.2f);
            if (ball != null) ball.transform.localScale = Vector3.one * 4.1f;
            yield return ElementLinger(0.42f);
            if (ball != null) UnityEngine.Object.Destroy(ball);
            if (core != null) UnityEngine.Object.Destroy(core);
            if (puddle != null) UnityEngine.Object.Destroy(puddle);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementLightBlitzPunch(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color warm = new Color(1f, 0.92f, 0.55f);
            Color coreC = new Color(1f, 0.98f, 0.82f);
            Vector3 selfHome = self != null && self.Root != null ? self.Root.position : Vector3.zero;
            Quaternion selfRot = self != null && self.Root != null ? self.Root.rotation : Quaternion.identity;
            Vector3 chest = foe != null ? foe.position + Vector3.up * 1.05f : to;
            Vector3 spot = selfHome;
            Vector3 flat = Vector3.forward;
            if (self != null && self.Root != null && foe != null)
            {
                flat = foe.position - selfHome;
                flat.y = 0f;
                if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
                flat.Normalize();
                spot = foe.position - flat * 0.95f;
                spot.y = selfHome.y;
            }
            const int ghostN = 4;
            var ghosts = new GameObject[ghostN];
            float dash = 0f;
            while (dash < 0.07f)
            {
                dash += Time.deltaTime;
                float u = Mathf.Clamp01(dash / 0.07f);
                if (self != null && self.Root != null)
                {
                    self.Root.position = Vector3.Lerp(selfHome, spot, u);
                    self.Root.rotation = Quaternion.LookRotation(flat, Vector3.up);
                }
                int g = Mathf.Clamp(Mathf.FloorToInt(u * ghostN), 0, ghostN - 1);
                if (ghosts[g] == null)
                {
                    ghosts[g] = BattleVfx.MakePropCube(coreC, 0.55f);
                    ghosts[g].transform.position = Vector3.Lerp(selfHome, spot, u) + Vector3.up * 1.0f;
                    ghosts[g].transform.localScale = new Vector3(0.35f, 1.35f, 0.28f);
                    ghosts[g].transform.rotation = Quaternion.LookRotation(flat, Vector3.up);
                }
                yield return null;
            }
            if (self != null && self.Root != null)
            {
                self.Root.position = spot;
                self.Root.rotation = Quaternion.LookRotation(flat, Vector3.up);
            }
            Vector3 fistFrom = self != null ? self.RightHandPos() : spot + Vector3.up * 1.1f;
            var fist = BattleVfx.MakePropSphere(coreC, 0.92f, 0.28f);
            fist.transform.position = fistFrom;
            float t = 0f;
            bool hit = false;
            while (t < 0.1f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.1f);
                if (fist != null)
                    fist.transform.position = Vector3.Lerp(fistFrom, chest, u * u);
                if (!hit && u > 0.45f)
                {
                    hit = true;
                    Burst(chest, warm, 0.48f, 18);
                    ElementGroundRing(chest, warm, 2.2f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat(true);
                }
                yield return null;
            }
            yield return ElementLinger(0.38f);
            if (fist != null) UnityEngine.Object.Destroy(fist);
            DestroyAll(ghosts);
            if (self != null && self.Root != null)
            {
                self.Root.position = selfHome;
                self.Root.rotation = selfRot;
            }
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementNatureBindPierce(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color bark = new Color(0.42f, 0.28f, 0.14f);
            Color barkDark = new Color(0.28f, 0.18f, 0.08f);
            Color leaf = new Color(0.2f, 0.48f, 0.18f);
            Vector3 center = foe != null ? foe.position : to;
            float ground = center.y;
            Vector3 body = center + Vector3.up * 1.05f;
            Vector3 away = Vector3.forward;
            Vector3 allyDir = Vector3.back;
            if (self != null && self.Root != null)
            {
                away = center - self.Root.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                away.Normalize();
                allyDir = -away;
            }
            Vector3 treeBase = center + away * 3.15f;
            treeBase.y = ground;
            const float treeH = 13.2f;
            var trunk = BattleVfx.MakeSolidBeam(bark, 0.55f, 1f);
            var trunkDark = BattleVfx.MakeSolidBeam(barkDark, 0.32f, 1f);
            const int canopyN = 10;
            var canopy = new GameObject[canopyN];
            for (int i = 0; i < canopyN; i++)
            {
                canopy[i] = BattleVfx.MakePropCube(leaf, 1f);
                canopy[i].transform.localScale = new Vector3(3.6f + (i % 3) * 0.7f, 0.95f, 3.2f + (i % 2) * 0.7f);
            }
            const int vineN = 5;
            var vines = new GameObject[vineN];
            for (int i = 0; i < vineN; i++)
                vines[i] = BattleVfx.MakeSolidBeam(i % 2 == 0 ? bark : barkDark, 0.1f, 1f);

            float t = 0f;
            while (t < 0.48f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.48f));
                float h = Mathf.Lerp(0.3f, treeH, u);
                BattleVfx.PlaceBeam(trunk, treeBase, treeBase + Vector3.up * h, 4.15f + u * 1.05f);
                BattleVfx.PlaceBeam(trunkDark, treeBase + away * 0.28f, treeBase + away * 0.28f + Vector3.up * (h * 0.92f), 2.05f);
                Vector3 top = treeBase + Vector3.up * h;
                for (int i = 0; i < canopyN; i++)
                {
                    if (canopy[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / canopyN);
                    float rad = 1.55f + (i % 3) * 0.95f;
                    Vector3 leafPos = top + new Vector3(Mathf.Cos(ang) * rad, 0.2f + (i % 2) * 0.38f, Mathf.Sin(ang) * rad);
                    leafPos += away * 1.15f;
                    canopy[i].transform.position = leafPos;
                    canopy[i].transform.rotation = Quaternion.Euler(8f, ang * Mathf.Rad2Deg, 0f);
                }
                yield return null;
            }

            t = 0f;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.32f));
                for (int i = 0; i < vineN; i++)
                {
                    float y = 0.45f + i * 0.32f;
                    Vector3 start = treeBase + Vector3.up * y + away * 0.15f;
                    Vector3 across = Vector3.Cross(Vector3.up, away);
                    if (across.sqrMagnitude < 0.0001f) across = Vector3.right;
                    across.Normalize();
                    float side = (i % 2 == 0) ? 1f : -1f;
                    Vector3 end = body + across * (side * 0.38f) + Vector3.up * ((i - 2) * 0.08f);
                    Vector3 tip = Vector3.Lerp(start, end, u);
                    if (vines[i] != null)
                        BattleVfx.PlaceBeam(vines[i], start, tip, 0.48f + u * 0.12f);
                }
                yield return null;
            }

            var sword = SpawnWoodSword();
            Vector3 swordFrom = body + allyDir * 2.2f + Vector3.up * 1.15f;
            if (self != null && self.Root != null)
            {
                Vector3 allyPos = self.Root.position;
                swordFrom = allyPos + away * 1.55f + Vector3.up * 1.2f;
                Vector3 toFoe = body - swordFrom;
                toFoe.y = 0f;
                if (toFoe.sqrMagnitude < 0.8f)
                    swordFrom = allyPos + away * 2.1f + Vector3.up * 1.2f;
            }
            Vector3 swordTo = body + away * 0.45f;
            Vector3 aim0 = swordTo - swordFrom;
            if (aim0.sqrMagnitude > 0.0001f)
                sword.transform.rotation = Quaternion.LookRotation(aim0.normalized, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
            sword.transform.position = swordFrom;
            yield return ElementLinger(0.22f);
            t = 0f;
            bool hit = false;
            while (t < 1.15f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 1.15f);
                float ease = u * u;
                Vector3 p = Vector3.Lerp(swordFrom, swordTo, ease);
                Vector3 aim = swordTo - swordFrom;
                if (aim.sqrMagnitude > 0.0001f)
                    sword.transform.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                sword.transform.position = p;
                if (!hit && u > 0.86f)
                {
                    hit = true;
                    Burst(body, bark, 0.28f, 12);
                    ElementGroundRing(center, barkDark, 1.8f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat(true);
                }
                yield return null;
            }
            yield return ElementLinger(0.55f);
            if (sword != null) UnityEngine.Object.Destroy(sword);
            if (trunk != null) UnityEngine.Object.Destroy(trunk);
            if (trunkDark != null) UnityEngine.Object.Destroy(trunkDark);
            DestroyAll(canopy);
            DestroyAll(vines);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementDarkSwallow(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color ink = new Color(0.02f, 0.01f, 0.04f);
            Color violet = new Color(0.42f, 0.08f, 0.62f);
            Vector3 pit = foe != null ? foe.position + Vector3.up * 1.0f : to;
            Vector3 foeHome = foe != null ? foe.position : pit;
            Vector3 foeScale = foe != null ? foe.localScale : Vector3.one;
            var hole = BattleVfx.MakeOpaqueSphere(ink, 0.7f);
            hole.transform.position = pit;
            var core = BattleVfx.MakeOpaqueSphere(new Color(0.05f, 0.01f, 0.08f), 0.25f);
            core.transform.position = pit;
            float t = 0f;
            bool hit = false;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.7f));
                float s = Mathf.Lerp(0.12f, 3.4f, u);
                if (hole != null)
                {
                    hole.transform.position = pit;
                    hole.transform.localScale = Vector3.one * s;
                }
                if (core != null)
                    core.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.95f, u);
                if (foe != null)
                {
                    foe.position = Vector3.Lerp(foeHome, pit, u * 0.92f);
                    foe.localScale = Vector3.Lerp(foeScale, foeScale * 0.08f, u);
                }
                if (!hit && u > 0.72f)
                {
                    hit = true;
                    Burst(pit, violet, 0.32f, 14);
                    ElementGroundRing(pit, ink, 2.4f);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }
            yield return ElementLinger(0.48f);
            if (hole != null) UnityEngine.Object.Destroy(hole);
            if (core != null) UnityEngine.Object.Destroy(core);
            if (foe != null)
            {
                foe.position = foeHome;
                foe.localScale = foeScale;
            }
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementMuPillarBind(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color stone = new Color(0.72f, 0.74f, 0.82f);
            Color dark = new Color(0.48f, 0.5f, 0.58f);
            Vector3 center = foe != null ? foe.position : to;
            float ground = center.y;
            const int pillarN = 6;
            const float cageR = 1.35f;
            const float pillarH = 3.2f;
            var pillars = new GameObject[pillarN];
            var roots = new Vector3[pillarN];
            for (int i = 0; i < pillarN; i++)
            {
                float ang = i * (Mathf.PI * 2f / pillarN);
                roots[i] = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * cageR;
                roots[i].y = ground;
                pillars[i] = BattleVfx.MakeSolidBeam(stone, 0.22f, 1f);
            }
            const int ringH = 4;
            var rings = new GameObject[ringH * pillarN];
            for (int i = 0; i < rings.Length; i++)
                rings[i] = BattleVfx.MakeSolidBeam(dark, 0.12f, 1f);
            var cap = BattleVfx.MakePropCube(stone, 1f);
            cap.transform.localScale = new Vector3(2.1f, 0.22f, 2.1f);
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
                float h = Mathf.Lerp(0.2f, pillarH, u);
                for (int i = 0; i < pillarN; i++)
                    BattleVfx.PlaceBeam(pillars[i], roots[i], roots[i] + Vector3.up * h, 0.55f);
                if (cap != null)
                    cap.transform.position = center + Vector3.up * h;
                yield return null;
            }
            t = 0f;
            bool hit = false;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.32f));
                float r = Mathf.Lerp(cageR, 0.72f, u);
                for (int layer = 0; layer < ringH; layer++)
                    PlaceWoodRing(rings, layer, pillarN, center, ground + 0.4f + layer * 0.62f, r, 0.42f);
                if (!hit && u > 0.55f)
                {
                    hit = true;
                    Burst(center + Vector3.up * 1.05f, stone, 0.22f, 10);
                    if (onHit != null) onHit();
                    yield return ElementHitBeat();
                }
                yield return null;
            }
            yield return ElementLinger(0.55f);
            if (cap != null) UnityEngine.Object.Destroy(cap);
            DestroyAll(pillars);
            DestroyAll(rings);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementFireCore(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color lava = new Color(0.92f, 0.26f, 0.04f);
            Color coal = new Color(0.42f, 0.1f, 0.03f);
            Color yolk = new Color(1f, 0.68f, 0.08f);
            Vector3 hand = self != null ? self.RightHandPos() : to;
            Vector3 body = foe != null ? foe.position + Vector3.up * 1.05f : to;
            Vector3 feet = foe != null ? foe.position : to;

            const int orbN = 5;
            var orbs = new GameObject[orbN];
            Color[] orbColors = { yolk, lava, coal };
            for (int i = 0; i < orbN; i++)
                orbs[i] = BattleVfx.MakeOpaqueSphere(orbColors[i % orbColors.Length], 0.28f);

            float t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.55f));
                hand = self != null ? self.RightHandPos() : hand;
                for (int i = 0; i < orbN; i++)
                {
                    if (orbs[i] == null) continue;
                    float a = t * 7f + i * Mathf.PI * 2f / orbN;
                    float r = Mathf.Lerp(0.12f, 0.58f, u);
                    orbs[i].transform.position = hand + new Vector3(Mathf.Cos(a), 0.08f + Mathf.Sin(t * 9f + i) * 0.1f, Mathf.Sin(a)) * r;
                    orbs[i].transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.46f, u);
                }
                yield return null;
            }

            var core = BattleVfx.MakeOpaqueSphere(yolk, 0.55f);
            var shell = BattleVfx.MakeOpaqueSphere(lava, 0.86f);
            core.transform.position = hand;
            shell.transform.position = hand;
            DestroyAll(orbs);

            const int discN = 8;
            var discs = new GameObject[discN];
            for (int i = 0; i < discN; i++)
                discs[i] = BattleVfx.MakeOpaqueBeam(i % 2 == 0 ? lava : coal, 0.35f);

            Vector3 launch = hand;
            t = 0f;
            while (t < 0.46f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.46f);
                float ease = u * u;
                Vector3 head = Vector3.Lerp(launch, body, ease);
                Vector3 dir = body - launch;
                if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
                dir.Normalize();
                if (core != null)
                {
                    core.transform.position = head;
                    core.transform.localScale = Vector3.one * 0.42f;
                }
                if (shell != null)
                {
                    shell.transform.position = head;
                    shell.transform.localScale = Vector3.one * 0.72f;
                }
                for (int i = 0; i < discN; i++)
                {
                    if (discs[i] == null) continue;
                    discs[i].transform.position = head - dir * (0.16f + i * 0.2f);
                    discs[i].transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, t * 640f + i * 18f, 0f);
                    float rad = 0.62f - i * 0.07f;
                    discs[i].transform.localScale = new Vector3(rad, 0.045f, rad);
                }
                yield return null;
            }

            t = 0f;
            bool hit = false;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.2f);
                if (core != null)
                {
                    core.transform.position = body;
                    core.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 0.12f, u);
                }
                if (shell != null)
                {
                    shell.transform.position = body;
                    shell.transform.localScale = Vector3.one * Mathf.Lerp(0.72f, 0.18f, u);
                }
                for (int i = 0; i < discN; i++)
                {
                    if (discs[i] == null) continue;
                    discs[i].transform.position = body;
                    discs[i].transform.Rotate(Vector3.up, 48f, Space.World);
                    float rad = Mathf.Lerp(0.5f - i * 0.05f, 0.12f, u);
                    discs[i].transform.localScale = new Vector3(rad, 0.04f, rad);
                }
                if (!hit && u > 0.4f)
                {
                    hit = true;
                    if (onHit != null) onHit();
                    yield return ElementHitBeat(true, foe);
                }
                yield return null;
            }
            if (core != null) UnityEngine.Object.Destroy(core);
            if (shell != null) UnityEngine.Object.Destroy(shell);
            DestroyAll(discs);

            const int chunkN = 16;
            var chunks = new GameObject[chunkN];
            var risen = new Vector3[chunkN];
            var settled = new Vector3[chunkN];
            for (int i = 0; i < chunkN; i++)
            {
                float ang = i * (Mathf.PI * 2f / chunkN);
                chunks[i] = BattleVfx.MakeOpaqueCube(i % 3 == 0 ? yolk : (i % 2 == 0 ? lava : coal));
                float rad = 0.45f + (i % 3) * 0.35f;
                risen[i] = feet + new Vector3(Mathf.Cos(ang) * 0.25f, 2.6f + (i % 4) * 0.35f, Mathf.Sin(ang) * 0.25f);
                settled[i] = feet + new Vector3(Mathf.Cos(ang) * rad, 0.16f + (i % 2) * 0.08f, Mathf.Sin(ang) * rad);
            }
            var crater = OpaqueDisc(coal, 0.4f, 0.1f);
            crater.transform.position = feet + Vector3.up * 0.05f;
            ElementFlinch(foe);
            t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.22f));
                for (int i = 0; i < chunkN; i++)
                {
                    if (chunks[i] == null) continue;
                    chunks[i].transform.position = Vector3.Lerp(feet + Vector3.up * 0.2f, risen[i], u);
                    float s = 0.2f + (i % 3) * 0.07f;
                    chunks[i].transform.localScale = new Vector3(s, s * 1.5f, s);
                    chunks[i].transform.rotation = Quaternion.Euler(u * 50f, i * 30f, 0f);
                }
                if (crater != null)
                    crater.transform.localScale = new Vector3(Mathf.Lerp(0.4f, 1.2f, u), 0.05f, Mathf.Lerp(0.4f, 1.2f, u));
                yield return null;
            }
            t = 0f;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.28f));
                for (int i = 0; i < chunkN; i++)
                {
                    if (chunks[i] == null) continue;
                    chunks[i].transform.position = Vector3.Lerp(risen[i], settled[i], u);
                    chunks[i].transform.rotation = Quaternion.Euler(50f + u * 40f, i * 30f + u * 20f, u * 25f);
                }
                if (crater != null)
                    crater.transform.localScale = new Vector3(Mathf.Lerp(1.2f, 2.8f, u), 0.08f, Mathf.Lerp(1.2f, 2.8f, u));
                yield return null;
            }

            const int pillarN = 8;
            var pillars = new GameObject[pillarN];
            var pillarRoots = new Vector3[pillarN];
            for (int i = 0; i < pillarN; i++)
            {
                float ang = i * (Mathf.PI * 2f / pillarN) + 0.2f;
                pillarRoots[i] = feet + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 2.55f;
                pillarRoots[i].y = feet.y;
                pillars[i] = BattleVfx.MakeOpaqueBeam(i % 2 == 0 ? lava : yolk, 0.22f);
            }
            t = 0f;
            while (t < 0.36f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.36f));
                float h = Mathf.Lerp(0.25f, 4.6f, u);
                for (int i = 0; i < pillarN; i++)
                    BattleVfx.PlaceBeam(pillars[i], pillarRoots[i], pillarRoots[i] + Vector3.up * h, 0.7f);
                yield return null;
            }
            t = 0f;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.32f));
                for (int i = 0; i < pillarN; i++)
                {
                    Vector3 tip = Vector3.Lerp(pillarRoots[i] + Vector3.up * 4.6f, body, u);
                    BattleVfx.PlaceBeam(pillars[i], pillarRoots[i], tip, 0.78f);
                }
                for (int i = 0; i < chunkN; i++)
                {
                    if (chunks[i] == null) continue;
                    chunks[i].transform.position = Vector3.Lerp(settled[i], body, u * 0.72f);
                }
                yield return null;
            }
            yield return ElementHitBeat(true, foe);
            DestroyAll(pillars);
            yield return ElementLinger(0.42f);
            DestroyAll(chunks);
            if (crater != null) UnityEngine.Object.Destroy(crater);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementWaterSpear(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color water = new Color(0.1f, 0.38f, 0.74f);
            Color deep = new Color(0.05f, 0.18f, 0.42f);
            Color foam = new Color(0.78f, 0.88f, 0.94f);
            Vector3 center = foe != null ? foe.position : to;
            float ground = center.y;
            Vector3 body = center + Vector3.up * 1.05f;
            FlatBasis(self, center, out Vector3 fwd, out Vector3 right);

            const int beadN = 12;
            var beads = new GameObject[beadN];
            for (int i = 0; i < beadN; i++)
                beads[i] = BattleVfx.MakeOpaqueSphere(i % 2 == 0 ? water : foam, 0.28f);
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
                for (int i = 0; i < beadN; i++)
                {
                    if (beads[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / beadN) + t * 3.2f;
                    float rad = Mathf.Lerp(0.35f, 2.3f, u);
                    float h = Mathf.Lerp(0.15f, 1.1f + (i % 4) * 0.35f, u);
                    beads[i].transform.position = center + new Vector3(Mathf.Cos(ang) * rad, h, Mathf.Sin(ang) * rad);
                    beads[i].transform.position = new Vector3(beads[i].transform.position.x, ground + h, beads[i].transform.position.z);
                    beads[i].transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.42f, u);
                }
                yield return null;
            }
            DestroyAll(beads);

            const int slabN = 14;
            var slabs = new GameObject[slabN];
            for (int i = 0; i < slabN; i++)
                slabs[i] = BattleVfx.MakeOpaqueCube(i % 2 == 0 ? water : deep);

            t = 0f;
            while (t < 0.48f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.48f));
                float h = Mathf.Lerp(0.15f, 4.1f, u);
                for (int i = 0; i < slabN; i++)
                {
                    if (slabs[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / slabN);
                    Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    slabs[i].transform.position = center + radial * 2.15f + Vector3.up * (h * 0.5f);
                    slabs[i].transform.position = new Vector3(slabs[i].transform.position.x, ground + h * 0.5f, slabs[i].transform.position.z);
                    slabs[i].transform.rotation = Quaternion.LookRotation(radial, Vector3.up);
                    slabs[i].transform.localScale = new Vector3(0.95f, h, 0.18f);
                }
                yield return null;
            }

            var spear = BattleVfx.MakeOpaqueBeam(foam, 0.22f);
            var tip = BattleVfx.MakeOpaqueSphere(water, 0.48f);
            Vector3 sky = body + Vector3.up * 7.4f;
            t = 0f;
            while (t < 0.52f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.52f));
                float spin = t * 2.8f;
                float radius = Mathf.Lerp(2.15f, 0.95f, u);
                float lean = Mathf.Lerp(0f, 28f, u);
                for (int i = 0; i < slabN; i++)
                {
                    if (slabs[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / slabN) + spin;
                    Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    float h = 4.1f;
                    Vector3 pos = center + radial * radius;
                    pos.y = ground + h * 0.5f;
                    slabs[i].transform.position = pos;
                    slabs[i].transform.rotation = Quaternion.LookRotation(radial, Vector3.up) * Quaternion.Euler(lean, 0f, 0f);
                    slabs[i].transform.localScale = new Vector3(0.9f, h, 0.2f);
                }
                Vector3 spearTip = Vector3.Lerp(sky, body + Vector3.up * 2.4f, u * 0.35f);
                BattleVfx.PlaceBeam(spear, spearTip, spearTip + Vector3.up * 2.8f, 0.42f);
                if (tip != null) tip.transform.position = spearTip;
                yield return null;
            }

            t = 0f;
            bool hit = false;
            Vector3 spearFrom = body + Vector3.up * 5.4f;
            while (t < 0.36f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.36f);
                float ease = u * u;
                Vector3 spearTip = Vector3.Lerp(spearFrom, body, ease);
                BattleVfx.PlaceBeam(spear, spearTip, spearTip + Vector3.up * Mathf.Lerp(2.4f, 0.7f, u), 0.5f);
                if (tip != null)
                {
                    tip.transform.position = spearTip;
                    tip.transform.localScale = Vector3.one * Mathf.Lerp(0.48f, 0.28f, u);
                }
                float radius = Mathf.Lerp(0.95f, 0.42f, ease);
                for (int i = 0; i < slabN; i++)
                {
                    if (slabs[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / slabN) + t * 3f;
                    Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 pos = center + radial * radius;
                    pos.y = ground + 1.45f;
                    slabs[i].transform.position = pos;
                    slabs[i].transform.rotation = Quaternion.LookRotation(-radial, Vector3.up) * Quaternion.Euler(55f, 0f, 0f);
                    slabs[i].transform.localScale = new Vector3(0.7f, 2.2f, 0.22f);
                }
                if (!hit && u > 0.72f)
                {
                    hit = true;
                    if (onHit != null) onHit();
                    yield return ElementHitBeat(true, foe);
                }
                yield return null;
            }

            var sideA = BattleVfx.MakeOpaqueBeam(water, 0.2f);
            var sideB = BattleVfx.MakeOpaqueBeam(deep, 0.2f);
            var sideTipA = BattleVfx.MakeOpaqueSphere(foam, 0.42f);
            var sideTipB = BattleVfx.MakeOpaqueSphere(foam, 0.42f);
            Vector3 fromA = body - right * 6.2f + Vector3.up * 1.3f;
            Vector3 fromB = body + right * 6.2f + Vector3.up * 1.3f;
            Vector3 fromC = body - fwd * 6.2f + Vector3.up * 0.8f;
            var sideC = BattleVfx.MakeOpaqueBeam(foam, 0.18f);
            var sideTipC = BattleVfx.MakeOpaqueSphere(water, 0.36f);
            t = 0f;
            while (t < 0.38f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.38f);
                float ease = u * u;
                Vector3 a = Vector3.Lerp(fromA, body, ease);
                Vector3 b = Vector3.Lerp(fromB, body, ease);
                Vector3 c = Vector3.Lerp(fromC, body, ease);
                BattleVfx.PlaceBeam(sideA, fromA, a, 0.46f);
                BattleVfx.PlaceBeam(sideB, fromB, b, 0.46f);
                BattleVfx.PlaceBeam(sideC, fromC, c, 0.4f);
                if (sideTipA != null) sideTipA.transform.position = a;
                if (sideTipB != null) sideTipB.transform.position = b;
                if (sideTipC != null) sideTipC.transform.position = c;
                float radius = Mathf.Lerp(0.42f, 0.28f, ease);
                for (int i = 0; i < slabN; i++)
                {
                    if (slabs[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / slabN) + t * 4f;
                    Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 pos = center + radial * radius;
                    pos.y = ground + 1.2f;
                    slabs[i].transform.position = pos;
                }
                yield return null;
            }
            yield return ElementHitBeat(true, foe);
            if (sideA != null) UnityEngine.Object.Destroy(sideA);
            if (sideB != null) UnityEngine.Object.Destroy(sideB);
            if (sideC != null) UnityEngine.Object.Destroy(sideC);
            if (sideTipA != null) UnityEngine.Object.Destroy(sideTipA);
            if (sideTipB != null) UnityEngine.Object.Destroy(sideTipB);
            if (sideTipC != null) UnityEngine.Object.Destroy(sideTipC);

            ElementFlinch(foe);
            var puddle = OpaqueDisc(deep, 0.6f, 0.12f);
            puddle.transform.position = new Vector3(center.x, ground + 0.06f, center.z);
            var lodged = BattleVfx.MakeOpaqueBeam(foam, 0.16f);
            BattleVfx.PlaceBeam(lodged, new Vector3(center.x, ground + 0.1f, center.z), body + Vector3.up * 0.35f, 0.38f);
            if (tip != null) tip.transform.position = body + Vector3.up * 0.35f;
            if (spear != null) UnityEngine.Object.Destroy(spear);
            t = 0f;
            while (t < 0.24f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.24f));
                if (puddle != null)
                    puddle.transform.localScale = new Vector3(Mathf.Lerp(0.8f, 3.4f, u), 0.06f, Mathf.Lerp(0.8f, 3.4f, u));
                for (int i = 0; i < slabN; i++)
                {
                    if (slabs[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / slabN);
                    Vector3 radial = (fwd * Mathf.Cos(ang) + right * Mathf.Sin(ang));
                    Vector3 pos = center + radial * Mathf.Lerp(0.42f, 1.7f, u);
                    pos.y = ground + 0.12f;
                    slabs[i].transform.position = pos;
                    slabs[i].transform.rotation = Quaternion.LookRotation(radial, Vector3.up) * Quaternion.Euler(80f, 0f, 0f);
                    slabs[i].transform.localScale = new Vector3(0.85f, 0.16f, 0.28f);
                }
                yield return null;
            }
            yield return ElementLinger(0.4f);
            DestroyAll(slabs);
            if (spear != null) UnityEngine.Object.Destroy(spear);
            if (tip != null) UnityEngine.Object.Destroy(tip);
            if (lodged != null) UnityEngine.Object.Destroy(lodged);
            if (puddle != null) UnityEngine.Object.Destroy(puddle);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementLightCross(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color gold = new Color(1f, 0.82f, 0.22f);
            Color coreC = new Color(1f, 0.96f, 0.72f);
            Vector3 center = foe != null ? foe.position : to;
            float ground = center.y;
            Vector3 body = center + Vector3.up * 1.05f;
            FlatBasis(self, center, out Vector3 fwd, out Vector3 right);
            Vector3 diagA = (fwd + right).normalized;
            Vector3 diagB = (fwd - right).normalized;
            Vector3[] arms = { fwd, -fwd, right, -right, diagA, -diagA, diagB, -diagB };

            const int seg = 5;
            var bits = new GameObject[arms.Length * seg];
            var home = new Vector3[bits.Length];
            var scatter = new Vector3[bits.Length];
            for (int a = 0; a < arms.Length; a++)
            {
                for (int s = 0; s < seg; s++)
                {
                    int i = a * seg + s;
                    bits[i] = BattleVfx.MakeOpaqueCube(s == 0 ? coreC : gold);
                    float reach = 0.55f + s * 0.62f;
                    home[i] = body + arms[a] * reach;
                    scatter[i] = body + arms[a] * (3.4f + s * 0.35f) + Vector3.up * (2.2f + (s % 3) * 0.4f);
                }
            }
            var hub = BattleVfx.MakeOpaqueCube(coreC, 0.45f);

            float t = 0f;
            const float crossY = 6.4f;
            while (t < 0.58f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.58f));
                if (hub != null)
                {
                    hub.transform.position = new Vector3(center.x, ground + crossY, center.z);
                    hub.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.55f, u);
                    hub.transform.rotation = Quaternion.Euler(0f, t * 80f, 0f);
                }
                for (int i = 0; i < bits.Length; i++)
                {
                    if (bits[i] == null) continue;
                    Vector3 slot = home[i];
                    slot.y = ground + crossY;
                    bits[i].transform.position = Vector3.Lerp(scatter[i], slot, u);
                    float w = i % seg == 0 ? 0.48f : 0.34f;
                    bits[i].transform.localScale = new Vector3(w, w * 0.72f, 0.95f);
                    Vector3 aim = slot - new Vector3(center.x, slot.y, center.z);
                    if (aim.sqrMagnitude > 0.001f)
                        bits[i].transform.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up);
                }
                yield return null;
            }

            t = 0f;
            bool hit = false;
            while (t < 0.38f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.38f);
                float ease = u * u;
                float spin = t * 220f;
                float y = Mathf.Lerp(crossY, 1.15f, ease);
                if (hub != null)
                    hub.transform.rotation = Quaternion.Euler(0f, spin, 0f);
                for (int i = 0; i < bits.Length; i++)
                {
                    int arm = i / seg;
                    int s = i % seg;
                    float reach = 0.55f + s * 0.62f;
                    Vector3 swung = Quaternion.Euler(0f, spin, 0f) * (arms[arm] * reach);
                    home[i] = new Vector3(center.x, body.y, center.z) + swung;
                }
                if (hub != null)
                {
                    hub.transform.position = new Vector3(center.x, ground + y, center.z);
                    hub.transform.localScale = Vector3.one * 0.62f;
                }
                for (int i = 0; i < bits.Length; i++)
                {
                    if (bits[i] == null) continue;
                    Vector3 slot = home[i];
                    slot.y = ground + y;
                    bits[i].transform.position = slot;
                    Vector3 flatAim = slot - new Vector3(center.x, slot.y, center.z);
                    if (flatAim.sqrMagnitude > 0.001f)
                        bits[i].transform.rotation = Quaternion.LookRotation(flatAim.normalized, Vector3.up);
                }
                if (!hit && u > 0.78f)
                {
                    hit = true;
                    if (hub != null) hub.transform.localScale = Vector3.one * 1.15f;
                    if (onHit != null) onHit();
                    yield return ElementHitBeat(true, foe);
                }
                yield return null;
            }

            var flash = BattleVfx.MakeOpaqueCube(coreC, 0.8f);
            flash.transform.position = body;
            t = 0f;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.28f));
                if (flash != null)
                {
                    flash.transform.position = body;
                    flash.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 2.1f, u);
                    flash.transform.rotation = Quaternion.Euler(0f, u * 40f, 0f);
                }
                for (int i = 0; i < bits.Length; i++)
                {
                    if (bits[i] == null) continue;
                    Vector3 slot = home[i];
                    slot.y = ground + 1.15f;
                    Vector3 outPos = center + (home[i] - new Vector3(center.x, home[i].y, center.z)).normalized * Mathf.Lerp(1.2f, 3.6f, u);
                    outPos.y = ground + Mathf.Lerp(1.15f, 0.35f + (i % 3) * 0.2f, u);
                    bits[i].transform.position = Vector3.Lerp(slot, outPos, u);
                    bits[i].transform.localScale = Vector3.one * Mathf.Lerp(0.34f, 0.16f, u);
                }
                yield return null;
            }
            ElementFlinch(foe);
            const int colN = 7;
            var column = new GameObject[colN];
            for (int i = 0; i < colN; i++)
                column[i] = BattleVfx.MakeOpaqueCube(i == colN - 1 ? coreC : gold);
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
                for (int i = 0; i < colN; i++)
                {
                    if (column[i] == null) continue;
                    float h = Mathf.Lerp(0.3f, 0.55f + i * 0.72f, u);
                    column[i].transform.position = new Vector3(center.x, ground + h, center.z);
                    float s = i == colN - 1 ? 0.85f : 0.55f;
                    column[i].transform.localScale = new Vector3(s, 0.42f, s);
                    column[i].transform.rotation = Quaternion.Euler(0f, i * 18f + t * 40f, 0f);
                }
                yield return null;
            }
            t = 0f;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.28f);
                float ease = u * u;
                for (int i = 0; i < colN; i++)
                {
                    if (column[i] == null) continue;
                    float from = 0.55f + i * 0.72f;
                    float h = Mathf.Lerp(from, 0.45f + i * 0.08f, ease);
                    column[i].transform.position = new Vector3(center.x, ground + h, center.z);
                    column[i].transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.15f, ease);
                }
                yield return null;
            }
            yield return ElementHitBeat(true, foe);
            yield return ElementLinger(0.36f);
            if (hub != null) UnityEngine.Object.Destroy(hub);
            if (flash != null) UnityEngine.Object.Destroy(flash);
            DestroyAll(bits);
            DestroyAll(column);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementNatureJaw(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color bark = new Color(0.4f, 0.26f, 0.12f);
            Color barkDark = new Color(0.26f, 0.16f, 0.07f);
            Color leaf = new Color(0.18f, 0.42f, 0.16f);
            Vector3 center = foe != null ? foe.position : to;
            float ground = center.y;
            Vector3 body = center + Vector3.up * 1.15f;
            FlatBasis(self, center, out Vector3 fwd, out Vector3 right);

            const int rootN = 10;
            var roots = new GameObject[rootN];
            for (int i = 0; i < rootN; i++)
                roots[i] = BattleVfx.MakeOpaqueBeam(i % 2 == 0 ? bark : barkDark, 0.14f);
            float t = 0f;
            while (t < 0.42f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.42f));
                for (int i = 0; i < rootN; i++)
                {
                    float ang = i * (Mathf.PI * 2f / rootN);
                    Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 outer = center + radial * 4.4f;
                    outer.y = ground + 0.08f;
                    Vector3 inner = center + radial * Mathf.Lerp(3.6f, 0.7f, u);
                    inner.y = ground + 0.12f + Mathf.Sin(u * Mathf.PI) * 0.45f;
                    BattleVfx.PlaceBeam(roots[i], outer, inner, 0.38f + u * 0.2f);
                }
                yield return null;
            }

            const int toothN = 9;
            var left = new GameObject[toothN];
            var rightTeeth = new GameObject[toothN];
            for (int i = 0; i < toothN; i++)
            {
                left[i] = BattleVfx.MakeOpaqueBeam(i % 2 == 0 ? bark : barkDark, 0.16f);
                rightTeeth[i] = BattleVfx.MakeOpaqueBeam(i % 2 == 0 ? barkDark : bark, 0.16f);
            }
            var canopy = new GameObject[6];
            for (int i = 0; i < canopy.Length; i++)
            {
                canopy[i] = BattleVfx.MakeOpaqueCube(leaf);
                canopy[i].transform.localScale = new Vector3(0.7f, 0.22f, 0.55f);
            }

            t = 0f;
            while (t < 0.46f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.46f));
                float len = Mathf.Lerp(0.4f, 3.8f, u);
                PlaceJaw(left, center, ground, fwd, right, -1f, 72f, len);
                PlaceJaw(rightTeeth, center, ground, fwd, right, 1f, 72f, len);
                for (int i = 0; i < canopy.Length; i++)
                {
                    if (canopy[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / canopy.Length);
                    Vector3 p = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 1.3f;
                    p.y = ground + Mathf.Lerp(4.2f, 3.5f, u);
                    canopy[i].transform.position = p;
                }
                yield return null;
            }

            t = 0f;
            while (t < 0.42f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.42f));
                float tilt = Mathf.Lerp(72f, -22f, u);
                PlaceJaw(left, center, ground, fwd, right, -1f, tilt, 3.8f);
                PlaceJaw(rightTeeth, center, ground, fwd, right, 1f, tilt, 3.8f);
                for (int i = 0; i < canopy.Length; i++)
                {
                    if (canopy[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / canopy.Length);
                    Vector3 p = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Mathf.Lerp(1.3f, 0.45f, u);
                    p.y = ground + Mathf.Lerp(3.5f, 2.7f, u);
                    canopy[i].transform.position = p;
                }
                yield return null;
            }

            ElementFlinch(foe);
            var stake = BattleVfx.MakeOpaqueBeam(barkDark, 0.28f);
            Vector3 stakeTop = body + Vector3.up * 7.2f;
            t = 0f;
            bool hit = false;
            while (t < 0.34f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.34f);
                float ease = u * u;
                Vector3 tip = Vector3.Lerp(stakeTop, body, ease);
                BattleVfx.PlaceBeam(stake, tip + Vector3.up * 2.2f, tip, 0.7f);
                if (!hit && u > 0.8f)
                {
                    hit = true;
                    if (onHit != null) onHit();
                    yield return ElementHitBeat(true, foe);
                }
                yield return null;
            }

            t = 0f;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.32f));
                for (int i = 0; i < canopy.Length; i++)
                {
                    if (canopy[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / canopy.Length);
                    Vector3 p = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Mathf.Lerp(0.45f, 0.2f, u);
                    p.y = ground + Mathf.Lerp(2.7f, 1.15f, u);
                    canopy[i].transform.position = p;
                    canopy[i].transform.localScale = new Vector3(Mathf.Lerp(0.7f, 1.35f, u), 0.28f, Mathf.Lerp(0.55f, 1.1f, u));
                }
                for (int i = 0; i < rootN; i++)
                {
                    float ang = i * (Mathf.PI * 2f / rootN) + t;
                    Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 outer = center + radial * 2.2f;
                    outer.y = ground + 0.1f;
                    Vector3 inner = center + radial * 0.35f + Vector3.up * 0.4f;
                    BattleVfx.PlaceBeam(roots[i], outer, inner, 0.5f);
                }
                yield return null;
            }
            yield return ElementHitBeat(false, foe);

            var plate = BattleVfx.MakeOpaqueCube(bark, 1f);
            plate.transform.position = new Vector3(center.x, ground + 0.08f, center.z);
            t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.22f));
                if (plate != null)
                    plate.transform.localScale = new Vector3(Mathf.Lerp(0.4f, 3.2f, u), 0.14f, Mathf.Lerp(0.4f, 3.2f, u));
                yield return null;
            }
            yield return ElementLinger(0.4f);
            if (stake != null) UnityEngine.Object.Destroy(stake);
            if (plate != null) UnityEngine.Object.Destroy(plate);
            DestroyAll(left);
            DestroyAll(rightTeeth);
            DestroyAll(canopy);
            DestroyAll(roots);
            if (!hit && onHit != null) onHit();
        }

        static void PlaceJaw(GameObject[] teeth, Vector3 center, float ground, Vector3 fwd, Vector3 right, float side, float tilt, float length)
        {
            Vector3 hinge = center + right * (side * 2.35f);
            hinge.y = ground;
            Vector3 toothDir = Quaternion.AngleAxis(side * tilt, fwd) * Vector3.up;
            for (int i = 0; i < teeth.Length; i++)
            {
                if (teeth[i] == null) continue;
                float along = -1.35f + i * 0.45f;
                Vector3 root = hinge + fwd * along;
                root.y = ground;
                Vector3 tip = root + toothDir * length;
                BattleVfx.PlaceBeam(teeth[i], root, tip, 0.42f + (i % 2) * 0.08f);
            }
        }

        public static IEnumerator ElementDarkHex(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color ink = new Color(0.04f, 0.02f, 0.05f);
            Color edge = new Color(0.22f, 0.05f, 0.32f);
            Vector3 center = foe != null ? foe.position : to;
            Vector3 body = center + Vector3.up * 1.05f;
            FlatBasis(self, center, out Vector3 fwd, out Vector3 right);
            Vector3[] dirs =
            {
                Vector3.up, Vector3.down, fwd, -fwd, right, -right
            };

            const int moonN = 8;
            var moons = new GameObject[moonN];
            for (int i = 0; i < moonN; i++)
                moons[i] = BattleVfx.MakeOpaqueCube(i % 2 == 0 ? ink : edge, 0.28f);
            float t = 0f;
            while (t < 0.42f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.42f));
                float rad = Mathf.Lerp(2.6f, 0.15f, u);
                for (int i = 0; i < moonN; i++)
                {
                    if (moons[i] == null) continue;
                    float ang = i * (Mathf.PI * 2f / moonN) + t * 4f;
                    float lift = 1.6f + Mathf.Sin(ang) * 0.45f;
                    moons[i].transform.position = body + new Vector3(Mathf.Cos(ang) * rad, lift, Mathf.Sin(ang) * rad);
                    moons[i].transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 0.55f, 1f - u * 0.3f);
                    moons[i].transform.rotation = Quaternion.Euler(t * 80f, ang * Mathf.Rad2Deg, t * 40f);
                }
                yield return null;
            }
            DestroyAll(moons);

            var cube = BattleVfx.MakeOpaqueCube(ink, 0.35f);
            cube.transform.position = body + Vector3.up * 2.1f;
            t = 0f;
            while (t < 0.42f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.36f));
                if (cube != null)
                {
                    cube.transform.position = body + Vector3.up * Mathf.Lerp(2.4f, 1.7f, u);
                    cube.transform.localScale = Vector3.one * Mathf.Lerp(0.28f, 1.25f, u);
                    cube.transform.rotation = Quaternion.Euler(t * 140f, t * 90f, t * 40f);
                }
                yield return null;
            }
            if (cube != null) UnityEngine.Object.Destroy(cube);

            var spikes = new GameObject[dirs.Length];
            for (int i = 0; i < spikes.Length; i++)
                spikes[i] = BattleVfx.MakeOpaqueBeam(i % 2 == 0 ? ink : edge, 0.18f);

            t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < dirs.Length; i++)
                {
                    Vector3 outer = body + dirs[i] * 3.1f;
                    Vector3 inner = body + dirs[i] * 1.7f;
                    BattleVfx.PlaceBeam(spikes[i], inner, outer, 0.48f);
                }
                yield return null;
            }

            t = 0f;
            bool hit = false;
            while (t < 0.36f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.36f);
                float ease = u * u * (3f - 2f * u);
                for (int i = 0; i < dirs.Length; i++)
                {
                    float far = Mathf.Lerp(3.1f, 0.28f, ease);
                    Vector3 outer = body + dirs[i] * far;
                    Vector3 inner = body + dirs[i] * 0.08f;
                    BattleVfx.PlaceBeam(spikes[i], inner, outer, 0.62f);
                }
                if (!hit && u > 0.7f)
                {
                    hit = true;
                    if (onHit != null) onHit();
                    yield return ElementHitBeat(true, foe);
                }
                yield return null;
            }

            ElementFlinch(foe);
            var core = BattleVfx.MakeOpaqueSphere(ink, 0.35f);
            core.transform.position = body;
            t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.16f);
                if (core != null)
                    core.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 0.22f, u);
                for (int i = 0; i < dirs.Length; i++)
                {
                    float far = Mathf.Lerp(0.28f, 0.05f, u);
                    BattleVfx.PlaceBeam(spikes[i], body, body + dirs[i] * far, Mathf.Lerp(0.62f, 0.15f, u));
                }
                yield return null;
            }
            DestroyAll(spikes);

            Vector3[] diags =
            {
                (Vector3.up + fwd).normalized,
                (Vector3.up - fwd).normalized,
                (Vector3.up + right).normalized,
                (Vector3.up - right).normalized,
                (fwd + right).normalized,
                (fwd - right).normalized,
                (-fwd + right).normalized,
                (-fwd - right).normalized
            };
            var diagSpikes = new GameObject[diags.Length];
            for (int i = 0; i < diagSpikes.Length; i++)
                diagSpikes[i] = BattleVfx.MakeOpaqueBeam(i % 2 == 0 ? edge : ink, 0.16f);
            t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < diags.Length; i++)
                    BattleVfx.PlaceBeam(diagSpikes[i], body + diags[i] * 3.4f, body + diags[i] * 2.1f, 0.4f);
                yield return null;
            }
            t = 0f;
            while (t < 0.34f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.34f);
                float ease = u * u;
                for (int i = 0; i < diags.Length; i++)
                {
                    float far = Mathf.Lerp(3.4f, 0.2f, ease);
                    BattleVfx.PlaceBeam(diagSpikes[i], body + diags[i] * 0.08f, body + diags[i] * far, 0.55f);
                }
                if (core != null)
                    core.transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 0.85f, u);
                yield return null;
            }
            yield return ElementHitBeat(true, foe);
            DestroyAll(diagSpikes);

            var shock = BattleVfx.MakeOpaqueCube(edge, 0.4f);
            shock.transform.position = body;
            var ring = OpaqueDisc(ink, 0.5f, 0.1f);
            ring.transform.position = new Vector3(center.x, center.y + 0.06f, center.z);
            t = 0f;
            while (t < 0.34f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.34f));
                if (shock != null)
                {
                    shock.transform.position = body;
                    shock.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 2.8f, u);
                    shock.transform.rotation = Quaternion.Euler(18f, u * 50f, 12f);
                }
                if (core != null)
                    core.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.15f, u);
                if (ring != null)
                    ring.transform.localScale = new Vector3(Mathf.Lerp(0.4f, 3.6f, u), 0.07f, Mathf.Lerp(0.4f, 3.6f, u));
                yield return null;
            }
            yield return ElementLinger(0.4f);
            if (ring != null) UnityEngine.Object.Destroy(ring);
            if (core != null) UnityEngine.Object.Destroy(core);
            if (shock != null) UnityEngine.Object.Destroy(shock);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ElementMuPress(BattleFighter self, Transform foe, Vector3 to, Action onHit)
        {
            Color stone = new Color(0.78f, 0.8f, 0.86f);
            Color dark = new Color(0.52f, 0.54f, 0.62f);
            Vector3 center = foe != null ? foe.position : to;
            float ground = center.y;
            FlatBasis(self, center, out Vector3 fwd, out Vector3 right);
            Vector3[] outs = { fwd, -fwd, right, -right };

            var floor = OpaqueDisc(dark, 0.5f, 0.1f);
            floor.transform.position = new Vector3(center.x, ground + 0.05f, center.z);
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.3f));
                if (floor != null)
                    floor.transform.localScale = new Vector3(Mathf.Lerp(0.3f, 2.8f, u), 0.08f, Mathf.Lerp(0.3f, 2.8f, u));
                yield return null;
            }

            var walls = new GameObject[4];
            for (int i = 0; i < walls.Length; i++)
                walls[i] = BattleVfx.MakeOpaqueCube(i % 2 == 0 ? stone : dark);
            var lid = BattleVfx.MakeOpaqueCube(stone);
            var corners = new GameObject[4];
            for (int i = 0; i < corners.Length; i++)
                corners[i] = BattleVfx.MakeOpaqueCube(stone, 0.35f);

            const float span0 = 2.45f;
            t = 0f;
            while (t < 0.48f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.48f));
                float h = Mathf.Lerp(0.12f, 4.2f, u);
                PlaceMuBox(walls, lid, center, ground, outs, span0, h, 0.24f);
                PlaceMuCorners(corners, center, ground, fwd, right, span0, h);
                yield return null;
            }

            t = 0f;
            bool hit = false;
            while (t < 0.42f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.42f));
                float span = Mathf.Lerp(span0, 0.85f, u);
                float h = Mathf.Lerp(4.2f, 2.1f, u);
                PlaceMuBox(walls, lid, center, ground, outs, span, h, 0.26f);
                PlaceMuCorners(corners, center, ground, fwd, right, span, h);
                if (!hit && u > 0.62f)
                {
                    hit = true;
                    if (onHit != null) onHit();
                    yield return ElementHitBeat(true, foe);
                }
                yield return null;
            }

            t = 0f;
            while (t < 0.46f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.46f));
                float yaw = u * 50f;
                Vector3 f2 = Quaternion.Euler(0f, yaw, 0f) * fwd;
                Vector3 r2 = Quaternion.Euler(0f, yaw, 0f) * right;
                float span = u < 0.4f
                    ? Mathf.Lerp(0.85f, 2.1f, u / 0.4f)
                    : Mathf.Lerp(2.1f, 0.55f, (u - 0.4f) / 0.6f);
                float h = 1.7f + Mathf.Sin(u * Mathf.PI) * 1.15f;
                PlaceMuBox(walls, lid, center, ground, new[] { f2, -f2, r2, -r2 }, span, h, 0.28f);
                PlaceMuCorners(corners, center, ground, f2, r2, span, h);
                yield return null;
            }
            yield return ElementHitBeat(true, foe);

            var pillar = BattleVfx.MakeOpaqueBeam(dark, 0.35f);
            Vector3 pit = center + Vector3.up * 0.2f;
            t = 0f;
            bool pillarFlinch = false;
            while (t < 0.36f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.36f);
                float ease = u * u;
                if (!pillarFlinch && ease > 0.72f)
                {
                    pillarFlinch = true;
                    ElementFlinch(foe);
                }
                Vector3 top = pit + Vector3.up * Mathf.Lerp(7.4f, 1.5f, ease);
                BattleVfx.PlaceBeam(pillar, pit, top, 1.05f);
                float span = Mathf.Lerp(0.55f, 3.1f, ease);
                PlaceMuBox(walls, lid, center, ground, outs, span, Mathf.Lerp(1.7f, 0.35f, ease), 0.22f);
                for (int i = 0; i < corners.Length; i++)
                {
                    if (corners[i] == null) continue;
                    float ang = i * (Mathf.PI * 0.5f) + 0.4f;
                    Vector3 fly = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Mathf.Lerp(0.8f, 3.4f, ease);
                    fly.y = ground + Mathf.Lerp(1.4f, 0.35f, ease);
                    corners[i].transform.position = fly;
                    corners[i].transform.localScale = Vector3.one * Mathf.Lerp(0.45f, 0.7f, ease);
                }
                yield return null;
            }
            var cap = OpaqueDisc(stone, 1.1f, 0.1f);
            cap.transform.position = new Vector3(center.x, ground + 0.06f, center.z);
            yield return ElementLinger(0.42f);
            if (floor != null) UnityEngine.Object.Destroy(floor);
            DestroyAll(corners);
            if (pillar != null) UnityEngine.Object.Destroy(pillar);
            if (lid != null) UnityEngine.Object.Destroy(lid);
            if (cap != null) UnityEngine.Object.Destroy(cap);
            DestroyAll(walls);
            if (!hit && onHit != null) onHit();
        }

        static void PlaceMuCorners(GameObject[] corners, Vector3 center, float ground, Vector3 fwd, Vector3 right, float span, float height)
        {
            Vector3[] spots = { fwd + right, fwd - right, -fwd + right, -fwd - right };
            for (int i = 0; i < corners.Length; i++)
            {
                if (corners[i] == null) continue;
                Vector3 dir = spots[i].normalized;
                Vector3 pos = center + dir * (span * 1.15f);
                pos.y = ground + height * 0.7f;
                corners[i].transform.position = pos;
                corners[i].transform.localScale = Vector3.one * 0.42f;
                corners[i].transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        static void PlaceMuBox(GameObject[] walls, GameObject lid, Vector3 center, float ground, Vector3[] outs, float span, float height, float thick)
        {
            float width = span * 1.85f;
            for (int i = 0; i < walls.Length; i++)
            {
                if (walls[i] == null) continue;
                Vector3 pos = center + outs[i] * span;
                pos.y = ground + height * 0.5f;
                walls[i].transform.position = pos;
                walls[i].transform.rotation = Quaternion.LookRotation(outs[i], Vector3.up);
                walls[i].transform.localScale = new Vector3(width, height, thick);
            }
            if (lid != null)
            {
                lid.transform.position = new Vector3(center.x, ground + height, center.z);
                lid.transform.rotation = Quaternion.identity;
                lid.transform.localScale = new Vector3(span * 2f + thick, 0.22f, span * 2f + thick);
            }
        }

        public static IEnumerator Ryuken(BattleFighter self, Vector3 to, Action onHit)
        {
            Color gold = new Color(1f, 0.82f, 0.12f);
            Color jade = new Color(0.28f, 0.92f, 0.22f);
            Vector3 from = self != null ? self.RightHandPos() : to;
            Vector3 start = self != null && self.Root != null ? self.Root.position : from;
            float groundY = start.y;
            Burst(from, gold, 0.2f, 10);
            Vector3 n = to - from;
            n.y = 0f;
            if (n.sqrMagnitude < 0.0001f) n = Vector3.forward;
            n.Normalize();
            Vector3 launch = from + n * 0.9f;
            Vector3 far = to + n * 6.4f;
            Vector3 side = Vector3.Cross(Vector3.up, n);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(n, side);
            Vector3 flat = to - start;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) flat = n;
            flat.Normalize();
            Vector3 overlap = new Vector3(to.x, groundY, to.z);
            Vector3 past = overlap + flat * 4.2f;
            past.y = groundY;
            start.y = groundY;
            Quaternion lockRot = self != null && self.Root != null ? self.Root.rotation : Quaternion.identity;
            const int segs = 36;
            var joints = new Vector3[segs];
            var beads = new GameObject[segs];
            var links = new GameObject[segs - 1];
            Color core = Color.Lerp(gold, Color.white, 0.35f);
            for (int i = 0; i < segs; i++)
            {
                float k = i / (float)(segs - 1);
                Color c = Color.Lerp(jade, gold, 0.25f + k * 0.75f);
                beads[i] = BattleVfx.MakeEnergyOrb(core, c, Mathf.Lerp(0.08f, 0.2f, k));
            }
            for (int i = 0; i < links.Length; i++)
                links[i] = BattleVfx.MakeEnergyBeam(core, gold, 0.07f, 0.16f);
            var head = BattleVfx.MakeEnergyOrb(core, gold, 0.34f);
            var snout = BattleVfx.MakeEnergyOrb(gold, jade, 0.16f);
            var hornL = BattleVfx.MakeEnergyBeam(jade, gold, 0.025f, 0.05f);
            var hornR = BattleVfx.MakeEnergyBeam(jade, gold, 0.025f, 0.05f);
            var whiskL = BattleVfx.MakeEnergyBeam(gold, jade, 0.012f, 0.028f);
            var whiskR = BattleVfx.MakeEnergyBeam(gold, jade, 0.012f, 0.028f);
            var eyeL = BattleVfx.MakeEnergyOrb(Color.white, jade, 0.05f);
            var eyeR = BattleVfx.MakeEnergyOrb(Color.white, jade, 0.05f);
            var trail = BattleVfx.MakeAfterglowTrail(head.transform, gold, 0.85f, 0.45f);
            float hitAt = Mathf.Clamp01(Vector3.Distance(launch, to) / Mathf.Max(0.4f, Vector3.Distance(launch, far)));
            bool hit = false;
            float t = 0f;
            const float fly = 0.48f;
            while (t < fly)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / fly);
                if (self != null && self.Root != null)
                {
                    Vector3 p = Vector3.Lerp(start, past, u);
                    p.y = groundY;
                    self.Root.position = p;
                    self.Root.rotation = lockRot;
                }
                Vector3 headPos = launch;
                Vector3 headDir = n;
                for (int i = 0; i < segs; i++)
                {
                    float along = u - (segs - 1 - i) * 0.008f;
                    if (along <= 0f) along = 0.001f;
                    along = Mathf.Clamp01(along);
                    joints[i] = DragonPoint(launch, far, side, up, along, t);
                    if (beads[i] != null)
                    {
                        beads[i].transform.position = joints[i];
                        float thick = Mathf.Lerp(0.45f, 1.2f, along);
                        if (i < 4) thick *= 0.55f;
                        if (i >= segs - 5) thick *= 1.25f;
                        beads[i].transform.localScale = Vector3.one * thick;
                    }
                    if (i == segs - 1)
                    {
                        headPos = joints[i];
                        Vector3 nxt = DragonPoint(launch, far, side, up, Mathf.Min(1f, along + 0.05f), t);
                        headDir = nxt - joints[i];
                        if (headDir.sqrMagnitude < 0.0001f) headDir = n;
                        headDir.Normalize();
                    }
                }
                for (int i = 0; i < links.Length; i++)
                {
                    float thick = Mathf.Lerp(0.55f, 1.35f, i / (float)links.Length);
                    BattleVfx.PlaceBeam(links[i], joints[i], joints[i + 1], thick);
                }
                Quaternion face = Quaternion.LookRotation(headDir, up);
                Vector3 hUp = face * Vector3.up;
                Vector3 hSide = face * Vector3.right;
                if (head != null)
                {
                    head.transform.position = headPos + headDir * 0.18f;
                    head.transform.rotation = face;
                    head.transform.localScale = new Vector3(1.15f, 0.95f, 1.85f);
                }
                if (snout != null)
                {
                    snout.transform.position = headPos + headDir * 0.58f - hUp * 0.04f;
                    snout.transform.rotation = face;
                    snout.transform.localScale = new Vector3(0.72f, 0.5f, 1.35f);
                }
                BattleVfx.PlaceBeam(hornL, headPos - headDir * 0.02f + hUp * 0.16f + hSide * 0.1f,
                    headPos - headDir * 0.12f + hUp * 0.55f + hSide * 0.16f, 0.85f);
                BattleVfx.PlaceBeam(hornR, headPos - headDir * 0.02f + hUp * 0.16f - hSide * 0.1f,
                    headPos - headDir * 0.12f + hUp * 0.55f - hSide * 0.16f, 0.85f);
                BattleVfx.PlaceBeam(whiskL, headPos + headDir * 0.28f + hSide * 0.12f,
                    headPos + headDir * 0.15f + hSide * 0.55f - hUp * 0.22f, 0.7f);
                BattleVfx.PlaceBeam(whiskR, headPos + headDir * 0.28f - hSide * 0.12f,
                    headPos + headDir * 0.15f - hSide * 0.55f - hUp * 0.22f, 0.7f);
                if (eyeL != null) eyeL.transform.position = headPos + headDir * 0.32f + hUp * 0.1f + hSide * 0.14f;
                if (eyeR != null) eyeR.transform.position = headPos + headDir * 0.32f + hUp * 0.1f - hSide * 0.14f;
                if (!hit && u >= hitAt - 0.03f)
                {
                    hit = true;
                    Burst(to, gold, 0.75f, 30);
                    Burst(to, jade, 0.42f, 16);
                    Bloom(to, gold, 1.4f);
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            t = 0f;
            while (t < 0.08f)
            {
                t += Time.deltaTime;
                if (self != null && self.Root != null)
                {
                    Vector3 p = past;
                    p.y = groundY;
                    self.Root.position = p;
                    self.Root.rotation = lockRot;
                }
                for (int i = 0; i < segs; i++)
                    if (beads[i] != null) beads[i].transform.localScale *= 0.84f;
                if (head != null) head.transform.localScale *= 0.84f;
                if (snout != null) snout.transform.localScale *= 0.84f;
                yield return null;
            }
            if (trail != null) UnityEngine.Object.Destroy(trail);
            if (head != null) UnityEngine.Object.Destroy(head);
            if (snout != null) UnityEngine.Object.Destroy(snout);
            if (hornL != null) UnityEngine.Object.Destroy(hornL);
            if (hornR != null) UnityEngine.Object.Destroy(hornR);
            if (whiskL != null) UnityEngine.Object.Destroy(whiskL);
            if (whiskR != null) UnityEngine.Object.Destroy(whiskR);
            if (eyeL != null) UnityEngine.Object.Destroy(eyeL);
            if (eyeR != null) UnityEngine.Object.Destroy(eyeR);
            for (int i = 0; i < segs; i++)
                if (beads[i] != null) UnityEngine.Object.Destroy(beads[i]);
            for (int i = 0; i < links.Length; i++)
                if (links[i] != null) UnityEngine.Object.Destroy(links[i]);
            if (!hit && onHit != null) onHit();
        }

        static Vector3 DragonPoint(Vector3 launch, Vector3 far, Vector3 side, Vector3 up, float along, float t)
        {
            Vector3 basePos = Vector3.Lerp(launch, far, along);
            float sway = Mathf.Sin(along * 12.5f - t * 5.5f);
            float lift = Mathf.Cos(along * 7.2f + t * 3.2f);
            float amp = 0.22f + along * 0.42f;
            return basePos + side * (sway * amp) + up * (0.18f + lift * amp * 0.85f);
        }

        public static IEnumerator Hakai(BattleFighter self, Transform target, Vector3 to, Action onHit)
        {
            Color voidC = new Color(0.08f, 0.02f, 0.12f);
            Color hakai = new Color(0.82f, 0.32f, 1f);
            Color ash = Color.Lerp(hakai, Color.white, 0.55f);
            Vector3 hand = self != null ? self.RightHandPos() : to;
            var palm = BattleVfx.MakeEnergyOrb(ash, hakai, 0.1f);
            palm.transform.position = hand;
            Burst(hand, hakai, 0.14f, 8);
            float t = 0f;
            while (t < 0.52f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.52f);
                hand = self != null ? self.RightHandPos() : hand;
                if (palm != null)
                {
                    palm.transform.position = hand;
                    palm.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.72f, u);
                }
                yield return null;
            }
            Vector3 dest = target != null ? BattleVfx.AtBody(target) : to;
            if (palm != null) UnityEngine.Object.Destroy(palm);
            Burst(hand, hakai, 0.2f, 10);
            Burst(dest, hakai, 0.35f, 16);
            var mass = BattleVfx.MakeDarkSphere(voidC, 0.92f);
            mass.name = "VfxHakai";
            mass.transform.position = dest;
            mass.transform.localScale = Vector3.one * 0.2f;
            var glow = BattleVfx.MakeEnergyOrb(ash, hakai, 0.35f);
            glow.transform.position = dest;
            const int bitsN = 22;
            var bits = new GameObject[bitsN];
            var bitDir = new Vector3[bitsN];
            for (int i = 0; i < bitsN; i++)
            {
                bits[i] = BattleVfx.MakeEnergyOrb(ash, hakai, 0.05f);
                bitDir[i] = UnityEngine.Random.onUnitSphere;
                bits[i].transform.position = dest;
            }
            t = 0f;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.32f);
                dest = target != null ? BattleVfx.AtBody(target) : dest;
                if (mass != null)
                {
                    mass.transform.position = dest;
                    mass.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.15f, u);
                }
                if (glow != null)
                {
                    glow.transform.position = dest;
                    glow.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 2.6f, u);
                }
                yield return null;
            }
            Bloom(dest, hakai, 1.15f);
            Burst(dest, ash, 0.42f, 18);
            if (onHit != null) onHit();
            t = 0f;
            const float collapse = 0.16f;
            while (t < collapse)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / collapse);
                dest = target != null ? BattleVfx.AtBody(target) : dest;
                if (mass != null)
                {
                    mass.transform.position = dest;
                    mass.transform.localScale = Vector3.one * Mathf.Lerp(2.15f, 0.04f, u);
                }
                if (glow != null)
                {
                    glow.transform.position = dest;
                    glow.transform.localScale = Vector3.one * Mathf.Lerp(2.6f, 0.05f, u);
                }
                for (int i = 0; i < bitsN; i++)
                {
                    if (bits[i] == null) continue;
                    bits[i].transform.position = dest + bitDir[i] * (0.2f + u * 2.4f);
                    bits[i].transform.localScale = Vector3.one * (1f - u);
                }
                yield return null;
            }
            if (mass != null) UnityEngine.Object.Destroy(mass);
            if (glow != null) UnityEngine.Object.Destroy(glow);
            for (int i = 0; i < bitsN; i++)
                if (bits[i] != null) UnityEngine.Object.Destroy(bits[i]);
        }

        static void DestroyRasengan(GameObject core, GameObject shell, GameObject[] bits, int bitsN, GameObject ringA, GameObject ringB)
        {
            if (core != null) UnityEngine.Object.Destroy(core);
            if (shell != null) UnityEngine.Object.Destroy(shell);
            if (ringA != null) UnityEngine.Object.Destroy(ringA);
            if (ringB != null) UnityEngine.Object.Destroy(ringB);
            for (int i = 0; i < bitsN; i++)
                if (bits != null && bits[i] != null) UnityEngine.Object.Destroy(bits[i]);
        }

        static void PutRasengan(Vector3 pos, Vector3 to, GameObject core, GameObject shell, GameObject[] bits, GameObject ringA, GameObject ringB, float size, float spin)
        {
            Vector3 n = to - pos;
            n.y = 0f;
            if (n.sqrMagnitude < 0.0001f) n = Vector3.forward;
            n.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, n);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(n, side);
            if (core != null)
            {
                core.transform.position = pos;
                core.transform.localScale = Vector3.one * (0.26f * size);
            }
            if (shell != null)
            {
                shell.transform.position = pos;
                shell.transform.localScale = Vector3.one * (0.36f * size);
            }
            int nBits = bits != null ? bits.Length : 0;
            for (int i = 0; i < nBits; i++)
            {
                if (bits[i] == null) continue;
                float a = spin + i * (Mathf.PI * 2f / nBits);
                float b = spin * 1.4f + i * 0.7f;
                Vector3 off = (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * (0.14f * size)
                    + n * (Mathf.Sin(b) * 0.06f * size);
                bits[i].transform.position = pos + off;
                bits[i].transform.localScale = Vector3.one * (0.072f * size);
            }
            Quaternion face = Quaternion.LookRotation(n, up);
            if (ringA != null)
            {
                ringA.transform.position = pos;
                ringA.transform.rotation = face * Quaternion.Euler(90f, spin * 40f, 0f);
                ringA.transform.localScale = new Vector3(0.32f * size, 0.016f, 0.32f * size);
            }
            if (ringB != null)
            {
                ringB.transform.position = pos;
                ringB.transform.rotation = face * Quaternion.Euler(90f, -spin * 55f, 18f);
                ringB.transform.localScale = new Vector3(0.42f * size, 0.014f, 0.42f * size);
            }
        }

        public static IEnumerator KyoshikiMurasaki(BattleFighter self, Transform caster, Vector3 to, Action onHit)
        {
            Color blue = new Color(0.12f, 0.45f, 1f);
            Color red = new Color(1f, 0.1f, 0.16f);
            Color purple = new Color(0.72f, 0.16f, 1f);
            Color core = Color.Lerp(purple, Color.white, 0.45f);
            Vector3 origin = caster != null ? caster.position : Vector3.zero;
            Vector3 aim = to - origin;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();
            Vector3 handL = self != null ? self.LeftHandPos() : origin + Vector3.up * 1.2f - Vector3.right * 0.4f;
            Vector3 handR = self != null ? self.RightHandPos() : origin + Vector3.up * 1.2f + Vector3.right * 0.4f;
            var orbB = BattleVfx.MakeEnergyOrb(Color.Lerp(blue, Color.white, 0.35f), blue, 0.12f);
            var orbR = BattleVfx.MakeEnergyOrb(Color.Lerp(red, Color.white, 0.28f), red, 0.12f);
            orbB.transform.position = handL;
            orbR.transform.position = handR;
            Burst(handL, blue, 0.16f, 8);
            Burst(handR, red, 0.16f, 8);
            float t = 0f;
            const float grow = 0.55f;
            while (t < grow)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / grow);
                handL = self != null ? self.LeftHandPos() : handL;
                handR = self != null ? self.RightHandPos() : handR;
                if (orbB != null)
                {
                    orbB.transform.position = handL;
                    orbB.transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.78f, u);
                }
                if (orbR != null)
                {
                    orbR.transform.position = handR;
                    orbR.transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.78f, u);
                }
                yield return null;
            }
            Vector3 merge = (self != null ? self.HandsCenter() : origin + Vector3.up * 1.28f) + aim * 1.05f;
            Vector3 fromB = orbB != null ? orbB.transform.position : handL;
            Vector3 fromR = orbR != null ? orbR.transform.position : handR;
            t = 0f;
            const float converge = 0.38f;
            while (t < converge)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / converge));
                merge = (self != null ? self.HandsCenter() : origin + Vector3.up * 1.28f) + aim * 1.05f;
                if (orbB != null)
                {
                    orbB.transform.position = Vector3.Lerp(fromB, merge, u);
                    orbB.transform.localScale = Vector3.one * Mathf.Lerp(0.78f, 0.92f, u);
                }
                if (orbR != null)
                {
                    orbR.transform.position = Vector3.Lerp(fromR, merge, u);
                    orbR.transform.localScale = Vector3.one * Mathf.Lerp(0.78f, 0.92f, u);
                }
                yield return null;
            }
            if (orbB != null) UnityEngine.Object.Destroy(orbB);
            if (orbR != null) UnityEngine.Object.Destroy(orbR);
            Burst(merge, blue, 0.28f, 14);
            Burst(merge, red, 0.28f, 14);
            Burst(merge, purple, 0.42f, 22);
            var mass = BattleVfx.MakeDarkSphere(new Color(0.22f, 0.02f, 0.38f), 0.95f);
            mass.name = "VfxMurasaki";
            mass.transform.position = merge;
            mass.transform.localScale = Vector3.one * 0.35f;
            var glow = BattleVfx.MakeEnergyOrb(core, purple, 0.42f);
            glow.transform.position = merge;
            var trail = BattleVfx.MakeAfterglowTrail(mass.transform, purple, 0.62f, 0.38f);
            const int swirlN = 8;
            var swirl = new GameObject[swirlN];
            for (int i = 0; i < swirlN; i++)
            {
                Color c = (i % 2 == 0) ? blue : red;
                swirl[i] = BattleVfx.MakeEnergyOrb(Color.Lerp(c, purple, 0.45f), c, 0.07f);
            }
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.18f);
                float s = Mathf.Lerp(0.35f, 1.05f, u);
                if (mass != null) mass.transform.localScale = Vector3.one * s;
                if (glow != null)
                {
                    glow.transform.position = merge;
                    glow.transform.localScale = Vector3.one * (s * 1.35f);
                }
                yield return null;
            }
            Vector3 dest = to;
            t = 0f;
            const float fly = 0.38f;
            while (t < fly)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fly));
                Vector3 pos = Vector3.Lerp(merge, dest, u);
                if (mass != null)
                {
                    mass.transform.position = pos;
                    mass.transform.localScale = Vector3.one * (1.05f + Mathf.Sin(t * 18f) * 0.06f);
                }
                if (glow != null)
                {
                    glow.transform.position = pos;
                    glow.transform.localScale = Vector3.one * (1.45f + Mathf.Sin(t * 18f) * 0.08f);
                }
                for (int i = 0; i < swirlN; i++)
                {
                    if (swirl[i] == null) continue;
                    float a = t * 9f + i * (Mathf.PI * 2f / swirlN);
                    swirl[i].transform.position = pos
                        + aim * Mathf.Sin(a) * 0.18f
                        + Vector3.up * (Mathf.Cos(a) * 0.32f)
                        + Vector3.Cross(Vector3.up, aim) * (Mathf.Cos(a * 1.3f) * 0.28f);
                }
                yield return null;
            }
            if (mass != null) mass.transform.position = dest;
            if (glow != null) glow.transform.position = dest;
            Burst(dest, purple, 0.85f, 36);
            Burst(dest, core, 0.5f, 20);
            Burst(dest, blue, 0.28f, 12);
            Burst(dest, red, 0.28f, 12);
            Bloom(dest, purple, 2.1f);
            if (onHit != null) onHit();
            for (int i = 0; i < swirlN; i++)
                if (swirl[i] != null) UnityEngine.Object.Destroy(swirl[i]);
            if (trail != null) UnityEngine.Object.Destroy(trail);
            t = 0f;
            while (t < 0.36f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.36f);
                if (mass != null) mass.transform.localScale = Vector3.one * Mathf.Lerp(1.05f, 3.6f, u);
                if (glow != null) glow.transform.localScale = Vector3.one * Mathf.Lerp(1.45f, 4.2f, u);
                yield return null;
            }
            if (mass != null) UnityEngine.Object.Destroy(mass);
            if (glow != null) UnityEngine.Object.Destroy(glow);
        }

        public static IEnumerator Kamehameha(BattleFighter self, Vector3 to, Color color, Action onHit)
        {
            Color core = Color.Lerp(color, Color.white, 0.62f);
            Vector3 from = self != null ? self.KiFront(to, 0.95f, false) : to;
            var orb = BattleVfx.MakeEnergyOrb(color, 0.12f);
            orb.transform.position = from;
            Burst(from, color, 0.12f, 8);
            float t = 0f;
            while (t < 1.15f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 1.15f);
                from = self != null ? self.KiFront(to, 0.95f, false) : from;
                orb.transform.position = from;
                orb.transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 1.05f, u);
                yield return null;
            }
            Burst(from, color, 0.22f, 12);
            var beam = BattleVfx.MakeEnergyBeam(core, color, 0.16f, 0.38f);
            t = 0f;
            bool hit = false;
            while (t < 0.48f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.48f);
                from = self != null ? self.KiFront(to, 0.95f, false) : from;
                if (orb != null) orb.transform.position = from;
                float thick = u < 0.14f ? Mathf.Lerp(0.22f, 1.12f, u / 0.14f) : 1.05f + Mathf.Sin(t * 22f) * 0.08f;
                BattleVfx.PlaceBeam(beam, from, to, thick);
                if (!hit && u > 0.1f)
                {
                    hit = true;
                    Burst(to, color, 0.55f, 28);
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            if (orb != null) UnityEngine.Object.Destroy(orb);
            if (beam != null) UnityEngine.Object.Destroy(beam);
            Burst(to, color, 0.42f, 18);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator Cero(BattleFighter self, Vector3 to, Color color, Action onHit)
        {
            Color ink = new Color(0.02f, 0.01f, 0.02f);
            Color rim = new Color(0.28f, 0.02f, 0.06f);
            Vector3 from = self != null ? self.KiFront(to, 0.58f, true) : to;
            var orb = BattleVfx.MakeEnergyOrb(ink, rim, 0.1f);
            orb.transform.position = from;
            Burst(from, rim, 0.08f, 4);
            float t = 0f;
            while (t < 0.85f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.85f);
                from = self != null ? self.KiFront(to, 0.58f, true) : from;
                orb.transform.position = from;
                orb.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.62f, u);
                yield return null;
            }
            if (orb != null) UnityEngine.Object.Destroy(orb);
            var beam = BattleVfx.MakeEnergyBeam(ink, rim, 0.1f, 0.34f);
            t = 0f;
            bool hit = false;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.28f);
                from = self != null ? self.KiFront(to, 0.58f, true) : from;
                float thick = u < 0.28f ? Mathf.Lerp(0.12f, 1.15f, u / 0.28f) : 1.05f;
                BattleVfx.PlaceBeam(beam, from, to, thick);
                if (!hit && u > 0.08f)
                {
                    hit = true;
                    Burst(to, rim, 0.42f, 18);
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            if (beam != null) UnityEngine.Object.Destroy(beam);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator GranReyCero(BattleFighter self, Vector3 to, Color color, Action onHit)
        {
            Color ink = new Color(0.02f, 0.0f, 0.02f);
            Color blood = new Color(0.72f, 0.05f, 0.07f);
            Color glow = new Color(1f, 0.16f, 0.08f);
            Vector3 mouth = self != null ? self.KiFront(to, 0.62f, true) : to;
            Vector3 hand = self != null ? self.RightHandPos() : mouth;
            var bleed = BattleVfx.MakeEnergyBeam(blood, glow, 0.02f, 0.05f);
            var orb = BattleVfx.MakeEnergyOrb(ink, blood, 0.12f);
            orb.transform.position = mouth;
            const int crownN = 16;
            var crown = new GameObject[crownN];
            for (int i = 0; i < crownN; i++)
                crown[i] = BattleVfx.MakeEnergyBeam(glow, blood, 0.028f, 0.07f);
            Burst(hand, blood, 0.1f, 6);
            float t = 0f;
            while (t < 0.82f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.82f);
                hand = self != null ? self.RightHandPos() : hand;
                mouth = self != null ? self.KiFront(to, 0.62f, true) : mouth;
                if (u < 0.35f) BattleVfx.PlaceBeam(bleed, hand, mouth, 0.7f + u);
                else if (bleed != null)
                {
                    UnityEngine.Object.Destroy(bleed);
                    bleed = null;
                }
                if (orb != null)
                {
                    orb.transform.position = mouth;
                    orb.transform.localScale = Vector3.one * Mathf.Lerp(0.14f, 0.72f, u);
                }
                Vector3 aim = to - mouth;
                if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
                PlaceSawHelix(crown, mouth - aim.normalized * 0.08f, mouth + aim.normalized * 0.18f,
                    t * 9f, 4, 0.1f, 0.62f + u * 0.28f, 3.2f, 1.15f);
                yield return null;
            }
            if (bleed != null) UnityEngine.Object.Destroy(bleed);
            if (orb != null) UnityEngine.Object.Destroy(orb);
            for (int i = 0; i < crownN; i++)
                if (crown[i] != null) UnityEngine.Object.Destroy(crown[i]);

            Vector3 from = self != null ? self.KiFront(to, 0.62f, true) : mouth;
            var core = BattleVfx.MakeEnergyBeam(ink, blood, 0.12f, 0.36f);
            Burst(from, glow, 0.24f, 14);
            t = 0f;
            bool hit = false;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.55f);
                from = self != null ? self.KiFront(to, 0.62f, true) : from;
                float thick = u < 0.16f ? Mathf.Lerp(0.45f, 1.35f, u / 0.16f) : 1.18f + Mathf.Sin(t * 18f) * 0.08f;
                BattleVfx.PlaceBeam(core, from, to, thick);
                if (!hit && u > 0.14f)
                {
                    hit = true;
                    Burst(to, glow, 0.85f, 36);
                    Burst(to, ink, 0.5f, 18);
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            if (core != null) UnityEngine.Object.Destroy(core);
            var boom = BattleVfx.MakeMistSphere(new Color(0.08f, 0.01f, 0.02f), 0.32f);
            boom.transform.position = to;
            t = 0f;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.32f);
                if (boom != null) boom.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 3.2f, u);
                yield return null;
            }
            Burst(to, blood, 0.65f, 24);
            if (boom != null) UnityEngine.Object.Destroy(boom);
            if (!hit && onHit != null) onHit();
        }

        static void PlaceSawHelix(GameObject[] segs, Vector3 from, Vector3 to, float phase, int ridges, float rIn, float rOut, float twists, float thick)
        {
            if (segs == null || segs.Length == 0) return;
            ridges = Mathf.Max(2, ridges);
            int slices = segs.Length / ridges + 1;
            if (slices < 2) slices = 2;
            Vector3 n = to - from;
            float len = Mathf.Max(0.08f, n.magnitude);
            n /= len;
            Vector3 side = Vector3.Cross(n, Vector3.up);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(n, side);
            int idx = 0;
            for (int ridge = 0; ridge < ridges; ridge++)
            {
                Vector3 prev = Vector3.zero;
                for (int s = 0; s < slices; s++)
                {
                    float z = (s / (float)(slices - 1)) * len;
                    float jag = (s % 2 == 0) ? 0.75f : -0.75f;
                    float ang = z * twists + ridge * (Mathf.PI * 2f / ridges) + phase + jag;
                    float r = (s % 2 == 0) ? rOut : rIn;
                    Vector3 p = from + n * z + (side * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * r;
                    if (s > 0 && idx < segs.Length)
                    {
                        BattleVfx.PlaceBeam(segs[idx], prev, p, thick);
                        idx++;
                    }
                    prev = p;
                }
            }
            for (int i = idx; i < segs.Length; i++)
                BattleVfx.PlaceBeam(segs[i], from, from + n * 0.02f, 0.05f);
        }

        public static IEnumerator Sakanade(Action onHit)
        {
            BattleCam.SetSenseInvert(true);
            yield return null;
            if (onHit != null) onHit();
        }

        public static IEnumerator Getsuga(Vector3 from, Vector3 to, Color color, Action onHit)
        {
            var blade = BattleVfx.MakeGetsugaBlade(color);
            Vector3 dir = to - from;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            blade.transform.position = from;
            blade.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Burst(from, color, 0.22f, 12);
            float t = 0f;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.32f));
                blade.transform.position = Vector3.Lerp(from, to, u);
                blade.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up)
                    * Quaternion.Euler(0f, 0f, u * 40f);
                yield return null;
            }
            if (blade != null) UnityEngine.Object.Destroy(blade);
            Burst(to, color, 0.5f, 26);
            if (onHit != null) onHit();
        }

        public static IEnumerator SpiritGun(Vector3 from, Vector3 to, Color color, Action onHit)
        {
            var orb = BattleVfx.MakeEnergyOrb(color, 0.07f);
            orb.transform.position = from;
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                if (orb != null) orb.transform.localScale = Vector3.one * Mathf.Lerp(0.08f, 0.28f, Mathf.Clamp01(t / 0.16f));
                yield return null;
            }
            if (orb != null) UnityEngine.Object.Destroy(orb);
            var beam = BattleVfx.MakeEnergyBeam(Color.white, color, 0.05f, 0.12f);
            BattleVfx.PlaceBeam(beam, from, to, 0.7f);
            Burst(from, color, 0.14f, 8);
            t = 0f;
            bool hit = false;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.16f);
                BattleVfx.PlaceBeam(beam, from, to, 0.7f + Mathf.Sin(u * Mathf.PI) * 0.2f);
                if (!hit && u > 0.35f)
                {
                    hit = true;
                    Burst(to, color, 0.36f, 18);
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            if (beam != null) UnityEngine.Object.Destroy(beam);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator ShinraTensei(Vector3 from, Vector3 to, Color color, Action onHit)
        {
            Color black = new Color(0.05f, 0.02f, 0.08f);
            Color rim = new Color(0.22f, 0.04f, 0.32f);
            var dome = BattleVfx.MakeMistSphere(black, 0.32f);
            dome.transform.position = from;
            var rings = new GameObject[5];
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = BattleVfx.MakeWaveRing(i % 2 == 0 ? black : rim, true);
                rings[i].transform.position = from;
                rings[i].transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Burst(from, rim, 0.28f, 14);
            float t = 0f;
            bool hit = false;
            float reach = Mathf.Max(1.8f, Vector3.Distance(from, to) + 1.1f);
            while (t < 0.75f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.75f);
                if (dome != null)
                    dome.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, reach * 1.15f, u);
                for (int i = 0; i < rings.Length; i++)
                {
                    if (rings[i] == null) continue;
                    float ru = Mathf.Clamp01(u - i * 0.06f);
                    float rad = Mathf.Lerp(0.2f, reach, ru);
                    rings[i].transform.position = from;
                    rings[i].transform.localScale = new Vector3(rad, 0.08f + i * 0.02f, rad);
                }
                if (!hit && u > 0.38f)
                {
                    hit = true;
                    Burst(to, rim, 0.55f, 24);
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            if (dome != null) UnityEngine.Object.Destroy(dome);
            for (int i = 0; i < rings.Length; i++)
                if (rings[i] != null) UnityEngine.Object.Destroy(rings[i]);
            if (!hit && onHit != null) onHit();
        }

        static void PaintSheathSolid(GameObject go, string child, Color color)
        {
            if (go == null) return;
            var tr = go.transform.Find(child);
            if (tr == null) return;
            var rend = tr.GetComponent<MeshRenderer>();
            if (rend != null) rend.sharedMaterial = BattleVfx.SolidMat(color, 0.96f);
        }

        static Vector3 MakankoMuzzle(BattleFighter self, Vector3 to, float push)
        {
            Vector3 bone = self != null ? self.ForeheadPos() : to;
            Vector3 origin = self != null && self.Root != null ? self.Root.position : bone;
            Vector3 aim = to - origin;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            return bone + aim.normalized * push;
        }

        public static IEnumerator Makankosappo(BattleFighter self, Vector3 to, Color color, Action onHit)
        {
            Color gold = new Color(1f, 0.92f, 0.35f);
            Color violet = new Color(0.28f, 0.0f, 0.48f);
            Vector3 from = MakankoMuzzle(self, to, 0.42f);
            var orb = BattleVfx.MakeEnergyOrb(gold, violet, 0.08f);
            PaintSheathSolid(orb, "Shell", violet);
            orb.transform.position = from;
            float t = 0f;
            while (t < 1.05f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 1.05f);
                from = MakankoMuzzle(self, to, 0.42f);
                orb.transform.position = from;
                orb.transform.localScale = Vector3.one * Mathf.Lerp(0.06f, 0.28f, u);
                orb.transform.Rotate(0f, 420f * Time.deltaTime, 0f);
                yield return null;
            }
            if (orb != null) UnityEngine.Object.Destroy(orb);
            Vector3 n = to - from;
            float len = Mathf.Max(0.2f, n.magnitude);
            n /= len;
            Vector3 side = Vector3.Cross(n, Vector3.up);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(n, side);
            var core = BattleVfx.MakeEnergyBeam(Color.white, violet, 0.024f, 0.078f);
            PaintSheathSolid(core, "Sheath", violet);
            const int perTurn = 12;
            const int turns = 8;
            const int segs = perTurn * turns;
            var coil = new GameObject[segs];
            for (int i = 0; i < segs; i++)
            {
                coil[i] = BattleVfx.MakeEnergyBeam(gold, violet, 0.011f, 0.032f);
                PaintSheathSolid(coil[i], "Sheath", violet);
            }
            Burst(from, violet, 0.12f, 8);
            t = 0f;
            bool hit = false;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.55f);
                from = MakankoMuzzle(self, to, 0.58f);
                n = to - from;
                len = Mathf.Max(0.2f, n.magnitude);
                n /= len;
                side = Vector3.Cross(n, Vector3.up);
                if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
                side.Normalize();
                up = Vector3.Cross(n, side);
                BattleVfx.PlaceBeam(core, from, to, 0.48f + Mathf.Sin(t * 30f) * 0.07f);
                float spin = t * 58f;
                float twists = turns * Mathf.PI * 2f / len;
                const float r = 0.13f;
                for (int i = 0; i < segs; i++)
                {
                    float z0 = (i / (float)segs) * len;
                    float z1 = ((i + 1) / (float)segs) * len;
                    float a0 = z0 * twists + spin;
                    float a1 = z1 * twists + spin;
                    Vector3 p0 = from + n * z0 + (side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * r;
                    Vector3 p1 = from + n * z1 + (side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * r;
                    BattleVfx.PlaceBeam(coil[i], p0, p1, 0.7f);
                }
                if (!hit && u > 0.28f)
                {
                    hit = true;
                    Burst(to, gold, 0.38f, 18);
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            if (core != null) UnityEngine.Object.Destroy(core);
            for (int i = 0; i < segs; i++)
                if (coil[i] != null) UnityEngine.Object.Destroy(coil[i]);
            if (!hit && onHit != null) onHit();
        }

        public static IEnumerator Genkidama(Vector3 origin, Vector3 to, Color color, Action onHit)
        {
            Vector3 hold = origin + Vector3.up * 3.8f;
            var orb = BattleVfx.MakeEnergyOrb(color, 0.35f);
            orb.transform.position = origin + Vector3.up * 2.4f;
            int sparks = 16;
            var bits = new GameObject[sparks];
            var bitFrom = new Vector3[sparks];
            for (int i = 0; i < sparks; i++)
            {
                Vector3 around = origin + new Vector3(
                    UnityEngine.Random.Range(-2.8f, 2.8f),
                    UnityEngine.Random.Range(0.2f, 1.8f),
                    UnityEngine.Random.Range(-2.8f, 2.8f));
                bitFrom[i] = around;
                bits[i] = BattleVfx.MakeEnergyOrb(Color.Lerp(color, Color.white, 0.4f), 0.07f);
                bits[i].transform.position = around;
            }
            Burst(orb.transform.position, color, 0.2f, 10);
            float t = 0f;
            while (t < 1.25f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 1.25f);
                Vector3 center = Vector3.Lerp(origin + Vector3.up * 2.4f, hold, u);
                orb.transform.position = center;
                orb.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 3.4f, u);
                for (int i = 0; i < sparks; i++)
                {
                    if (bits[i] == null) continue;
                    float bu = Mathf.Clamp01((u - i * 0.035f) / 0.5f);
                    bits[i].transform.position = Vector3.Lerp(bitFrom[i], center, bu);
                    if (bu >= 1f)
                    {
                        UnityEngine.Object.Destroy(bits[i]);
                        bits[i] = null;
                    }
                }
                yield return null;
            }
            for (int i = 0; i < sparks; i++)
                if (bits[i] != null) UnityEngine.Object.Destroy(bits[i]);
            t = 0f;
            while (t < 0.38f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.38f));
                orb.transform.position = Vector3.Lerp(hold, to, u);
                yield return null;
            }
            t = 0f;
            while (t < 0.42f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.42f);
                orb.transform.position = to;
                orb.transform.localScale = Vector3.one * Mathf.Lerp(3.4f, 5.4f, u);
                yield return null;
            }
            if (orb != null) UnityEngine.Object.Destroy(orb);
            yield return BigBoom(to, color, 3.2f, onHit);
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            Vector3 ab = Vector3.Lerp(a, b, t);
            Vector3 bc = Vector3.Lerp(b, c, t);
            return Vector3.Lerp(ab, bc, t);
        }

        static IEnumerator FormMagazine(Vector3 magPos, Color color, GameObject mag)
        {
            mag.transform.position = magPos;
            mag.transform.rotation = Quaternion.Euler(18f, 28f, 12f);
            Burst(magPos, color, 0.22f, 10);
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.2f));
                mag.transform.localScale = Vector3.one * u;
                mag.transform.rotation = Quaternion.Euler(18f, 28f + u * 20f, 12f);
                yield return null;
            }
            mag.transform.localScale = Vector3.one;
        }

        public static IEnumerator Hound(Vector3 from, Vector3 to, Transform target, Color color, Action onHit)
        {
            Vector3 aim = to - from;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, aim);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            const int split = 27;
            var bits = new GameObject[split];
            var packedPos = new Vector3[split];
            var spreadPos = new Vector3[split];
            yield return FormSplit27(from, aim, color, 0.92f, 0.34f, bits, packedPos, spreadPos, 0.72f);
            const int segs = 6;
            var beams = new GameObject[split * segs];
            Color core = Color.Lerp(color, Color.white, 0.35f);
            for (int i = 0; i < beams.Length; i++)
                beams[i] = BattleVfx.MakeEnergyBeam(core, color, 0.016f, 0.04f);
            var apex = new Vector3[split];
            bool hit = false;
            float t = 0f;
            const float fly = 0.72f;
            while (t < fly)
            {
                t += Time.deltaTime;
                Vector3 homing = target != null ? BattleVfx.AtBody(target) : to;
                float u = Mathf.Clamp01(t / fly);
                for (int i = 0; i < split; i++)
                {
                    Vector3 shot = bits[i] != null ? bits[i].transform.position : spreadPos[i];
                    float lane = ((i % 9) - 4f);
                    apex[i] = Vector3.Lerp(shot, homing, 0.4f)
                        + Vector3.up * (18.5f + (i % 4) * 2.4f)
                        + side * (lane * 0.22f);
                    for (int s = 0; s < segs; s++)
                    {
                        float a = (s / (float)segs) * u;
                        float b = ((s + 1) / (float)segs) * u;
                        Vector3 p0 = Bezier(shot, apex[i], homing, a);
                        Vector3 p1 = Bezier(shot, apex[i], homing, Mathf.Max(a + 0.02f, b));
                        BattleVfx.PlaceBeam(beams[i * segs + s], p0, p1, 0.62f);
                    }
                }
                if (!hit && u > 0.84f)
                {
                    hit = true;
                    Burst(homing, color, 0.48f, 22);
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            Vector3 end = target != null ? BattleVfx.AtBody(target) : to;
            Burst(end, color, 0.42f, 16);
            if (!hit && onHit != null) onHit();
            t = 0f;
            const float linger = 0.25f;
            while (t < linger)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < split; i++)
                {
                    Vector3 shot = bits[i] != null ? bits[i].transform.position : spreadPos[i];
                    for (int s = 0; s < segs; s++)
                    {
                        float a = s / (float)segs;
                        float b = (s + 1) / (float)segs;
                        Vector3 p0 = Bezier(shot, apex[i], end, a);
                        Vector3 p1 = Bezier(shot, apex[i], end, b);
                        BattleVfx.PlaceBeam(beams[i * segs + s], p0, p1, 0.62f);
                    }
                }
                yield return null;
            }
            for (int i = 0; i < split; i++)
                if (bits[i] != null) UnityEngine.Object.Destroy(bits[i]);
            for (int i = 0; i < beams.Length; i++)
                if (beams[i] != null) UnityEngine.Object.Destroy(beams[i]);
        }

        public static IEnumerator Meteora(Vector3 from, Vector3 to, Color color, Action onHit)
        {
            Vector3 aim = to - from;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();
            const int split = 27;
            var bits = new GameObject[split];
            var packedPos = new Vector3[split];
            var spreadPos = new Vector3[split];
            yield return FormSplit27(from, aim, color, 0.92f, 0.34f, bits, packedPos, spreadPos, 0.72f);
            var beams = new GameObject[split];
            Color core = Color.Lerp(color, Color.white, 0.3f);
            for (int i = 0; i < split; i++)
                beams[i] = BattleVfx.MakeEnergyBeam(core, color, 0.022f, 0.055f);
            Vector3 dest = to;
            bool hit = false;
            float t = 0f;
            const float fly = 0.48f;
            while (t < fly)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / fly);
                for (int i = 0; i < split; i++)
                {
                    Vector3 shot = bits[i] != null ? bits[i].transform.position : spreadPos[i];
                    Vector3 tip = Vector3.Lerp(shot, dest, u);
                    BattleVfx.PlaceBeam(beams[i], shot, tip, 0.72f);
                }
                if (!hit && u > 0.82f)
                {
                    hit = true;
                    if (onHit != null) onHit();
                }
                yield return null;
            }
            t = 0f;
            const float linger = 0.25f;
            while (t < linger)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < split; i++)
                {
                    Vector3 shot = bits[i] != null ? bits[i].transform.position : spreadPos[i];
                    BattleVfx.PlaceBeam(beams[i], shot, dest, 0.72f);
                }
                yield return null;
            }
            yield return BigBoom(to, color, 3.6f, hit ? null : onHit);
            float fade = 0f;
            while (fade < 0.22f)
            {
                fade += Time.deltaTime;
                float u = 1f - fade / 0.22f;
                for (int i = 0; i < split; i++)
                {
                    if (bits[i] != null) bits[i].transform.localScale = Vector3.one * u;
                    if (beams[i] != null) beams[i].transform.localScale *= 0.9f;
                }
                yield return null;
            }
            for (int i = 0; i < split; i++)
            {
                if (bits[i] != null) UnityEngine.Object.Destroy(bits[i]);
                if (beams[i] != null) UnityEngine.Object.Destroy(beams[i]);
            }
        }

        static IEnumerator BigBoom(Vector3 pos, Color color, float size, Action onHit)
        {
            var ball = BattleVfx.MakeEnergyOrb(color, 0.4f);
            ball.transform.position = pos;
            var ring = BattleVfx.MakeWaveRing(Color.Lerp(color, Color.white, 0.35f), true);
            ring.transform.position = pos;
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Burst(pos, color, 0.85f, 40);
            Burst(pos, Color.white, 0.55f, 22);
            if (onHit != null) onHit();
            float t = 0f;
            while (t < 0.48f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.48f);
                if (ball != null) ball.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, size, u);
                if (ring != null) ring.transform.localScale = new Vector3(size * u, 0.08f, size * u);
                yield return null;
            }
            Burst(pos, color, 0.7f, 28);
            if (ball != null) UnityEngine.Object.Destroy(ball);
            if (ring != null) UnityEngine.Object.Destroy(ring);
        }

        static GameObject CoffinSlab(Color fill, Color edge, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "VfxCoffinSlab";
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
            go.transform.localScale = scale;
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = BattleVfx.SolidMat(fill, 1f);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col2 = shell.GetComponent<Collider>();
            if (col2 != null) UnityEngine.Object.Destroy(col2);
            shell.transform.SetParent(go.transform, false);
            shell.transform.localScale = Vector3.one * 1.05f;
            var sr = shell.GetComponent<MeshRenderer>();
            sr.sharedMaterial = BattleVfx.SolidMat(edge, 0.92f);
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            return go;
        }

        public static IEnumerator Kurohitsugi(Vector3 to, Transform target, Color color, Action onHit)
        {
            Vector3 center = target != null ? BattleVfx.AtBody(target) : to;
            Color black = new Color(0.02f, 0.0f, 0.03f);
            Color rim = new Color(0.28f, 0.02f, 0.36f);
            const float w = 2.35f;
            const float h = 4.35f;
            const float d = 1.35f;
            const float th = 0.12f;
            var slabs = new GameObject[8];
            var fromPos = new Vector3[8];
            var restPos = new Vector3[8];
            var restRot = new Quaternion[8];
            slabs[0] = CoffinSlab(black, rim, new Vector3(w, th, d));
            slabs[1] = CoffinSlab(black, rim, new Vector3(w * 1.08f, th, d * 1.1f));
            slabs[2] = CoffinSlab(black, rim, new Vector3(th, h, d));
            slabs[3] = CoffinSlab(black, rim, new Vector3(th, h, d));
            slabs[4] = CoffinSlab(black, rim, new Vector3(w, h, th));
            slabs[5] = CoffinSlab(black, rim, new Vector3(w, h, th));
            slabs[6] = CoffinSlab(black, rim, new Vector3(w * 0.78f, 0.28f, d * 0.78f));
            slabs[7] = CoffinSlab(black, rim, new Vector3(w * 0.78f, 0.28f, d * 0.78f));
            Vector3 right = Vector3.right;
            Vector3 fwd = Vector3.forward;
            if (target != null)
            {
                Vector3 look = target.forward;
                look.y = 0f;
                if (look.sqrMagnitude > 0.0001f) fwd = look.normalized;
                right = Vector3.Cross(Vector3.up, fwd);
                if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
                right.Normalize();
            }
            restPos[0] = center - Vector3.up * (h * 0.5f);
            restPos[1] = center + Vector3.up * (h * 0.5f);
            restPos[2] = center - right * (w * 0.5f);
            restPos[3] = center + right * (w * 0.5f);
            restPos[4] = center - fwd * (d * 0.5f);
            restPos[5] = center + fwd * (d * 0.5f);
            restPos[6] = center + Vector3.up * (h * 0.5f + 0.28f) - fwd * 0.28f;
            restPos[7] = center + Vector3.up * (h * 0.5f + 0.28f) + fwd * 0.28f;
            restRot[0] = Quaternion.identity;
            restRot[1] = Quaternion.identity;
            restRot[2] = Quaternion.identity;
            restRot[3] = Quaternion.identity;
            restRot[4] = Quaternion.identity;
            restRot[5] = Quaternion.identity;
            restRot[6] = Quaternion.AngleAxis(-32f, right);
            restRot[7] = Quaternion.AngleAxis(32f, right);
            Vector3[] outDir =
            {
                -Vector3.up, Vector3.up, -right, right, -fwd, fwd, Vector3.up - fwd, Vector3.up + fwd
            };
            for (int i = 0; i < 8; i++)
            {
                fromPos[i] = restPos[i] + outDir[i].normalized * 4.2f;
                slabs[i].transform.position = fromPos[i];
                slabs[i].transform.rotation = restRot[i];
            }
            Burst(center, rim, 0.38f, 22);
            float t = 0f;
            while (t < 0.58f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.58f));
                center = target != null ? BattleVfx.AtBody(target) : to;
                restPos[0] = center - Vector3.up * (h * 0.5f);
                restPos[1] = center + Vector3.up * (h * 0.5f);
                restPos[2] = center - right * (w * 0.5f);
                restPos[3] = center + right * (w * 0.5f);
                restPos[4] = center - fwd * (d * 0.5f);
                restPos[5] = center + fwd * (d * 0.5f);
                restPos[6] = center + Vector3.up * (h * 0.5f + 0.28f) - fwd * 0.28f;
                restPos[7] = center + Vector3.up * (h * 0.5f + 0.28f) + fwd * 0.28f;
                for (int i = 0; i < 8; i++)
                {
                    fromPos[i] = restPos[i] + outDir[i].normalized * 4.2f;
                    slabs[i].transform.position = Vector3.Lerp(fromPos[i], restPos[i], u);
                    slabs[i].transform.rotation = restRot[i];
                }
                yield return null;
            }
            Burst(center, rim, 0.55f, 24);
            if (onHit != null) onHit();
            t = 0f;
            var hold = new Vector3[8];
            for (int i = 0; i < 8; i++) hold[i] = slabs[i].transform.position;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.28f);
                center = target != null ? BattleVfx.AtBody(target) : to;
                for (int i = 0; i < 8; i++)
                {
                    if (slabs[i] == null) continue;
                    slabs[i].transform.position = Vector3.Lerp(hold[i], Vector3.Lerp(hold[i], center, 0.16f), u);
                }
                yield return null;
            }
            Burst(center, rim, 0.7f, 28);
            for (int i = 0; i < 8; i++)
                if (slabs[i] != null) UnityEngine.Object.Destroy(slabs[i]);
        }

        public static IEnumerator KaiokenKi(Transform caster, Color color, Action onHit)
        {
            Color red = new Color(1f, 0.08f, 0.04f);
            Color hot = new Color(1f, 0.32f, 0.08f);
            Vector3 feet = caster != null ? caster.position + Vector3.up * 0.05f : Vector3.zero;
            Vector3 chest = caster != null ? caster.position + Vector3.up * 1.05f : Vector3.up;
            var dome = BattleVfx.MakeEnergyOrb(hot, red, 0.55f);
            dome.transform.position = chest;
            var pillar = BattleVfx.MakeEnergyBeam(hot, red, 0.28f, 0.55f);
            var rings = new GameObject[4];
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = BattleVfx.MakeWaveRing(i % 2 == 0 ? red : hot, true);
                rings[i].transform.position = feet;
                rings[i].transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Burst(chest, red, 0.32f, 16);
            float t = 0f;
            while (t < 0.85f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.85f);
                if (caster != null)
                {
                    feet = caster.position + Vector3.up * 0.05f;
                    chest = caster.position + Vector3.up * 1.05f;
                }
                if (dome != null)
                {
                    dome.transform.position = chest;
                    dome.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 3.8f, u);
                }
                if (pillar != null)
                    BattleVfx.PlaceBeam(pillar, feet, feet + Vector3.up * Mathf.Lerp(0.4f, 4.2f, u), 1.2f + u);
                for (int i = 0; i < rings.Length; i++)
                {
                    if (rings[i] == null) continue;
                    float ru = Mathf.Clamp01(u - i * 0.08f);
                    rings[i].transform.position = feet;
                    rings[i].transform.localScale = new Vector3(0.3f + ru * 3.2f, 0.07f, 0.3f + ru * 3.2f);
                }
                yield return null;
            }
            Burst(chest, red, 0.42f, 18);
            if (dome != null) UnityEngine.Object.Destroy(dome);
            if (pillar != null) UnityEngine.Object.Destroy(pillar);
            for (int i = 0; i < rings.Length; i++)
                if (rings[i] != null) UnityEngine.Object.Destroy(rings[i]);
            if (onHit != null) onHit();
        }

        public static IEnumerator ReiatsuRelease(Transform caster, Color color, Action onHit)
        {
            Color black = new Color(0.06f, 0.02f, 0.1f);
            Color rim = new Color(0.32f, 0.08f, 0.48f);
            Vector3 feet = caster != null ? caster.position + Vector3.up * 0.05f : Vector3.zero;
            Vector3 chest = caster != null ? caster.position + Vector3.up * 1.05f : Vector3.up;
            var dome = BattleVfx.MakeMistSphere(black, 0.32f);
            dome.transform.position = chest;
            var pillar = BattleVfx.MakeEnergyBeam(black, rim, 0.28f, 0.55f);
            var rings = new GameObject[4];
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = BattleVfx.MakeWaveRing(i % 2 == 0 ? black : rim, true);
                rings[i].transform.position = feet;
                rings[i].transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Burst(chest, rim, 0.3f, 16);
            float t = 0f;
            while (t < 0.85f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.85f);
                if (caster != null)
                {
                    feet = caster.position + Vector3.up * 0.05f;
                    chest = caster.position + Vector3.up * 1.05f;
                }
                if (dome != null)
                {
                    dome.transform.position = chest;
                    dome.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 2.05f, u);
                }
                if (pillar != null)
                    BattleVfx.PlaceBeam(pillar, feet, feet + Vector3.up * Mathf.Lerp(0.4f, 2.35f, u), 1.05f + u * 0.35f);
                for (int i = 0; i < rings.Length; i++)
                {
                    if (rings[i] == null) continue;
                    float ru = Mathf.Clamp01(u - i * 0.08f);
                    rings[i].transform.position = feet;
                    rings[i].transform.localScale = new Vector3(0.3f + ru * 2.2f, 0.07f, 0.3f + ru * 2.2f);
                }
                yield return null;
            }
            Burst(chest, rim, 0.42f, 18);
            if (dome != null) UnityEngine.Object.Destroy(dome);
            if (pillar != null) UnityEngine.Object.Destroy(pillar);
            for (int i = 0; i < rings.Length; i++)
                if (rings[i] != null) UnityEngine.Object.Destroy(rings[i]);
            if (onHit != null) onHit();
        }

        public static IEnumerator ShogunHenshin(Transform caster, Vector3 to, Color color, Action onHit)
        {
            Color black = new Color(0.05f, 0.02f, 0.06f);
            Color rim = new Color(0.28f, 0.04f, 0.08f);
            Vector3 pos = caster != null ? caster.position + Vector3.up * 1.05f : Vector3.up;
            var dome = BattleVfx.MakeMistSphere(black, 0.32f);
            dome.transform.position = pos;
            Burst(pos, rim, 0.28f, 14);
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.7f);
                if (caster != null) pos = caster.position + Vector3.up * 0.95f;
                if (dome != null)
                {
                    dome.transform.position = pos;
                    dome.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.05f, u);
                }
                yield return null;
            }
            Burst(pos, rim, 0.4f, 16);
            if (dome != null) UnityEngine.Object.Destroy(dome);
            if (onHit != null) onHit();
        }

        public static void Aura(Transform follow, Color color, float seconds)
        {
            var go = new GameObject("BattleAura");
            if (follow != null) go.transform.SetParent(follow, false);
            go.transform.localPosition = Vector3.up * 0.05f;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = seconds;
            main.loop = false;
            main.startLifetime = 0.55f;
            main.startSpeed = 0.35f;
            main.startSize = 0.18f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 22f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.45f;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = BattleVfx.GlowMat(color);
            ps.Play();
            UnityEngine.Object.Destroy(go, seconds + 0.8f);
        }

        public static void Popup(Vector3 pos, string text, bool crit)
        {
            var go = new GameObject("BattlePopup");
            go.transform.position = pos + Vector3.up * 0.85f;
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(go.transform, false);
            shadowGo.transform.localPosition = new Vector3(0.04f, -0.04f, 0.02f);
            var shadow = shadowGo.AddComponent<TextMesh>();
            shadow.text = text;
            shadow.fontSize = crit ? 72 : 56;
            shadow.characterSize = 0.09f;
            shadow.anchor = TextAnchor.MiddleCenter;
            shadow.alignment = TextAlignment.Center;
            shadow.color = new Color(0.05f, 0.02f, 0.02f, 0.9f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = crit ? 72 : 56;
            tm.characterSize = 0.09f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = crit ? new Color(1f, 0.82f, 0.15f) : new Color(1f, 0.97f, 0.82f);
            PutOnTop(shadowGo.GetComponent<MeshRenderer>(), 4998);
            PutOnTop(go.GetComponent<MeshRenderer>(), 4999);
            var runner = go.AddComponent<BattleFxPopup>();
            runner.life = 1.15f;
        }

        static void PutOnTop(MeshRenderer mr, int sortingOrder)
        {
            if (mr == null) return;
            mr.sortingOrder = sortingOrder;
            var mat = mr.material;
            if (mat != null) mat.renderQueue = 5000;
        }

        public static IEnumerator HitStop(float realtime)
        {
            float prev = Time.timeScale;
            Time.timeScale = 0.07f;
            yield return new WaitForSecondsRealtime(realtime);
            if (Time.timeScale > 0.08f) yield break;
            Time.timeScale = prev < 0.2f ? 1f : prev;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        public static IEnumerator PunchCam(float push, float seconds)
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            var tr = cam.transform;
            Vector3 home = tr.position;
            tr.position = home + tr.forward * push;
            yield return new WaitForSecondsRealtime(seconds);
            if (tr != null) tr.position = home;
        }

        public static Material ParticleMat(Color color)
        {
            return BattleVfx.GlowMat(color);
        }

        static Material LitMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }
    }

    public class BattleFxPopup : MonoBehaviour
    {
        public float life = 1.15f;
        float t;

        void LateUpdate()
        {
            t += Time.unscaledDeltaTime;
            float pop = t < 0.12f ? Mathf.Lerp(1.45f, 1f, t / 0.12f) : 1f;
            transform.localScale = Vector3.one * pop;
            transform.position += Vector3.up * (0.85f * Time.unscaledDeltaTime);
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            if (t >= life) Destroy(gameObject);
        }
    }
}
