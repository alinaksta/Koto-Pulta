using Game.Services;
using Itemworks.UnityEngine;
using UnityEngine;

namespace Game.Items
{
    public class PhysicsItemSpawner : MonoBehaviour
    {
        [SerializeField] private ItemDefinitionAsset _asset;

        private void Start()
        {
            var service = ServiceLocator.Get<PhysicsItemService>();

            var item = Item.FromId(_asset.Id);

            if (item.HasValue)
                service.Spawn(transform.position, Vector3.zero, item.Value);
        }
    }
}
