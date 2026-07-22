using System.Collections.Generic;
using System.Text;
using Game.Input;
using Game.Services;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Diagnostics
{
    /// <summary>
    /// Logs the UI objects hit by the current EventSystem pointer raycast.
    /// Attach to any active scene object while debugging world-space UI input.
    /// </summary>
    public class EventSystemRaycastLogger : MonoBehaviour
    {
        [SerializeField] private bool _logEveryFrame;
        [SerializeField] private bool _logOnClick = true;
        [SerializeField] private bool _logOnScroll = true;

        private readonly List<RaycastResult> _results = new();
        private IInputService _inputService;
        private PointerEventData _pointerEventData;

        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
        }

        private void Update()
        {
            if (!ShouldLog())
                return;

            LogRaycastResults();
        }

        private bool ShouldLog()
        {
            if (_logEveryFrame)
                return true;

            if (_logOnClick && (_inputService.InteractLeft.Pressed || _inputService.InteractRight.Pressed))
                return true;

            return _logOnScroll && Mathf.Abs(_inputService.MouseScroll.y) > 0f;
        }

        private void LogRaycastResults()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogWarning("EventSystemRaycastLogger: EventSystem.current is null.", this);
                return;
            }

            _pointerEventData ??= new PointerEventData(eventSystem);
            _pointerEventData.Reset();
            _pointerEventData.position = _inputService.MousePosition;
            _pointerEventData.scrollDelta = _inputService.MouseScroll;

            _results.Clear();
            eventSystem.RaycastAll(_pointerEventData, _results);

            var builder = new StringBuilder();
            builder.Append("EventSystemRaycastLogger at ");
            builder.Append(_inputService.MousePosition);
            builder.Append(": ");
            builder.Append(_results.Count);
            builder.AppendLine(" hit(s)");

            for (int i = 0; i < _results.Count; i++)
            {
                RaycastResult result = _results[i];
                builder.Append(i);
                builder.Append(": ");
                builder.Append(result.gameObject != null ? result.gameObject.name : "<null>");
                builder.Append(" | module=");
                builder.Append(result.module != null ? result.module.GetType().Name : "<null>");
                builder.Append(" | canvas=");
                builder.Append(result.gameObject != null ? GetCanvasName(result.gameObject) : "<null>");
                builder.Append(" | distance=");
                builder.Append(result.distance.ToString("0.###"));
                builder.Append(" | depth=");
                builder.Append(result.depth);
                builder.Append(" | sorting=");
                builder.Append(result.sortingLayer);
                builder.Append('/');
                builder.Append(result.sortingOrder);
                builder.Append(" | world=");
                builder.Append(result.worldPosition.ToString("F3"));
                builder.AppendLine();
            }

            Debug.Log(builder.ToString(), this);
        }

        private static string GetCanvasName(GameObject target)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.name : "<none>";
        }
    }
}
