using LitMotion;
using LitMotion.Extensions;
using System;
using UnityEngine;

namespace Game.Interaction
{
    public class WobbleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private float _duration = 4f;
        [SerializeField] private float _strength = 1f;

        public void Interact()
        {
            LMotion.Punch.Create(Vector3.one, Vector3.up * _strength, _duration)
                .BindToLocalScale(transform);
        }
    }
}