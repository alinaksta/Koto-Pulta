using Game.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Order board at the top of the screen. Pure UI with a tiny API:
    /// AddOrder -> returns an id, SetPatience / SetRecipe / RemoveOrder use that id.
    /// Registers itself in the ServiceLocator, so other code can call
    /// ServiceLocator.Get&lt;OrderBoardView&gt;().
    /// </summary>
    public sealed class OrderBoardView : MonoBehaviour
    {
        [SerializeField] private OrderNoteView _notePrefab;

        [Header("Debug (right-click the component header in Play Mode)")]
        [SerializeField] private Sprite _debugDish;
        [SerializeField] private Sprite _debugRecipe;

        private readonly Dictionary<int, OrderNoteView> _notes = new();
        private int _nextId = 1;

        public int Count => _notes.Count;

        /// <summary>
        /// Optional: ingredient pictures for a dish picture, shown on the note (null = none).
        /// </summary>
        public System.Func<Sprite, Sprite[]> IngredientsForDish { get; set; }

        /// <summary>
        /// Replaces the note prefab (e.g. with a note built in code). New notes use it; existing ones stay.
        /// </summary>
        public void SetNoteTemplate(OrderNoteView template)
        {
            if (template != null)
                _notePrefab = template;
        }

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            // Scenes reload, the locator doesn't, so always clean up.
            if (ServiceLocator.TryGet(out OrderBoardView registered) && registered == this)
                ServiceLocator.Unregister<OrderBoardView>();
        }

        /// <summary>Adds a note and returns an id used for later changes.</summary>
        public int AddOrder(Sprite dish, Sprite recipe = null, float patience = 1f)
        {
            OrderNoteView note = Instantiate(_notePrefab, transform);
            note.gameObject.SetActive(true); // a template kept inactive in the scene
            note.SetDish(dish);
            note.SetIngredients(IngredientsForDish?.Invoke(dish));
            note.SetRecipe(recipe);
            note.SetPatience(patience);
            note.PlayPopIn();

            int id = _nextId++;
            _notes.Add(id, note);
            return id;
        }

        /// <summary>Updates the patience bar. 1 = full, 0 = empty.</summary>
        public void SetPatience(int id, float normalized)
        {
            if (_notes.TryGetValue(id, out OrderNoteView note) && note != null)
                note.SetPatience(normalized);
        }

        /// <summary>Shows a short label on the note, e.g. the table number.</summary>
        public void SetLabel(int id, string text)
        {
            if (_notes.TryGetValue(id, out OrderNoteView note) && note != null)
                note.SetLabel(text);
        }

        /// <summary>Changes the recipe picture later (when recipes exist).</summary>
        public void SetRecipe(int id, Sprite recipe)
        {
            if (_notes.TryGetValue(id, out OrderNoteView note) && note != null)
                note.SetRecipe(recipe);
        }

        public void RemoveOrder(int id)
        {
            if (_notes.Remove(id, out OrderNoteView note) && note != null)
                Destroy(note.gameObject);
        }

        public void Clear()
        {
            foreach (OrderNoteView note in _notes.Values)
            {
                if (note != null)
                    Destroy(note.gameObject);
            }

            _notes.Clear();
        }

        [ContextMenu("Debug/Add test order")]
        private void DebugAdd() => AddOrder(_debugDish, _debugRecipe);

        [ContextMenu("Debug/Remove oldest order")]
        private void DebugRemoveOldest()
        {
            foreach (int id in _notes.Keys)
            {
                RemoveOrder(id);
                return;
            }
        }

        [ContextMenu("Debug/Clear")]
        private void DebugClear() => Clear();
    }
}