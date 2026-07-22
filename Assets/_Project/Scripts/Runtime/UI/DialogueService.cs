using Game.Lifecycle;
using Game.Input;
using Game.Services;
using LitMotion;
using LitMotion.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using System;

namespace Game.UI
{
    /// <summary>
    /// Displays and animates the global dialogue window.
    /// </summary>
    public sealed class DialogueService : MonoBehaviour, IBootstrapable
    {
        [Header("Dependencies")]
        [SerializeField] private RectTransform _window;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private CanvasGroup _continuePrompt;

        [Header("Window Animation")]
        [SerializeField, Min(0f)] private float _showDuration = 0.3f;
        [SerializeField, Min(0f)] private float _hideDuration = 0.2f;
        [SerializeField, Min(0f)] private float _hiddenDistance = 180f;
        [SerializeField] private Ease _showEase = Ease.OutCubic;
        [SerializeField] private Ease _hideEase = Ease.InCubic;

        [Header("Continue Prompt")]
        [SerializeField, Min(0f)] private float _promptShowDuration = 0.15f;
        [SerializeField, Min(0f)] private float _promptHideDuration = 0.1f;
        [SerializeField] private Ease _promptShowEase = Ease.OutCubic;
        [SerializeField] private Ease _promptHideEase = Ease.InCubic;

        [Header("Text Animation")]
        [SerializeField, Min(0f)] private float _charactersPerSecond = 40f;

        private MotionHandle _windowMotion;
        private MotionHandle _alphaMotion;
        private MotionHandle _textMotion;
        private MotionHandle _promptMotion;
        private Vector2 _shownPosition;
        private Vector2 _hiddenPosition;
        private readonly Queue<DialogueRequest> _requests = new();

        private IInputService _input;
        private DialogueRequest _activeRequest;
        private TaskCompletionSource<bool> _advanceSource;
        private CancellationTokenSource _queueCancellation;
        private bool _processingQueue;
        private bool _waitForAdvanceRelease;

        private sealed class DialogueRequest
        {
            public DialogueRequest(string text, bool waitForAdvance, CancellationToken cancellationToken)
            {
                Text = text;
                WaitForAdvance = waitForAdvance;
                Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                CancellationToken = cancellationToken;
            }

            public string Text { get; }
            public bool WaitForAdvance { get; }
            public TaskCompletionSource<bool> Completion { get; }
            public CancellationToken CancellationToken { get; }
        }

        /// <summary>
        /// Gets whether the dialogue window is intended to be visible.
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Gets whether a modal line is currently consuming dialogue input.
        /// </summary>
        public bool IsModal => _processingQueue;

        private void Awake()
        {
            _shownPosition = _window.anchoredPosition;
            _hiddenPosition = _shownPosition + Vector2.down * _hiddenDistance;
            HideImmediate();
            _canvasGroup.blocksRaycasts = false;
            SetContinuePromptAlpha(0f);
        }

        private void Update()
        {
            if (!_processingQueue || _input == null)
                return;

            ButtonState advance = _input.DialogueAdvance;
            if (!advance.Pressed)
            {
                if (_waitForAdvanceRelease && !advance.Held)
                    _waitForAdvanceRelease = false;

                return;
            }

            if (_waitForAdvanceRelease)
                return;

            if (_textMotion.IsActive())
            {
                _textMotion.Complete();
                _waitForAdvanceRelease = true;
                return;
            }

            HideContinuePrompt();
            _waitForAdvanceRelease = true;
            _advanceSource?.TrySetResult(true);
        }

        private void OnDisable()
        {
            CancelQueue();
            CancelMotions();
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            _input = ServiceLocator.Get<IInputService>();
            ServiceLocator.Register(this);
        }

        /// <summary>
        /// Shows the dialogue window and reveals the supplied text.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public void Display(string text)
        {
            _ = DisplayAsync(text);
        }

        /// <summary>
        /// Queues a modal dialogue line and waits for the player to advance it.
        /// </summary>
        public Task DisplayAsync(string text, CancellationToken cancellationToken = default)
        {
            return Enqueue(text, true, cancellationToken);
        }

        private Task Enqueue(string text, bool waitForAdvance, CancellationToken cancellationToken)
        {
            var request = new DialogueRequest(text ?? string.Empty, waitForAdvance, cancellationToken);
            _requests.Enqueue(request);

            if (!_processingQueue)
                _ = ProcessQueueAsync();

            return request.Completion.Task;
        }

        /// <summary>
        /// Queues modal dialogue lines and waits until all of them are advanced.
        /// </summary>
        public async Task DisplayLinesAsync(
            IEnumerable<string> lines,
            CancellationToken cancellationToken = default,
            bool waitForLastLine = true)
        {
            if (lines == null)
                return;

            var bufferedLines = lines as IReadOnlyList<string> ?? new List<string>(lines);
            for (int i = 0; i < bufferedLines.Count; i++)
            {
                bool waitForAdvance = i < bufferedLines.Count - 1 || waitForLastLine;
                await Enqueue(bufferedLines[i], waitForAdvance, cancellationToken);
            }
        }

        /// <summary>
        /// Shows a non-modal instruction while gameplay remains enabled.
        /// </summary>
        public void DisplayInstruction(string text)
        {
            CancelQueue();
            _canvasGroup.blocksRaycasts = false;
            ShowText(text ?? string.Empty, false);
        }

        private void ShowText(string text, bool showPromptOnComplete)
        {
            CancelMotions();
            HideContinuePrompt();

            IsVisible = true;
            _label.text = text;
            _label.ForceMeshUpdate();
            _label.maxVisibleCharacters = 0;

            if (_showDuration <= 0f)
            {
                _window.anchoredPosition = _shownPosition;
                _canvasGroup.alpha = 1f;
            }
            else
            {
                _windowMotion = LMotion
                    .Create(_window.anchoredPosition, _shownPosition, _showDuration)
                    .WithEase(_showEase)
                    .Bind(value => _window.anchoredPosition = value);

                _alphaMotion = LMotion
                    .Create(_canvasGroup.alpha, 1f, _showDuration)
                    .WithEase(_showEase)
                    .Bind(value => _canvasGroup.alpha = value);
            }

            int characterCount = _label.textInfo.characterCount;
            float textDuration = _charactersPerSecond > 0f
                ? characterCount / _charactersPerSecond
                : 0f;

            if (characterCount == 0 || textDuration <= 0f)
            {
                _label.maxVisibleCharacters = characterCount;

                if (showPromptOnComplete)
                    ShowContinuePrompt();

                return;
            }

            var textMotion = LMotion
                .Create(0, characterCount, textDuration)
                .WithEase(Ease.Linear);

            if (showPromptOnComplete)
                textMotion = textMotion.WithOnComplete(ShowContinuePrompt);

            _textMotion = textMotion.Bind(value => _label.maxVisibleCharacters = value);
        }

        /// <summary>
        /// Hides the dialogue window.
        /// </summary>
        public void Hide()
        {
            CancelQueue();
            CancelMotions();
            IsVisible = false;
            _canvasGroup.blocksRaycasts = false;
            HideContinuePrompt();

            if (_hideDuration <= 0f)
            {
                HideImmediate();
                return;
            }

            _windowMotion = LMotion
                .Create(_window.anchoredPosition, _hiddenPosition, _hideDuration)
                .WithEase(_hideEase)
                .WithOnComplete(ClearText)
                .Bind(value => _window.anchoredPosition = value);

            _alphaMotion = LMotion
                .Create(_canvasGroup.alpha, 0f, _hideDuration)
                .WithEase(_hideEase)
                .Bind(value => _canvasGroup.alpha = value);
        }

        private void HideImmediate()
        {
            IsVisible = false;
            _window.anchoredPosition = _hiddenPosition;
            _canvasGroup.alpha = 0f;
            ClearText();
        }

        private void ClearText()
        {
            _label.text = string.Empty;
            _label.maxVisibleCharacters = 0;
        }

        private void CancelMotions()
        {
            CancelMotion(ref _windowMotion);
            CancelMotion(ref _alphaMotion);
            CancelMotion(ref _textMotion);
            CancelMotion(ref _promptMotion);
        }

        private async Task ProcessQueueAsync()
        {
            _processingQueue = true;
            _queueCancellation = new CancellationTokenSource();

            try
            {
                while (_requests.Count > 0)
                {
                    _activeRequest = _requests.Dequeue();
                    using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                        _queueCancellation.Token,
                        _activeRequest.CancellationToken);

                    linkedCancellation.Token.ThrowIfCancellationRequested();
                    _waitForAdvanceRelease = _input != null && _input.DialogueAdvance.Held;
                    ShowText(_activeRequest.Text, _activeRequest.WaitForAdvance);

                    if (!_activeRequest.WaitForAdvance)
                    {
                        _activeRequest.Completion.TrySetResult(true);
                        _activeRequest = null;
                        continue;
                    }

                    _advanceSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    TaskCompletionSource<bool> advanceSource = _advanceSource;
                    using CancellationTokenRegistration registration = linkedCancellation.Token.Register(
                        () => advanceSource.TrySetCanceled());

                    await advanceSource.Task;
                    _activeRequest.Completion.TrySetResult(true);
                    _activeRequest = null;
                    _advanceSource = null;
                }
            }
            catch (OperationCanceledException)
            {
                _activeRequest?.Completion.TrySetCanceled();

                while (_requests.Count > 0)
                    _requests.Dequeue().Completion.TrySetCanceled();
            }
            finally
            {
                _activeRequest = null;
                _advanceSource = null;
                _queueCancellation?.Dispose();
                _queueCancellation = null;
                _processingQueue = false;
            }
        }

        private void CancelQueue()
        {
            _queueCancellation?.Cancel();

            while (_requests.Count > 0)
                _requests.Dequeue().Completion.TrySetCanceled();

        }

        private void ShowContinuePrompt()
        {
            AnimateContinuePrompt(1f, _promptShowDuration, _promptShowEase);
        }

        private void HideContinuePrompt()
        {
            AnimateContinuePrompt(0f, _promptHideDuration, _promptHideEase);
        }

        private void AnimateContinuePrompt(float targetAlpha, float duration, Ease ease)
        {
            CancelMotion(ref _promptMotion);

            if (_continuePrompt == null)
                return;

            if (duration <= 0f)
            {
                SetContinuePromptAlpha(targetAlpha);
                return;
            }

            _promptMotion = LMotion
                .Create(_continuePrompt.alpha, targetAlpha, duration)
                .WithEase(ease)
                .Bind(SetContinuePromptAlpha);
        }

        private void SetContinuePromptAlpha(float alpha)
        {
            if (_continuePrompt == null)
                return;

            _continuePrompt.alpha = alpha;
            _continuePrompt.interactable = false;
            _continuePrompt.blocksRaycasts = false;
        }

        private static void CancelMotion(ref MotionHandle motion)
        {
            if (motion.IsActive())
                motion.Cancel();
        }
    }
}
