using Game.Items;
using Game.Interaction;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// Utilite for shuffling buttons on site 
    /// </summary>
    public class SiteButtonRandomizer : MonoBehaviour
    {
        [SerializeField] private List<Button> buttonsToShuffle;
        
        public void Randomize()
        {
            if (buttonsToShuffle == null)
            {
                Debug.LogError("button List is empty!");
                return;
            }
            List<Vector3> positions = new List<Vector3>();
            foreach (Button btn in buttonsToShuffle)
            {
                positions.Add(btn.transform.position);
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
                buttonsToShuffle[i].transform.position = positions[i];
            }

        }        
    }
}