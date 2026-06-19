using LitMotion;
using LitMotion.Extensions;
using System;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Placeholder interactable for testing wobble reactions.
    /// </summary>
    public class WobbleInteractable : MonoBehaviour//, IInteractable
    {
        [SerializeField] private float _duration = 4f;
        [SerializeField] private float _strength = 1f;

        //public bool CanInteract(in InteractionContext context)
        //{
        //    return true;
        //}

        //public void OnInteractionHeld(float delta) { }

        //public void OnInteractionStarted()
        //{
        //    LMotion.Punch.Create(Vector3.one, Vector3.up * _strength, _duration)
        //        .BindToLocalScale(transform);
        //}

        //public void OnInteractionStopped() { }
    }
}
