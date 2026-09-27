using System.Collections.Generic;
using UnityEngine;
using ShiftingMetropolis.App;

namespace ShiftingMetropolis.Battle
{
    public enum VfxHitKind
    {
        Slash,
        Kick,
        Magic,
        Smash,
        Crit,
        Ultimate
    }

    public static class BattleVfx
    {
        const string Pack = "Assets/hotondo/エフェクト/brackeys_vfx_bundle/brackeys_vfx_bundle/";
        const string GradientPath = "Assets/hotondo/エフェクト/Color_B_Gradient.jpg";
        static readonly Dictionary<string, Texture> texCache = new Dictionary<string, Texture>();
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
        static Texture gradient;

        public static Vector3 AtBody(Transform t)
        {
            if (t == null) return Vector3.up;
            var rends = t.GetComponentsInChildren<Renderer>();
            bool started = false;
            Bounds b = new Bounds();
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null || !rends[i].enabled) continue;
                if (rends[i] is ParticleSystemRenderer) continue;
                if (!started)
                {
                    b = rends[i].bounds;
                    started = true;
                }
                else b.Encapsulate(rends[i].bounds);
            }
            Vector3 pos = started ? b.center : t.position + Vector3.up * 1.05f;
            var cam = Camera.main;
            if (cam != null)
                pos += (cam.transform.position - pos).normalized * 0.12f;
            return pos;
        }

        public static void PlayHit(Vector3 pos, Color color, VfxHitKind kind)
        {
            PlayHit(pos, color, kind, false);
        }

        public static void PlayHit(Vector3 pos, Color color, VfxHitKind kind, bool crit)
        {
            Color bright = Color.Lerp(color, Color.white, 0.22f);
            bright.a = 1f;
            VfxHitKind look = kind == VfxHitKind.Crit ? VfxHitKind.Slash : kind;
            switch (look)
            {
                case VfxHitKind.Kick:
                    Stamp(pos, "slash_04_a.png", bright, 2.5f, 1.05f, 0.28f, Tilt());
                    Stamp(pos, "slash_02_a.png", bright, 2.1f, 0.9f, 0.24f, Tilt());
                    Stamp(pos, "effect_01_a.png", bright, 1.9f, 1.9f, 0.26f, Tilt());
                    Stamp(pos, "circle_02_a.png", bright, 1.55f, 1.55f, 0.22f, 0f);
                    Spray(pos, "star_04_a.png", bright, 0.12f, 18, 5.8f, 0.32f);
                    Spray(pos, "star_08_a.png", Color.white, 0.1f, 10, 4.6f, 0.28f);
                    break;
                case VfxHitKind.Magic:
                    bool fire = color.r > color.b + 0.18f;
                    Stamp(pos, fire ? "twirl_02_a.png" : "magic_04_a.png", bright, 2.05f, 2.05f, 0.34f, Tilt());
                    Stamp(pos, "twirl_01_a.png", bright, 1.7f, 1.7f, 0.36f, 20f);
                    Stamp(pos, "circle_01_a.png", bright, 1.6f, 1.6f, 0.24f, 0f);
                    Stamp(pos, "star_08_a.png", Color.white, 1.8f, 1.8f, 0.28f, 0f);
                    if (fire)
                    {
                        Stamp(pos, "flame_01_a.png", bright, 1.4f, 1.8f, 0.32f, 0f);
                        Spray(pos, "flame_03_a.png", bright, 0.22f, 14, 3.4f, 0.36f);
                    }
                    Spray(pos, "star_04_a.png", Color.white, 0.13f, 16, 4.2f, 0.34f);
                    Spray(pos, "star_08_a.png", bright, 0.11f, 12, 5.0f, 0.3f);
                    break;
                case VfxHitKind.Smash:
                    Stamp(pos, "slash_02_a.png", bright, 2.6f, 1.1f, 0.28f, Tilt());
                    Stamp(pos, "effect_01_a.png", bright, 2.0f, 2.0f, 0.26f, Tilt());
                    Stamp(pos, "circle_02_a.png", bright, 1.5f, 1.5f, 0.2f, 0f);
                    Spray(pos, "star_04_a.png", Color.white, 0.12f, 20, 6.2f, 0.3f);
                    break;
                case VfxHitKind.Ultimate:
                    Stamp(pos, "effect_01_a.png", bright, 2.6f, 2.6f, 0.4f, Tilt());
                    Stamp(pos, "twirl_02_a.png", bright, 2.3f, 2.3f, 0.42f, 15f);
                    Stamp(pos, "slash_01_a.png", bright, 2.8f, 1.2f, 0.32f, Tilt());
                    Stamp(pos, "slash_03_a.png", bright, 2.5f, 1.1f, 0.3f, Tilt());
                    Stamp(pos, "star_08_a.png", Color.white, 2.2f, 2.2f, 0.34f, 0f);
                    Stamp(pos, "circle_03_a.png", bright, 2.0f, 2.0f, 0.3f, 0f);
                    Spray(pos, "flame_03_a.png", bright, 0.28f, 18, 4.2f, 0.42f);
                    Spray(pos, "star_04_a.png", Color.white, 0.14f, 24, 6.4f, 0.38f);
                    Spray(pos, "star_08_a.png", bright, 0.12f, 16, 5.2f, 0.36f);
                    break;
                default:
                    Stamp(pos, "slash_03_a.png", bright, 2.55f, 1.1f, 0.26f, Tilt());
                    Stamp(pos, "slash_01_a.png", bright, 2.15f, 0.95f, 0.22f, Tilt());
                    Stamp(pos, "effect_01_a.png", bright, 1.75f, 1.75f, 0.24f, Tilt());
                    Stamp(pos, "circle_02_a.png", bright, 1.35f, 1.35f, 0.2f, 0f);
                    Spray(pos, "star_04_a.png", Color.white, 0.11f, 16, 5.6f, 0.28f);
                    Spray(pos, "star_08_a.png", bright, 0.1f, 10, 4.4f, 0.26f);
                    break;
            }

            if (crit || kind == VfxHitKind.Crit)
            {
                Stamp(pos, "star_08_a.png", new Color(1f, 0.92f, 0.45f), 2.15f, 2.15f, 0.3f, 0f);
                Stamp(pos, "slash_01_a.png", Color.white, 2.7f, 1.15f, 0.24f, Tilt());
                Spray(pos, "star_02_a.png", new Color(1f, 0.92f, 0.4f), 0.14f, 18, 5.4f, 0.34f);
            }
        }

        public static void PlaySwing(Vector3 pos, Color color, bool kick)
        {
            Color bright = Color.Lerp(color, Color.white, 0.25f);
            Stamp(pos, kick ? "slash_04_a.png" : "slash_03_a.png", bright, 2.2f, 0.95f, 0.22f, Tilt());
            Stamp(pos, "twirl_01_a.png", bright, 1.2f, 1.2f, 0.2f, Tilt());
            Spray(pos, "star_04_a.png", Color.white, 0.1f, 10, 4.2f, 0.22f);
        }

        public static void PlayCharge(Transform follow, float seconds)
        {
            Vector3 pos = AtBody(follow);
            float life = Mathf.Clamp(seconds, 0.5f, 1.3f);
            Stamp(pos, "circle_03_a.png", new Color(1f, 0.88f, 0.4f), 1.9f, 1.9f, life * 0.5f, 0f);
            Stamp(pos, "star_08_a.png", new Color(1f, 0.95f, 0.55f), 1.5f, 1.5f, 0.35f, 0f);
            Aura(follow, "particles/alpha/star_06_a.png", new Color(1f, 0.95f, 0.55f), life, 0.14f, 16f);
            Aura(follow, "particles/alpha/star_04_a.png", Color.white, life, 0.1f, 12f);
        }

        public static void PlayMagicCast(Vector3 from, Color color)
        {
            bool fire = color.r > color.b + 0.18f;
            Stamp(from, fire ? "flame_01_a.png" : "magic_04_a.png", color, 1.45f, 1.6f, 0.3f, 0f);
            Stamp(from, "twirl_01_a.png", color, 1.25f, 1.25f, 0.32f, 15f);
            Stamp(from, "circle_01_a.png", color, 1.2f, 1.2f, 0.22f, 0f);
            Spray(from, "star_08_a.png", Color.white, 0.12f, 12, 3.2f, 0.28f);
        }

        public static void AttachBoltTrail(Transform follow, Color color)
        {
            if (follow == null) return;
            var go = new GameObject("VfxBoltTrail");
            go.transform.SetParent(follow, false);
            var ps = MakePs(go, "trace_03_a.png", color);
            var main = ps.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = 0.2f;
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = 0.12f;
            main.startSizeY = 0.32f;
            main.maxParticles = 22;
            var emission = ps.emission;
            emission.rateOverTime = 28f;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.04f;
            ps.Play();
        }

        public static GameObject MakeBoltBody(Color color, bool fire)
        {
            var go = new GameObject("VfxBolt");
            var ps = MakePs(go, fire ? "flame_03_a.png" : "star_08_a.png", color);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.18f;
            main.startSpeed = 0.2f;
            main.startSize = fire ? 0.32f : 0.42f;
            main.maxParticles = 18;
            var emission = ps.emission;
            emission.rateOverTime = 26f;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.05f;
            ps.Play();
            return go;
        }

        public static GameObject MakeBigBolt(Color color, float scale)
        {
            var go = new GameObject("VfxBigBolt");
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = Vector3.one * 0.95f;
            var col = core.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = NewMat(shader);
            Color hdr = Color.Lerp(color, new Color(0.92f, 0.55f, 1f), 0.18f) * 1.65f;
            hdr.a = 1f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            var rend = core.GetComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            var shell = new GameObject("Shell");
            shell.transform.SetParent(go.transform, false);
            var ps = MakePs(shell, "magic_04_a.png", color);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.28f;
            main.startSpeed = 0.08f;
            main.startSize = 0.85f;
            main.maxParticles = 28;
            var emission = ps.emission;
            emission.rateOverTime = 32f;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.38f;
            ps.Play();

            var sparks = new GameObject("Sparks");
            sparks.transform.SetParent(go.transform, false);
            var ps2 = MakePs(sparks, "star_08_a.png", Color.white);
            var main2 = ps2.main;
            main2.loop = true;
            main2.startLifetime = 0.22f;
            main2.startSpeed = 0.35f;
            main2.startSize = 0.22f;
            main2.maxParticles = 20;
            var em2 = ps2.emission;
            em2.rateOverTime = 24f;
            var sh2 = ps2.shape;
            sh2.enabled = true;
            sh2.shapeType = ParticleSystemShapeType.Sphere;
            sh2.radius = 0.45f;
            ps2.Play();

            go.transform.localScale = Vector3.one * Mathf.Max(1.2f, scale);
            return go;
        }

        public static GameObject MakeDomainShell(Color color, bool ring)
        {
            var go = GameObject.CreatePrimitive(ring ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
            go.name = ring ? "DomainRing" : "DomainDome";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = NewMat(shader);
            Color hdr = Color.Lerp(color, new Color(1f, 0.7f, 0.25f), 0.2f) * 1.55f;
            hdr.a = ring ? 0.95f : 0.55f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        public static GameObject MakePellet(Color color)
        {
            var go = new GameObject("VfxPellet");
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = Vector3.one * 0.16f;
            var col = core.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = NewMat(shader);
            Color hdr = Color.Lerp(color, Color.white, 0.2f) * 1.7f;
            hdr.a = 1f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            var rend = core.GetComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        public static GameObject MakeTrionCube(Color color, float size = 0.11f)
        {
            var go = new GameObject("VfxTrionCube");
            var core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Core";
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = Vector3.one * Mathf.Max(0.04f, size);
            var col = core.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = NewMat(shader);
            Color hdr = Color.Lerp(color, new Color(0.85f, 0.97f, 1f), 0.42f) * 1.55f;
            hdr.a = 0.92f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            var rend2 = core.GetComponent<MeshRenderer>();
            rend2.sharedMaterial = mat;
            rend2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend2.receiveShadows = false;
            return go;
        }

        public static Material AdditiveMat(Color color, float alpha = 0.92f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = NewMat(shader);
            float boost = (color.r + color.g + color.b) < 0.35f ? 1.08f : 1.7f;
            Color hdr = color * boost;
            hdr.a = alpha;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            return mat;
        }

        public static Material WoodMat(Color tint)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            var mat = NewMat(shader);
            Texture tex = PlayerAssets.Load<Texture>("Assets/hotondo/Trailer_Park/Textures/WoodFine.jpg");
            if (tex == null)
                tex = PlayerAssets.Load<Texture>("Assets/hotondo/Trailer_Park/Textures/Wood.jpg");
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
            Color c = tint;
            c.a = 1f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = 2000;
            return mat;
        }

        public static void PaintWood(GameObject go, Color tint)
        {
            if (go == null) return;
            var wood = WoodMat(tint);
            var rends = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                var mats = rends[i].materials;
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = wood;
                rends[i].materials = mats;
            }
        }

        public static Material OpaqueMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = NewMat(shader);
            Color c = color;
            if (c.r + c.g + c.b < 0.04f) c = new Color(0.02f, 0.01f, 0.03f);
            c.a = 1f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 2f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = 2000;
            return mat;
        }

        public static GameObject MakeOpaqueSphere(Color color, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "VfxOpaqueSphere";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = OpaqueMat(color);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = Vector3.one * size;
            return go;
        }

        public static GameObject MakeOpaqueCube(Color color, float size = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "VfxOpaqueCube";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = OpaqueMat(color);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = Vector3.one * size;
            return go;
        }

        public static GameObject MakeOpaqueBeam(Color color, float radius = 0.2f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "VfxOpaqueBeam";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = OpaqueMat(color);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
            return go;
        }

        public static GameObject MakeFlameTongue(Color color, float height)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "VfxFlame";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = AdditiveMat(color, 0.9f);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = new Vector3(0.22f, Mathf.Max(0.25f, height), 0.14f);
            return go;
        }

        public static Material SolidMat(Color color, float alpha = 0.96f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = NewMat(shader);
            Color c = color;
            if (c.r + c.g + c.b < 0.06f) c = new Color(0.03f, 0.02f, 0.04f);
            c.a = alpha;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            return mat;
        }

        static bool IsDark(Color color)
        {
            return color.r + color.g + color.b < 0.5f;
        }

        public static GameObject MakeAfterglowTrail(Transform follow, Color color, float width, float linger)
        {
            var go = new GameObject("VfxAfterglow");
            if (follow != null)
            {
                go.transform.SetParent(follow, false);
                go.transform.localPosition = Vector3.zero;
            }
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = Mathf.Max(0.18f, linger);
            tr.startWidth = width;
            tr.endWidth = width * 0.08f;
            tr.minVertexDistance = 0.05f;
            tr.numCapVertices = 4;
            tr.numCornerVertices = 3;
            tr.alignment = LineAlignment.View;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = NewMat(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            tr.material = mat;
            Color c = color;
            float peak = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (peak < 0.18f) c = new Color(0.45f, 0.12f, 0.55f);
            tr.startColor = new Color(c.r, c.g, c.b, 0.82f);
            tr.endColor = new Color(c.r, c.g, c.b, 0f);
            return go;
        }

        public static GameObject MakeSolidBeam(Color color, float radius, float alpha = 0.72f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "VfxSolidBeam";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = go.GetComponent<MeshRenderer>();
            Color c = color;
            c.a = alpha;
            rend.sharedMaterial = SolidMat(c, alpha);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
            return go;
        }

        public static GameObject MakePropCube(Color color, float alpha)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "VfxPropCube";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = SolidMat(color, alpha);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        public static GameObject MakePropSphere(Color color, float alpha, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "VfxPropSphere";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = SolidMat(color, alpha);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = Vector3.one * size;
            return go;
        }

        public static void PlaceBladeCube(GameObject cube, Vector3 from, Vector3 to, float width, float thickness)
        {
            if (cube == null) return;
            Vector3 d = to - from;
            float len = Mathf.Max(0.08f, d.magnitude);
            cube.transform.position = (from + to) * 0.5f;
            cube.transform.rotation = len > 0.001f
                ? Quaternion.FromToRotation(Vector3.right, d / len)
                : Quaternion.identity;
            cube.transform.localScale = new Vector3(len, thickness, width);
        }

        public static GameObject MakeEnergyOrb(Color color, float size)
        {
            return MakeEnergyOrb(Color.Lerp(color, Color.white, 0.45f), color, size);
        }

        public static GameObject MakeEnergyOrb(Color coreColor, Color shellColor, float size)
        {
            var go = new GameObject("VfxEnergyOrb");
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = Vector3.one * 0.62f;
            var col = core.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            core.GetComponent<MeshRenderer>().sharedMaterial = IsDark(coreColor)
                ? SolidMat(coreColor, 1f)
                : AdditiveMat(coreColor, 1f);
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Shell";
            shell.transform.SetParent(go.transform, false);
            shell.transform.localScale = Vector3.one * 1.12f;
            var col2 = shell.GetComponent<Collider>();
            if (col2 != null) Object.Destroy(col2);
            var rend = shell.GetComponent<MeshRenderer>();
            rend.sharedMaterial = AdditiveMat(shellColor, 0.72f);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = Vector3.one * Mathf.Max(0.08f, size);
            return go;
        }

        public static GameObject MakeEnergyBeam(Color coreColor, Color sheathColor, float coreRadius, float sheathRadius)
        {
            var go = new GameObject("VfxEnergyBeam");
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "Core";
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = new Vector3(coreRadius * 2f, 1f, coreRadius * 2f);
            var col = core.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var cr = core.GetComponent<MeshRenderer>();
            cr.sharedMaterial = IsDark(coreColor) ? SolidMat(coreColor, 1f) : AdditiveMat(coreColor, 1f);
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cr.receiveShadows = false;
            var sheath = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sheath.name = "Sheath";
            sheath.transform.SetParent(go.transform, false);
            sheath.transform.localScale = new Vector3(sheathRadius * 2f, 1f, sheathRadius * 2f);
            var col2 = sheath.GetComponent<Collider>();
            if (col2 != null) Object.Destroy(col2);
            var sr = sheath.GetComponent<MeshRenderer>();
            sr.sharedMaterial = AdditiveMat(sheathColor, 0.7f);
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            return go;
        }

        public static void PlaceBeam(GameObject beam, Vector3 from, Vector3 to, float thickMul = 1f)
        {
            if (beam == null) return;
            Vector3 d = to - from;
            float len = Mathf.Max(0.05f, d.magnitude);
            beam.transform.position = (from + to) * 0.5f;
            beam.transform.rotation = Quaternion.FromToRotation(Vector3.up, d / len);
            beam.transform.localScale = new Vector3(thickMul, len * 0.5f, thickMul);
        }

        public static GameObject MakeGetsugaBlade(Color color)
        {
            var go = new GameObject("VfxGetsuga");
            var blade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blade.name = "Blade";
            blade.transform.SetParent(go.transform, false);
            blade.transform.localScale = new Vector3(2.35f, 0.12f, 0.72f);
            var col = blade.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = blade.GetComponent<MeshRenderer>();
            rend.sharedMaterial = SolidMat(new Color(0.02f, 0.01f, 0.02f), 1f);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            var edge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            edge.name = "Edge";
            edge.transform.SetParent(go.transform, false);
            edge.transform.localScale = new Vector3(2.85f, 0.045f, 1.02f);
            var col2 = edge.GetComponent<Collider>();
            if (col2 != null) Object.Destroy(col2);
            var er = edge.GetComponent<MeshRenderer>();
            er.sharedMaterial = SolidMat(new Color(0.55f, 0.05f, 0.16f), 0.95f);
            er.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            er.receiveShadows = false;
            return go;
        }

        public static GameObject MakeWaveRing(Color color, bool flat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "VfxWaveRing";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.localScale = flat ? new Vector3(1f, 0.03f, 1f) : new Vector3(1f, 0.08f, 1f);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = IsDark(color) ? SolidMat(color, 0.9f) : AdditiveMat(color, 0.7f);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        public static GameObject MakeDarkSphere(Color color, float alpha = 0.92f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "VfxDarkSphere";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = SolidMat(color, alpha);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        public static GameObject MakeMistSphere(Color color, float alpha)
        {
            var go = MakeDarkSphere(color, alpha);
            go.name = "VfxMistSphere";
            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                var mat = rend.sharedMaterial;
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                mat.renderQueue = 3000;
            }
            return go;
        }

        public static GameObject MakeIceChunk(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "VfxIceChunk";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.localScale = new Vector3(0.3f, 0.4f, 0.28f);
            Color ice = Color.Lerp(color, new Color(0.78f, 0.92f, 1f), 0.55f);
            ice.a = 0.9f;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = NewMat(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ice);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", ice);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", ice * 0.4f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        public static Material LoopMat(string texName, Color color)
        {
            return ParticleMat("particles/alpha/" + texName, color);
        }

        public static Material TexMat(string relative, Color color, bool additive)
        {
            return ParticleMat(relative, color);
        }

        public static void BossAmbience(Transform parent, Color ember, Color haze)
        {
            var fire = LoopCloud(parent, "BossEmbers", ember, "flame_03_a.png", true);
            fire.transform.localPosition = new Vector3(0f, 0.35f, 3.2f);
            var sparks = LoopCloud(parent, "BossSparks", Color.Lerp(ember, Color.white, 0.3f), "star_04_a.png", true);
            sparks.transform.localPosition = new Vector3(0f, 0.45f, 3.0f);
            var smoke = LoopCloud(parent, "BossMist", new Color(haze.r, haze.g, haze.b, 0.22f), "twirl_01_a.png", false);
            smoke.transform.localPosition = new Vector3(0f, 0.7f, 4.6f);
        }

        public static void Burst(Vector3 pos, Color color, float size, int count, float speed)
        {
            Spray(pos, "star_04_a.png", color, Mathf.Clamp(size, 0.08f, 0.16f), Mathf.Max(count, 8), speed, 0.3f);
        }

        public static void Aura(Transform follow, Color color, float seconds, float size, float rate)
        {
            Aura(follow, "particles/alpha/star_04_a.png", color, seconds, size, rate);
        }

        public static void Aura(Transform follow, string tex, Color color, float seconds, float size, float rate)
        {
            var go = new GameObject("VfxAura");
            if (follow != null) go.transform.SetParent(follow, false);
            go.transform.localPosition = Vector3.up * 0.85f;
            string rel = tex.Contains("/") ? tex : "particles/alpha/" + tex;
            var ps = MakePs(go, rel, color);
            var main = ps.main;
            main.loop = false;
            main.duration = seconds;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.4f;
            main.startSize = Mathf.Min(size, 0.18f);
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = 0.45f;
            ps.Play();
            Object.Destroy(go, seconds + 0.7f);
        }

        public static Material GlowMat(Color color)
        {
            return ParticleMat("particles/alpha/star_04_a.png", color);
        }

        static float Tilt()
        {
            return UnityEngine.Random.Range(-42f, 42f);
        }

        static void Stamp(Vector3 pos, string file, Color color, float width, float height, float life, float tilt)
        {
            var go = new GameObject("VfxStamp");
            go.transform.position = pos;
            var ps = MakePs(go, file, color);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.04f;
            main.startLifetime = life;
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = width;
            main.startSizeY = height;
            main.startSizeZ = 1f;
            main.startRotation = tilt * Mathf.Deg2Rad;
            main.maxParticles = 1;
            main.startColor = Color.white;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var sh = ps.shape;
            sh.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.82f, 1f, 1.18f));
            ps.Play();
            Object.Destroy(go, life + 0.35f);
        }

        static void Spray(Vector3 pos, string file, Color color, float size, int count, float speed, float life)
        {
            var go = new GameObject("VfxSpray");
            go.transform.position = pos;
            var ps = MakePs(go, file, color);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.04f;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.gravityModifier = speed > 1f ? 0.1f : 0f;
            main.maxParticles = Mathf.Max(count, 1);
            main.startColor = Color.white;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 48)) });
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.08f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.12f));
            ps.Play();
            Object.Destroy(go, life + 0.4f);
        }

        static ParticleSystem LoopCloud(Transform parent, string name, Color color, string file, bool rise)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = MakePs(go, file, color);
            var main = ps.main;
            main.loop = true;
            main.duration = 4f;
            main.startLifetime = rise ? 1.2f : 2.4f;
            main.startSpeed = rise ? 0.7f : 0.08f;
            main.startSize = rise ? 0.16f : 0.28f;
            main.maxParticles = rise ? 16 : 6;
            main.gravityModifier = rise ? -0.25f : 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.rateOverTime = rise ? 6f : 1.1f;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = rise ? 2.4f : 2.8f;
            ps.Play();
            return ps;
        }

        static ParticleSystem MakePs(GameObject go, string tex, Color color)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Color.white;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.alignment = ParticleSystemRenderSpace.View;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.sharedMaterial = ParticleMat(tex, color);
            return ps;
        }

        static Material NewMat(Shader shader)
        {
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null) return null;
            try { return new Material(shader); }
            catch (System.Exception) { return null; }
        }

        static Material ParticleMat(string relative, Color color)
        {
            if (!relative.Contains("/"))
                relative = "particles/alpha/" + relative;
            string key = "p:" + relative + color.r.ToString("F2") + color.g.ToString("F2") + color.b.ToString("F2");
            Material mat;
            if (matCache.TryGetValue(key, out mat) && mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = NewMat(shader);
            if (mat == null) return null;
            var tex = LoadTex(relative);
            if (tex == null) tex = GradientTex();
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
            Color hdr = Color.Lerp(color, Color.white, 0.15f) * 1.25f;
            hdr.a = 1f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", hdr);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_ColorMode")) mat.SetFloat("_ColorMode", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHAMODULATE_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            matCache[key] = mat;
            return mat;
        }

        static Texture LoadTex(string relative)
        {
            Texture tex;
            if (texCache.TryGetValue(relative, out tex) && tex != null) return tex;
            tex = PlayerAssets.Load<Texture>(Pack + relative);
            texCache[relative] = tex;
            return tex;
        }

        static Texture GradientTex()
        {
            if (gradient != null) return gradient;
            gradient = PlayerAssets.Load<Texture>(GradientPath);
            return gradient;
        }
    }
}
