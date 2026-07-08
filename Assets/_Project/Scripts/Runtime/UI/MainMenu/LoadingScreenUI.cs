using System.Collections;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Animates the main menu loading screen.
    /// </summary>
    public class LoadingScreenUI : MonoBehaviour
    {
        private enum LoadingScreenState
        {
            Hidden,
            Showing,
            Shown,
            Hiding
        }

        [Header("Dependencies")]
        [SerializeField] private RectTransform _leftPart;
        [SerializeField] private RectTransform _rightPart;
        [SerializeField] private RectTransform _loadingIcon;

        [Header("Animation")]
        [SerializeField] private float _curtainDuration = 0.35f;
        [SerializeField] private float _iconDuration = 0.2f;
        [SerializeField] private float _curtainTravelDistance = 600f;
        [SerializeField] private float _iconSpinSpeed = 180f;

        private bool _isLoading;
        private Coroutine _animationCoroutine;
        private Awaitable _currentTransitionAwaitable;
        private AwaitableCompletionSource _transitionCompletionSource;
        private Vector2 _leftShownPosition;
        private Vector2 _rightShownPosition;
        private Vector2 _leftHiddenPosition;
        private Vector2 _rightHiddenPosition;
        private Vector3 _iconShownScale;
        private LoadingScreenState _state;

        /// <summary>
        /// Gets whether the loading screen is currently marked as active.
        /// </summary>
        public bool IsLoading => _isLoading;

        private void Awake()
        {
            _leftShownPosition = _leftPart.anchoredPosition;
            _rightShownPosition = _rightPart.anchoredPosition;
            _leftHiddenPosition = _leftShownPosition + Vector2.left * _curtainTravelDistance;
            _rightHiddenPosition = _rightShownPosition + Vector2.right * _curtainTravelDistance;
            _iconShownScale = _loadingIcon.localScale;

            HideImmediate();
            _currentTransitionAwaitable = CreateCompletedAwaitable();
        }

        private void Update()
        {
            if (_loadingIcon.gameObject.activeSelf)
                _loadingIcon.Rotate(0f, 0f, -_iconSpinSpeed * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            RestartAnimation();
            HideImmediate();
        }

        /// <summary>
        /// Starts showing the loading screen.
        /// </summary>
        public void Show()
        {
            _ = ShowAsync();
        }

        /// <summary>
        /// Starts hiding the loading screen.
        /// </summary>
        public void Hide()
        {
            _ = HideAsync();
        }

        /// <summary>
        /// Shows the loading screen and waits until the curtains finish closing.
        /// </summary>
        public Awaitable ShowAsync()
        {
            _isLoading = true;

            if (_state == LoadingScreenState.Shown || _state == LoadingScreenState.Showing)
                return _currentTransitionAwaitable;

            RestartAnimation();
            _state = LoadingScreenState.Showing;
            _transitionCompletionSource = CreateCompletionSource();
            _currentTransitionAwaitable = _transitionCompletionSource.Awaitable;
            _animationCoroutine = StartCoroutine(ShowRoutine(_transitionCompletionSource));
            return _currentTransitionAwaitable;
        }

        /// <summary>
        /// Hides the loading screen and waits until the curtains finish opening.
        /// </summary>
        public Awaitable HideAsync()
        {
            _isLoading = false;

            if (_state == LoadingScreenState.Hidden || _state == LoadingScreenState.Hiding)
                return _currentTransitionAwaitable;

            RestartAnimation();
            _state = LoadingScreenState.Hiding;
            _transitionCompletionSource = CreateCompletionSource();
            _currentTransitionAwaitable = _transitionCompletionSource.Awaitable;
            _animationCoroutine = StartCoroutine(HideRoutine(_transitionCompletionSource));
            return _currentTransitionAwaitable;
        }

        private void RestartAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            _transitionCompletionSource?.TrySetCanceled();
        }

        private IEnumerator ShowRoutine(AwaitableCompletionSource completionSource)
        {
            SetCurtainsActive(true);
            _loadingIcon.gameObject.SetActive(false);
            _loadingIcon.localScale = Vector3.zero;

            yield return AnimateCurtains(_leftPart.anchoredPosition, _leftShownPosition, _rightPart.anchoredPosition, _rightShownPosition, _curtainDuration);

            _state = LoadingScreenState.Shown;
            completionSource.TrySetResult();

            _loadingIcon.gameObject.SetActive(true);
            yield return AnimateIconScale(_loadingIcon.localScale, _iconShownScale, _iconDuration);

            _animationCoroutine = null;
        }

        private IEnumerator HideRoutine(AwaitableCompletionSource completionSource)
        {
            if (_loadingIcon.gameObject.activeSelf)
            {
                yield return AnimateIconScale(_loadingIcon.localScale, Vector3.zero, _iconDuration);
                _loadingIcon.gameObject.SetActive(false);
            }

            if (_leftPart.gameObject.activeSelf || _rightPart.gameObject.activeSelf)
                yield return AnimateCurtains(_leftPart.anchoredPosition, _leftHiddenPosition, _rightPart.anchoredPosition, _rightHiddenPosition, _curtainDuration);

            SetCurtainsActive(false);
            _state = LoadingScreenState.Hidden;
            completionSource.TrySetResult();
            _animationCoroutine = null;
        }

        private IEnumerator AnimateCurtains(Vector2 leftFrom, Vector2 leftTo, Vector2 rightFrom, Vector2 rightTo, float duration)
        {
            if (duration <= 0f)
            {
                _leftPart.anchoredPosition = leftTo;
                _rightPart.anchoredPosition = rightTo;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Ease(Mathf.Clamp01(elapsed / duration));
                _leftPart.anchoredPosition = Vector2.LerpUnclamped(leftFrom, leftTo, t);
                _rightPart.anchoredPosition = Vector2.LerpUnclamped(rightFrom, rightTo, t);
                yield return null;
            }

            _leftPart.anchoredPosition = leftTo;
            _rightPart.anchoredPosition = rightTo;
        }

        private IEnumerator AnimateIconScale(Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                _loadingIcon.localScale = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Ease(Mathf.Clamp01(elapsed / duration));
                _loadingIcon.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            _loadingIcon.localScale = to;
        }

        private void HideImmediate()
        {
            _leftPart.anchoredPosition = _leftHiddenPosition;
            _rightPart.anchoredPosition = _rightHiddenPosition;
            _loadingIcon.localScale = Vector3.zero;
            _loadingIcon.gameObject.SetActive(false);
            SetCurtainsActive(false);
            _state = LoadingScreenState.Hidden;
        }

        private void SetCurtainsActive(bool active)
        {
            _leftPart.gameObject.SetActive(active);
            _rightPart.gameObject.SetActive(active);
        }

        private static float Ease(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static AwaitableCompletionSource CreateCompletionSource()
        {
            return new AwaitableCompletionSource();
        }

        private static Awaitable CreateCompletedAwaitable()
        {
            var completionSource = CreateCompletionSource();
            completionSource.TrySetResult();
            return completionSource.Awaitable;
        }
    }
}
