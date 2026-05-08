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
        state.SavedHp[state.ActiveIndex] = target.CurrentHp;

        var activePet = state.Active;
        if (activePet != null)
        {
            activePet.SetMaxHpInternal(target.MaxHp);
            activePet.SetCurrentHpInternal(target.CurrentHp);
        }
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
        var next = state.GetNextLivingIndex();
        if (next < 0) return true;

        state.SavedHp[state.ActiveIndex] = 0;
        GremlinsCmd.KillGremlin(player.Creature, state.ActiveIndex);
        state.ActiveIndex = next;
        GremlinsCmd.SwitchGremlin(player.Creature, next);
        player.Creature.SetMaxHpInternal(state.SavedMaxHp[next]);
        player.Creature.SetCurrentHpInternal(state.SavedHp[next]);

        __result = Task.CompletedTask; // ← prevent null Task await
        return false;
    }
}