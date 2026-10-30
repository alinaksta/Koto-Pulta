using System.Collections.Generic;
using Game.Characters;
using Game.Interaction;
using Game.Items;
using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// While the player holds a cat's order, outlines the cat that would take it: the one the crosshair is on
    /// (handing over by click), or the one the throw from the current aim would reach.
    /// </summary>
    public class ProtoThrowTargetHighlight : MonoBehaviour
    {
        [SerializeField] private Color _outlineColor = Color.white;
        [SerializeField, Min(1f)] private float _outlineWidth = 2f;
        [Tooltip("How far ahead the throw is simulated, in seconds.")]
        [SerializeField, Min(0.1f)] private float _simulationTime = 2.5f;
        [SerializeField, Min(2)] private int _simulationSteps = 50;

        private const float RefreshInterval = 0.5f;

        private DualHandInteractor _interactor;
        private readonly List<QueuedCustomer> _customers = new();
        private readonly List<QueuedCustomer> _candidates = new();
        private float _refreshTimer;

        private QueuedCustomer _target;
        private readonly List<Renderer> _targetRenderers = new();

        private void Update()
        {
            QueuedCustomer target = FindTarget();
            if (target != _target)
            {
                SetOutlined(false);
                _target = target;
                _targetRenderers.Clear();
                if (_target != null)
                {
                    foreach (SpriteRenderer sprite in _target.GetComponentsInChildren<SpriteRenderer>())
                        _targetRenderers.Add(sprite);
                }
            }

            // Re-applied every frame: hover outlines elsewhere may switch the shared settings.
            SetOutlined(_target != null);
        }

        private void OnDisable()
        {
            SetOutlined(false);
            _target = null;
        }

        private QueuedCustomer FindTarget()
        {
            if (_interactor == null)
            {
                _interactor = FindAnyObjectByType<DualHandInteractor>();
                if (_interactor == null)
                    return null;
            }

            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = RefreshInterval;
                _customers.Clear();
                _customers.AddRange(FindObjectsByType<QueuedCustomer>(FindObjectsSortMode.None));
            }

            Hand[] hands = { _interactor.GetHand(HandType.Right), _interactor.GetHand(HandType.Left) };
            foreach (Hand hand in hands)
            {
                if (hand == null || hand.IsEmpty || !hand.Item.HasValue)
                    continue;

                Item item = hand.Item.Value;
                _candidates.Clear();
                foreach (QueuedCustomer customer in _customers)
                {
                    if (customer != null && customer.WouldAccept(item))
                        _candidates.Add(customer);
                }

                if (_candidates.Count == 0)
                    continue;

                QueuedCustomer target = FindUnderCrosshair();
                if (target == null && hand.ThrowForce > 0f)
                    target = FindOnTrajectory(hand.ThrowOrigin, _interactor.LookForward * hand.ThrowForce);
                if (target != null)
                    return target;
            }

            return null;
        }

        /// <summary>Handing over by click: the crosshair is on a waiting cat within reach.</summary>
        private QueuedCustomer FindUnderCrosshair()
        {
            if (!_interactor.TryRaycastInteraction(out RaycastHit hit))
                return null;

            QueuedCustomer customer = hit.collider.GetComponentInParent<QueuedCustomer>();
            return customer != null && _candidates.Contains(customer) ? customer : null;
        }

        /// <summary>
        /// Follows the throw's arc (same physics as the trajectory preview) until it hits something,
        /// and returns the first waiting cat whose catch area it passes through or lands in.
        /// </summary>
        private QueuedCustomer FindOnTrajectory(Vector3 start, Vector3 velocity)
        {
            Vector3 gravity = Physics.gravity;
            float step = _simulationTime / (_simulationSteps - 1);
            Vector3 previous = start;
            Transform player = _interactor.transform.root;

            for (int i = 1; i < _simulationSteps; i++)
            {
                float t = step * i;
                Vector3 current = start + velocity * t + 0.5f * gravity * t * t;

                bool blocked = false;
                if (Physics.Linecast(previous, current, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.transform.IsChildOf(player))
                {
                    QueuedCustomer hitCustomer = hit.collider.GetComponentInParent<QueuedCustomer>();
                    if (hitCustomer != null && _candidates.Contains(hitCustomer))
                        return hitCustomer;

                    current = hit.point;
                    blocked = true;
                }

                foreach (QueuedCustomer customer in _candidates)
                {
                    if (DistanceToSegment(customer.CatchCenter, previous, current) <= QueuedCustomer.CatchAreaRadius)
                        return customer;
                }

                if (blocked)
                    return null;
                previous = current;
            }

            return null;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector3.Distance(point, a + ab * t);
        }

        private void SetOutlined(bool outlined)
        {
            if (outlined)
                ScreenSpaceOutlineRendererFeature.SetVisuals(_outlineColor, _outlineWidth);

            foreach (Renderer renderer in _targetRenderers)
            {
                if (renderer == null)
                    continue;
                if (outlined)
                    renderer.renderingLayerMask |= ScreenSpaceOutlineRendererFeature.RenderingLayerMask;
                else
                    renderer.renderingLayerMask &= ~ScreenSpaceOutlineRendererFeature.RenderingLayerMask;
            }
        }
    }
}
