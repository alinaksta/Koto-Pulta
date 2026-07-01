using Game.Items;
using Game.Items.Components;
using Game.Items.Properties;
using Game.Movement;
using Itemworks.Core;
using LitMotion;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Interaction
{
    /// <summary>
    /// Drives the UI presentation for one player hand.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [System.Serializable]
        private struct HandVisibilitySettings
        {
            [Min(0f)] public float HiddenDistance;
            [Min(0f)] public float Smoothness;
        }

        [System.Serializable]
        private struct HandBobSettings
        {
            [Tooltip("Minimum player speed required before bobbing begins.")]
            [Min(0f)] public float SpeedThreshold;

            [Tooltip("Maximum vertical distance of the bob in UI units.")]
            [Min(0f)] public float Amplitude;

            [Tooltip("Number of complete bob cycles per second.")]
            [Min(0f)] public float Frequency;

            [Tooltip("How quickly bobbing fades in and out.")]
            [Min(0f)] public float BlendSpeed;

            [Tooltip("Smooths the measured movement speed.")]
            [Min(0f)] public float SpeedSmoothness;

            [Tooltip("Gradually increase bob amplitude based on player speed.")]
            public bool ScaleWithSpeed;

            [Tooltip("Player speed at which the bob reaches full amplitude.")]
            [Min(0.01f)] public float SpeedForFullAmplitude;
        }

        [System.Serializable]
        private struct HandSwapAnimationSettings
        {
            [Min(0f)] public float Distance;
            [Min(0f)] public float Duration;
        }

        private readonly struct WaiterOverlayVisual
        {
            public WaiterOverlayVisual(Sprite sprite, string tableNumber)
            {
                Sprite = sprite;
                TableNumber = tableNumber;
            }

            public Sprite Sprite { get; }
            public string TableNumber { get; }
        }

        private readonly struct HandItemVisual
        {
            public HandItemVisual(bool hasItem, bool usesAnimator, Sprite handSprite, Sprite heldItemSprite, Vector2 heldItemOffset, WaiterOverlayVisual waiterOverlay)
            {
                HasItem = hasItem;
                UsesAnimator = usesAnimator;
                HandSprite = handSprite;
                HeldItemSprite = heldItemSprite;
                HeldItemOffset = heldItemOffset;
                WaiterOverlay = waiterOverlay;
            }

            public bool HasItem { get; }
            public bool UsesAnimator { get; }
            public Sprite HandSprite { get; }
            public Sprite HeldItemSprite { get; }
            public Vector2 HeldItemOffset { get; }
            public WaiterOverlayVisual WaiterOverlay { get; }
            public bool ShowsHeldItem => HeldItemSprite != null;
        }

        private static readonly int HoldingBoolHash =
            Animator.StringToHash("Holding");

        [Header("Dependencies")]
        [SerializeField] private DualHandInteractor _interactor;
        [SerializeField] private PlayerController _playerController;

        [Header("Hand UI")]
        [SerializeField] private Animator _animator;
        [SerializeField] private RectTransform _handRoot;
        [SerializeField] private Image _baseHandImage;
        [SerializeField] private Image _heldItemImage;
        [SerializeField] private Image _waiterHeldItemImage;
        [SerializeField] private TextMeshProUGUI _waiterTableNumberLabel;

        [Header("Presentation")]
        [SerializeField] private HandType _handType;
        [SerializeField] private HandVisibilitySettings _visibility = new HandVisibilitySettings
        {
            HiddenDistance = 480f,
            Smoothness = 8f
        };
        [SerializeField] private HandBobSettings _headBob = new HandBobSettings
        {
            SpeedThreshold = 0.25f,
            Amplitude = 14f,
            Frequency = 2.5f,
            BlendSpeed = 8f,
            SpeedSmoothness = 12f,
            ScaleWithSpeed = true,
            SpeedForFullAmplitude = 5f
        };
        [SerializeField] private HandSwapAnimationSettings _itemSwap = new HandSwapAnimationSettings
        {
            Distance = 480f,
            Duration = 0.4f
        };

        private float BobPhaseOffset =>
            _handType == HandType.Right ? Mathf.PI : 0f;

        private Hand _hand;

        private RectTransform _heldItemRect;
        private Vector2 _visibleAnchoredPosition;
        private Vector2 _heldItemInitialAnchoredPosition;
        private Vector2 _currentVisibilityPosition;
        private Vector2 _itemAnimationOffset;
        private Sprite _defaultHandSprite;

        private Vector3 _previousPlayerPosition;
        private float _smoothedPlayerSpeed;
        private float _bobWeight;

        private MotionHandle _itemAnimationMotion;
        private int _visualVersion;

        private void Start()
        {
            _hand = _interactor.GetHand(_handType);
            _heldItemRect = _heldItemImage.rectTransform;
            _visibleAnchoredPosition = _handRoot.anchoredPosition;
            _currentVisibilityPosition = _visibleAnchoredPosition;
            _heldItemInitialAnchoredPosition = _heldItemRect.anchoredPosition;
            _defaultHandSprite = _baseHandImage.sprite;
            _previousPlayerPosition = _playerController.transform.position;

            _hand.OnItemChanged += HandleHandItemChanged;
            _previousPlayerPosition = _playerController.transform.position;
            ApplyVisual(ResolveVisual(_hand.Item));
        }

        private void OnDestroy()
        {
            _hand.OnItemChanged -= HandleHandItemChanged;
            CancelItemAnimation();
            _itemAnimationOffset = Vector2.zero;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            UpdatePlayerSpeed(deltaTime);
            UpdateVisibility(deltaTime);
            UpdateHeadBob(deltaTime);

            _handRoot.anchoredPosition =
                _currentVisibilityPosition +
                _itemAnimationOffset +
                GetHeadBobOffset();
        }

        private void UpdatePlayerSpeed(float deltaTime)
        {
            Vector3 playerPosition = _playerController.transform.position;
            float measuredSpeed =
                Vector3.Distance(playerPosition, _previousPlayerPosition) /
                deltaTime;

            _previousPlayerPosition = playerPosition;
            float smoothingFactor =
                1f - Mathf.Exp(-_headBob.SpeedSmoothness * deltaTime);

            _smoothedPlayerSpeed = Mathf.Lerp(
                _smoothedPlayerSpeed,
                measuredSpeed,
                smoothingFactor);
        }

        private void UpdateVisibility(float deltaTime)
        {
            Vector2 hiddenPosition =
                _visibleAnchoredPosition + Vector2.down * _visibility.HiddenDistance;

            Vector2 targetPosition =
                _hand.Visible ? _visibleAnchoredPosition : hiddenPosition;

            float smoothingFactor =
                1f - Mathf.Exp(-_visibility.Smoothness * deltaTime);

            _currentVisibilityPosition = Vector2.Lerp(
                _currentVisibilityPosition,
                targetPosition,
                smoothingFactor);
        }

        private void UpdateHeadBob(float deltaTime)
        {
            float targetWeight = 0f;

            if (_smoothedPlayerSpeed > _headBob.SpeedThreshold && _playerController.IsGrounded)
            {
                targetWeight = _headBob.ScaleWithSpeed
                    ? Mathf.InverseLerp(
                        _headBob.SpeedThreshold,
                        Mathf.Max(
                            _headBob.SpeedForFullAmplitude,
                            _headBob.SpeedThreshold + 0.01f),
                        _smoothedPlayerSpeed)
                    : 1f;
            }

            _bobWeight = Mathf.MoveTowards(
                _bobWeight,
                targetWeight,
                _headBob.BlendSpeed * deltaTime);
        }

        private Vector2 GetHeadBobOffset()
        {
            float phase = Time.time * _headBob.Frequency * Mathf.PI * 2f + BobPhaseOffset;
            float bobY = Mathf.Sin(phase) * _headBob.Amplitude * _bobWeight;
            return Vector2.up * bobY;
        }

        private void HandleHandItemChanged(Item? item)
        {
            HandItemVisual visual = ResolveVisual(item);
            _ = PlayItemSwapAnimationAsync(visual, ++_visualVersion);
        }

        private HandItemVisual ResolveVisual(Item? item)
        {
            if (!item.HasValue)
                return new HandItemVisual(false, true, null, null, Vector2.zero, default);

            Item value = item.Value;
            ItemDefinition definition = value.Definition;
            WaiterOverlayVisual waiterOverlay = ResolveWaiterOverlay(value);

            if (definition.TryGetProperty<HandSpriteProperty>(out var handSpriteProperty))
                return new HandItemVisual(true, false, handSpriteProperty.HandSprite, null, Vector2.zero, waiterOverlay);

            if (TryResolveHeldItemSprite(definition, out Sprite heldItemSprite))
                return new HandItemVisual(true, true, null, heldItemSprite, CalculateHeldItemPivotOffset(heldItemSprite), waiterOverlay);

            Debug.LogWarning($"The item {definition.Id} does not have a hand-compatible sprite property!", this);
            return new HandItemVisual(true, true, null, null, Vector2.zero, waiterOverlay);
        }

        private WaiterOverlayVisual ResolveWaiterOverlay(Item item)
        {
            if (!item.IsInstance || !item.Instance.TryGetComponent<WaiterComponent>(out var waiterComponent))
                return default;

            Sprite waiterItemSprite = null;
            Item? waiterItem = waiterComponent.Waiter.CarryContainer.Item;
            if (waiterItem.HasValue && waiterItem.Value.Definition.TryGetProperty<FoodProperty>(out var waiterFoodProperty))
                waiterItemSprite = waiterFoodProperty.WorldSprite;

            string tableNumber = waiterComponent.Waiter.TableNumber.HasValue
                ? waiterComponent.Waiter.TableNumber.Value.ToString()
                : string.Empty;

            return new WaiterOverlayVisual(waiterItemSprite, tableNumber);
        }

        private bool TryResolveHeldItemSprite(ItemDefinition definition, out Sprite sprite)
        {
            if (definition.TryGetProperty<FoodProperty>(out var foodProperty))
            {
                sprite = foodProperty.WorldSprite;
                return sprite != null;
            }

            sprite = null;
            return false;
        }

        private Vector2 CalculateHeldItemPivotOffset(Sprite sprite)
        {
            if (sprite == null)
                return Vector2.zero;

            Rect spriteRect = sprite.rect;
            if (spriteRect.width <= 0f || spriteRect.height <= 0f)
                return Vector2.zero;

            Vector2 normalizedPivot = new Vector2(
                sprite.pivot.x / spriteRect.width,
                sprite.pivot.y / spriteRect.height);

            Vector2 pivotOffset = Vector2.Scale((Vector2.one * 0.5f) - normalizedPivot, _heldItemRect.rect.size);
            if (_heldItemRect.localScale.x < 0f)
                pivotOffset.x = -pivotOffset.x;

            return pivotOffset;
        }

        private async Task PlayItemSwapAnimationAsync(HandItemVisual visual, int version)
        {
            CancelItemAnimation();
            _itemAnimationOffset = Vector2.zero;
            SetAnimatorMode(visual.UsesAnimator);

            if (!visual.HasItem)
                ApplyVisual(visual);

            Vector2 hiddenOffset = Vector2.down * _itemSwap.Distance;
            float halfDuration = _itemSwap.Duration * 0.5f;

            _itemAnimationMotion = LMotion
                .Create(Vector2.zero, hiddenOffset, halfDuration)
                .WithOnComplete(() => _itemAnimationOffset = hiddenOffset)
                .Bind(value => _itemAnimationOffset = value);

            await _itemAnimationMotion;

            if (version != _visualVersion)
                return;

            if (visual.HasItem)
                ApplyVisual(visual);

            _itemAnimationMotion = LMotion
                .Create(hiddenOffset, Vector2.zero, halfDuration)
                .WithOnComplete(() => _itemAnimationOffset = Vector2.zero)
                .Bind(value => _itemAnimationOffset = value);

            await _itemAnimationMotion;
        }

        private void ApplyVisual(HandItemVisual visual)
        {
            SetAnimatorMode(visual.UsesAnimator);
            _animator.SetBool(HoldingBoolHash, visual.HasItem);

            if (visual.UsesAnimator)
                _baseHandImage.sprite = _defaultHandSprite;
            else
                _baseHandImage.sprite = visual.HandSprite;

            ApplyHeldItemVisual(visual);
            ApplyWaiterOverlay(visual.WaiterOverlay);
        }

        private void ApplyHeldItemVisual(HandItemVisual visual)
        {
            _heldItemRect.anchoredPosition = _heldItemInitialAnchoredPosition + visual.HeldItemOffset;
            _heldItemImage.sprite = visual.HeldItemSprite;
            _heldItemImage.enabled = visual.ShowsHeldItem;
        }

        private void ApplyWaiterOverlay(WaiterOverlayVisual waiterOverlay)
        {
            _waiterHeldItemImage.sprite = waiterOverlay.Sprite;
            _waiterHeldItemImage.enabled = waiterOverlay.Sprite != null;
            _waiterTableNumberLabel.text = waiterOverlay.TableNumber ?? string.Empty;
        }

        private void SetAnimatorMode(bool usesAnimator)
        {
            _animator.enabled = usesAnimator;
        }

        private void CancelItemAnimation()
        {
            if (_itemAnimationMotion.IsActive())
                _itemAnimationMotion.Cancel();
        }
    }
}
