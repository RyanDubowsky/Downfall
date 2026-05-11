using BaseLib.Abstracts;
using Gremlins.GremlinsCode.Vfx;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Gremlins.GremlinsCode.Core;

public class GremlinsRunModel() : CustomSingletonModel(false, true)
{
    private static readonly CustomMonsterModel[] FormModels =
    [
        ModelDb.Monster<ShieldGremlin>(),
        ModelDb.Monster<MadGremlin>(),
        ModelDb.Monster<FatGremlin>(),
        ModelDb.Monster<SneakGremlin>(),
        ModelDb.Monster<WizardGremlin>()
    ];

    private static readonly Dictionary<Player, GremlinState> _states = new();

    public static GremlinState GetState(Player player)
    {
        if (_states.TryGetValue(player, out var state)) return state;
        state = new GremlinState();
        _states[player] = state;
        return state;
    }

    public override Task BeforeCombatStart()
    {
        var combatState = CombatManager.Instance.DebugOnlyGetState();
        if (combatState == null) return Task.CompletedTask;

        foreach (var player in combatState.Players)
        {
            if (player.Character is not Gremlins) continue;

            var state = new GremlinState();
            _states[player] = state;

            if (player.PlayerCombatState == null) continue;

            foreach (var model in FormModels)
            {
                var mutable  = model.ToMutable();
                var creature = combatState.CreateCreature(mutable, CombatSide.Player, null);
                mutable.SetUpForCombat();
                creature.PetOwner = player;
                player.PlayerCombatState.AddPetInternal(creature);
                state.Register(creature, (int)creature.CurrentHp, (int)creature.MaxHp);
                NCombatRoom.Instance!.AddCreature(creature);
            }

            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
            if (creatureNode?.Visuals is NGremlinsCreatureVisuals visuals)
                visuals.ArrangeGremlins(state.Gremlins);
        }

        return Task.CompletedTask;
    }
}

public class GremlinState
{
    private static readonly Logger Log = GremlinsMainFile.Logger;

    private readonly LinkedList<Creature> _rotation = new();
    private readonly Dictionary<Creature, (int Hp, int MaxHp)> _hp = new();
    private readonly List<Creature> _gremlins = [];
    public Creature? Next => _rotation.First?.Next?.Value;
    public IReadOnlyList<Creature> Gremlins => _gremlins;
    public Creature? Active                 => _rotation.First?.Value;
    public IEnumerable<Creature> Bench      => _rotation.Skip(1);

    internal void Register(Creature gremlin, int hp, int maxHp)
    {
        ArgumentNullException.ThrowIfNull(gremlin);
        if (_hp.ContainsKey(gremlin)) throw new InvalidOperationException($"{gremlin} already registered.");
        _gremlins.Add(gremlin);
        _hp[gremlin] = (hp, maxHp);
        _rotation.AddLast(gremlin);
        Log.Info($"[GremlinState] Registered {gremlin} hp={hp} maxHp={maxHp} | rotation={RotationString()}");
    }

    internal void SwapTo(Creature target)
    {
        if (!_rotation.Contains(target)) throw new InvalidOperationException($"{target} is not alive.");
        if (target == Active) return;
        var current = _rotation.First!.Value;
        _rotation.RemoveFirst();
        _rotation.AddLast(current);
        _rotation.Remove(target);
        _rotation.AddFirst(target);
        Log.Info($"[GremlinState] SwapTo {target} | previous={current} | rotation={RotationString()}");
    }

    internal void Kill(Creature gremlin)
    {
        if (!_hp.ContainsKey(gremlin)) throw new ArgumentException($"Unknown gremlin {gremlin}.");
        _rotation.Remove(gremlin);
        _hp[gremlin] = (0, _hp[gremlin].MaxHp);
        Log.Info($"[GremlinState] Killed {gremlin} | rotation={RotationString()}");
    }

    internal void Revive(Creature gremlin, int hp, int maxHp)
    {
        if (!_hp.ContainsKey(gremlin)) throw new ArgumentException($"Unknown gremlin {gremlin}.");
        if (_rotation.Contains(gremlin)) throw new InvalidOperationException($"{gremlin} is already alive.");
        _hp[gremlin] = (hp, Math.Max(maxHp, hp));
        _rotation.AddLast(gremlin);
        Log.Info($"[GremlinState] Revived {gremlin} hp={hp} maxHp={maxHp} | rotation={RotationString()}");
    }

    internal void SaveHp(int hp)
    {
        if (Active == null) return;
        _hp[Active] = (hp, _hp[Active].MaxHp);
        Log.Info($"[GremlinState] SaveHp {Active} hp={hp}");
    }

    internal (int Hp, int MaxHp) HpOf(Creature gremlin) =>
        _hp.TryGetValue(gremlin, out var v) ? v : throw new ArgumentException($"Unknown gremlin {gremlin}.");

   

    public IEnumerable<(Creature Gremlin, int Hp, int MaxHp)> GetRotationState() =>
        _rotation.Select(g => (g, _hp[g].Hp, _hp[g].MaxHp));

    private string RotationString() =>
        string.Join(" -> ", _rotation.Select(g => $"{g}({_hp[g].Hp}/{_hp[g].MaxHp})"));
}