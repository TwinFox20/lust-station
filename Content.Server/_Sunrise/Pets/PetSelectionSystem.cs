using Content.Server._Sunrise.PlayerCache;
using Content.Shared._Sunrise.Pets;
using Content.Shared._Sunrise.PlayerCache;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Pets;

public sealed class PetSelectionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;
    [Dependency] private readonly PlayerCacheManager _playerCache = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PetSelectionPrototypeSelectedEvent>(OnPetSelectionSelected);
    }

    private void OnPetSelectionSelected(PetSelectionPrototypeSelectedEvent ev, EntitySessionEventArgs args)
    {
        if (!_prototypeManager.HasIndex<PetSelectionPrototype>(ev.SelectedPetSelection))
            return;

        if (_playerCache.TryGetCache(args.SenderSession.UserId, out var cache))
        {
            cache.Pet = ev.SelectedPetSelection;
            _playerCache.SetCache(args.SenderSession.UserId, cache);
            return;
        }
        cache = new PlayerCacheData();
        _playerCache.SetCache(args.SenderSession.UserId, cache);
    }
}
