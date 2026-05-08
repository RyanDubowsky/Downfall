using BaseLib.Utils;
using Gremlins.GremlinsCode.Cards.Token;
using Gremlins.GremlinsCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Gremlins.GremlinsCode.Cards.Common;

[Pool(typeof(GremlinsCardPool))]
public class Glimmer : GremlinsCardModel
{
    public Glimmer() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithUpgradingCardTip<Ward>();
    }

    // TODO: Implement
    protected override async Task PlayEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
    }
}