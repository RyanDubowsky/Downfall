using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Gremlins.GremlinsCode.Cards;

public abstract class GremlinsCardModel(
    int cost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool showInCardLibrary = true,
    bool autoAdd = true)
    : DownfallCardModel<Core.Gremlins>(cost, type, rarity, targetType, showInCardLibrary, autoAdd);