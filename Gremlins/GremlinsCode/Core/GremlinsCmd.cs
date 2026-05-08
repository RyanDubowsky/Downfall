using Godot;
using Gremlins.GremlinsCode.Vfx;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace Gremlins.GremlinsCode.Core;

public static class GremlinsCmd
{
    public static void SwitchGremlin(Creature creature, int index)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (node?.Visuals is NGremlinsCreatureVisuals visuals)
            visuals.SwitchToGremlin(index);
    }

    public static void KillGremlin(Creature creature, int index)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (node?.Visuals is NGremlinsCreatureVisuals visuals)
            visuals.KillGremlin(index);
    }
    
    public static Creature? GetCurrentGremlin(Player? player)
    {
        if (player == null) return null;
        var state = GremlinsRunModel.GetState(player);
        return state.Active;
    }

    public static async Task<int> SelectGremlin(PlayerChoiceContext ctx, Player player)
    {
        var state = GremlinsRunModel.GetState(player);
        var living = state.Gremlins
            .Select((g, i) => (g, i))
            .Where(x => state.SavedHp[x.i] > 0 && x.i != state.ActiveIndex)
            .Select(x => x.g)
            .ToList();

        if (living.Count == 0) return -1;
        if (living.Count == 1) return state.Gremlins.IndexOf(living[0]);

        var choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        await ctx.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);

        int chosenIndex;
        if (LocalContext.IsMe(player))
        {
            var overlay = NGremlinSelectOverlay.Create(living);
            NOverlayStack.Instance!.Push(overlay);
            overlay.ZIndex = 10;
            var slot = await overlay.AwaitSelection();
            NOverlayStack.Instance.Remove(overlay);
            chosenIndex = state.Gremlins.IndexOf(living[slot]);
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                player, choiceId, PlayerChoiceResult.FromIndex(slot));
        }
        else
        {
            var slot = (await RunManager.Instance.PlayerChoiceSynchronizer
                .WaitForRemoteChoice(player, choiceId)).AsIndex();
            chosenIndex = slot < 0 ? -1 : state.Gremlins.IndexOf(living[slot]);
        }

        await ctx.SignalPlayerChoiceEnded();
        return chosenIndex;
    }


    public static async Task SwapToSelected(PlayerChoiceContext ctx, Player player)
    {
        var index = await SelectGremlin(ctx, player);
        if (index < 0) return;
        SwapToIndex(player, index);
    }
    
    public static void SwapToNext(Player player)
    {
        var state = GremlinsRunModel.GetState(player);
        var next = state.GetNextLivingIndex();
        if (next < 0) return;
        SwapToIndex(player, next);
    }
    
    
    public static void SwapToGremlinType<T>(Player player) where T : GremlinsMonsterModel
    {
        var state = GremlinsRunModel.GetState(player);
        var index = -1;
        for (var i = 0; i < state.Gremlins.Count; i++)
        {
            if (state.Gremlins[i].Monster is not T || state.SavedHp[i] <= 0) continue;
            index = i;
            break;
        }
        if (index < 0) return;
        SwapToIndex(player, index);
    }

    
     public static void SwapToRandomGremlin(Player player)
    {
        var state = GremlinsRunModel.GetState(player);
        var candidates = Enumerable.Range(0, state.Gremlins.Count)
            .Where(i => i != state.ActiveIndex && state.SavedHp[i] > 0)
            .ToList();
        if (candidates.Count == 0) return;
        var index = candidates[Rng.Chaotic.NextInt(candidates.Count)];
        SwapToIndex(player, index);
    }

    // -- swap to player-chosen gremlin (for Tag Team, Gremlin Arms) --
    // call this then await the selection result
    public static List<Creature> GetLivingGremlins(Player player)
    {
        var state = GremlinsRunModel.GetState(player);
        return state.Gremlins
            .Where((g, i) => state.SavedHp[i] > 0 && i != state.ActiveIndex)
            .ToList();
    }

    public static void SwapToGremlin(Player player, Creature gremlin)
    {
        var state = GremlinsRunModel.GetState(player);
        var index = state.Gremlins.IndexOf(gremlin);
        if (index < 0) return;
        SwapToIndex(player, index);
    }

    // -- resurrect a random dead gremlin with given HP --
    public static Creature? ResurrectRandomGremlin(Player player, int hp)
    {
        var state = GremlinsRunModel.GetState(player);
        var dead = Enumerable.Range(0, state.Gremlins.Count)
            .Where(i => state.SavedHp[i] <= 0)
            .ToList();
        if (dead.Count == 0) return null;

        var index = dead[Rng.Chaotic.NextInt(dead.Count)];
        state.SavedHp[index]    = hp;
        state.SavedMaxHp[index] = Math.Max(state.SavedMaxHp[index], hp);

        // Sync onto the pet creature too
        var gremlin = state.Gremlins[index];
        gremlin.SetCurrentHpInternal(hp);

        // Re-show the gremlin visually
        var node = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (node?.Visuals is NGremlinsCreatureVisuals visuals)
            visuals.ReviveGremlin(index);

        return gremlin;
    }

    // -- get living gremlin count (for Raid, Revel) --
    public static int GetLivingGremlinCount(Player player)
    {
        var state = GremlinsRunModel.GetState(player);
        return state.SavedHp.Count(hp => hp > 0);
    }

    // -- shared swap logic --
    private static void SwapToIndex(Player player, int index)
    {
        var state = GremlinsRunModel.GetState(player);
        state.SavedHp[state.ActiveIndex] = (int)player.Creature.CurrentHp;
        state.ActiveIndex = index;
        SwitchGremlin(player.Creature, index);
        player.Creature.SetMaxHpInternal(state.SavedMaxHp[index]);
        player.Creature.SetCurrentHpInternal(state.SavedHp[index]);
    }

    public static async Task TriggerGremlinBonus(PlayerChoiceContext ctx, Player player)
    {
        var gremlin = GetCurrentGremlin(player);
        if (gremlin?.Monster is not GremlinsMonsterModel monster) return;
        await monster.TriggerGremlinBonus(ctx, player);
    }
}