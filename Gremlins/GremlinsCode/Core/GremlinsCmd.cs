using Gremlins.GremlinsCode.Events;
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
    public static async Task SwitchGremlin(PlayerChoiceContext? ctx, Player player, Creature gremlin)
    {
        if (player.Creature.CombatState == null) return;
        var state = GremlinsRunModel.GetState(player);
        var node  = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (node?.Visuals is NGremlinsCreatureVisuals visuals)
            visuals.SwitchToGremlin(gremlin, state.Bench.Prepend(gremlin));
        if (ctx == null) return;
        await GremlinsHook.AfterGremlinSwap(player.Creature.CombatState, ctx, player);
    }
    
    public static void KillGremlin(Creature playerCreature, Creature gremlin)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(playerCreature);
        if (node?.Visuals is NGremlinsCreatureVisuals visuals)
            NGremlinsCreatureVisuals.KillGremlin(gremlin);
    }

    public static Creature? GetCurrentGremlin(Player? player) =>
        player == null ? null : GremlinsRunModel.GetState(player).Active;

    private static async Task<Creature?> SelectGremlin(PlayerChoiceContext ctx, Player player)
    {
        var state = GremlinsRunModel.GetState(player);
        var bench = state.Bench.ToList();

        if (bench.Count == 0) return null;
        if (bench.Count == 1) return bench[0];

        var choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        await ctx.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);

        Creature? chosen;
        if (LocalContext.IsMe(player))
        {
            var overlay = NGremlinSelectOverlay.Create(bench);
            NOverlayStack.Instance!.Push(overlay);
            overlay.ZIndex = 10;
            var slot = await overlay.AwaitSelection();
            NOverlayStack.Instance.Remove(overlay);
            chosen = bench[slot];
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                player, choiceId, PlayerChoiceResult.FromIndex(slot));
        }
        else
        {
            var slot = (await RunManager.Instance.PlayerChoiceSynchronizer
                .WaitForRemoteChoice(player, choiceId)).AsIndex();
            chosen = slot < 0 ? null : bench[slot];
        }

        await ctx.SignalPlayerChoiceEnded();
        return chosen;
    }

    public static async Task SwapToSelected(PlayerChoiceContext ctx, Player player)
    {
        var gremlin = await SelectGremlin(ctx, player);
        if (gremlin == null) return;
        await Swap(ctx, player, gremlin);
    }

    public static async Task SwapToNext(PlayerChoiceContext ctx, Player player)
    {
        var state = GremlinsRunModel.GetState(player);
        if (state.Next == null) return;
        await Swap(ctx, player, state.Next);
    }

    public static async Task SwapToType<T>(PlayerChoiceContext ctx, Player player)
        where T : GremlinsMonsterModel
    {
        var target = GremlinsRunModel.GetState(player).Bench
            .FirstOrDefault(g => g.Monster is T);
        if (target == null) return;
        await Swap(ctx, player, target);
    }

    public static async Task SwapToRandom(PlayerChoiceContext ctx, Player player)
    {
        var bench = GremlinsRunModel.GetState(player).Bench.ToList();
        if (bench.Count == 0) return;
        await Swap(ctx, player, bench[Rng.Chaotic.NextInt(bench.Count)]);
    }

    public static List<Creature> GetLivingGremlins(Player player) =>
        GremlinsRunModel.GetState(player).Bench.ToList();

    public static int GetLivingGremlinCount(Player player)
    {
        var state = GremlinsRunModel.GetState(player);
        return (state.Active != null ? 1 : 0) + state.Bench.Count();
    }

    public static Creature? ResurrectRandomGremlin(Player player, int hp)
    {
        var state = GremlinsRunModel.GetState(player);
        var dead  = state.Gremlins
            .Where(g => state.HpOf(g).Hp <= 0)
            .ToList();
        if (dead.Count == 0) return null;

        var gremlin = dead[Rng.Chaotic.NextInt(dead.Count)];
        var maxHp   = Math.Max(state.HpOf(gremlin).MaxHp, hp);
        state.Revive(gremlin, hp, maxHp);
        gremlin.SetCurrentHpInternal(hp);

        var node = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (node?.Visuals is NGremlinsCreatureVisuals visuals)
            visuals.ReviveGremlin(gremlin);

        return gremlin;
    }

    public static async Task SwapToGremlin(PlayerChoiceContext ctx, Player player, Creature gremlin)
    {
        if (!GremlinsRunModel.GetState(player).Gremlins.Contains(gremlin)) return;
        await Swap(ctx, player, gremlin);
    }

    public static async Task TriggerGremlinBonus(PlayerChoiceContext ctx, Player player)
    {
        var gremlin = GetCurrentGremlin(player);
        if (gremlin?.Monster is not GremlinsMonsterModel monster) return;
        await monster.TriggerGremlinBonus(ctx, player);
    }

    private static async Task Swap(PlayerChoiceContext ctx, Player player, Creature target)
    {
        var state = GremlinsRunModel.GetState(player);
        state.SaveHp((int)player.Creature.CurrentHp);
        state.SwapTo(target);
        var (hp, maxHp) = state.HpOf(target);
        await SwitchGremlin(ctx, player, target);
        player.Creature.SetMaxHpInternal(maxHp);
        player.Creature.SetCurrentHpInternal(hp);
    }
}