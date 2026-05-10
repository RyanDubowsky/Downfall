using Downfall.DownfallCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Gremlins.GremlinsCode.Events;

public class GremlinsHook
{
    
    public static Task AfterGremlinSwap(ICombatState cs, PlayerChoiceContext ctx, Player player)
    {
        return DownfallHook.Dispatch<IAfterGremlinSwap>(cs, ctx,
            m => m.AfterGremlinSwap(ctx, player));
    }
}