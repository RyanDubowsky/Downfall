using Gremlins.GremlinsCode.Powers;

namespace Gremlins.GremlinsCode.Events;

internal interface IModifyWizExtraDamage
{
    decimal ModifyWizExtraDamage(WizPower wiz, decimal extraDamage);
}