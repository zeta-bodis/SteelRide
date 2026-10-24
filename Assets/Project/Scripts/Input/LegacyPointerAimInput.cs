using SteelRide.Aiming;
using UnityEngine;

namespace SteelRide.Input
{
    [DisallowMultipleComponent]
    public sealed class LegacyPointerAimInput : MonoBehaviour, IPointerPositionSource
    {
        [SerializeField] private bool _requireMouseButton;

        private int _activeFingerId = -1;
        private UiPointerBlocker _ui;

        public void SetUiBlocker(UiPointerBlocker ui) => _ui = ui;
        public void ResetPointer() => _activeFingerId = -1;

        private bool IsBlocked(Vector2 position) => _ui != null && _ui.IsBlocked(position);

        private void OnEnable()
        {
            _activeFingerId = -1;
        }

        private void OnDisable() => _activeFingerId = -1;

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                _activeFingerId = -1;
        }

        public bool TryGetPosition(out Vector2 screenPosition)
        {
            screenPosition = default;

            if (!isActiveAndEnabled || !Application.isFocused)
                return false;

            if (UnityEngine.Input.touchCount > 0 || _activeFingerId >= 0)
                return TryGetTouchPosition(out screenPosition);

            if (Application.isMobilePlatform || !UnityEngine.Input.mousePresent)
                return false;

            if (_requireMouseButton && !UnityEngine.Input.GetMouseButton(0))
                return false;

            screenPosition = UnityEngine.Input.mousePosition;

            return !IsBlocked(screenPosition);
        }

        private bool TryGetTouchPosition(out Vector2 screenPosition)
        {
            screenPosition = default;

            if (_activeFingerId >= 0)
            {
                for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                {
                    Touch touch = UnityEngine.Input.GetTouch(i);

                    if (touch.fingerId != _activeFingerId)
                        continue;

                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        _activeFingerId = -1;
                        return false;
                    }

                    screenPosition = touch.position;
                    return !IsBlocked(screenPosition);
                }

                _activeFingerId = -1;
                return false;
            }

            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);

                if (touch.phase != TouchPhase.Began || IsBlocked(touch.position))
                    continue;

                _activeFingerId = touch.fingerId;
                screenPosition = touch.position;

                return true;
            }

            return false;
        }
    }
}