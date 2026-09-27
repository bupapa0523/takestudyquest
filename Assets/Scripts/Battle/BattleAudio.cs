using System.Collections.Generic;
using UnityEngine;
using ShiftingMetropolis.App;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// hotondo/音/400 Sounds Pack の効果音をバトル演出に鳴らすだけの薄いヘルパー。
    /// </summary>
    public static class BattleAudio
    {
        const string Root = "Assets/hotondo/音/400 Sounds Pack/";
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static AudioSource shared;

        public static void Punch() => Play(Root + "Combat and Gore/punch.wav", 0.9f);
        public static void Punch2() => Play(Root + "Combat and Gore/punch_2.wav", 0.9f);
        public static void Kick() => Play(Root + "Combat and Gore/kick.wav", 0.9f);
        public static void Crunch() => Play(Root + "Combat and Gore/crunch_splat_2.wav", 0.9f);
        public static void SwordSlice() => Play(Root + "Weapons/sword_slice.wav", 0.85f);
        public static void SwordClash() => Play(Root + "Weapons/sword_clash.wav", 0.85f);
        public static void Miss() => Play(Root + "Other/whoosh_2.wav", 0.7f);
        public static void UiClick() => Play(Root + "UI/click_double_on.wav", 0.6f);
        public static void UiCancel() => Play(Root + "UI/cancel.wav", 0.6f);
        public static void Coin() => Play(Root + "Retro/coin_2.wav", 0.8f);
        public static void ExplosionSmall() => Play(Root + "Retro/explosion_small.wav", 0.9f);
        public static void ExplosionLarge() => Play(Root + "Retro/explosion_large.wav", 1f);
        public static void LevelComplete() => Play(Root + "Musical Effects/8_bit_level_complete.wav", 0.9f);
        public static void Defeated() => Play(Root + "Musical Effects/8_bit_defeated.wav", 0.9f);
        public static void Mystery() => Play(Root + "Musical Effects/8_bit_mystery.wav", 0.85f);
        public static void GhostSting() => Play(Root + "Retro/ghost.wav", 0.9f);
        public static void CreakyDoor() => Play(Root + "Environment/creaky_door_long.wav", 0.8f);

        public static void PlayHitBySkill(SkillCommandType type, bool isUltimate)
        {
            if (isUltimate) { ExplosionSmall(); return; }
            switch (type)
            {
                case SkillCommandType.Skill1: Punch(); break;
                case SkillCommandType.Skill2: Kick(); break;
                case SkillCommandType.Skill3: SwordSlice(); break;
                default: Punch2(); break;
            }
        }

        static void Play(string path, float volume)
        {
            var clip = Load(path);
            if (clip == null) return;
            EnsureSource();
            shared.PlayOneShot(clip, volume);
        }

        static void EnsureSource()
        {
            if (shared != null) return;
            var go = new GameObject("BattleAudioSource");
            UnityEngine.Object.DontDestroyOnLoad(go);
            shared = go.AddComponent<AudioSource>();
            shared.playOnAwake = false;
            shared.spatialBlend = 0f;
        }

        static AudioClip Load(string path)
        {
            if (cache.TryGetValue(path, out var c)) return c;
            AudioClip clip = PlayerAssets.Load<AudioClip>(path);
            cache[path] = clip;
            return clip;
        }
    }
}
