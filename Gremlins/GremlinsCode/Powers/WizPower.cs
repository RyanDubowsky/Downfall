using Gremlins.GremlinsCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Gremlins.GremlinsCode.Powers;

public class WizPower : GremlinsPowerModel
{
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || Amount < 3 || Owner.HasPower<BangPower>()) return;
        await PowerCmd.Apply<BangPower>(ctx, Owner, 7, applier, cardSource);
    }
}