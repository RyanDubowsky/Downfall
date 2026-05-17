using Gremlins.GremlinsCode.Core;
using Gremlins.GremlinsCode.Events;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Gremlins.GremlinsCode.Powers;

public class EncorePower : GremlinsPowerModel, IModifyWizExtraDamage
{
    public decimal ModifyWizExtraDamage(WizPower wiz, decimal extraDamage)
    {
        return wiz.Owner == Owner ? extraDamage + Amount : extraDamage;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power == this) Owner.GetPower<WizPower>()?.UpdateExtraDamage();
        return Task.CompletedTask;
    }
}