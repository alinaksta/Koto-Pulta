using Game.Interaction;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Itemworks.UnityEngine;
using Game.Progression;
using Game.Services;

namespace Game.UI
{
    /// <summary>
    /// Utilite for shuffling buttons on site 
    /// </summary>
    public class SiteButtonRandomizer : MonoBehaviour
    {
        [SerializeField] private ComputerInteractable _computerInteractable;
        [SerializeField] private List<SiteItemButton> buttonsToShuffle;
        private int shiftId;

        private void OnEnable()
        {
            _computerInteractable.FocusStarted += HandleFocusStarted;
            shiftId = ServiceLocator.Get<ShiftService>().ShiftIndex;
        }

        private void OnDisable()
        {
            _computerInteractable.FocusStarted -= HandleFocusStarted;
        }

        private void HandleFocusStarted()
        {
            Randomize();
        }

        /// <summary>
        /// Randomizes the positions of the configured buttons.
        /// </summary>
        public void Randomize()
        {
            if (buttonsToShuffle == null)
            {
                Debug.LogError("button List is empty!");
                return;
            }
            List<Vector3> positions = new List<Vector3>();
            foreach (var btn in buttonsToShuffle)
            {
                positions.Add(btn._button.transform.position);
                int requiredTier = btn.RequiredTier();
                int unlockedTier = shiftId + 1;
                btn.SetUnlocked(requiredTier != -1 && requiredTier <= unlockedTier);
            }
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 temp = positions[i];
                int randomIndex = Random.Range(i, positions.Count);
                positions[i] = positions[randomIndex];
                positions[randomIndex] = temp;
            }

            for (int i = 0; i < buttonsToShuffle.Count; i++)
            {
                buttonsToShuffle[i]._button.transform.position = positions[i];
            }

        }        
    }
}
