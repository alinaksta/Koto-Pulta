using Game.Interaction;
using Game.Items;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Cutting board: holds up to two items.
    /// Click with an item to put it down, click with an empty hand to take the last one back.
    /// Click with the stapler to combine prepared apples + prepared dough into an apple pie (it stays on the board).
    /// </summary>
    public sealed class BoardStation : ProtoStation
    {
        [SerializeField] private int _capacity = 2;
        [SerializeField] private float _itemHeight = 0.44f;
        [SerializeField] private float _bounceHeight = 0.04f;

        public ItemDefinitionAsset Stapler;
        public ItemDefinitionAsset FirstInput;
        public ItemDefinitionAsset SecondInput;
        public ItemDefinitionAsset Output;

        private readonly List<ItemDefinition> _items = new();
        private ItemSlotView[] _slots;

        public override void Setup(Transform visual)
        {
            base.Setup(visual);

            Bounds bounds = GetLocalBounds(visual, transform);
            bool alongX = bounds.size.x >= bounds.size.z;
            _slots = new ItemSlotView[_capacity];
            for (int i = 0; i < _capacity; i++)
            {
                // Spread the slots along the board's long side.
                float t = (i + 1f) / (_capacity + 1f);
                var local = new Vector3(
                    alongX ? Mathf.Lerp(bounds.min.x, bounds.max.x, t) : bounds.center.x,
                    bounds.max.y,
                    alongX ? bounds.center.z : Mathf.Lerp(bounds.min.z, bounds.max.z, t));
                _slots[i] = CreateSlot($"BoardItem{i}", transform, transform.TransformPoint(local), _itemHeight);
            }
        }

        protected override bool Interact(in InteractionContext context)
        {
            IContainer hand = context.ActiveHand;
            if (hand == null)
                return false;

            // Stapler: combine the ingredients on the board.
            if (Holds(hand, Stapler))
                return TryStaple();

            // Empty hand: take the last item back.
            if (hand.IsEmpty)
            {
                if (_items.Count == 0)
                    return false;

                ItemDefinition last = _items[_items.Count - 1];
                if (!GiveToHand(hand, last))
                    return false;

                _items.RemoveAt(_items.Count - 1);
                Refresh();
                return true;
            }

            // Full hand: put the item down.
            if (_items.Count >= _capacity)
                return false;

            _items.Add(TakeFromHand(hand));
            Refresh();
            return true;
        }

        private bool TryStaple()
        {
            int first = _items.FindIndex(item => Is(item, FirstInput));
            int second = _items.FindIndex(item => Is(item, SecondInput));
            if (first < 0 || second < 0)
                return false;

            ItemDefinition result = Definition(Output);
            if (result == null)
                return false;

            _items.RemoveAt(Mathf.Max(first, second));
            _items.RemoveAt(Mathf.Min(first, second));
            RunBusy(StapleRoutine(result));
            return true;
        }

        private IEnumerator StapleRoutine(ItemDefinition result)
        {
            // Quick squash-and-pop of the ingredients, then the pie appears.
            const float squashTime = 0.12f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / squashTime)
            {
                foreach (var slot in _slots)
                    slot.SetSquash(new Vector2(1f + 0.3f * t, 1f - 0.5f * t));
                yield return null;
            }

            _items.Insert(0, result);
            Refresh();

            Vector3 start = Visual.localPosition;
            const float bounceTime = 0.2f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / bounceTime)
            {
                float height = Mathf.Sin(t * Mathf.PI) * _bounceHeight;
                Visual.localPosition = start + Visual.parent.InverseTransformVector(Vector3.up * height);
                yield return null;
            }

            Visual.localPosition = start;
        }

        private void Refresh()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i < _items.Count)
                    _slots[i].Show(SpriteOf(_items[i]));
                else
                    _slots[i].Hide();
            }
        }
    }
}
