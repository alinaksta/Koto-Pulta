using Game.Services;
using System.Collections;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Positions a world-space tutorial marker above the current tutorial target.
    /// </summary>
    public sealed class TutorialMarkerView : MonoBehaviour
    {
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField, Min(0f)] private float _bobAmplitude = 0.15f;
        [SerializeField, Min(0f)] private float _bobFrequency = 2f;
        [SerializeField] private bool _faceCamera = true;
        [SerializeField] private Camera _camera;

        private TutorialService _tutorial;
        private Transform _target;
        private Vector3 _targetOffset;

        private IEnumerator Start()
        {
            while (!ServiceLocator.TryGet(out _tutorial))
                yield return null;

            _tutorial.OnTargetChanged += HandleTargetChanged;
            HandleTargetChanged(_tutorial.Target, _tutorial.TargetOffset);
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            float bob = _bobAmplitude > 0f && _bobFrequency > 0f
                ? Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude
                : 0f;

            transform.position = _target.position + _targetOffset + Vector3.up * bob;

            gameObject.SetActive(_target.gameObject.activeSelf);

            if (!_faceCamera)
                return;

            Camera targetCamera = _camera != null ? _camera : Camera.main;
            if (targetCamera != null)
                transform.forward = targetCamera.transform.forward;
        }

        private void OnDestroy()
        {
            if (_tutorial != null)
                _tutorial.OnTargetChanged -= HandleTargetChanged;
        }

        private void HandleTargetChanged(Transform target, Vector3 offset)
        {
            _target = target;
            _targetOffset = offset;

            if (_visualRoot != null)
                _visualRoot.SetActive(_target != null);
        }
    }
}
