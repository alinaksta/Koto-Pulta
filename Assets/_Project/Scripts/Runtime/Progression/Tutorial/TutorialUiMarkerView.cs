using Game.Services;
using System.Collections;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Positions a canvas-space tutorial marker at the active tutorial UI target.
    /// </summary>
    public sealed class TutorialUiMarkerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _marker;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Vector2 _offset = new Vector2(0f, 48f);
        [SerializeField, Min(0f)] private float _bobAmplitude = 12f;
        [SerializeField, Min(0f)] private float _bobFrequency = 2f;

        private readonly Vector3[] _targetCorners = new Vector3[4];

        private TutorialService _tutorial;
        private RectTransform _target;
        private Vector2 _targetOffset;
        private RectTransform _parent;
        private Canvas _parentCanvas;

        private IEnumerator Start()
        {
            if (_marker == null)
                _marker = transform as RectTransform;

            _parent = _marker != null ? _marker.parent as RectTransform : null;
            _parentCanvas = _marker != null ? _marker.GetComponentInParent<Canvas>() : null;

            while (!ServiceLocator.TryGet(out _tutorial))
                yield return null;

            _tutorial.OnUiTargetChanged += HandleUiTargetChanged;
            HandleUiTargetChanged(_tutorial.UiTarget, _tutorial.UiTargetOffset);
        }

        private void LateUpdate()
        {
            if (_target == null || _marker == null || _parent == null)
                return;

            bool visible = _target.gameObject.activeInHierarchy;
            SetVisible(visible);

            if (!visible)
                return;

            _target.GetWorldCorners(_targetCorners);
            Vector3 worldCenter = (_targetCorners[0] + _targetCorners[2]) * 0.5f;

            Camera camera = GetCanvasCamera(_parentCanvas);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, screenPoint, camera, out Vector2 localPoint))
            {
                float bob = _bobAmplitude > 0f && _bobFrequency > 0f
                    ? Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude
                    : 0f;

                _marker.anchoredPosition = localPoint + _offset + _targetOffset + Vector2.up * bob;
            }
        }

        private void OnDestroy()
        {
            if (_tutorial != null)
                _tutorial.OnUiTargetChanged -= HandleUiTargetChanged;
        }

        private void HandleUiTargetChanged(RectTransform target, Vector2 offset)
        {
            _target = target;
            _targetOffset = offset;
            SetVisible(_target != null);
        }

        private void SetVisible(bool visible)
        {
            if (_visualRoot != null)
                _visualRoot.SetActive(visible);
        }

        private static Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }
    }
}
