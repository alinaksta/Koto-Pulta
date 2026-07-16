using Game.Items;
using Game.Interaction;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Game.Services;

namespace Game.UI
{
    public class SiteActivator: MonoBehaviour
    {
        private ComputerInteractable _computer;
        [SerializeField] private List<GameObject> sites;
        private int activeSite;
        
        private void Start()
        {
            _computer = GetComponent<ComputerInteractable>();
            _computer.FocusEnded += OnFocusEnded;
            _computer.FocusStarted += OnFocusStartes;
            if (sites.Count == 0)
            {
                Debug.LogError("No Sites Added");
                return;
            }
            for (int i = 0; i < sites.Count; i++){
                sites[i].SetActive(false);
            }
        }

        public void OnFocusStartes()
        {
            if (sites.Count == 0)
            {
                Debug.LogError("No Sites Added");
                return;
            }
            activeSite = Random.Range(0, sites.Count);
            sites[activeSite].SetActive(true);
            sites[activeSite].GetComponent<ScrollRect>().verticalNormalizedPosition=1f;
            sites[activeSite].GetComponent<SiteButtonRandomizer>().Randomize();
        }    

        public void OnFocusEnded()
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