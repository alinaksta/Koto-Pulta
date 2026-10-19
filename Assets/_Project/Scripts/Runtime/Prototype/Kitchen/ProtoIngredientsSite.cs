using Game.Interaction;
using Game.Items;
using Game.Items.Properties;
using Game.UI;
using Itemworks.UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// "Ingredients" website on the computer, built at runtime in place of the meals site.
    /// The computer is not zoomed into: the site stays on, the player looks at an ingredient
    /// (crosshair over its card) and clicks with the hand that should take it.
    /// </summary>
    public sealed class ProtoIngredientsSite : MonoBehaviour
    {
        private static readonly Color CardColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color CardHoverColor = new Color(1f, 0.85f, 0.45f, 1f);

        private SiteActivator _computer;
        private ComputerController _controller;
        private ComputerInteractable _interactable;
        private DualHandInteractor _player;
        private readonly List<(RectTransform rect, Image image, string id)> _cards = new();

        /// <summary>
        /// Builds the site next to the meals site of <paramref name="computer"/>.
        /// </summary>
        public static ProtoIngredientsSite Create(SiteActivator computer, IReadOnlyList<ItemDefinitionAsset> ingredients, string title)
        {
            if (!computer.TryGetTabRoot(ComputerSiteTab.Meals, out RectTransform mealsRoot))
            {
                Debug.LogWarning($"[{nameof(ProtoIngredientsSite)}] The computer has no meals site to replace.", computer);
                return null;
            }

            var go = new GameObject("IngredientsSite", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(mealsRoot.parent, false);
            rect.SetSiblingIndex(mealsRoot.GetSiblingIndex() + 1);
            rect.anchorMin = mealsRoot.anchorMin;
            rect.anchorMax = mealsRoot.anchorMax;
            rect.pivot = mealsRoot.pivot;
            rect.sizeDelta = mealsRoot.sizeDelta;
            rect.anchoredPosition = mealsRoot.anchoredPosition;
            rect.localScale = mealsRoot.localScale;
            rect.localRotation = mealsRoot.localRotation;
            go.layer = mealsRoot.gameObject.layer;

            var site = go.AddComponent<ProtoIngredientsSite>();
            site._computer = computer;
            site._controller = computer.GetComponentInParent<ComputerController>(true);
            if (site._controller == null)
                site._controller = computer.GetComponentInChildren<ComputerController>(true);
            if (site._controller == null)
                site._controller = FindAnyObjectByType<ComputerController>(FindObjectsInactive.Include);

            TMP_FontAsset font = mealsRoot.GetComponentInChildren<TMP_Text>(true)?.font;
            Color background = mealsRoot.TryGetComponent(out Image mealsBackground) ? mealsBackground.color : new Color(0.96f, 0.93f, 0.85f);
            site.Build(ingredients, title, font, background);

            // No zooming into the computer: the screen stays on and is used by looking at it.
            site._interactable = computer.GetComponentInParent<ComputerInteractable>(true);
            if (site._interactable == null)
                site._interactable = computer.GetComponentInChildren<ComputerInteractable>(true);
            if (site._interactable != null)
            {
                site._interactable.FocusEnabled = false;
                site._interactable.DirectInteraction += site.HandleDirectInteraction;
            }

            go.SetActive(true);
            return site;
        }

        private void OnDestroy()
        {
            if (_interactable != null)
                _interactable.DirectInteraction -= HandleDirectInteraction;
        }

        private void LateUpdate()
        {
            // Keep the site visible even though the computer is never "entered".
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            int hovered = IsPlayerLookingAtComputer() ? FindCardUnderCrosshair() : -1;
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].image.color = i == hovered ? CardHoverColor : CardColor;
        }

        private void HandleDirectInteraction(InteractionContext context)
        {
            int index = FindCardUnderCrosshair();
            if (index < 0 || context.ActiveHand == null || !context.ActiveHand.IsEmpty)
                return;

            Item? item = Item.FromId(_cards[index].id);
            if (item.HasValue)
                context.ActiveHand.Insert(item.Value);
        }

        private bool IsPlayerLookingAtComputer()
        {
            if (_player == null)
                _player = FindAnyObjectByType<DualHandInteractor>();

            if (_player == null || _interactable == null || _player.HoveredObject == null)
                return false;

            Transform hovered = _player.HoveredObject.transform;
            return hovered == _interactable.transform || hovered.IsChildOf(_interactable.transform) ||
                   _interactable.transform.IsChildOf(hovered);
        }

        private int FindCardUnderCrosshair()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return -1;

            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            for (int i = 0; i < _cards.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_cards[i].rect, center, camera))
                    return i;
            }

            return -1;
        }

        private void Build(IReadOnlyList<ItemDefinitionAsset> ingredients, string title, TMP_FontAsset font, Color background)
        {
            gameObject.AddComponent<Image>().color = background;

            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 20, 30);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text header = CreateText("Title", transform, title, font, 60f);
            AddLayout(header.gameObject, minHeight: 80f, flexibleHeight: 0f);

            var row = new GameObject("Ingredients", typeof(RectTransform));
            row.transform.SetParent(transform, false);
            AddLayout(row, minHeight: 100f, flexibleHeight: 1f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 30f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            foreach (ItemDefinitionAsset ingredient in ingredients)
            {
                if (ingredient != null)
                    CreateCard(row.transform, ingredient, font);
            }
        }

        private void CreateCard(Transform parent, ItemDefinitionAsset ingredient, TMP_FontAsset font)
        {
            var card = new GameObject(ingredient.name, typeof(RectTransform));
            card.transform.SetParent(parent, false);
            var cardImage = card.AddComponent<Image>();
            cardImage.color = CardColor;
            _cards.Add(((RectTransform)card.transform, cardImage, ingredient.Id));

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var icon = new GameObject("Icon", typeof(RectTransform));
            icon.transform.SetParent(card.transform, false);
            var iconImage = icon.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.sprite = GetSprite(ingredient);
            AddLayout(icon, minHeight: 80f, flexibleHeight: 1f);

            TMP_Text label = CreateText("Label", card.transform, ingredient.name.Replace("Proto_", ""), font, 40f);
            label.color = new Color(0.15f, 0.12f, 0.1f);
            AddLayout(label.gameObject, minHeight: 50f, flexibleHeight: 0f);
        }

        private static Sprite GetSprite(ItemDefinitionAsset asset)
        {
            Item? item = Item.FromId(asset.Id);
            if (item.HasValue && item.Value.Definition.TryGetProperty<FoodProperty>(out var food))
                return food.WorldSprite;
            return null;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, TMP_FontAsset font, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                label.font = font;
            label.text = text;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.15f, 0.12f, 0.1f);
            label.raycastTarget = false;
            return label;
        }

        private static void AddLayout(GameObject go, float minHeight, float flexibleHeight)
        {
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = minHeight;
            element.preferredHeight = minHeight;
            element.flexibleHeight = flexibleHeight;
        }
    }
}
