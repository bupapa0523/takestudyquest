using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ShiftingMetropolis.App
{
    /// <summary>
    /// Right-arm IK for a held weapon. Move <see cref="handTarget"/> in the
    /// Scene view during Play; the hand (and parented sword) follow in LateUpdate.
    /// </summary>
    public class HeroWeaponRig : MonoBehaviour
    {
        public Transform handTarget;
        public Transform elbowHint;
        public Transform upperArm;
        public Transform foreArm;
        public Transform hand;

        [Range(0f, 1f)] public float weight = 1f;
        [Range(0f, 1f)] public float rotationWeight = 1f;

        static readonly string[] UpperArmNames =
        {
            "PT_RightArm", "upperarm_r", "mixamorig:RightArm", "RightArm"
        };
        static readonly string[] ForearmNames =
        {
            "PT_RightForeArm", "lowerarm_r", "forearm_r", "mixamorig:RightForeArm", "RightForeArm"
        };
        static readonly string[] HandNames =
        {
            "PT_RightHand", "hand_r", "Hand_R", "mixamorig:RightHand", "RightHand"
        };

        public static void Setup(Transform hero)
        {
            Teardown(hero);
            if (hero == null || !HeroAppearance.HasEquippedWeapon(hero)) return;

            var anim = hero.GetComponentInChildren<Animator>();
            if (anim == null) return;

            Transform root = FindBone(hero, UpperArmNames);
            Transform mid = FindBone(hero, ForearmNames);
            Transform tip = FindBone(hero, HandNames);
            if (root == null || mid == null || tip == null) return;
            if (!tip.IsChildOf(mid) || !mid.IsChildOf(root)) return;

            var host = anim.gameObject;

            var rigGo = new GameObject("WeaponRig");
            rigGo.transform.SetParent(host.transform, false);

            var target = new GameObject("RightHandTarget").transform;
            target.SetParent(rigGo.transform, false);
            target.SetPositionAndRotation(tip.position, tip.rotation);

            Vector3 midDir = (tip.position - root.position).normalized;
            Vector3 hintDir = Vector3.Cross(midDir, Vector3.up);
            if (hintDir.sqrMagnitude < 0.01f) hintDir = -hero.forward;
            var hint = new GameObject("RightElbowHint").transform;
            hint.SetParent(rigGo.transform, false);
            hint.position = mid.position + hintDir.normalized * 0.18f;

            var helper = host.GetComponent<HeroWeaponRig>();
            if (helper == null) helper = host.AddComponent<HeroWeaponRig>();
            helper.handTarget = target;
            helper.elbowHint = hint;
            helper.upperArm = root;
            helper.foreArm = mid;
            helper.hand = tip;
            helper.weight = 1f;
            helper.rotationWeight = 1f;
        }

        public static void Teardown(Transform hero)
        {
            if (hero == null) return;
            var anim = hero.GetComponentInChildren<Animator>();
            if (anim == null) return;
            var host = anim.transform;
            var existing = host.Find("WeaponRig");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var helper = host.GetComponent<HeroWeaponRig>();
            if (helper != null) Object.DestroyImmediate(helper);
            var builder = host.GetComponent<RigBuilder>();
            if (builder != null)
            {
                builder.Clear();
                Object.DestroyImmediate(builder);
            }
        }

        void LateUpdate()
        {
            if (weight <= 0.001f) return;
            if (upperArm == null || foreArm == null || hand == null || handTarget == null) return;
            SolveTwoBoneIK(
                upperArm,
                foreArm,
                hand,
                handTarget.position,
                elbowHint != null ? elbowHint.position : foreArm.position - transform.forward * 0.2f,
                handTarget.rotation,
                weight,
                rotationWeight);
        }

        static void SolveTwoBoneIK(
            Transform boneA,
            Transform boneB,
            Transform boneC,
            Vector3 target,
            Vector3 hint,
            Quaternion targetRotation,
            float posWeight,
            float rotWeight)
        {
            Vector3 aPos = boneA.position;
            Vector3 bPos = boneB.position;
            Vector3 cPos = boneC.position;

            float lenAB = Vector3.Distance(aPos, bPos);
            float lenBC = Vector3.Distance(bPos, cPos);
            if (lenAB < 1e-4f || lenBC < 1e-4f) return;

            Vector3 goal = Vector3.Lerp(cPos, target, posWeight);
            float reach = Vector3.Distance(aPos, goal);
            float maxReach = lenAB + lenBC - 0.001f;
            if (reach < 0.001f) return;
            if (reach > maxReach) goal = aPos + (goal - aPos).normalized * maxReach;

            Vector3 ac = goal - aPos;
            Vector3 hintDir = hint - aPos;
            Vector3 bendNormal = Vector3.Cross(ac, hintDir);
            if (bendNormal.sqrMagnitude < 1e-6f) bendNormal = Vector3.Cross(ac, boneA.up);
            if (bendNormal.sqrMagnitude < 1e-6f) bendNormal = Vector3.up;
            bendNormal.Normalize();

            float dist = ac.magnitude;
            float cosA = Mathf.Clamp((lenAB * lenAB + dist * dist - lenBC * lenBC) / (2f * lenAB * dist), -1f, 1f);
            float angleA = Mathf.Acos(cosA) * Mathf.Rad2Deg;

            Quaternion rotA = Quaternion.LookRotation(ac, bendNormal) * Quaternion.Euler(0f, 0f, angleA);
            Vector3 oldFwdA = bPos - aPos;
            Quaternion fromA = Quaternion.FromToRotation(oldFwdA, rotA * Vector3.forward);
            boneA.rotation = fromA * boneA.rotation;

            Vector3 newB = boneB.position;
            Vector3 bc = goal - newB;
            if (bc.sqrMagnitude < 1e-8f) return;
            Vector3 oldFwdB = boneC.position - newB;
            boneB.rotation = Quaternion.FromToRotation(oldFwdB, bc) * boneB.rotation;

            if (rotWeight > 0.001f)
                boneC.rotation = Quaternion.Slerp(boneC.rotation, targetRotation, rotWeight);
        }

        static Transform FindBone(Transform root, string[] names)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].name == names[n]) return all[i];
                }
            }
            return null;
        }
    }
}
