using Kingmaker.Controllers.Enums;
using Kingmaker.EntitySystem.Properties.BaseGetter;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SpaceCodex
{
    public class CheckAbilityAttackSpellGetter : PropertyGetter, PropertyContextAccessor.IAbility
    {
        public override string GetInnerCaption() => "SpaceCodex";

        public override int GetBaseValue()
        {
            var ability = this.PropertyContext.Ability;
            if (ability is null)
                return 0;

            if (ability.Blueprint.UsingInOverwatchArea != BlueprintAbility.UsingInOverwatchAreaType.WillCauseAttack)
                return 0;

            if ((ability.Blueprint.AbilityParamsSource & (WarhammerAbilityParamsSource.PsychicPower | WarhammerAbilityParamsSource.NavigatorPower)) != 0)
                return 1;
            
            return 0;
        }
    }
}
