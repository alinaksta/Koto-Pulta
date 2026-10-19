using Game.Interaction;
using Game.Items;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System.Collections;
using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Shredder, with a crank minigame.
    /// 1) Click with an apple: the apple is put on top and the minigame starts (camera looks at the crank, progress bar).
    ///    Any mouse movement turns the crank forward, as fast as the mouse moves: gears spin and the apple sinks deeper into the slot.
    /// 2) After enough turns the apple disappears and the drawer opens with apple slices; the minigame ends.
    ///    Leaving early (Esc) keeps the apple on top but resets the progress; click the shredder to try again.
    /// 3) Click with an empty hand: take the slices; the drawer closes.
    /// </summary>
    public sealed class ShredderStation : ProtoStation
    {
        private enum State { Empty, Loaded, Done }

        [SerializeField] private string[] _gearNames = { "shesterni1", "shesterni2" };
        [SerializeField] private string _crankName = "ruchka";
        [SerializeField] private string _bodyName = "box";
        [SerializeField] private string _drawerName = "small";
        [Header("Crank minigame")]
        [Tooltip("Full mouse circles needed to shred the apple.")]
        [SerializeField] private float _requiredTurns = 4f;
        [Tooltip("Crank degrees per pixel of mouse movement (before the speed limit).")]
        [SerializeField] private float _degreesPerPixel = 0.5f;
        [Tooltip("Crank degrees per degree of mouse circle.")]
        [SerializeField] private float _crankRatio = 1f;
        [Tooltip("Gear degrees per crank degree.")]
        [SerializeField] private float _gearRatio = 2f;
        [Tooltip("Camera distance from the crank side, in shredder sizes.")]
        [SerializeField] private float _focusDistance = 1.1f;
        [Tooltip("Camera height above the crank, in shredder sizes.")]
        [SerializeField] private float _focusHeight = 0.6f;
        [Tooltip("How much the camera leans from the front toward the crank side (0 = straight from the front).")]
        [SerializeField] private float _focusTowardCrank = 0f;
        [Tooltip("Shifts the camera's aim from the box center toward the crank (0 = box center).")]
        [SerializeField, Range(0f, 1f)] private float _focusCrankBias = 0f;
        [SerializeField] private float _itemHeight = 0.22f;
        [Tooltip("How far below the top of the shredder the apple sits, in apple heights.")]
        [SerializeField] private float _inputDrop = 0.5f;

        [Header("Drawer")]
        [Tooltip("How far the drawer slides out, relative to its own size along the slide direction.")]
        [SerializeField] private float _drawerTravel = 0.25f;
        [SerializeField] private float _drawerOpenDuration = 0.25f;
        [SerializeField] private float _drawerCloseDuration = 0.3f;

        public ItemDefinitionAsset Input;
        public ItemDefinitionAsset Output;


        private Transform[] _gears;
        private Vector3[] _gearAxes;
        private Transform _crank;
        private Vector3 _crankAxis;
        private float _crankSign = 1f;
        private float[] _gearSigns;
        private Transform _body;
        private Transform _drawer;
        private Vector3 _drawerClosed;
        private ItemSlotView _topSlot;
        private ItemSlotView _drawerSlot;
        // Just enough to get the top of the apple below the top of the shredder.
        private float SinkDepth => _itemHeight * (1f - _inputDrop) + 0.02f;
        private State _state;
        private float _rattleTime;
        private Vector3 _visualRest;
        private float _turnedDegrees;

        public override void Setup(Transform visual)
        {
            base.Setup(visual);

            _gears = new Transform[_gearNames.Length];
            _gearAxes = new Vector3[_gearNames.Length];
            for (int i = 0; i < _gearNames.Length; i++)
            {
                _gears[i] = FindDeep(visual, _gearNames[i]);
                _gearAxes[i] = MeshAxis(_gears[i], longest: true);
            }

            _crank = FindDeep(visual, _crankName);
            _crankAxis = MeshAxis(_crank, longest: false);
            _body = FindDeep(visual, _bodyName);
            _drawer = FindDeep(visual, _drawerName);
            _visualRest = visual.localPosition;
            ComputeSpinDirections();
            SetUseFocus(false); // the camera only locks on during the minigame
            if (_drawer != null)
                _drawerClosed = _drawer.localPosition;
            else
                Debug.LogWarning($"[{nameof(ShredderStation)}] No '{_drawerName}' drawer found in the shredder model.", this);

            // Apple sits on top of the body; slices lie in the drawer.
            Bounds body = GetLocalBounds(_body != null ? _body : visual, transform);
            Vector3 top = transform.TransformPoint(new Vector3(body.center.x, body.max.y, body.center.z));
            _topSlot = CreateSlot("ShredderInput", transform, top + Vector3.down * _itemHeight * _inputDrop, _itemHeight);

            if (_drawer != null)
            {
                Bounds drawer = GetLocalBounds(_drawer, transform);
                Vector3 drawerSurface = transform.TransformPoint(new Vector3(drawer.center.x, drawer.min.y + drawer.size.y * 0.3f, drawer.center.z));
                _drawerSlot = CreateSlot("ShredderOutput", _drawer, drawerSurface, _itemHeight * 0.8f);
            }
        }

        protected override string MinigameTitle => "Shred the apple!";
        protected override MinigameInput MinigameMode => MinigameInput.MouseCircles;
        protected override string MinigameHint => "Move the mouse to turn the crank    Esc - leave";

        /// <summary>
        /// The camera stands at the crank's side of the shredder, slightly above, looking at the crank.
        /// </summary>
        protected override void GetFocusPose(Vector3 towardViewer, out Vector3 position, out Vector3 lookAt)
        {
            Bounds body = (_body != null ? GetLocalBounds(_body, transform, includeChildren: false) : GetLocalBounds(Visual, transform));
            Vector3 bodyCenter = transform.TransformPoint(body.center);
            float size = Mathf.Max(body.size.x, body.size.y, body.size.z);

            if (_crank == null)
            {
                base.GetFocusPose(towardViewer, out position, out lookAt);
                return;
            }

            Vector3 crankCenter = transform.TransformPoint(GetLocalBounds(_crank, transform).center);
            Vector3 side = crankCenter - bodyCenter;
            side.y = 0f;
            side = side.sqrMagnitude > 0.000001f ? side.normalized : transform.right;

            // Centered on the body (the box), optionally shifted toward the crank.
            lookAt = Vector3.Lerp(bodyCenter, crankCenter, _focusCrankBias);
            // From the front (the player's side), optionally leaning toward the crank.
            Vector3 offset = (towardViewer + side * _focusTowardCrank).normalized;
            position = lookAt + offset * (size * _focusDistance + 0.2f) + Vector3.up * (size * _focusHeight + 0.15f);
        }

        protected override bool Interact(in InteractionContext context)
        {
            IContainer hand = context.ActiveHand;

            switch (_state)
            {
                case State.Empty:
                    if (!Holds(hand, Input))
                        return false;
                    ItemDefinition apple = TakeFromHand(hand);
                    _topSlot.Show(SpriteOf(apple));
                    _state = State.Loaded;
                    TryStartMinigame(in context);
                    return true;

                case State.Loaded:
                    // The apple is still on top (minigame was left early): start again.
                    return TryStartMinigame(in context);

                case State.Done:
                    if (!GiveToHand(hand, Definition(Output)))
                        return false;
                    _drawerSlot?.Hide();
                    _state = State.Empty;
                    RunBusy(SlideDrawer(open: false));
                    return true;
            }

            return false;
        }

        #region Minigame
        protected override void OnMinigameStarted()
        {
            _topSlot.SetWorldOffset(Vector3.zero);
            _turnedDegrees = 0f;
        }

        protected override void OnMinigameMouse(Vector2 mouseDelta)
        {
            // Any mouse movement turns the crank forward, proportionally to the mouse speed,
            // whatever the path.
            float turn = mouseDelta.magnitude * _degreesPerPixel;
            if (turn <= 0f)
                return;

            TurnCrank(turn);

            _turnedDegrees += turn;
            ReportMinigameProgress(_turnedDegrees / (360f * Mathf.Max(0.1f, _requiredTurns)));
        }

        protected override void OnMinigameProgress(float progress)
        {
            // The apple is pulled deeper into the slot as the crank turns.
            _topSlot.SetWorldOffset(Vector3.down * SinkDepth * progress);
        }

        /// <summary>
        /// Turns the crank and gears by the mouse's turn; nothing moves while the mouse stands still.
        /// </summary>
        private void TurnCrank(float mouseDegrees)
        {
            float crank = mouseDegrees * _crankRatio;
            if (_crank != null)
                _crank.Rotate(_crankAxis, crank * _crankSign, Space.Self);

            for (int i = 0; i < _gears.Length; i++)
            {
                if (_gears[i] != null)
                    _gears[i].Rotate(_gearAxes[i], crank * _gearRatio * _gearSigns[i], Space.Self);
            }

            if (Mathf.Abs(mouseDegrees) > 0.5f)
                _rattleTime = 0.08f;
        }

        protected override void OnMinigameCompleted()
        {
            _topSlot.Hide();
            RunBusy(FinishRoutine());
        }

        protected override void OnMinigameAborted()
        {
            // The apple pops back up, the progress is lost.
            _topSlot.SetWorldOffset(Vector3.zero);
            _turnedDegrees = 0f;
        }
        #endregion

        private void Update()
        {
            if (_rattleTime <= 0f || Visual == null)
                return;

            _rattleTime -= Time.deltaTime;

            // Rattle while the crank is turning.
            Visual.localPosition = _visualRest + (_rattleTime > 0f
                ? Visual.parent.InverseTransformVector(Random.insideUnitSphere * 0.004f)
                : Vector3.zero);
        }

        private IEnumerator FinishRoutine()
        {
            // Let the rattle settle, then the slices come out.
            while (_rattleTime > 0f)
                yield return null;

            _drawerSlot?.Show(SpriteOf(Definition(Output)));
            yield return SlideDrawer(open: true);
            _state = State.Done;
        }

        private IEnumerator SlideDrawer(bool open)
        {
            if (_drawer == null)
                yield break;

            Vector3 openPosition = _drawerClosed + GetDrawerOffset();
            Vector3 from = open ? _drawerClosed : openPosition;
            Vector3 to = open ? openPosition : _drawerClosed;
            float duration = open ? _drawerOpenDuration : _drawerCloseDuration;

            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duration))
            {
                _drawer.localPosition = Vector3.Lerp(from, to, EaseInOut(t));
                yield return null;
            }

            _drawer.localPosition = to;
        }

        /// <summary>
        /// The drawer slides horizontally away from the body's center, by part of its own size.
        /// </summary>
        private Vector3 GetDrawerOffset()
        {
            Vector3 saved = _drawer.localPosition;
            _drawer.localPosition = _drawerClosed;
            Bounds drawer = GetLocalBounds(_drawer, transform);
            Bounds body = GetLocalBounds(_body != null ? _body : Visual, transform);
            _drawer.localPosition = saved;

            Vector3 direction = drawer.center - body.center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f)
                direction = Vector3.forward;
            direction.Normalize();

            float depth = 2f * (Mathf.Abs(direction.x) * drawer.extents.x + Mathf.Abs(direction.z) * drawer.extents.z);
            Vector3 worldOffset = transform.TransformVector(direction * depth * _drawerTravel);
            return _drawer.parent.InverseTransformVector(worldOffset);
        }

        /// <summary>
        /// Crank: clockwise as seen from the crank side.
        /// Gears: counter-rotating so their teeth move top to bottom where they meet (they pull the apple in).
        /// </summary>
        private void ComputeSpinDirections()
        {
            _gearSigns = new float[_gears.Length];
            Vector3 bodyCenter = transform.TransformPoint(GetLocalBounds(_body != null ? _body : Visual, transform).center);

            if (_crank != null)
            {
                Vector3 crankCenter = transform.TransformPoint(GetLocalBounds(_crank, transform).center);
                Vector3 side = crankCenter - bodyCenter;
                side.y = 0f;
                if (side.sqrMagnitude < 0.000001f)
                    side = transform.right;

                // Matched to the model visually: clockwise as seen from the crank side.
                Vector3 worldAxis = _crank.TransformDirection(_crankAxis);
                _crankSign = Vector3.Dot(worldAxis, side) > 0f ? 1f : -1f;
            }

            // Contact point: between the gear centers.
            Vector3 contact = Vector3.zero;
            int count = 0;
            var centers = new Vector3[_gears.Length];
            for (int i = 0; i < _gears.Length; i++)
            {
                if (_gears[i] == null)
                    continue;
                centers[i] = transform.TransformPoint(GetLocalBounds(_gears[i], transform).center);
                contact += centers[i];
                count++;
            }
            if (count > 0)
                contact /= count;

            for (int i = 0; i < _gears.Length; i++)
            {
                _gearSigns[i] = i % 2 == 0 ? 1f : -1f;
                if (_gears[i] == null || count < 2)
                    continue;

                // Velocity of the gear surface at the contact point for a positive rotation; we want it pointing down.
                Vector3 worldAxis = _gears[i].TransformDirection(_gearAxes[i]);
                Vector3 velocity = Vector3.Cross(worldAxis, contact - centers[i]);
                if (Mathf.Abs(velocity.y) > 0.000001f)
                    _gearSigns[i] = velocity.y < 0f ? 1f : -1f;
            }
        }

        /// <summary>
        /// Picks a spin axis from the mesh shape: gears spin around their longest axis, the crank around the middle one.
        /// </summary>
        private static Vector3 MeshAxis(Transform target, bool longest)
        {
            if (target == null || !target.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                return Vector3.forward;

            Vector3 size = filter.sharedMesh.bounds.size;
            float[] values = { size.x, size.y, size.z };
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };

            int max = 0, min = 0;
            for (int i = 1; i < 3; i++)
            {
                if (values[i] > values[max]) max = i;
                if (values[i] < values[min]) min = i;
            }

            if (longest)
                return axes[max];

            int middle = 3 - max - min;
            return axes[middle == max || middle == min ? max : middle];
        }
    }
}
