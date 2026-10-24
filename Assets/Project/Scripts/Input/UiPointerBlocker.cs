using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SteelRide.Input
{
    public sealed class UiPointerBlocker : MonoBehaviour
    {
        private readonly List<RaycastResult> _hits = new List<RaycastResult>(16);
        private EventSystem _eventSystem;
        private PointerEventData _pointer;

        public bool IsBlocked(Vector2 position)
        {
            EventSystem current = EventSystem.current;

            if (!isActiveAndEnabled || current == null)
                return false;

            if (_eventSystem != current || _pointer == null)
            {
                _eventSystem = current;
                _pointer = new PointerEventData(current);
            }

            _pointer.Reset();
            _pointer.position = position;
            _hits.Clear();

            current.RaycastAll(_pointer, _hits);

            for (int i = 0; i < _hits.Count; i++)
                if (_hits[i].module is GraphicRaycaster)
                    return true;

            return false;
        }
    }
}