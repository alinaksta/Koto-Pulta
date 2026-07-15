using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Progression
{
    /// <summary>
    /// Identifies a completion signal consumed by the active tutorial step.
    /// </summary>
    public enum TutorialSignal
    {
        None,
        Moved,
        PickedUpObject,
        DroppedObject,
        ThrewWaiter,
        VisitedComputerTabs,
        ExitedComputer,
        StartedPracticeShift,
        WaiterAskedCustomer,
        GaveMealToWaiter,
        WaiterLanded,
        CustomerServed
    }

    /// <summary>
    /// Stores dialogue and Inspector callbacks for one tutorial step.
    /// </summary>
    [Serializable]
    public sealed class TutorialStep
    {
        [SerializeField, TextArea(2, 5)] private string[] _lines = Array.Empty<string>();
        [SerializeField] private UnityEvent _onStarted = new();
        [SerializeField] private UnityEvent _onCompleted = new();

        public TutorialStep(params string[] lines)
        {
            _lines = lines ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets the dialogue lines displayed by this step.
        /// </summary>
        public IReadOnlyList<string> Lines => _lines;

        internal void InvokeStarted() => _onStarted.Invoke();

        internal void InvokeCompleted() => _onCompleted.Invoke();
    }
}
