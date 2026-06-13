using Game.Services;
using UnityEngine;

namespace Game.Lifecycle
{
    public sealed class BootstrapService : MonoBehaviour, IBootstrapService
    {
        public void Bootstrap()
        {
            ServiceLocator.Register<IBootstrapService>(this);
        }
    }
}