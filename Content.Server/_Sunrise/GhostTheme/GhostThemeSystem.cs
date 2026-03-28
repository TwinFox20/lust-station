using System.Linq;
using Content.Shared._Sunrise.GhostTheme;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Content.Server._Sunrise.PlayerCache;
using Content.Shared._Sunrise.PlayerCache;

namespace Content.Server._Sunrise.GhostTheme;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PlayerCacheManager _playerCache = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<GhostComponent, GhostThemeActionEvent>(OnGhostThemeChange);
        SubscribeLocalEvent<GhostComponent, GhostThemePrototypeSelectedMessage>(OnGhostThemeSelected);
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, GhostComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(user, out ActorComponent? actor))
            return;

        _uiSystem.TryToggleUi(uid, GhostThemeUiKey.Key, actor.PlayerSession);
        UpdateUi(uid, actor.PlayerSession, component);
    }

    private void OnGhostThemeChange(EntityUid uid, GhostComponent observerComponent, GhostThemeActionEvent args)
    {
        TryOpenUi(uid, args.Performer, observerComponent);
        args.Handled = true;
    }

    private void OnGhostThemeSelected(Entity<GhostComponent> ent, ref GhostThemePrototypeSelectedMessage msg)
    {
        if (!TryComp(msg.Actor, out ActorComponent? actorComp))
            return;

        if (!_prototypeManager.TryIndex<GhostThemePrototype>(msg.SelectedGhostTheme, out var ghostThemePrototype))
            return;

        if (_playerCache.TryGetCache(actorComp.PlayerSession.UserId, out var cache))
        {
            cache.GhostTheme = ghostThemePrototype.ID;
            _playerCache.SetCache(actorComp.PlayerSession.UserId, cache);
        }
        else
        {
            cache = new PlayerCacheData();
            _playerCache.SetCache(actorComp.PlayerSession.UserId, cache);
        }
        var ghostTheme = EnsureComp<GhostThemeComponent>(ent);
        ghostTheme.GhostTheme = msg.SelectedGhostTheme;
        Dirty(ent, ghostTheme);
    }

    private void UpdateUi(EntityUid uid, ICommonSession session, GhostComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var allGhostThemes = _prototypeManager
            .EnumeratePrototypes<GhostThemePrototype>()
            .Select(ghostTheme => new GhostThemeInfo(ghostTheme.ID, true))
            .ToList();
        var state = new GhostThemeBoundUserInterfaceState(allGhostThemes);

        _uiSystem.SetUiState(uid, GhostThemeUiKey.Key, state);
    }

    private void OnPlayerAttached(EntityUid uid, GhostComponent component, PlayerAttachedEvent args)
    {
        if (!_playerCache.TryGetCachedGhostTheme(args.Player.UserId, out var ghostTheme))
            return;

        if (!_prototypeManager.TryIndex<GhostThemePrototype>(ghostTheme, out _))
            return;

        EnsureComp<GhostThemeComponent>(uid).GhostTheme = ghostTheme;
    }
}
