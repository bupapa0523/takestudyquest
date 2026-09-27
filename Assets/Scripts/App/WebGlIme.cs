using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftingMetropolis.App
{
    public class WebGlIme : MonoBehaviour
    {
        static WebGlIme inst;
        static InputField target;
        System.Action<string> onPhoto;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void WebGlIme_Open(string text, float x, float y, float w, float h);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void WebGlIme_Close();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void WebGlPhoto_Open();
#endif

        public static void PickPhoto(System.Action<string> onPicked)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Ensure();
            inst.onPhoto = onPicked;
            WebGlPhoto_Open();
#else
            if (onPicked != null) onPicked(null);
#endif
        }

        public static void Bind(InputField field)
        {
            if (field == null) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            Ensure();
            if (field.GetComponent<ImeFocus>() == null)
                field.gameObject.AddComponent<ImeFocus>();
#endif
        }

        sealed class ImeFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
        {
            public void OnSelect(BaseEventData eventData)
            {
                Open(GetComponent<InputField>());
            }

            public void OnDeselect(BaseEventData eventData)
            {
            }
        }

        static void Ensure()
        {
            if (inst != null) return;
            var go = new GameObject("WebGlIme");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<WebGlIme>();
        }

        public static void Open(InputField field)
        {
            target = field;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = false;
            FieldRect(field, out float x, out float y, out float w, out float h);
            WebGlIme_Open(field != null ? field.text : "", x, y, w, h);
#endif
        }

        static void FieldRect(InputField field, out float x, out float y, out float w, out float h)
        {
            x = 0f;
            y = 0f;
            w = Screen.width;
            h = 48f;
            if (field == null) return;
            var rt = field.GetComponent<RectTransform>();
            if (rt == null) return;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var canvas = field.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            float left = Mathf.Min(a.x, b.x);
            float right = Mathf.Max(a.x, b.x);
            float bottom = Mathf.Min(a.y, b.y);
            float top = Mathf.Max(a.y, b.y);
            x = left;
            y = Screen.height - top;
            w = Mathf.Max(40f, right - left);
            h = Mathf.Max(28f, top - bottom);
        }

        static void Close()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGlIme_Close();
            WebGLInput.captureAllKeyboardInput = true;
#endif
        }

        public void OnImeText(string value)
        {
            if (target == null) return;
            target.text = value ?? "";
            var text = target.textComponent;
            if (text != null && text.font != null)
                text.font.RequestCharactersInTexture(target.text, text.fontSize, text.fontStyle);
            target.ForceLabelUpdate();
        }

        public void OnImeBlur(string value)
        {
            OnImeText(value);
            if (target != null) target.DeactivateInputField();
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = true;
#endif
        }

        public void OnImeSubmit(string value)
        {
            OnImeText(value);
            if (target != null) target.DeactivateInputField();
            Close();
        }

        public void OnPhotoPicked(string base64)
        {
            var cb = onPhoto;
            onPhoto = null;
            if (cb != null) cb(base64);
        }

        public void OnImeCancel(string unused)
        {
            if (target != null) target.DeactivateInputField();
            Close();
        }
    }
}
