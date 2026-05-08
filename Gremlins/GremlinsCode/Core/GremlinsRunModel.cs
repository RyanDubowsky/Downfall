using BaseLib.Abstracts;
using Downfall.DownfallCode.Nodes;
using Gremlins.GremlinsCode.Vfx;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Gremlins.GremlinsCode.Core;

public class GremlinsRunModel() : CustomSingletonModel(false, true)
{
    private static readonly CustomMonsterModel[] FormModels =
    [
        ModelDb.Monster<ShieldGremlin>(),
        ModelDb.Monster<AngryGremlin>(),
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

    public static List<Creature>? GetGremlins(Player player) =>
        _states.TryGetValue(player, out var s) ? s.Gremlins : null;

    public static int GetActiveIndex(Player player) =>
        _states.TryGetValue(player, out var s) ? s.ActiveIndex : 0;

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
                var mutable = model.ToMutable();
                var creature = combatState.CreateCreature(mutable, CombatSide.Player, null);
                mutable.SetUpForCombat();
                creature.PetOwner = player;
                player.PlayerCombatState.AddPetInternal(creature);
                state.Gremlins.Add(creature);
                NCombatRoom.Instance!.AddCreature(creature);
            }

            state.SavedHp    = state.Gremlins.Select(g => (int)g.CurrentHp).ToArray();
            state.SavedMaxHp = state.Gremlins.Select(g => (int)g.MaxHp).ToArray();

            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
            if (creatureNode?.Visuals is NGremlinsCreatureVisuals visuals)
                visuals.ArrangeGremlins(state.Gremlins);
        }

        return Task.CompletedTask;
    }

    public class GremlinState
    {
        public List<Creature> Gremlins { get; } = [];
        public int[] SavedHp    { get; set; } = [];
        public int[] SavedMaxHp { get; set; } = [];
        public int ActiveIndex  { get; set; }

        public Creature? Active => Gremlins.ElementAtOrDefault(ActiveIndex);

        public bool HasLivingGremlins => SavedHp.Any(hp => hp > 0);

        public int GetNextLivingIndex()
        {
            var count = Gremlins.Count;
            for (var i = 1; i < count; i++)
            {
                var next = (ActiveIndex + i) % count;
                if (SavedHp[next] > 0) return next;
            }
            return -1;
        }
    }
}