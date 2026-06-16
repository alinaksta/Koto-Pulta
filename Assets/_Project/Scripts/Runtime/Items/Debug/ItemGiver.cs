using Game.Interaction;
using Game.Items;
using Game.Services;
using Itemworks.UnityEngine;
using UnityEngine;

public class ItemGiver : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _spawnLocation;
    [SerializeField] private ItemDefinitionAsset _item;

    private PhysicsItemService _physicsService;

    private void Awake()
    {
        _physicsService = ServiceLocator.Get<PhysicsItemService>();
    }

    public bool CanInteract(in InteractionContext context) => true;

    public void OnInteractionHeld(in InteractionContext context, float delta) { }

    public void OnInteractionStarted(in InteractionContext context)
    {
        var item = Item.FromId(_item.Id);
        if (item.HasValue)
            _physicsService.Spawn(_spawnLocation.position, Vector3.zero, item.Value);
    }

    public void OnInteractionStopped(in InteractionContext context) { }


}
