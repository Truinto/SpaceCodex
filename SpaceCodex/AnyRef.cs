using Code.GameCore.Blueprints;
using Kingmaker.AI.Blueprints;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Credits;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Fx;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.DLC;
using Kingmaker.Formations;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.RandomEncounters.Settings;
using Kingmaker.UI.Sound;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Visual.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.UnitLogic.Levelup.CharGen;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Spells;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints;
using Kingmaker.UnitLogic.Progression.Features;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Visual.HitSystem;
using Kingmaker.Visual.LightSelector;
using Kingmaker.Visual.Sound;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SpaceCodex
{
    /// <summary>
    /// Wrapper class that combines common references into one. Will implicitly cast into most types. Usually does not throw, but may return null.
    /// </summary>
    public class AnyRef : BlueprintReferenceBase
    {
        /// <summary>List of delayed actions to execute after blueprint is generated.</summary>
        public List<Action<BlueprintScriptableObject>> Actions;

        private BlueprintReferenceBase cache;

        /// <inheritdoc cref="AnyRef"/>
        public AnyRef()
        {
        }

        /// <inheritdoc cref="AnyRef"/>
        public AnyRef(Action<BlueprintScriptableObject> action)
        {
            AddDelayedPatch(action);
        }

        /// <summary>
        /// Property to access <see cref="Get()"/> and <see cref="Set(SimpleBlueprint)"/>
        /// </summary>
        public BlueprintScriptableObject Value { get => this; set => Set(value); }

        /// <summary>
        /// Add a delayed action. Will execute immedately if blueprint is already loaded. Otherwise will wait.
        /// </summary>
        public void AddDelayedPatch(Action<BlueprintScriptableObject> action)
        {
            if (this.Cached is BlueprintScriptableObject obj)
                action(obj);
            else
                (Actions ??= []).Add(action);
        }

        /// <summary>
        /// Sets blueprint to reference. Will throw, if guid mismatch. Runs delayed actions, then clears them.
        /// </summary>
        public void Set(BlueprintScriptableObject bp)
        {
            SetReference(this, bp);

            if (Actions != null && this.Cached is BlueprintScriptableObject obj)
            {
                foreach (var action in Actions)
                    action(obj);
                Actions = null;
            }
        }

        private static void SetReference(BlueprintReferenceBase reference, BlueprintScriptableObject bp)
        {
            if (reference is null || bp is null)
            {
                var guid = bp?.AssetGuid ?? reference?.guid;
                Main.logger.Error($"SetReference argument is null for {guid}");
                return;
            }

            if (reference.guid != null && reference.guid != bp.AssetGuid)
                throw new InvalidOperationException($"tried to set blueprint reference to a new guid old={reference.guid} new={bp.AssetGuid}");

            reference.guid = bp.AssetGuid;
            reference.Cached = bp;
        }

        /// <summary>
        /// Returns blueprint.
        /// </summary>
        /// <remarks>
        /// This returns <see cref="BlueprintScriptableObject"/> instead of <see cref="SimpleBlueprint"/>, because almost all inherit the former. This allows access to Components without cast.<br/>
        /// If the blueprint does not inherit it, this returns null. In that case use Get&lt;SimpleBlueprint&gt;().
        /// </remarks>
        public BlueprintScriptableObject Get() => Get<BlueprintScriptableObject>();

        /// <summary>
        /// Returns blueprint. If the blueprint isn't <typeparamref name="T"/> or the reference is empty, then an error is printed and null returned.
        /// </summary>
        public T Get<T>()
        {
            var bp = this.Cached;
            if (bp == null)
            {
                bp = ResourcesLibrary.TryGetBlueprint(this.guid) as BlueprintScriptableObject;
                if (bp != null)
                    Set(bp);
            }

            if (bp is T t)
                return t;
            else if (bp == null)
                Main.logger.Error($"AnyRef could not resolve {this.guid}");
            else
                Main.logger.Error($"AnyRef {this.guid} is not {typeof(T)}, it is {bp.GetType()}");

            return default;
        }

        /// <summary>
        /// Converts to a BlueprintReference of <typeparamref name="T"/>. Does not validate.
        /// </summary>
        public T ToRef<T>() where T : BlueprintReferenceBase, new()
        {
            if (cache is not T t)
                cache = t = new T() { guid = this.guid };

            if (string.IsNullOrEmpty(t.guid))
                Main.logger.Error("AnyRef.ToRef was used without any GUID");

            return t;
        }

        /// <inheritdoc cref="Equals(object)"/>
        public bool Is(BlueprintReferenceBase bp) => bp != null && this.guid.Equals(bp.guid);

        /// <inheritdoc cref="Equals(object)"/>
        public bool Is(SimpleBlueprint bp) => bp != null && this.guid.Equals(bp.AssetGuid);

        /// <inheritdoc cref="Equals(object)"/>
        public bool Is(string bp) => this.guid == bp;

        /// <summary>returns as BlueprintAbility</summary>
        public BlueprintAbility AsAbility => Get<BlueprintAbility>();
        /// <summary>returns as BlueprintBuff</summary>
        public BlueprintBuff AsBuff => Get<BlueprintBuff>();
        /// <summary>returns as BlueprintActivatableAbility</summary>
        public BlueprintActivatableAbility AsActivatable => Get<BlueprintActivatableAbility>();
        /// <summary>returns as BlueprintFeature</summary>
        public BlueprintFeature AsFeature => Get<BlueprintFeature>();

        /// <inheritdoc cref="Get{T}()"/>
        public static T Get<T>(AnyRef bp) where T : SimpleBlueprint
        {
            return bp?.Get<T>();
        }

        /// <inheritdoc cref="ToRef{T}()"/>
        public static T ToRef<T>(AnyRef bp) where T : BlueprintReferenceBase, new()
        {
            return bp?.ToRef<T>();
        }

        /// <summary>
        /// Converts to AnyRef. <paramref name="obj"/> can be any blueprint, blueprint reference type, or guid string.
        /// </summary>
        public static AnyRef ToAny(object obj)
        {
            if (obj is AnyRef any)
                return any;
            if (obj is string str)
                return new AnyRef() { guid = str };
            if (obj is BlueprintReferenceBase bp)
                return new AnyRef() { guid = bp.guid, Cached = bp.Cached };
            if (obj is BlueprintScriptableObject sb)
                return new AnyRef() { guid = sb.AssetGuid, Cached = sb };
            Main.logger.Error($"AnyRef could not resolve type '{obj?.GetType()}'");
            return null;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is BlueprintReferenceBase bp)
                return this.guid == bp.guid;
            if (obj is string str)
                return this.guid == str;
            if (obj is SimpleBlueprint sb)
                return this.guid == sb.AssetGuid;
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

#pragma warning disable CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element

        public static implicit operator AnyRef(string guid) => guid == null ? null : new() { guid = guid };
        public static implicit operator AnyRef(SimpleBlueprint bp) => bp == null ? null : new() { guid = bp.AssetGuid };

        public static implicit operator AnyRef(AnyBlueprintReference bp) => ToAny(bp);
        public static implicit operator AnyBlueprintReference(AnyRef bp) => ToRef<AnyBlueprintReference>(bp);


        public static implicit operator AnyRef(BlueprintAbilityAreaEffectReference bp) => ToAny(bp);
        public static implicit operator BlueprintAbilityAreaEffectReference(AnyRef bp) => ToRef<BlueprintAbilityAreaEffectReference>(bp);
        public static implicit operator BlueprintAbilityAreaEffect(AnyRef bp) => Get<BlueprintAbilityAreaEffect>(bp);

        public static implicit operator AnyRef(BlueprintAbilityReference bp) => ToAny(bp);
        public static implicit operator BlueprintAbilityReference(AnyRef bp) => ToRef<BlueprintAbilityReference>(bp);
        public static implicit operator BlueprintAbility(AnyRef bp) => Get<BlueprintAbility>(bp);

        public static implicit operator AnyRef(BlueprintAbilityResourceReference bp) => ToAny(bp);
        public static implicit operator BlueprintAbilityResourceReference(AnyRef bp) => ToRef<BlueprintAbilityResourceReference>(bp);
        public static implicit operator BlueprintAbilityResource(AnyRef bp) => Get<BlueprintAbilityResource>(bp);

        public static implicit operator AnyRef(BlueprintActionListReference bp) => ToAny(bp);
        public static implicit operator BlueprintActionListReference(AnyRef bp) => ToRef<BlueprintActionListReference>(bp);
        public static implicit operator BlueprintActionList(AnyRef bp) => Get<BlueprintActionList>(bp);

        public static implicit operator AnyRef(BlueprintActivatableAbilityReference bp) => ToAny(bp);
        public static implicit operator BlueprintActivatableAbilityReference(AnyRef bp) => ToRef<BlueprintActivatableAbilityReference>(bp);
        public static implicit operator BlueprintActivatableAbility(AnyRef bp) => Get<BlueprintActivatableAbility>(bp);

        public static implicit operator AnyRef(BlueprintAnswerBaseReference bp) => ToAny(bp);
        public static implicit operator BlueprintAnswerBaseReference(AnyRef bp) => ToRef<BlueprintAnswerBaseReference>(bp);
        public static implicit operator BlueprintAnswerBase(AnyRef bp) => Get<BlueprintAnswerBase>(bp);

        public static implicit operator AnyRef(BlueprintAnswerReference bp) => ToAny(bp);
        public static implicit operator BlueprintAnswerReference(AnyRef bp) => ToRef<BlueprintAnswerReference>(bp);
        public static implicit operator BlueprintAnswer(AnyRef bp) => Get<BlueprintAnswer>(bp);

        public static implicit operator AnyRef(BlueprintAnswersListReference bp) => ToAny(bp);
        public static implicit operator BlueprintAnswersListReference(AnyRef bp) => ToRef<BlueprintAnswersListReference>(bp);
        public static implicit operator BlueprintAnswersList(AnyRef bp) => Get<BlueprintAnswersList>(bp);

        public static implicit operator AnyRef(BlueprintArchetypeReference bp) => ToAny(bp);
        public static implicit operator BlueprintArchetypeReference(AnyRef bp) => ToRef<BlueprintArchetypeReference>(bp);
        public static implicit operator BlueprintArchetype(AnyRef bp) => Get<BlueprintArchetype>(bp);

        public static implicit operator AnyRef(BlueprintAreaEffectPitVisualSettingsReference bp) => ToAny(bp);
        public static implicit operator BlueprintAreaEffectPitVisualSettingsReference(AnyRef bp) => ToRef<BlueprintAreaEffectPitVisualSettingsReference>(bp);
        public static implicit operator BlueprintAreaEffectPitVisualSettings(AnyRef bp) => Get<BlueprintAreaEffectPitVisualSettings>(bp);

        public static implicit operator AnyRef(BlueprintAreaEnterPointReference bp) => ToAny(bp);
        public static implicit operator BlueprintAreaEnterPointReference(AnyRef bp) => ToRef<BlueprintAreaEnterPointReference>(bp);
        public static implicit operator BlueprintAreaEnterPoint(AnyRef bp) => Get<BlueprintAreaEnterPoint>(bp);

        public static implicit operator AnyRef(BlueprintAreaPartReference bp) => ToAny(bp);
        public static implicit operator BlueprintAreaPartReference(AnyRef bp) => ToRef<BlueprintAreaPartReference>(bp);
        public static implicit operator BlueprintAreaPart(AnyRef bp) => Get<BlueprintAreaPart>(bp);

        public static implicit operator AnyRef(BlueprintAreaPresetReference bp) => ToAny(bp);
        public static implicit operator BlueprintAreaPresetReference(AnyRef bp) => ToRef<BlueprintAreaPresetReference>(bp);
        public static implicit operator BlueprintAreaPreset(AnyRef bp) => Get<BlueprintAreaPreset>(bp);

        public static implicit operator AnyRef(BlueprintAreaReference bp) => ToAny(bp);
        public static implicit operator BlueprintAreaReference(AnyRef bp) => ToRef<BlueprintAreaReference>(bp);
        public static implicit operator BlueprintArea(AnyRef bp) => Get<BlueprintArea>(bp);

        public static implicit operator AnyRef(BlueprintAreaTransitionReference bp) => ToAny(bp);
        public static implicit operator BlueprintAreaTransitionReference(AnyRef bp) => ToRef<BlueprintAreaTransitionReference>(bp);
        public static implicit operator BlueprintAreaTransition(AnyRef bp) => Get<BlueprintAreaTransition>(bp);

        public static implicit operator AnyRef(BlueprintArmorEnchantmentReference bp) => ToAny(bp);
        public static implicit operator BlueprintArmorEnchantmentReference(AnyRef bp) => ToRef<BlueprintArmorEnchantmentReference>(bp);
        public static implicit operator BlueprintArmorEnchantment(AnyRef bp) => Get<BlueprintArmorEnchantment>(bp);

        public static implicit operator AnyRef(BlueprintArmorTypeReference bp) => ToAny(bp);
        public static implicit operator BlueprintArmorTypeReference(AnyRef bp) => ToRef<BlueprintArmorTypeReference>(bp);
        public static implicit operator BlueprintArmorType(AnyRef bp) => Get<BlueprintArmorType>(bp);

        public static implicit operator AnyRef(BlueprintBarkBanterReference bp) => ToAny(bp);
        public static implicit operator BlueprintBarkBanterReference(AnyRef bp) => ToRef<BlueprintBarkBanterReference>(bp);
        public static implicit operator BlueprintBarkBanter(AnyRef bp) => Get<BlueprintBarkBanter>(bp);

        public static implicit operator AnyRef(BlueprintBuffReference bp) => ToAny(bp);
        public static implicit operator BlueprintBuffReference(AnyRef bp) => ToRef<BlueprintBuffReference>(bp);
        public static implicit operator BlueprintBuff(AnyRef bp) => Get<BlueprintBuff>(bp);

        public static implicit operator AnyRef(BlueprintCampaignReference bp) => ToAny(bp);
        public static implicit operator BlueprintCampaignReference(AnyRef bp) => ToRef<BlueprintCampaignReference>(bp);
        public static implicit operator BlueprintCampaign(AnyRef bp) => Get<BlueprintCampaign>(bp);

        public static implicit operator AnyRef(BlueprintCategoryDefaultsReference bp) => ToAny(bp);
        public static implicit operator BlueprintCategoryDefaultsReference(AnyRef bp) => ToRef<BlueprintCategoryDefaultsReference>(bp);
        public static implicit operator BlueprintCategoryDefaults(AnyRef bp) => Get<BlueprintCategoryDefaults>(bp);

        public static implicit operator AnyRef(BlueprintCharacterClassGroupReference bp) => ToAny(bp);
        public static implicit operator BlueprintCharacterClassGroupReference(AnyRef bp) => ToRef<BlueprintCharacterClassGroupReference>(bp);
        public static implicit operator BlueprintCharacterClassGroup(AnyRef bp) => Get<BlueprintCharacterClassGroup>(bp);

        public static implicit operator AnyRef(BlueprintCheckReference bp) => ToAny(bp);
        public static implicit operator BlueprintCheckReference(AnyRef bp) => ToRef<BlueprintCheckReference>(bp);
        public static implicit operator BlueprintCheck(AnyRef bp) => Get<BlueprintCheck>(bp);

        public static implicit operator AnyRef(BlueprintCompanionStoryReference bp) => ToAny(bp);
        public static implicit operator BlueprintCompanionStoryReference(AnyRef bp) => ToRef<BlueprintCompanionStoryReference>(bp);
        public static implicit operator BlueprintCompanionStory(AnyRef bp) => Get<BlueprintCompanionStory>(bp);

        public static implicit operator AnyRef(BlueprintControllableProjectileReference bp) => ToAny(bp);
        public static implicit operator BlueprintControllableProjectileReference(AnyRef bp) => ToRef<BlueprintControllableProjectileReference>(bp);
        public static implicit operator BlueprintControllableProjectile(AnyRef bp) => Get<BlueprintControllableProjectile>(bp);

        public static implicit operator AnyRef(BlueprintCreditsGroupReference bp) => ToAny(bp);
        public static implicit operator BlueprintCreditsGroupReference(AnyRef bp) => ToRef<BlueprintCreditsGroupReference>(bp);
        public static implicit operator BlueprintCreditsGroup(AnyRef bp) => Get<BlueprintCreditsGroup>(bp);

        public static implicit operator AnyRef(BlueprintCreditsRolesReference bp) => ToAny(bp);
        public static implicit operator BlueprintCreditsRolesReference(AnyRef bp) => ToRef<BlueprintCreditsRolesReference>(bp);
        public static implicit operator BlueprintCreditsRoles(AnyRef bp) => Get<BlueprintCreditsRoles>(bp);

        public static implicit operator AnyRef(BlueprintCreditsTeamsReference bp) => ToAny(bp);
        public static implicit operator BlueprintCreditsTeamsReference(AnyRef bp) => ToRef<BlueprintCreditsTeamsReference>(bp);
        public static implicit operator BlueprintCreditsTeams(AnyRef bp) => Get<BlueprintCreditsTeams>(bp);

        public static implicit operator AnyRef(BlueprintCueBaseReference bp) => ToAny(bp);
        public static implicit operator BlueprintCueBaseReference(AnyRef bp) => ToRef<BlueprintCueBaseReference>(bp);
        public static implicit operator BlueprintCueBase(AnyRef bp) => Get<BlueprintCueBase>(bp);

        public static implicit operator AnyRef(BlueprintDialogReference bp) => ToAny(bp);
        public static implicit operator BlueprintDialogReference(AnyRef bp) => ToRef<BlueprintDialogReference>(bp);
        public static implicit operator BlueprintDialog(AnyRef bp) => Get<BlueprintDialog>(bp);

        public static implicit operator AnyRef(BlueprintDlcReference bp) => ToAny(bp);
        public static implicit operator BlueprintDlcReference(AnyRef bp) => ToRef<BlueprintDlcReference>(bp);
        public static implicit operator BlueprintDlc(AnyRef bp) => Get<BlueprintDlc>(bp);

        public static implicit operator AnyRef(BlueprintDlcRewardCampaignReference bp) => ToAny(bp);
        public static implicit operator BlueprintDlcRewardCampaignReference(AnyRef bp) => ToRef<BlueprintDlcRewardCampaignReference>(bp);
        public static implicit operator BlueprintDlcRewardCampaign(AnyRef bp) => Get<BlueprintDlcRewardCampaign>(bp);

        public static implicit operator AnyRef(BlueprintDlcRewardReference bp) => ToAny(bp);
        public static implicit operator BlueprintDlcRewardReference(AnyRef bp) => ToRef<BlueprintDlcRewardReference>(bp);
        public static implicit operator BlueprintDlcReward(AnyRef bp) => Get<BlueprintDlcReward>(bp);

        public static implicit operator AnyRef(BlueprintDynamicMapObjectReference bp) => ToAny(bp);
        public static implicit operator BlueprintDynamicMapObjectReference(AnyRef bp) => ToRef<BlueprintDynamicMapObjectReference>(bp);
        public static implicit operator BlueprintDynamicMapObject(AnyRef bp) => Get<BlueprintDynamicMapObject>(bp);

        public static implicit operator AnyRef(BlueprintEncyclopediaChapterReference bp) => ToAny(bp);
        public static implicit operator BlueprintEncyclopediaChapterReference(AnyRef bp) => ToRef<BlueprintEncyclopediaChapterReference>(bp);
        public static implicit operator BlueprintEncyclopediaChapter(AnyRef bp) => Get<BlueprintEncyclopediaChapter>(bp);

        public static implicit operator AnyRef(BlueprintEncyclopediaNodeReference bp) => ToAny(bp);
        public static implicit operator BlueprintEncyclopediaNodeReference(AnyRef bp) => ToRef<BlueprintEncyclopediaNodeReference>(bp);
        public static implicit operator BlueprintEncyclopediaNode(AnyRef bp) => Get<BlueprintEncyclopediaNode>(bp);

        public static implicit operator AnyRef(BlueprintEncyclopediaPageReference bp) => ToAny(bp);
        public static implicit operator BlueprintEncyclopediaPageReference(AnyRef bp) => ToRef<BlueprintEncyclopediaPageReference>(bp);
        public static implicit operator BlueprintEncyclopediaPage(AnyRef bp) => Get<BlueprintEncyclopediaPage>(bp);

        public static implicit operator AnyRef(BlueprintEquipmentEnchantmentReference bp) => ToAny(bp);
        public static implicit operator BlueprintEquipmentEnchantmentReference(AnyRef bp) => ToRef<BlueprintEquipmentEnchantmentReference>(bp);
        public static implicit operator BlueprintEquipmentEnchantment(AnyRef bp) => Get<BlueprintEquipmentEnchantment>(bp);

        public static implicit operator AnyRef(BlueprintEtudeConflictingGroupReference bp) => ToAny(bp);
        public static implicit operator BlueprintEtudeConflictingGroupReference(AnyRef bp) => ToRef<BlueprintEtudeConflictingGroupReference>(bp);
        public static implicit operator BlueprintEtudeConflictingGroup(AnyRef bp) => Get<BlueprintEtudeConflictingGroup>(bp);

        public static implicit operator AnyRef(BlueprintEtudeReference bp) => ToAny(bp);
        public static implicit operator BlueprintEtudeReference(AnyRef bp) => ToRef<BlueprintEtudeReference>(bp);
        public static implicit operator BlueprintEtude(AnyRef bp) => Get<BlueprintEtude>(bp);

        public static implicit operator AnyRef(BlueprintFactionReference bp) => ToAny(bp);
        public static implicit operator BlueprintFactionReference(AnyRef bp) => ToRef<BlueprintFactionReference>(bp);
        public static implicit operator BlueprintFaction(AnyRef bp) => Get<BlueprintFaction>(bp);

        public static implicit operator AnyRef(BlueprintFeatureBaseReference bp) => ToAny(bp);
        public static implicit operator BlueprintFeatureBaseReference(AnyRef bp) => ToRef<BlueprintFeatureBaseReference>(bp);
        public static implicit operator BlueprintFeatureBase(AnyRef bp) => Get<BlueprintFeatureBase>(bp);

        public static implicit operator AnyRef(BlueprintFeatureReference bp) => ToAny(bp);
        public static implicit operator BlueprintFeatureReference(AnyRef bp) => ToRef<BlueprintFeatureReference>(bp);
        public static implicit operator BlueprintFeature(AnyRef bp) => Get<BlueprintFeature>(bp);

        public static implicit operator AnyRef(BlueprintItemArmorReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemArmorReference(AnyRef bp) => ToRef<BlueprintItemArmorReference>(bp);
        public static implicit operator BlueprintItemArmor(AnyRef bp) => Get<BlueprintItemArmor>(bp);

        public static implicit operator AnyRef(BlueprintItemEnchantmentReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEnchantmentReference(AnyRef bp) => ToRef<BlueprintItemEnchantmentReference>(bp);
        public static implicit operator BlueprintItemEnchantment(AnyRef bp) => Get<BlueprintItemEnchantment>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentBeltReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentBeltReference(AnyRef bp) => ToRef<BlueprintItemEquipmentBeltReference>(bp);
        public static implicit operator BlueprintItemEquipmentBelt(AnyRef bp) => Get<BlueprintItemEquipmentBelt>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentFeetReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentFeetReference(AnyRef bp) => ToRef<BlueprintItemEquipmentFeetReference>(bp);
        public static implicit operator BlueprintItemEquipmentFeet(AnyRef bp) => Get<BlueprintItemEquipmentFeet>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentGlassesReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentGlassesReference(AnyRef bp) => ToRef<BlueprintItemEquipmentGlassesReference>(bp);
        public static implicit operator BlueprintItemEquipmentGlasses(AnyRef bp) => Get<BlueprintItemEquipmentGlasses>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentGlovesReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentGlovesReference(AnyRef bp) => ToRef<BlueprintItemEquipmentGlovesReference>(bp);
        public static implicit operator BlueprintItemEquipmentGloves(AnyRef bp) => Get<BlueprintItemEquipmentGloves>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentHandReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentHandReference(AnyRef bp) => ToRef<BlueprintItemEquipmentHandReference>(bp);
        public static implicit operator BlueprintItemEquipmentHand(AnyRef bp) => Get<BlueprintItemEquipmentHand>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentHeadReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentHeadReference(AnyRef bp) => ToRef<BlueprintItemEquipmentHeadReference>(bp);
        public static implicit operator BlueprintItemEquipmentHead(AnyRef bp) => Get<BlueprintItemEquipmentHead>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentNeckReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentNeckReference(AnyRef bp) => ToRef<BlueprintItemEquipmentNeckReference>(bp);
        public static implicit operator BlueprintItemEquipmentNeck(AnyRef bp) => Get<BlueprintItemEquipmentNeck>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentReference(AnyRef bp) => ToRef<BlueprintItemEquipmentReference>(bp);
        public static implicit operator BlueprintItemEquipment(AnyRef bp) => Get<BlueprintItemEquipment>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentRingReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentRingReference(AnyRef bp) => ToRef<BlueprintItemEquipmentRingReference>(bp);
        public static implicit operator BlueprintItemEquipmentRing(AnyRef bp) => Get<BlueprintItemEquipmentRing>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentShirtReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentShirtReference(AnyRef bp) => ToRef<BlueprintItemEquipmentShirtReference>(bp);
        public static implicit operator BlueprintItemEquipmentShirt(AnyRef bp) => Get<BlueprintItemEquipmentShirt>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentShouldersReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentShouldersReference(AnyRef bp) => ToRef<BlueprintItemEquipmentShouldersReference>(bp);
        public static implicit operator BlueprintItemEquipmentShoulders(AnyRef bp) => Get<BlueprintItemEquipmentShoulders>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentUsableReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentUsableReference(AnyRef bp) => ToRef<BlueprintItemEquipmentUsableReference>(bp);
        public static implicit operator BlueprintItemEquipmentUsable(AnyRef bp) => Get<BlueprintItemEquipmentUsable>(bp);

        public static implicit operator AnyRef(BlueprintItemEquipmentWristReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemEquipmentWristReference(AnyRef bp) => ToRef<BlueprintItemEquipmentWristReference>(bp);
        public static implicit operator BlueprintItemEquipmentWrist(AnyRef bp) => Get<BlueprintItemEquipmentWrist>(bp);

        public static implicit operator AnyRef(BlueprintItemReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemReference(AnyRef bp) => ToRef<BlueprintItemReference>(bp);
        public static implicit operator BlueprintItem(AnyRef bp) => Get<BlueprintItem>(bp);

        public static implicit operator AnyRef(BlueprintItemWeaponReference bp) => ToAny(bp);
        public static implicit operator BlueprintItemWeaponReference(AnyRef bp) => ToRef<BlueprintItemWeaponReference>(bp);
        public static implicit operator BlueprintItemWeapon(AnyRef bp) => Get<BlueprintItemWeapon>(bp);

        public static implicit operator AnyRef(BlueprintLogicConnectorReference bp) => ToAny(bp);
        public static implicit operator BlueprintLogicConnectorReference(AnyRef bp) => ToRef<BlueprintLogicConnectorReference>(bp);
        public static implicit operator BlueprintLogicConnector(AnyRef bp) => Get<BlueprintLogicConnector>(bp);

        public static implicit operator AnyRef(BlueprintLootReference bp) => ToAny(bp);
        public static implicit operator BlueprintLootReference(AnyRef bp) => ToRef<BlueprintLootReference>(bp);
        public static implicit operator BlueprintLoot(AnyRef bp) => Get<BlueprintLoot>(bp);

        public static implicit operator AnyRef(BlueprintMultiEntranceEntryReference bp) => ToAny(bp);
        public static implicit operator BlueprintMultiEntranceEntryReference(AnyRef bp) => ToRef<BlueprintMultiEntranceEntryReference>(bp);
        public static implicit operator BlueprintMultiEntranceEntry(AnyRef bp) => Get<BlueprintMultiEntranceEntry>(bp);

        public static implicit operator AnyRef(BlueprintMultiEntranceReference bp) => ToAny(bp);
        public static implicit operator BlueprintMultiEntranceReference(AnyRef bp) => ToRef<BlueprintMultiEntranceReference>(bp);
        public static implicit operator BlueprintMultiEntrance(AnyRef bp) => Get<BlueprintMultiEntrance>(bp);

        public static implicit operator AnyRef(BlueprintMythicInfoReference bp) => ToAny(bp);
        public static implicit operator BlueprintMythicInfoReference(AnyRef bp) => ToRef<BlueprintMythicInfoReference>(bp);
        public static implicit operator BlueprintMythicInfo(AnyRef bp) => Get<BlueprintMythicInfo>(bp);

        public static implicit operator AnyRef(BlueprintMythicsSettingsReference bp) => ToAny(bp);
        public static implicit operator BlueprintMythicsSettingsReference(AnyRef bp) => ToRef<BlueprintMythicsSettingsReference>(bp);
        public static implicit operator BlueprintMythicsSettings(AnyRef bp) => Get<BlueprintMythicsSettings>(bp);

        public static implicit operator AnyRef(BlueprintPartyFormationReference bp) => ToAny(bp);
        public static implicit operator BlueprintPartyFormationReference(AnyRef bp) => ToRef<BlueprintPartyFormationReference>(bp);
        public static implicit operator BlueprintPartyFormation(AnyRef bp) => Get<BlueprintPartyFormation>(bp);

        public static implicit operator AnyRef(BlueprintPortraitReference bp) => ToAny(bp);
        public static implicit operator BlueprintPortraitReference(AnyRef bp) => ToRef<BlueprintPortraitReference>(bp);
        public static implicit operator BlueprintPortrait(AnyRef bp) => Get<BlueprintPortrait>(bp);

        public static implicit operator AnyRef(BlueprintProgressionReference bp) => ToAny(bp);
        public static implicit operator BlueprintProgressionReference(AnyRef bp) => ToRef<BlueprintProgressionReference>(bp);
        public static implicit operator BlueprintProgression(AnyRef bp) => Get<BlueprintProgression>(bp);

        public static implicit operator AnyRef(BlueprintProjectileReference bp) => ToAny(bp);
        public static implicit operator BlueprintProjectileReference(AnyRef bp) => ToRef<BlueprintProjectileReference>(bp);
        public static implicit operator BlueprintProjectile(AnyRef bp) => Get<BlueprintProjectile>(bp);

        public static implicit operator AnyRef(BlueprintProjectileTrajectoryReference bp) => ToAny(bp);
        public static implicit operator BlueprintProjectileTrajectoryReference(AnyRef bp) => ToRef<BlueprintProjectileTrajectoryReference>(bp);
        public static implicit operator BlueprintProjectileTrajectory(AnyRef bp) => Get<BlueprintProjectileTrajectory>(bp);

        public static implicit operator AnyRef(BlueprintQuestGroupsReference bp) => ToAny(bp);
        public static implicit operator BlueprintQuestGroupsReference(AnyRef bp) => ToRef<BlueprintQuestGroupsReference>(bp);
        public static implicit operator BlueprintQuestGroups(AnyRef bp) => Get<BlueprintQuestGroups>(bp);

        public static implicit operator AnyRef(BlueprintQuestObjectiveReference bp) => ToAny(bp);
        public static implicit operator BlueprintQuestObjectiveReference(AnyRef bp) => ToRef<BlueprintQuestObjectiveReference>(bp);
        public static implicit operator BlueprintQuestObjective(AnyRef bp) => Get<BlueprintQuestObjective>(bp);

        public static implicit operator AnyRef(BlueprintQuestReference bp) => ToAny(bp);
        public static implicit operator BlueprintQuestReference(AnyRef bp) => ToRef<BlueprintQuestReference>(bp);
        public static implicit operator BlueprintQuest(AnyRef bp) => Get<BlueprintQuest>(bp);

        public static implicit operator AnyRef(BlueprintRaceReference bp) => ToAny(bp);
        public static implicit operator BlueprintRaceReference(AnyRef bp) => ToRef<BlueprintRaceReference>(bp);
        public static implicit operator BlueprintRace(AnyRef bp) => Get<BlueprintRace>(bp);

        public static implicit operator AnyRef(BlueprintRaceVisualPresetReference bp) => ToAny(bp);
        public static implicit operator BlueprintRaceVisualPresetReference(AnyRef bp) => ToRef<BlueprintRaceVisualPresetReference>(bp);
        public static implicit operator BlueprintRaceVisualPreset(AnyRef bp) => Get<BlueprintRaceVisualPreset>(bp);

        public static implicit operator AnyRef(BlueprintRomanceCounterReference bp) => ToAny(bp);
        public static implicit operator BlueprintRomanceCounterReference(AnyRef bp) => ToRef<BlueprintRomanceCounterReference>(bp);
        public static implicit operator BlueprintRomanceCounter(AnyRef bp) => Get<BlueprintRomanceCounter>(bp);

        public static implicit operator AnyRef(BlueprintScriptableObjectReference bp) => ToAny(bp);
        public static implicit operator BlueprintScriptableObjectReference(AnyRef bp) => ToRef<BlueprintScriptableObjectReference>(bp);
        public static implicit operator BlueprintScriptableObject(AnyRef bp) => Get<BlueprintScriptableObject>(bp);

        public static implicit operator AnyRef(BlueprintScriptZoneReference bp) => ToAny(bp);
        public static implicit operator BlueprintScriptZoneReference(AnyRef bp) => ToRef<BlueprintScriptZoneReference>(bp);
        public static implicit operator BlueprintScriptZone(AnyRef bp) => Get<BlueprintScriptZone>(bp);

        public static implicit operator AnyRef(BlueprintSequenceExitReference bp) => ToAny(bp);
        public static implicit operator BlueprintSequenceExitReference(AnyRef bp) => ToRef<BlueprintSequenceExitReference>(bp);
        public static implicit operator BlueprintSequenceExit(AnyRef bp) => Get<BlueprintSequenceExit>(bp);

        public static implicit operator AnyRef(BlueprintSharedVendorTableReference bp) => ToAny(bp);
        public static implicit operator BlueprintSharedVendorTableReference(AnyRef bp) => ToRef<BlueprintSharedVendorTableReference>(bp);
        public static implicit operator BlueprintSharedVendorTable(AnyRef bp) => Get<BlueprintSharedVendorTable>(bp);

        public static implicit operator AnyRef(BlueprintSpellbookReference bp) => ToAny(bp);
        public static implicit operator BlueprintSpellbookReference(AnyRef bp) => ToRef<BlueprintSpellbookReference>(bp);
        public static implicit operator BlueprintSpellbook(AnyRef bp) => Get<BlueprintSpellbook>(bp);

        public static implicit operator AnyRef(BlueprintSpellListReference bp) => ToAny(bp);
        public static implicit operator BlueprintSpellListReference(AnyRef bp) => ToRef<BlueprintSpellListReference>(bp);
        public static implicit operator BlueprintSpellList(AnyRef bp) => Get<BlueprintSpellList>(bp);

        public static implicit operator AnyRef(BlueprintSpellsTableReference bp) => ToAny(bp);
        public static implicit operator BlueprintSpellsTableReference(AnyRef bp) => ToRef<BlueprintSpellsTableReference>(bp);
        public static implicit operator BlueprintSpellsTable(AnyRef bp) => Get<BlueprintSpellsTable>(bp);

        public static implicit operator AnyRef(BlueprintStatProgressionReference bp) => ToAny(bp);
        public static implicit operator BlueprintStatProgressionReference(AnyRef bp) => ToRef<BlueprintStatProgressionReference>(bp);
        public static implicit operator BlueprintStatProgression(AnyRef bp) => Get<BlueprintStatProgression>(bp);

        public static implicit operator AnyRef(BlueprintSummonPoolReference bp) => ToAny(bp);
        public static implicit operator BlueprintSummonPoolReference(AnyRef bp) => ToRef<BlueprintSummonPoolReference>(bp);
        public static implicit operator BlueprintSummonPool(AnyRef bp) => Get<BlueprintSummonPool>(bp);

        public static implicit operator AnyRef(BlueprintTimeOfDaySettingsReference bp) => ToAny(bp);
        public static implicit operator BlueprintTimeOfDaySettingsReference(AnyRef bp) => ToRef<BlueprintTimeOfDaySettingsReference>(bp);
        public static implicit operator BlueprintTimeOfDaySettings(AnyRef bp) => Get<BlueprintTimeOfDaySettings>(bp);

        public static implicit operator AnyRef(BlueprintTrapReference bp) => ToAny(bp);
        public static implicit operator BlueprintTrapReference(AnyRef bp) => ToRef<BlueprintTrapReference>(bp);
        public static implicit operator BlueprintTrap(AnyRef bp) => Get<BlueprintTrap>(bp);

        public static implicit operator AnyRef(BlueprintTrapSettingsReference bp) => ToAny(bp);
        public static implicit operator BlueprintTrapSettingsReference(AnyRef bp) => ToRef<BlueprintTrapSettingsReference>(bp);
        public static implicit operator BlueprintTrapSettings(AnyRef bp) => Get<BlueprintTrapSettings>(bp);

        public static implicit operator AnyRef(BlueprintTrapSettingsRootReference bp) => ToAny(bp);
        public static implicit operator BlueprintTrapSettingsRootReference(AnyRef bp) => ToRef<BlueprintTrapSettingsRootReference>(bp);
        public static implicit operator BlueprintTrapSettingsRoot(AnyRef bp) => Get<BlueprintTrapSettingsRoot>(bp);

        public static implicit operator AnyRef(BlueprintUISoundReference bp) => ToAny(bp);
        public static implicit operator BlueprintUISoundReference(AnyRef bp) => ToRef<BlueprintUISoundReference>(bp);
        public static implicit operator BlueprintUISound(AnyRef bp) => Get<BlueprintUISound>(bp);

        public static implicit operator AnyRef(BlueprintUnitAsksListReference bp) => ToAny(bp);
        public static implicit operator BlueprintUnitAsksListReference(AnyRef bp) => ToRef<BlueprintUnitAsksListReference>(bp);
        public static implicit operator BlueprintUnitAsksList(AnyRef bp) => Get<BlueprintUnitAsksList>(bp);

        public static implicit operator AnyRef(BlueprintUnitFactReference bp) => ToAny(bp);
        public static implicit operator BlueprintUnitFactReference(AnyRef bp) => ToRef<BlueprintUnitFactReference>(bp);
        public static implicit operator BlueprintUnitFact(AnyRef bp) => Get<BlueprintUnitFact>(bp);

        public static implicit operator AnyRef(BlueprintUnitLootReference bp) => ToAny(bp);
        public static implicit operator BlueprintUnitLootReference(AnyRef bp) => ToRef<BlueprintUnitLootReference>(bp);
        public static implicit operator BlueprintUnitLoot(AnyRef bp) => Get<BlueprintUnitLoot>(bp);

        public static implicit operator AnyRef(BlueprintUnitReference bp) => ToAny(bp);
        public static implicit operator BlueprintUnitReference(AnyRef bp) => ToRef<BlueprintUnitReference>(bp);
        public static implicit operator BlueprintUnit(AnyRef bp) => Get<BlueprintUnit>(bp);

        public static implicit operator AnyRef(BlueprintUnitTemplateReference bp) => ToAny(bp);
        public static implicit operator BlueprintUnitTemplateReference(AnyRef bp) => ToRef<BlueprintUnitTemplateReference>(bp);
        public static implicit operator BlueprintUnitTemplate(AnyRef bp) => Get<BlueprintUnitTemplate>(bp);

        public static implicit operator AnyRef(BlueprintUnitTypeReference bp) => ToAny(bp);
        public static implicit operator BlueprintUnitTypeReference(AnyRef bp) => ToRef<BlueprintUnitTypeReference>(bp);
        public static implicit operator BlueprintUnitType(AnyRef bp) => Get<BlueprintUnitType>(bp);

        public static implicit operator AnyRef(BlueprintUnlockableFlagReference bp) => ToAny(bp);
        public static implicit operator BlueprintUnlockableFlagReference(AnyRef bp) => ToRef<BlueprintUnlockableFlagReference>(bp);
        public static implicit operator BlueprintUnlockableFlag(AnyRef bp) => Get<BlueprintUnlockableFlag>(bp);

        public static implicit operator AnyRef(BlueprintWeaponEnchantmentReference bp) => ToAny(bp);
        public static implicit operator BlueprintWeaponEnchantmentReference(AnyRef bp) => ToRef<BlueprintWeaponEnchantmentReference>(bp);
        public static implicit operator BlueprintWeaponEnchantment(AnyRef bp) => Get<BlueprintWeaponEnchantment>(bp);

        public static implicit operator AnyRef(BlueprintWeaponTypeReference bp) => ToAny(bp);
        public static implicit operator BlueprintWeaponTypeReference(AnyRef bp) => ToRef<BlueprintWeaponTypeReference>(bp);
        public static implicit operator BlueprintWeaponType(AnyRef bp) => Get<BlueprintWeaponType>(bp);

        public static implicit operator AnyRef(ConsoleRootReference bp) => ToAny(bp);
        public static implicit operator ConsoleRootReference(AnyRef bp) => ToRef<ConsoleRootReference>(bp);
        public static implicit operator ConsoleRoot(AnyRef bp) => Get<ConsoleRoot>(bp);

        public static implicit operator AnyRef(CutsceneReference bp) => ToAny(bp);
        public static implicit operator CutsceneReference(AnyRef bp) => ToRef<CutsceneReference>(bp);
        public static implicit operator Cutscene(AnyRef bp) => Get<Cutscene>(bp);

        public static implicit operator AnyRef(FormationsRootReference bp) => ToAny(bp);
        public static implicit operator FormationsRootReference(AnyRef bp) => ToRef<FormationsRootReference>(bp);
        public static implicit operator FormationsRoot(AnyRef bp) => Get<FormationsRoot>(bp);

        public static implicit operator AnyRef(FxRootReference bp) => ToAny(bp);
        public static implicit operator FxRootReference(AnyRef bp) => ToRef<FxRootReference>(bp);
        public static implicit operator FxRoot(AnyRef bp) => Get<FxRoot>(bp);

        public static implicit operator AnyRef(GateReference bp) => ToAny(bp);
        public static implicit operator GateReference(AnyRef bp) => ToRef<GateReference>(bp);
        public static implicit operator Gate(AnyRef bp) => Get<Gate>(bp);

        public static implicit operator AnyRef(HitSystemRootReference bp) => ToAny(bp);
        public static implicit operator HitSystemRootReference(AnyRef bp) => ToRef<HitSystemRootReference>(bp);
        public static implicit operator HitSystemRoot(AnyRef bp) => Get<HitSystemRoot>(bp);

        public static implicit operator AnyRef(KingmakerEquipmentEntityReference bp) => ToAny(bp);
        public static implicit operator KingmakerEquipmentEntityReference(AnyRef bp) => ToRef<KingmakerEquipmentEntityReference>(bp);
        public static implicit operator KingmakerEquipmentEntity(AnyRef bp) => Get<KingmakerEquipmentEntity>(bp);

        public static implicit operator AnyRef(RaceGenderDistributionReference bp) => ToAny(bp);
        public static implicit operator RaceGenderDistributionReference(AnyRef bp) => ToRef<RaceGenderDistributionReference>(bp);
        public static implicit operator RaceGenderDistribution(AnyRef bp) => Get<RaceGenderDistribution>(bp);

        public static implicit operator AnyRef(TrashLootSettingsReference bp) => ToAny(bp);
        public static implicit operator TrashLootSettingsReference(AnyRef bp) => ToRef<TrashLootSettingsReference>(bp);
        public static implicit operator TrashLootSettings(AnyRef bp) => Get<TrashLootSettings>(bp);

        public static implicit operator AnyRef(UnitCustomizationPresetReference bp) => ToAny(bp);
        public static implicit operator UnitCustomizationPresetReference(AnyRef bp) => ToRef<UnitCustomizationPresetReference>(bp);
        public static implicit operator UnitCustomizationPreset(AnyRef bp) => Get<UnitCustomizationPreset>(bp);



        /* all reference types
            (.+)Reference
            public static implicit operator AnyRef\(${1}Reference bp\) => ToAny\(bp\);\npublic static implicit operator ${1}Reference\(AnyRef bp\) => ToRef<${1}Reference>\(bp\);\npublic static implicit operator ${1}\(AnyRef bp\) => Get<${1}>\(bp\);\n


         */
    }
}
