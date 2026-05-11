using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Gremlins.GremlinsCode.Core;

public class GremlinsModel() : CustomSingletonModel(true, false)
{
    public override async Task AfterDamageReceived(
        PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var player = target.Player;
        if (player?.Character is not Gremlins) return;

        var state = GremlinsRunModel.GetState(player);
        if (state.Active == null) return;

        state.SaveHp(target.CurrentHp);
        state.Active.SetMaxHpInternal(target.MaxHp);
        state.Active.SetCurrentHpInternal(target.CurrentHp);
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.KillWithoutCheckingWinCondition))]
public static class PatchGremlinDeath
{
    static bool Prefix(Creature creature, bool force, ref Task __result)
    {
        var player = creature.Player;
        if (player?.Character is not Gremlins) return true;

        var state = GremlinsRunModel.GetState(player);
        var dying = state.Active;
        if (dying == null) return true;

        state.Kill(dying);

        var next = state.Active;
        if (next == null) return true;

        __result = RunAsync(player, state, dying, next);
        return false;
    }

    static async Task RunAsync(
        Player player, GremlinState state,
        Creature dying, Creature next)
    {
        var (hp, maxHp) = state.HpOf(next);
        await GremlinsCmd.SwitchGremlin(null, player, next);
        player.Creature.SetMaxHpInternal(maxHp);
        player.Creature.SetCurrentHpInternal(hp);

        GremlinsCmd.KillGremlin(player.Creature, dying);
    }
}