using Game.Services;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Lifecycle
{
    public interface IBootstrapService 
    {

    }

    public sealed class BootstrapService : MonoBehaviour, IBootstrapService
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


            var instance = Object.Instantiate(prefab);
            instance.name = "[Services]";
            Object.DontDestroyOnLoad(instance);

            var bootstrapService = prefab.GetOrAddComponent<BootstrapService>();

            // Bootstrap service is a marker for identifying if bootstrap was succesful
            ServiceLocator.Register<IBootstrapService>(bootstrapService);

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