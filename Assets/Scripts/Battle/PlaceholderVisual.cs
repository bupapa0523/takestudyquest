using System.Collections;
using UnityEngine;

namespace ShiftingMetropolis.Battle
{
    /// <summary>
    /// Cube/Capsuleなどのプレースホルダー見た目に付ける、被弾時の簡易フィードバック。
    /// 将来的3Dモデルと差し替え前提の仮実装。
    /// </summary>
    public class PlaceholderVisual : MonoBehaviour
    {
        private Renderer rend;
        private Color originalColor;
        private Coroutine flashRoutine;
        private Vector3 originalPosition;

        void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null)
            {
                originalColor = rend.material.color;
            }
            originalPosition = transform.position;
        }

        public void Flash(Color flashColor)
        {
            if (rend == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(flashColor));
        }

        public void Shake(float duration = 0.2f, float magnitude = 0.15f)
        {
            StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator FlashRoutine(Color flashColor)
        {
            rend.material.color = flashColor;
            yield return new WaitForSeconds(0.15f);
            if (rend != null)
            {
                rend.material.color = originalColor;
            }
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float offsetX = Random.Range(-1f, 1f) * magnitude;
                transform.position = originalPosition + new Vector3(offsetX, 0f, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = originalPosition;
        }
    }
}
