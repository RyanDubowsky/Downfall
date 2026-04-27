using BaseLib.Utils;
using Gremlins.GremlinsCode.Core;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Gremlins.GremlinsCode.Relics;

[Pool(typeof(GremlinsRelicPool))]
public class ShortStatue : GremlinsRelicModel
{
    // TODO - Boss relic
    public override RelicRarity Rarity => RelicRarity.Ancient;
}