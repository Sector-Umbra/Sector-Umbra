using System.Diagnostics;
using Content.Shared.Access.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Lock;


namespace Content.Shared._Umbra.Container;


public sealed class ClaimableContainerSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClaimableContainerComponent, LockToggledEvent>(OnActivated);

    }

    private void OnActivated(EntityUid uid, ClaimableContainerComponent component, ref LockToggledEvent args)
    {
        EntityUid user = (EntityUid)args.User!;

        foreach (var item in _handsSystem.EnumerateHeld(user))
        {
            if (user == null)
            {
                Log.Debug("User is Null, Something went wrong.");
                return;
            }

            if (_accessReader.IsAllowed(user, uid))
            {
                Log.Debug(user + " " + uid + " claimed");
            }
        }

//        // maybe its inside an inventory slot?
//        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
//        {
//            items.Add(idUid.Value);
//        }
//
//        return items.Any();
    }

//    private void ClaimContainer(target)
//    {
//
//    }
}

