using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Collections;
using System.Diagnostics;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Properties.BaseGetter;
using Kingmaker.EntitySystem.Properties;
using Shared;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using System.Runtime.CompilerServices;
using static Kingmaker.Modding.OwlcatModificationsWindow;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace SpaceCodex
{
    public static class Helper
    {
        private static SimpleBlueprint placeholder = new() { name = "Placeholder", AssetGuid = "00000000000000000000000000000000" };

        public static T Init<T>(this T element) where T : Element
        {
            element.name ??= "$placeholder$00000000000000000000000000000000";
            element.Owner ??= placeholder;
            return element;
        }

        public static T AddComponents<T>(this T parent, params BlueprintComponent[] components) where T : BlueprintScriptableObject
        {
            parent.ComponentsArray = Collection.Append(parent.ComponentsArray, components);

            int num = 0;
            foreach (var comp in parent.ComponentsArray)
            {
                comp.name ??= $"${comp.GetType().Name}${parent.AssetGuid}${num}";
                if (comp.OwnerBlueprint == null)
                    comp.OwnerBlueprint = parent;
                else if (comp.OwnerBlueprint != parent)
                    Main.logger.Warning($"Warning: reused BlueprintComponent {comp.name} which is attached to {comp.OwnerBlueprint} instead of {parent}");

                //foreach (var fi in comp.GetType().GetFields(TranspilerTool.BindingInstance))
                //{
                //    if (fi.FieldType.IsValueType)
                //        continue;
                //}
            }

            return parent;
        }

        public static AbilityRuleTriggerInitiator CreateAbilityRuleTriggerInitiator(Operation conditionAnd = Operation.And, PropertyTargetType targetType = PropertyTargetType.CurrentEntity)
        {
            var result = new AbilityRuleTriggerInitiator();
            result.Action = new();
            result.AssignOwnerAsTarget = true;
            result.Restrictions.Property = new();
            result.Restrictions.Property.Operation = conditionAnd == Operation.And ? PropertyCalculator.OperationType.BoolAnd : PropertyCalculator.OperationType.BoolOr;
            result.Restrictions.Property.TargetType = targetType;
            result.Restrictions.Property.Getters = [];
            return result;
        }

        public static AbilityTrigger Add(this AbilityTrigger trigger, PropertyGetter condition)
        {
            condition.Init();
            Collection.AppendAndReplace(ref trigger.Restrictions.Property.Getters, condition);
            return trigger;
        }

        public static AbilityTrigger Add(this AbilityTrigger trigger, GameAction action)
        {
            action.Init();
            Collection.AppendAndReplace(ref trigger.Action.Actions, action);
            return trigger;
        }

        public static Conditional CreateConditional(Operation operation)
        {
            var result = new Conditional().Init();
            result.ConditionsChecker = new();
            result.ConditionsChecker.Operation = operation;
            result.ConditionsChecker.Conditions = [];
            result.IfTrue = new();
            result.IfFalse = new();
            return result;
        }

        public static Conditional Add(this Conditional conditional, Condition condition)
        {
            Collection.AppendAndReplace(ref conditional.ConditionsChecker.Conditions, condition);
            return conditional;
        }

        public static ContextConditionHasFact CreateContextConditionHasFact(AnyRef fact, bool not = false)
        {
            var result = new ContextConditionHasFact().Init();
            result.m_Fact = fact;
            result.Not = not;
            return result;
        }

        public static Conditional IfTrue(this Conditional conditional, GameAction action)
        {
            action.Init();
            Collection.AppendAndReplace(ref conditional.IfTrue.Actions, action);
            return conditional;
        }

        public static Conditional IfFalse(this Conditional conditional, GameAction action)
        {
            action.Init();
            Collection.AppendAndReplace(ref conditional.IfFalse.Actions, action);
            return conditional;
        }

        public static ContextActionApplyBuff CreateContextActionApplyBuff(AnyRef buff)
        {
            var result = new ContextActionApplyBuff().Init(); // todo
            result.m_Buff = buff;
            result.BuffEndCondition = BuffEndCondition.CombatEnd;
            result.Permanent = true;
            result.DurationValue = new();
            result.DurationValue.DiceCountValue = new();
            result.DurationValue.BonusValue = new();
            result.ToCaster = false;
            result.AsChild = false;
            result.SameDuration = false;
            result.Ranks = null;
            result.ActionsOnApply = null;
            result.ActionsOnImmune = null;
            return result;
        }

        public static ContextActionRemoveBuff CreateContextActionRemoveBuff(AnyRef buff)
        {
            var result = new ContextActionRemoveBuff().Init(); // todo
            result.m_Buff = buff;
            return result;
        }
    }
}
