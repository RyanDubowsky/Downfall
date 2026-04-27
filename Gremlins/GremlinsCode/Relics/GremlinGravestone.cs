using BaseLib.Utils;
using Gremlins.GremlinsCode.Core;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Gremlins.GremlinsCode.Relics;

[Pool(typeof(GremlinsRelicPool))]
public class GremlinGravestone : GremlinsRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
}