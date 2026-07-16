using UnityEngine;

namespace Game.Items
{
    public class TrajectoryView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private Transform _endMarker;

        [Header("Rendering")]
        [SerializeField] private int _resolution = 20;

        [Header("Animation")]
        [SerializeField] private float _showDuration = 0.2f;
        [SerializeField] private float _hideDuration = 0.2f;

        [Header("Simulation")]
        [SerializeField] private int _simulationSteps = 20;
        [SerializeField] private float _simulationTime = 2f;
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField] private SpriteRenderer _endMarkerRenderer;

        private Vector3[] _points;
        private float _defaultWidthMultiplier;
        private Color _endMarkerColor;
        private bool _targetVisible;
        private bool _markerVisible;
        private float _visibilityProgress;

        private void Awake()
        {
            _points = new Vector3[_resolution];
            _lineRenderer.positionCount = _resolution;

            _defaultWidthMultiplier = _lineRenderer.widthMultiplier;

            if (_endMarker == null)
            {
                _endMarker.TryGetComponent(out _endMarkerRenderer);
                if (_endMarkerRenderer != null)
                    _endMarkerColor = _endMarkerRenderer.color;
            }
            else
            {
                _endMarkerColor = _endMarkerRenderer.color;
            }

            HideImmediate();
        }

        public void Show()
        {
            _targetVisible = true;
            _lineRenderer.enabled = true;
        }

        public void Hide()
        {
            _targetVisible = false;
        }

        public void Tick(float deltaTime)
        {
            float duration = _targetVisible ? _showDuration : _hideDuration;
            float targetProgress = _targetVisible ? 1f : 0f;

            if (duration <= 0f)
            {
                _visibilityProgress = targetProgress;
            }
            else
            {
                _visibilityProgress = Mathf.MoveTowards(
                    _visibilityProgress,
                    targetProgress,
                    deltaTime / duration);
            }

            _lineRenderer.widthMultiplier = _defaultWidthMultiplier * _visibilityProgress;

            bool lineVisible = _targetVisible || _visibilityProgress > 0f;
            _lineRenderer.enabled = lineVisible;

            if (_endMarker != null)
            {
                bool markerActive = (_targetVisible || _visibilityProgress > 0f) && _markerVisible;
                _endMarker.gameObject.SetActive(markerActive);

                if (markerActive && _endMarkerRenderer != null)
                {
                    Color color = _endMarkerColor;
                    color.a *= _visibilityProgress;
                    _endMarkerRenderer.color = color;
                }
            }

            if (!lineVisible && _endMarkerRenderer != null)
                _endMarkerRenderer.color = _endMarkerColor;
        }

        public void Draw(float force, Vector3 start, Vector3 direction)
        {
            Show();

            Vector3 velocity = direction * force;
            Vector3 gravity = Physics.gravity;

            bool hasHit = false;
            RaycastHit hit = default;
            float flightTime = _simulationTime;

            // Collision simulation
            Vector3 previousPoint = start;
            float previousT = 0f;

            float simDelta = _simulationTime / (_simulationSteps - 1);

            for (int i = 1; i < _simulationSteps; i++)
            {
                float currentT = simDelta * i;

                Vector3 currentPoint =
                    start +
                    velocity * currentT +
                    0.5f * gravity * currentT * currentT;

                if (Physics.Linecast(previousPoint, currentPoint, out hit, _collisionMask))
                {
                    hasHit = true;

                    float segmentLength = Vector3.Distance(previousPoint, currentPoint);
                    float hitDistance = Vector3.Distance(previousPoint, hit.point);
                    float alpha = hitDistance / segmentLength;

                    flightTime = Mathf.Lerp(previousT, currentT, alpha);

                    i = _simulationSteps - 1; // Exit the loop early
                }

                previousPoint = currentPoint;
                previousT = currentT;
            }

            // Rendering
            float renderDelta = flightTime / (_resolution - 1);
            for (int i = 0; i < _resolution; i++)
            {
                float t = renderDelta * i;

                _points[i] = start +
                    velocity * t +
                    0.5f * gravity * t * t;
            }

            _lineRenderer.SetPositions(_points);

            // End marker
            if (_endMarker != null)
            {
                _markerVisible = hasHit;

                if (hasHit)
                {
                    _endMarker.position = hit.point;
                    _endMarker.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
            }
        }

        private void HideImmediate()
        {
            _targetVisible = false;
            _markerVisible = false;
            _visibilityProgress = 0f;
            _lineRenderer.widthMultiplier = 0f;
            _lineRenderer.enabled = false;

            if (_endMarker != null)
                _endMarker.gameObject.SetActive(false);

            if (_endMarkerRenderer != null)
                _endMarkerRenderer.color = _endMarkerColor;
        }
    }
}
