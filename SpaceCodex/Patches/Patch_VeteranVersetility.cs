using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Properties.BaseGetter;
using Kingmaker.EntitySystem.Properties.Getters;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Kingmaker.EntitySystem.Properties.BaseGetter.PropertyContextAccessor;

#pragma warning disable CS0612 // Typ oder Element ist veraltet

namespace SpaceCodex.Patches
{
    [HarmonyPatch]
    public static class Patch_VeteranVersetility
    {
        //[HarmonyPatch(typeof(CheckAbilityAttackTypeGetter), nameof(CheckAbilityAttackTypeGetter.GetBaseValue))]
        //[HarmonyPatch(typeof(BlueprintAbility), nameof(BlueprintAbility.GetAttackType))]
        //[HarmonyPostfix]
        public static void Postfix1(CheckAbilityAttackTypeGetter __instance, ref int __result)
        {
            //AbilityData ability = PropertyContextAccessor.GetAbility(__instance);
            //if (ability is null)
            //{
            //    return 0;
            //}
            //if (__instance.Type == ability.Blueprint.AttackType)
            //{
            //    return 1;
            //}
            //if (__instance.Type == AttackAbilityType.Pattern && ability.Blueprint.PatternSettings != null)
            //{
            //    return 1;
            //}
            //return 0;

            //if (__result is null && __instance.NotOffensive is false)
            //{
            //    __result = AttackAbilityType.SingleShot;
            //    var comps = __instance.ComponentsArray;
            //    for (int i = 0; i < comps.Length; i++)
            //    {
            //        if (comps[i] is AbilityDeliverChain)
            //            __result = AttackAbilityType.Scatter;
            //        else if (comps[i] is AbilityTargetsInPattern or AbilityTargetsInPatternTrail or AbilityTargetsAround)
            //            __result = AttackAbilityType.Pattern;
            //    }
            //}
        }
    }
}
