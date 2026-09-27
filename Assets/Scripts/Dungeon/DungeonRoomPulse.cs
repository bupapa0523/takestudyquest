using UnityEngine;

namespace ShiftingMetropolis.Dungeon
{
    public class DungeonRoomPulse : MonoBehaviour
    {
        public float baseIntensity = 4f;
        public float amp = 1.6f;
        public float speed = 2.2f;

        Light target;

        void Awake()
        {
            target = GetComponent<Light>();
        }

        void LateUpdate()
        {
            if (target == null) return;
            float t = Time.unscaledTime * speed;
            target.intensity = baseIntensity
                + Mathf.Sin(t) * amp * 0.5f
                + (Mathf.PerlinNoise(t * 0.37f, 0.21f) - 0.5f) * amp;
        }
    }
}
