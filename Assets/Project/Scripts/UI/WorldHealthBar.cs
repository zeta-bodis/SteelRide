using UnityEngine;

namespace SteelRide.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(500)]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private Vector2 _screenOffset = new Vector2(0f, -85f);

        private RectTransform _rect, _parent;
        private CanvasGroup _group;
        private Camera _camera;
        private Transform _target;
        private bool _visible;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _parent = (RectTransform)transform.parent;
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        public void Bind(Transform target, Camera camera)
        {
            _target = target;
            _camera = camera;
        }

        public void SetVisible(bool visible) => _visible = visible;

        public void Unbind()
        {
            _target = null;
            _camera = null;
        }

        private void LateUpdate()
        {
            if (_target == null || _camera == null || !_visible)
            {
                _group.alpha = 0f;
                return;
            }

            Vector3 screen = _camera.WorldToScreenPoint(_target.position + _worldOffset);

            bool inView = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;

            _group.alpha = inView ? 1f : 0f;

            if (!inView)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, screen, null, out Vector2 local))
                _rect.anchoredPosition = local + _screenOffset;
        }
    }
}