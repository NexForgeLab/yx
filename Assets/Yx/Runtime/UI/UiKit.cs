using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yx.Game
{
    public static class UiKit
    {
        public static Font Font;

        public static void EnsureFont()
        {
            if (Font != null) return;
            Font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static Canvas CreateCanvas(string name)
        {
            EnsureFont();
            EnsureEventSystem();
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, bool raycast)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return rect;
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);
            var label = go.AddComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = color;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button MakeButton(Transform parent, string name, string caption, Vector2 anchor, Vector2 pos, Vector2 size, UnityAction onClick)
        {
            RectTransform rect = Box(parent, name, anchor, new Vector2(0.5f, 0.5f), pos, size, Palette.Teal, true);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.62f, 0.58f, 1f);
            colors.pressedColor = Palette.Drum;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            Text label = Label(rect, caption, size.y > 140f ? 52 : 40, TextAnchor.MiddleCenter, Palette.Ink);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        public static void ApplySafeArea(RectTransform root)
        {
            if (root == null) return;
            Rect safe = Screen.safeArea;
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            root.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
            root.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }
    }

    public class PressCatch : MonoBehaviour, IPointerDownHandler
    {
        public bool Pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            Pressed = true;
        }
    }

    public class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value;
        RectTransform rect;
        RectTransform knob;

        public void Bind(RectTransform background, RectTransform knobRect)
        {
            rect = background;
            knob = knobRect;
        }

        public void OnPointerDown(PointerEventData eventData) { Drag(eventData); }
        public void OnDrag(PointerEventData eventData) { Drag(eventData); }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        void Drag(PointerEventData eventData)
        {
            if (rect == null) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out local);
            float radius = rect.sizeDelta.x * 0.36f;
            if (local.magnitude > radius) local = local.normalized * radius;
            if (knob != null) knob.anchoredPosition = local;
            Value = radius <= 0.01f ? Vector2.zero : local / radius;
        }
    }
}
