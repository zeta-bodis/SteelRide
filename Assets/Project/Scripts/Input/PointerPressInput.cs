using System;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Input
{
    [DisallowMultipleComponent]
    public sealed class PointerPressInput : MonoBehaviour, IPressInput
    {
        public event Action Pressed;

        private UiPointerBlocker _ui;
        private int _enabledFrame;
        private bool _waitForRelease;

        public void SetUiBlocker(UiPointerBlocker ui) => _ui = ui;

        public void SuppressUntilRelease()
        {
            _enabledFrame = Time.frameCount;
            _waitForRelease = true;
        }

        private void OnEnable()
        {
            SuppressUntilRelease();
        }

        private void Update()
        {
            if (_waitForRelease)
            {
                _waitForRelease = UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.touchCount > 0;
                return;
            }

            if (!Application.isFocused || Time.frameCount <= _enabledFrame)
                return;

            if (UnityEngine.Input.touchCount > 0)
            {
                for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                {
                    Touch touch = UnityEngine.Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !IsBlocked(touch.position))
                    {
                        Pressed?.Invoke();
                        return;
                    }
                }

                return;
            }

            if (UnityEngine.Input.GetMouseButtonDown(0) && !IsBlocked(UnityEngine.Input.mousePosition))
                Pressed?.Invoke();
        }

        private bool IsBlocked(Vector2 position) => _ui != null && _ui.IsBlocked(position);
    }
}