using Game.Services;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Lifecycle
{
    public interface IBootstrapService : IBootstrapable
    {

    }

    public static class Bootstraper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (ServiceLocator.TryGet<IBootstrapService>(out _))
                return;

            var prefab = Resources.Load<GameObject>("ServiceRoot");

            if (prefab == null)
            {
                Debug.LogError("Missing Resources/ServiceRoot prefab.");
                return;
            }

            if (prefab.GetComponent<IBootstrapService>() == null)
            {
                Debug.LogError("The ServiceRoot prefab must have an IBootstrapService on it.");
                return;
            }

            var instance = Object.Instantiate(prefab);
            instance.name = "[Services]";
            Object.DontDestroyOnLoad(instance);

            BootstrapChildren(instance);
        }

        private static void BootstrapChildren(GameObject instance)
        {
            // Bootstrap components
            foreach (IBootstrapable boot in instance.GetComponents<IBootstrapable>())
            {
                boot.Bootstrap();
            }

            // Bootstrap children and their components
            foreach (Transform child in instance.transform)
            {
                foreach (IBootstrapable boot in child.GetComponents<IBootstrapable>())
                {
                    boot.Bootstrap();
                }
            }
        }
    }
}