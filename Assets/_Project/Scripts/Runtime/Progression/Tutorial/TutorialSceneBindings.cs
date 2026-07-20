using Game.Interaction;
using Game.Services;
using Game.UI;
using System.Collections;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Supplies scene-owned player, computer, and marker references to the persistent tutorial mode.
    /// </summary>
    public sealed class TutorialSceneBindings : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private DualHandInteractor _playerInteractor;
        [SerializeField] private SiteActivator _computerTabs;

        [Header("Marker Targets")]
        [SerializeField] private Transform _pickupTarget;
        [SerializeField] private Transform _computerTarget;

        [Header("UI Marker Targets")]
        [SerializeField] private RectTransform _shiftTabTarget;
        [SerializeField] private RectTransform _shopTabTarget;
        [SerializeField] private RectTransform _mealsTabTarget;

        private TutorialService _tutorial;
        private Coroutine _bindRoutine;

        public DualHandInteractor PlayerInteractor => _playerInteractor;
        public SiteActivator ComputerTabs => _computerTabs;
        public Transform PickupTarget => _pickupTarget;
        public Transform ComputerTarget => _computerTarget != null
            ? _computerTarget
            : _computerTabs != null ? _computerTabs.transform : null;

        public RectTransform ShiftTabTarget => _shiftTabTarget;
        public RectTransform ShopTabTarget => _shopTabTarget;
        public RectTransform MealsTabTarget => _mealsTabTarget;

        private void OnEnable()
        {
            _bindRoutine = StartCoroutine(BindWhenReady());
        }

        private void OnDisable()
        {
            if (_bindRoutine != null)
            {
                StopCoroutine(_bindRoutine);
                _bindRoutine = null;
            }

            if (_tutorial != null)
                _tutorial.UnbindScene(this);

            _tutorial = null;
        }

        private IEnumerator BindWhenReady()
        {
            while (!ServiceLocator.TryGet(out _tutorial))
                yield return null;

            while (_playerInteractor == null || _playerInteractor.LeftHand == null || _playerInteractor.RightHand == null)
                yield return null;

            _tutorial.BindScene(this);
            _bindRoutine = null;
        }
    }
}
