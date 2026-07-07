using Game.Items;
using Game.Items.Components;
using Game.Items.Properties;
using LitMotion;
using LitMotion.Extensions;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Displays and exposes the item currently carried by a waiter.
    /// </summary>
    public class WaiterContainer : MonoBehaviour, IContainerHolder
    {
        [Header("Animation")]
        [SerializeField] private float _pickupDuration = 0.32f;
        [SerializeField] private Vector2 _pickupFirstSquishScale = new Vector2(1.28f, 0.72f);
        [SerializeField] private Vector2 _pickupFirstStretchScale = new Vector2(0.8f, 1.22f);
        [SerializeField] private Vector2 _pickupSecondSquishScale = new Vector2(1.16f, 0.86f);
        [SerializeField] private Vector2 _pickupSecondStretchScale = new Vector2(0.92f, 1.08f);
        [SerializeField] private float _deliveryDuration = 0.35f;
        [SerializeField] private float _deliveryRiseDistance = 0.35f;
        [SerializeField] private float _deliverySpinDegrees = 360f;

        [SerializeField] private SpriteRenderer _spriteRenderer;

        private readonly CarryContainer _container = new();
        private MotionHandle _scaleMotion;
        private MotionHandle _moveMotion;
        private MotionHandle _rotateMotion;
        private MotionHandle _alphaMotion;
        private Vector3 _defaultLocalScale;
        private Vector3 _defaultLocalPosition;
        private Quaternion _defaultLocalRotation;
        private Color _defaultColor;
        private int _animationVersion;

        /// <inheritdoc/>
        public IContainer Container => _container;

        private void Awake()
        {
            _defaultLocalScale = _spriteRenderer.transform.localScale;
            _defaultLocalPosition = _spriteRenderer.transform.localPosition;
            _defaultLocalRotation = _spriteRenderer.transform.localRotation;
            _defaultColor = _spriteRenderer.color;

            ResetVisualState();
            ApplySpriteForItem(_container.Item);
        }

        private void OnEnable()
        {
            _container.OnItemChanged += OnContainerItemChanged;
            CancelActiveAnimations();
            ResetVisualState();
            ApplySpriteForItem(_container.Item);
        }

        private void OnDisable()
        {
            _container.OnItemChanged -= OnContainerItemChanged;
            CancelActiveAnimations();
            ResetVisualState();
            ApplySpriteForItem(_container.Item);
        }

        private void OnContainerItemChanged(Item? item)
        {
            Sprite nextSprite = TryGetSprite(item, out var sprite) ? sprite : null;
            Sprite currentSprite = _spriteRenderer.sprite;

            CancelActiveAnimations();
            ResetVisualState();

            if (nextSprite != null)
            {
                ShowSprite(nextSprite);
                PlayPickupAnimation();
            }
            else if (currentSprite != null)
            {
                ShowSprite(currentSprite);
                PlayDeliveryAnimation();
            }
            else
            {
                HideSprite();
            }
        }

        private bool TryGetSprite(Item? item, out Sprite sprite)
        {
            if (item.HasValue && item.Value.Definition.TryGetProperty<FoodProperty>(out var spriteProperty))
            {
                sprite = spriteProperty.WorldSprite;
                return sprite != null;
            }

            sprite = null;
            return false;
        }

        private void ApplySpriteForItem(Item? item)
        {
            if (TryGetSprite(item, out var sprite))
                ShowSprite(sprite);
            else
                HideSprite();
        }

        private void ShowSprite(Sprite sprite)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.enabled = sprite != null;
            SetAlpha(1f);
        }

        private void HideSprite()
        {
            _spriteRenderer.sprite = null;
            _spriteRenderer.enabled = false;
            SetAlpha(1f);
        }

        private void PlayPickupAnimation()
        {
            _animationVersion++;
            _ = PlayPickupAnimationAsync(_animationVersion);
        }

        private async Task PlayPickupAnimationAsync(int version)
        {
            Vector3 firstSquishScale = new Vector3(_pickupFirstSquishScale.x, _pickupFirstSquishScale.y, _defaultLocalScale.z);
            Vector3 firstStretchScale = new Vector3(_pickupFirstStretchScale.x, _pickupFirstStretchScale.y, _defaultLocalScale.z);
            Vector3 secondSquishScale = new Vector3(_pickupSecondSquishScale.x, _pickupSecondSquishScale.y, _defaultLocalScale.z);
            Vector3 secondStretchScale = new Vector3(_pickupSecondStretchScale.x, _pickupSecondStretchScale.y, _defaultLocalScale.z);

            if (_pickupDuration <= 0f)
            {
                _spriteRenderer.transform.localScale = _defaultLocalScale;
                return;
            }

            _spriteRenderer.transform.localScale = firstSquishScale;

            float segmentDuration = _pickupDuration * 0.2f;

            _scaleMotion = LMotion
                .Create(firstSquishScale, firstStretchScale, segmentDuration)
                .BindToLocalScale(_spriteRenderer.transform);

            await _scaleMotion;

            if (version != _animationVersion || !_spriteRenderer.enabled)
                return;

            _scaleMotion = LMotion
                .Create(firstStretchScale, secondSquishScale, segmentDuration)
                .BindToLocalScale(_spriteRenderer.transform);

            await _scaleMotion;

            if (version != _animationVersion || !_spriteRenderer.enabled)
                return;

            _scaleMotion = LMotion
                .Create(secondSquishScale, secondStretchScale, segmentDuration)
                .BindToLocalScale(_spriteRenderer.transform);

            await _scaleMotion;

            if (version != _animationVersion || !_spriteRenderer.enabled)
                return;

            _scaleMotion = LMotion
                .Create(secondStretchScale, _defaultLocalScale, segmentDuration * 2f)
                .BindToLocalScale(_spriteRenderer.transform);

            await _scaleMotion;

            if (version != _animationVersion)
                return;

            _spriteRenderer.transform.localScale = _defaultLocalScale;
        }

        private void PlayDeliveryAnimation()
        {
            _animationVersion++;
            _ = PlayDeliveryAnimationAsync(_animationVersion);
        }

        private async Task PlayDeliveryAnimationAsync(int version)
        {
            Vector3 startPosition = _defaultLocalPosition;
            Vector3 targetPosition = startPosition + Vector3.up * _deliveryRiseDistance;

            if (_deliveryDuration <= 0f)
            {
                ResetVisualState();
                HideSprite();
                return;
            }

            _moveMotion = LMotion
                .Create(startPosition, targetPosition, _deliveryDuration)
                .Bind(value => _spriteRenderer.transform.localPosition = value);

            _rotateMotion = LMotion
                .Create(0f, _deliverySpinDegrees, _deliveryDuration)
                .Bind(value => _spriteRenderer.transform.localRotation = _defaultLocalRotation * Quaternion.Euler(0f, 0f, value));

            _alphaMotion = LMotion
                .Create(1f, 0f, _deliveryDuration)
                .BindToColorA(_spriteRenderer);

            await _alphaMotion;

            if (version != _animationVersion)
                return;

            ResetVisualState();
            HideSprite();
        }

        private void CancelActiveAnimations()
        {
            _animationVersion++;

            CancelMotion(ref _scaleMotion);
            CancelMotion(ref _moveMotion);
            CancelMotion(ref _rotateMotion);
            CancelMotion(ref _alphaMotion);
        }

        private void CancelMotion(ref MotionHandle motion)
        {
            if (motion.IsActive())
                motion.Cancel();
        }

        private void ResetVisualState()
        {
            _spriteRenderer.transform.localScale = _defaultLocalScale;
            _spriteRenderer.transform.localPosition = _defaultLocalPosition;
            _spriteRenderer.transform.localRotation = _defaultLocalRotation;
            _spriteRenderer.color = _defaultColor;
        }

        private void SetAlpha(float alpha)
        {
            var color = _spriteRenderer.color;
            color.a = alpha;
            _spriteRenderer.color = color;
        }

        private sealed class CarryContainer : IContainer
        {
            private Item? _item;

            public Item? Item => _item;
            public bool IsEmpty => !_item.HasValue;

            public event System.Action<Item?> OnItemChanged = delegate { };

            public bool CanInsert(in TransferRequest request)
            {
                if (!IsEmpty)
                    return false;

                return !request.Item.TryGetComponent<WaiterComponent>(out _);
            }

            public bool CanRemove(in TransferRequest request) => !IsEmpty;

            public void Insert(Item item)
            {
                _item = item;
                OnItemChanged.Invoke(_item);
            }

            public Item? Remove()
            {
                var removed = _item;
                _item = null;
                OnItemChanged.Invoke(_item);
                return removed;
            }
        }
    }
}
