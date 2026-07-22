using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Editor helper that places evenly spaced splitter marks inside a RectTransform.
    /// </summary>
    [ExecuteAlways]
    public sealed class ProgressBarSplitterPlacer : MonoBehaviour
    {
        [SerializeField] private GameObject _splitterTemplate;
        [SerializeField] private RectTransform _target;
        [SerializeField, Min(0)] private int _splitterCount;
        [SerializeField] private bool _hideTemplate = true;

        [SerializeField, HideInInspector] private List<GameObject> _generatedSplitters = new();

        public void Rebuild()
        {
            ClearGenerated();

            if (_splitterTemplate == null || _target == null || _splitterCount <= 0)
                return;

            if (_hideTemplate)
                _splitterTemplate.SetActive(false);

            for (int i = 0; i < _splitterCount; i++)
            {
                float normalizedPosition = (i + 1f) / (_splitterCount + 1f);
                GameObject splitter = Instantiate(_splitterTemplate, _target);
                splitter.name = $"{_splitterTemplate.name} {i + 1}";
                splitter.SetActive(true);

                if (splitter.transform is RectTransform rectTransform)
                {
                    rectTransform.anchorMin = new Vector2(normalizedPosition, 0.5f);
                    rectTransform.anchorMax = new Vector2(normalizedPosition, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.localRotation = _splitterTemplate.transform.localRotation;
                    rectTransform.localScale = _splitterTemplate.transform.localScale;
                }

                _generatedSplitters.Add(splitter);
            }
        }

        public void ClearGenerated()
        {
            for (int i = _generatedSplitters.Count - 1; i >= 0; i--)
            {
                GameObject splitter = _generatedSplitters[i];
                if (splitter == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(splitter);
                else
                    DestroyImmediate(splitter);
            }

            _generatedSplitters.Clear();
        }
    }
}
