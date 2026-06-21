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
    public class SiteActivator: MonoBehaviour
    {
        [SerializeField] private List<GameObject> sites;
        private int activeSite;
        
        private void Start()
        {
            if (sites.Count == 0)
            {
                Debug.LogError("No Sites Added");
                return;
            }
            for (int i = 0; i < sites.Count; i++){
                sites[i].SetActive(false);
            }
        }

        public void StartInteraction()
        {
            if (sites.Count == 0)
            {
                Debug.LogError("No Sites Added");
                return;
            }
            activeSite = Random.Range(0, sites.Count);
            sites[activeSite].SetActive(true);
            sites[activeSite].GetComponent<ScrollRect>().verticalNormalizedPosition=1f;
            sites[activeSite].GetComponent<SiteButtonRandomizer>().OnInteraction();
        }    

        public void EndInteraction()
        {
            if (sites.Count == 0)
            {
                Debug.LogError("No Sites Added");
                return;
            }
            sites[activeSite].SetActive(false);
        }
    }
}