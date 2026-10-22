using Game.Animation;
using Game.Input;
using Game.Interaction;
using Game.Items;
using Game.Player;
using Game.Services;
using Game.Items.Properties;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System.Collections;
using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Base for counter stations. Items are put on a station, the station is used, and the result is taken back.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class ProtoStation : MonoBehaviour, IInteractable, IFocusable, IKeepHandsVisibleWhileFocused
    {
        [Header("Focus (like the computer)")]
        [Tooltip("Click to lock the camera onto the station; then left/right clicks use that hand. Esc leaves.")]
        [SerializeField] private bool _useFocus = true;
        [SerializeField] private float _focusTransitionDuration = 0.35f;
        [SerializeField] private float _focusFov = 55f;
        [Tooltip("Tilts the focused camera up by this many degrees (keeps the model clear of the top HUD). Position stays.")]
        [SerializeField] private float _focusPitchUp = 7f;

        [Header("Scene placement")]
        [Tooltip("The station's model (child). When set, the station sets itself up on Awake (placed in the scene, not spawned).")]
        [SerializeField] private Transform _visual;
        [Tooltip("Direction to where the player stands, in this station's local space (used by the camera focus).")]
        [SerializeField] private Vector3 _viewerLocalDirection = Vector3.forward;

        [Header("Minigame")]
        [Tooltip("Clicks needed to finish this station's minigame (when it has one).")]
        [SerializeField] private int _requiredClicks = 10;

        private bool _busy;
        private bool _inMinigame;
        private bool _minigameCompleted;
        private int _minigameClicks;
        private FocusTarget _focusTarget;
        private IFocusHandler _focusHandler;
        private IDualHandInteractor _interactor;
        private IInputService _input;

        /// <summary>
        /// The instantiated model under this station root.
        /// </summary>
        protected Transform Visual { get; private set; }

        protected bool Busy => _busy;

        public virtual void Setup(Transform visual)
        {
            Visual = visual;
        }

        private void Awake()
        {
            // Stations placed in the scene set themselves up; spawned ones are set up by ProtoKitchen.
            if (_visual == null || Visual != null)
                return;

            Setup(_visual);
            ConfigureFocus(transform.TransformDirection(_viewerLocalDirection));
        }

        /// <summary>
        /// Editor: stores the scene placement so the station can set itself up at runtime.
        /// </summary>
        public void BakePlacement(Transform visual, Vector3 viewerWorldDirection)
        {
            _visual = visual;
            _viewerLocalDirection = transform.InverseTransformDirection(viewerWorldDirection).normalized;
        }

        /// <summary>
        /// Enables or disables the computer-like camera focus for this station.
        /// </summary>
        public void SetUseFocus(bool useFocus) => _useFocus = useFocus;

        /// <summary>
        /// Builds the camera focus point: above the station, on the side of <paramref name="towardViewer"/>, looking down at it.
        /// </summary>
        public void ConfigureFocus(Vector3 towardViewer)
        {
            towardViewer.y = 0f;
            towardViewer = towardViewer.sqrMagnitude > 0.0001f ? towardViewer.normalized : -transform.forward;

            GetFocusPose(towardViewer, out Vector3 position, out Vector3 lookAt);

            var point = new GameObject("FocusPoint").transform;
            point.SetParent(transform, false);
            point.position = position;
            point.rotation = Quaternion.LookRotation(lookAt - position, Vector3.up) * Quaternion.Euler(-_focusPitchUp, 0f, 0f);

            _focusTarget = new FocusTarget
            {
                Transform = point,
                ApplyRotation = true,
                Fov = _focusFov
            };
        }

        /// <summary>
        /// Where the focused camera stands and what it looks at. Default: above the station, from the viewer's side.
        /// </summary>
        protected virtual void GetFocusPose(Vector3 towardViewer, out Vector3 position, out Vector3 lookAt)
        {
            Bounds bounds = GetLocalBounds(Visual != null ? Visual : transform, transform);
            Vector3 top = transform.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            float size = Mathf.Max(bounds.size.x, bounds.size.z);

            position = top + towardViewer * (size * 0.55f + 0.25f) + Vector3.up * (size * 0.45f + 0.35f);
            lookAt = top;
        }

        #region IInteractable
        public bool CanInteract(in InteractionContext context) => !_busy;

        public void OnInteractionStarted(in InteractionContext context)
        {
            if (_busy)
                return;

            if (_useFocus && _focusTarget != null && context.FocusHandler != null)
            {
                // First click only locks the camera onto the station.
                if (context.FocusHandler.TryBeginFocus(this))
                {
                    _focusHandler = context.FocusHandler;
                    _interactor = context.DualHandInteractor;
                }
                return;
            }

            HandleClick(in context);
        }

        private void HandleClick(in InteractionContext context)
        {
            if (!Interact(in context))
                RunBusy(ShakeRoutine());
        }

        public void OnInteractionHeld(in InteractionContext context, float delta) { }

        public void OnInteractionStopped(in InteractionContext context) { }
        #endregion

        /// <summary>Hands stay visible while focused, except during a minigame.</summary>
        public bool KeepHandsVisible => !_inMinigame;

        #region IFocusable
        public FocusTarget Target => _focusTarget;
        public float StartFocusTransitionDuration => _focusTransitionDuration;
        public float EndFocusTransitionDuration => _focusTransitionDuration;

        public void OnFocusStarted() { }

        public void OnFocusHeld(float delta)
        {
            // While focused the regular world interaction is off; read the hand buttons here.
            if (_busy || _interactor == null)
                return;

            if (_input == null && !ServiceLocator.TryGet(out _input))
                return;

            bool pressed = _input.InteractLeft.Pressed || _input.InteractRight.Pressed;

            if (_inMinigame)
            {
                if (MinigameMode == MinigameInput.MouseCircles)
                    OnMinigameMouse(_input.MouseDelta);
                else if (pressed)
                    MinigameClick();
                return;
            }

            if (_input.InteractLeft.Pressed)
                ClickWith(HandType.Left);
            else if (_input.InteractRight.Pressed)
                ClickWith(HandType.Right);
        }

        public void OnFocusEnded()
        {
            if (_inMinigame)
            {
                _inMinigame = false;
                ProtoMinigameHud.Hide();

                // Left early (Esc): the ingredient stays, the progress is lost.
                if (!_minigameCompleted)
                    OnMinigameAborted();
            }

            _focusHandler = null;
            _interactor = null;
        }
        #endregion

        #region Minigame
        /// <summary>Title shown on the minigame HUD.</summary>
        protected virtual string MinigameTitle => "Work it!";

        protected enum MinigameInput { Clicks, MouseCircles }

        /// <summary>How the minigame is played: clicking, or moving the mouse in circles.</summary>
        protected virtual MinigameInput MinigameMode => MinigameInput.Clicks;

        /// <summary>Hint line under the progress bar.</summary>
        protected virtual string MinigameHint => $"Click (LMB / RMB) x{RequiredClicks}    Esc - leave";

        protected int RequiredClicks => Mathf.Max(1, _requiredClicks);

        /// <summary>
        /// Locks the camera on the station and starts the click minigame.
        /// </summary>
        protected bool TryStartMinigame(in InteractionContext context)
        {
            if (_inMinigame || _focusTarget == null || context.FocusHandler == null)
                return false;

            if (!context.FocusHandler.TryBeginFocus(this))
                return false;

            _focusHandler = context.FocusHandler;
            _interactor = context.DualHandInteractor;
            _inMinigame = true;
            _minigameCompleted = false;
            _minigameClicks = 0;

            ProtoMinigameHud.Show(MinigameTitle, MinigameHint,
                MinigameMode == MinigameInput.MouseCircles ? ProtoMinigameHud.Icon.MouseCircle : ProtoMinigameHud.Icon.None);
            ProtoMinigameHud.SetProgress(0f);
            OnMinigameStarted();
            return true;
        }

        private void MinigameClick()
        {
            _minigameClicks++;
            ReportMinigameProgress((float)_minigameClicks / RequiredClicks);
        }

        /// <summary>
        /// Updates the progress bar; at 1 the minigame is completed and the camera is released.
        /// </summary>
        protected void ReportMinigameProgress(float progress)
        {
            if (!_inMinigame || _minigameCompleted)
                return;

            progress = Mathf.Clamp01(progress);
            ProtoMinigameHud.SetProgress(progress);
            OnMinigameProgress(progress);

            if (progress < 1f)
                return;

            _minigameCompleted = true;
            IFocusHandler handler = _focusHandler;
            handler?.EndFocus(); // calls OnFocusEnded; completed, so nothing is reset
            OnMinigameCompleted();
        }

        protected virtual void OnMinigameStarted() { }
        /// <summary>Called every focused frame in <see cref="MinigameInput.MouseCircles"/> mode with the raw mouse delta.</summary>
        protected virtual void OnMinigameMouse(Vector2 mouseDelta) { }
        protected virtual void OnMinigameProgress(float progress) { }
        protected virtual void OnMinigameCompleted() { }
        protected virtual void OnMinigameAborted() { }

        private void ClickWith(HandType handType)
        {
            Hand hand = _interactor.GetHand(handType);
            var context = new InteractionContext(transform.position, Vector3.down, _focusHandler, hand, _interactor);
            HandleClick(in context);
        }
        #endregion

        /// <summary>
        /// Handles a click. Return false to play the "can't do that" wobble.
        /// </summary>
        protected abstract bool Interact(in InteractionContext context);

        /// <summary>
        /// Runs a routine while blocking further interaction.
        /// </summary>
        protected void RunBusy(IEnumerator routine)
        {
            StartCoroutine(BusyRoutine(routine));
        }

        private IEnumerator BusyRoutine(IEnumerator routine)
        {
            _busy = true;
            yield return routine;
            _busy = false;
        }

        #region Item helpers
        protected static bool Is(ItemDefinition definition, ItemDefinitionAsset asset)
            => definition != null && asset != null && definition.Id == asset.Id;

        protected static bool Holds(IContainer hand, ItemDefinitionAsset asset)
            => hand != null && !hand.IsEmpty && Is(hand.Item.Value.Definition, asset);

        /// <summary>
        /// Takes the item out of the hand and returns its definition.
        /// </summary>
        protected static ItemDefinition TakeFromHand(IContainer hand)
        {
            if (hand == null || hand.IsEmpty)
                return null;

            ItemDefinition definition = hand.Item.Value.Definition;
            hand.Remove();
            return definition;
        }

        /// <summary>
        /// Puts a new item of the given definition into an empty hand.
        /// </summary>
        protected static bool GiveToHand(IContainer hand, ItemDefinition definition)
        {
            if (hand == null || !hand.IsEmpty || definition == null)
                return false;

            Item? item = Item.FromId(definition.Id);
            if (!item.HasValue)
                return false;

            hand.Insert(item.Value);
            return true;
        }

        protected static ItemDefinition Definition(ItemDefinitionAsset asset)
            => asset != null && ItemRegistry.Instance.TryGet(asset.Id, out ItemDefinition definition) ? definition : null;

        protected static Sprite SpriteOf(ItemDefinition definition)
            => definition != null && definition.TryGetProperty<FoodProperty>(out var food) ? food.WorldSprite : null;
        #endregion

        #region Visual helpers
        /// <summary>
        /// Creates a camera-facing sprite that shows an item lying on the station.
        /// </summary>
        protected ItemSlotView CreateSlot(string slotName, Transform parent, Vector3 worldPosition, float worldHeight)
        {
            var go = new GameObject(slotName);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.AddComponent<SpriteRotator>();
            var view = go.AddComponent<ItemSlotView>();
            view.Init(worldHeight);
            return view;
        }

        /// <summary>
        /// Small "nope" wobble when the click does nothing.
        /// </summary>
        private IEnumerator ShakeRoutine()
        {
            if (Visual == null)
                yield break;

            Vector3 start = Visual.localPosition;
            const float duration = 0.25f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float offset = Mathf.Sin(t * 60f) * 0.01f * (1f - t / duration);
                Visual.localPosition = start + Visual.parent.InverseTransformVector(transform.right * offset);
                yield return null;
            }
            Visual.localPosition = start;
        }

        protected static float EaseInOut(float t) => t * t * (3f - 2f * t);

        protected static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Bounds of a part's mesh in <paramref name="space"/>'s local space (oriented, not a world AABB).
        /// </summary>
        protected static Bounds GetLocalBounds(Transform part, Transform space, bool includeChildren = true)
        {
            MeshFilter[] filters = includeChildren
                ? part.GetComponentsInChildren<MeshFilter>()
                : part.GetComponents<MeshFilter>();
            if (filters.Length == 0)
                filters = part.GetComponentsInChildren<MeshFilter>();
            bool initialized = false;
            var bounds = new Bounds(space.InverseTransformPoint(part.position), Vector3.zero);

            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null)
                    continue;

                Bounds mesh = filter.sharedMesh.bounds;
                Vector3 min = mesh.min, max = mesh.max;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                    Vector3 local = space.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!initialized)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            return bounds;
        }
        #endregion
    }
}
