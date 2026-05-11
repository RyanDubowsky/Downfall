using Downfall.DownfallCode.Events;
using Gremlins.GremlinsCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Gremlins.GremlinsCode.Events;

public class GremlinsHook
{
    
    public static Task AfterGremlinSwap(ICombatState cs, PlayerChoiceContext ctx, Player player)
     =>  DownfallHook.Dispatch<IAfterGremlinSwap>(cs, ctx,
            m => m.AfterGremlinSwap(ctx, player));


    public static decimal ModifyWizExtraDamage(WizPower wizPower, int extraDamage)
     =>  DownfallHook.Aggregate<IModifyWizExtraDamage, decimal>(wizPower.CombatState,
         extraDamage, (m,k ) => m.ModifyWizExtraDamage(wizPower, k));
    
}
