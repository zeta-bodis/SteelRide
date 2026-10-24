using UnityEngine;

namespace SteelRide.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private int _lastWidth, _lastHeight;

        private void Awake() => _rect = (RectTransform)transform;

        private void Update()
        {
            Rect safe = Screen.safeArea;

            if (Screen.width <= 0 || Screen.height <= 0 || (_lastSafeArea == safe && _lastWidth == Screen.width && _lastHeight == Screen.height))
                return;

            _lastSafeArea = safe;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _rect.offsetMin = _rect.offsetMax = Vector2.zero;
        }
    }
}