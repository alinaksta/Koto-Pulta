using Game.Interaction;
using Game.Items;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System.Collections;
using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Folder used as a press, with a click minigame.
    /// 1) Click with dough: the dough goes into the folder and the minigame starts: the folder closes,
    ///    the camera locks on, a progress bar appears. Every click presses the folder (and the dough) a bit more.
    /// 2) After enough clicks the dough becomes prepared dough, the folder opens and the minigame ends.
    ///    Leaving early (Esc) keeps the dough inside but resets the progress; click the folder to try again.
    /// 3) Click with an empty hand: take the prepared dough.
    /// The folder lies open flat (base cover, spine, front cover). Closing swings the spine up 90 degrees around
    /// its edge with the base and folds the front cover over it (180 degrees in total).
    /// </summary>
    public sealed class FolderPressStation : ProtoStation
    {
        private enum State { Empty, Loaded, Done }

        [SerializeField] private string _coverName = "Cube";
        [SerializeField] private string _spineName = "Cube.002";
        [SerializeField] private string _baseName = "Cube.003";
        [SerializeField] private float _spineAngle = 90f;
        [SerializeField] private float _coverAngle = 180f;
        [Tooltip("How closed the folder rests during the minigame (1 = fully shut).")]
        [SerializeField, Range(0f, 1f)] private float _minigameRest = 0.85f;
        [SerializeField] private float _closeDuration = 0.25f;
        [SerializeField] private float _pressDuration = 0.07f;
        [SerializeField] private float _holdDuration = 0.25f;
        [SerializeField] private float _openDuration = 0.35f;
        [Tooltip("Camera distance multiplier for the minigame focus.")]
        [SerializeField] private float _focusDistanceScale = 1.3f;
        [Tooltip("Size of the squashed item: x = width, y = height.")]
        [SerializeField] private Vector2 _squashed = new Vector2(1.35f, 0.25f);

        [SerializeField] private float _itemHeight = 0.28f;
        [Tooltip("Size multiplier for the item sprites on this station.")]
        [SerializeField] private float _spriteScale = 1.5f;
        private float ItemHeight => _itemHeight * _spriteScale;

        public ItemDefinitionAsset Input;
        public ItemDefinitionAsset Output;

        private Transform _cover;
        private Transform _spine;
        private Transform _base;
        private ItemSlotView _slot;
        private State _state;
        private FoldPose _pose;
        private float _closed;
        private Coroutine _poseRoutine;
        private Coroutine _hideRoutine;

        protected override string MinigameTitle => "Press the dough!";

        public override void Setup(Transform visual)
        {
            base.Setup(visual);
            SetUseFocus(false); // the camera only locks on during the minigame

            _cover = FindDeep(visual, _coverName);
            _spine = FindDeep(visual, _spineName);
            _base = FindDeep(visual, _baseName);
            if (_cover == null || _spine == null || _base == null)
                Debug.LogWarning($"[{nameof(FolderPressStation)}] Could not find '{_coverName}'/'{_spineName}'/'{_baseName}' in the folder model; the press animation is disabled.", this);
            else
                _pose = new FoldPose(this); // captured while the folder lies open

            // The item lies in the middle of the base cover.
            Vector3 surface = transform.position;
            if (_base != null)
            {
                Bounds bounds = GetLocalBounds(_base, transform);
                surface = transform.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            }
            _slot = CreateSlot("FolderItem", transform, surface, ItemHeight);
        }

        /// <summary>
        /// Centered on the base cover, where the closed folder ends up (not on the open folder's middle).
        /// </summary>
        protected override void GetFocusPose(Vector3 towardViewer, out Vector3 position, out Vector3 lookAt)
        {
            if (_base == null)
            {
                base.GetFocusPose(towardViewer, out position, out lookAt);
                return;
            }

            Bounds bounds = GetLocalBounds(_base, transform);
            lookAt = transform.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            float size = Mathf.Max(bounds.size.x, bounds.size.z);
            // A touch further back than the other stations.
            position = lookAt + (towardViewer * (size * 0.55f + 0.25f) + Vector3.up * (size * 0.45f + 0.35f)) * _focusDistanceScale;
        }

        protected override bool Interact(in InteractionContext context)
        {
            IContainer hand = context.ActiveHand;

            switch (_state)
            {
                case State.Empty:
                    if (!Holds(hand, Input))
                        return false;
                    ItemDefinition dough = TakeFromHand(hand);
                    _slot.Show(SpriteOf(dough));
                    _state = State.Loaded;
                    TryStartMinigame(in context);
                    return true;

                case State.Loaded:
                    // Dough is still inside (minigame was left early): start again.
                    return TryStartMinigame(in context);

                case State.Done:
                    if (!GiveToHand(hand, Definition(Output)))
                        return false;
                    _slot.Hide();
                    _state = State.Empty;
                    return true;
            }

            return false;
        }

        #region Minigame
        protected override void OnMinigameStarted()
        {
            _slot.SetSquash(Vector2.one);
            MovePose(_minigameRest, _closeDuration);
            // The dough would poke through the cover while pressing: hide it once the folder is shut.
            StopHide();
            _hideRoutine = StartCoroutine(HideDoughAfter(_closeDuration * 0.6f));
        }

        private IEnumerator HideDoughAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            _slot.Hide();
            _hideRoutine = null;
        }

        private void StopHide()
        {
            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        protected override void OnMinigameProgress(float progress)
        {
            // Each click slams the cover down.
            if (progress < 1f)
                Play(PressBump());
        }

        protected override void OnMinigameCompleted()
        {
            StopPose();
            StopHide();
            _slot.Hide();
            RunBusy(FinishRoutine());
        }

        protected override void OnMinigameAborted()
        {
            // The dough stays inside, the progress is lost.
            StopHide();
            _slot.Show(SpriteOf(Definition(Input)));
            _slot.SetSquash(Vector2.one);
            MovePose(0f, _openDuration);
        }
        #endregion

        private IEnumerator PressBump()
        {
            yield return PoseTo(1f, _pressDuration, accelerate: true);
            yield return PoseTo(_minigameRest, _pressDuration * 1.5f, accelerate: false);
        }

        private IEnumerator FinishRoutine()
        {
            yield return PoseTo(1f, _pressDuration, accelerate: true);
            yield return new WaitForSeconds(_holdDuration);

            // The prepared dough appears only as the folder opens (no clipping through the cover).
            _slot.Show(SpriteOf(Definition(Output)));
            _slot.SetSquash(_squashed);

            // Open: the prepared dough springs back to its full size as the cover lifts.
            float from = _closed;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, _openDuration))
            {
                float eased = EaseInOut(t);
                ApplyPose(Mathf.Lerp(from, 0f, eased));
                _slot.SetSquash(Vector2.Lerp(_squashed, Vector2.one, eased));
                yield return null;
            }

            ApplyPose(0f);
            _slot.SetSquash(Vector2.one);
            _state = State.Done;
        }

        #region Pose helpers
        private void Play(IEnumerator routine)
        {
            StopPose();
            _poseRoutine = StartCoroutine(routine);
        }

        private void MovePose(float target, float duration)
            => Play(PoseTo(target, duration, accelerate: target > _closed));

        private void StopPose()
        {
            if (_poseRoutine != null)
                StopCoroutine(_poseRoutine);
            _poseRoutine = null;
        }

        private IEnumerator PoseTo(float target, float duration, bool accelerate)
        {
            float from = _closed;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duration))
            {
                ApplyPose(Mathf.Lerp(from, target, accelerate ? t * t : EaseInOut(t)));
                yield return null;
            }

            ApplyPose(target);
        }

        private void ApplyPose(float closed)
        {
            _closed = closed;
            _pose?.Apply(closed);
        }
        #endregion

        /// <summary>
        /// Rest pose and hinge geometry, stored in the station's local space so it is independent of placement.
        /// </summary>
        private sealed class FoldPose
        {
            private readonly FolderPressStation _station;
            private readonly Vector3 _coverPosition, _spinePosition;
            private readonly Quaternion _coverRotation, _spineRotation;
            private readonly Vector3 _baseHinge;   // base <-> spine edge (local)
            private readonly Vector3 _coverHinge;  // spine <-> cover edge (local)
            private readonly Vector3 _axis;        // local

            public FoldPose(FolderPressStation station)
            {
                _station = station;
                _coverPosition = station._cover.localPosition;
                _coverRotation = station._cover.localRotation;
                _spinePosition = station._spine.localPosition;
                _spineRotation = station._spine.localRotation;

                // Work in the station's local space: the folder parts are axis-aligned there,
                // while world-space bounds of a rotated station would be inflated.
                Transform root = station.transform;
                Bounds baseBounds = GetLocalBounds(station._base, root);
                Bounds spineBounds = GetLocalBounds(station._spine, root);
                Bounds coverBounds = GetLocalBounds(station._cover, root);

                // Direction across the open folder: from the base towards the front cover.
                Vector3 across = coverBounds.center - baseBounds.center;
                across.y = 0f;
                across.Normalize();

                // Hinges sit on the shared edges: base|spine and spine|cover.
                float height = Mathf.Max(baseBounds.max.y, spineBounds.max.y);
                Vector3 baseHinge = spineBounds.center - across * HalfSize(spineBounds, across);
                Vector3 coverHinge = spineBounds.center + across * HalfSize(spineBounds, across);
                baseHinge.y = height;
                coverHinge.y = height;

                // Rotation axis along the spine; pick the sign that lifts the cover up rather than through the counter.
                Vector3 axis = Vector3.Cross(Vector3.up, across);
                Vector3 lever = coverBounds.center - baseHinge;
                if ((Quaternion.AngleAxis(90f, axis) * lever).y < 0f)
                    axis = -axis;

                _baseHinge = baseHinge;
                _coverHinge = coverHinge;
                _axis = axis;
            }

            /// <summary>
            /// 0 = open flat, 1 = closed.
            /// </summary>
            public void Apply(float closed)
            {
                Transform root = _station.transform;
                Vector3 axis = root.TransformDirection(_axis);
                Vector3 baseHinge = root.TransformPoint(_baseHinge);
                Vector3 coverHinge = root.TransformPoint(_coverHinge);

                float spineAngle = _station._spineAngle * closed;
                float coverAngle = _station._coverAngle * closed;

                Transform spine = _station._spine;
                Transform cover = _station._cover;

                spine.localPosition = _spinePosition;
                spine.localRotation = _spineRotation;
                cover.localPosition = _coverPosition;
                cover.localRotation = _coverRotation;

                // Cover folds around its edge with the spine (relative bend)...
                cover.RotateAround(coverHinge, axis, coverAngle - spineAngle);
                // ...and the spine carries it around the base edge.
                cover.RotateAround(baseHinge, axis, spineAngle);
                spine.RotateAround(baseHinge, axis, spineAngle);
            }

            private static float HalfSize(Bounds bounds, Vector3 direction)
                => Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.z) * bounds.extents.z;
        }

    }
}
