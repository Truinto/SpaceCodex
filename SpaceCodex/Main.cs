using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Enums;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Properties.Getters;
using Kingmaker.Localization;
using Kingmaker.UI.Legacy.MainMenuUI;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Progression.Features;
using Shared;
using Shared.Collections;
using SpaceCodex.Patches;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;
using UnityEngine;
using Kingmaker.DialogSystem.Blueprints;

namespace SpaceCodex
{
    public static class Main
    {
        public static bool hasRun;
        public static Harmony harmony;
        public static UnityModManager.ModEntry.ModLogger logger;

        public static void Load(UnityModManager.ModEntry modEntry)
        {
            logger = modEntry.Logger;
            harmony = new(modEntry.Info.Id);
            modEntry.OnGUI = OnGUI;
            EnsureLibrary(modEntry.Path, "UnityMod");

            PatchSafe(typeof(Patches));
            //PatchSafe(typeof(Patch_VeteranVersetility));
        }

        public static void OnBlueprintsLoaded()
        {
            if (hasRun)
                return;
            hasRun = true;
            Localization.Init();

            LoadSafe(Psyker);
            LoadSafe(Veteran_Versatility);
            LoadSafe(InfiniteConsumables);
        }

        public static void Psyker()
        {
            // fix firestorm not hitting primary target
            var firestorm = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("321a9274e3454d69ada142f4ce540b12"); //Pyromancy_FireStorm_Ability
            firestorm.GetComponent<AbilityTargetsInPattern>().m_Condition.Conditions = [];

            // fix invigorate adding 3 veil
            var invigorate = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("f51d5d8fdc8e49c4828c0180868e0be9"); //Biomancy_Invigorate_Ability
            invigorate.VeilThicknessPointsToAdd = 1;
            invigorate.PsychicPower = PsychicPower.Minor;

            // change inflame not hitting allies
            var inflame = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("24f1e49a2294434da2dc17edb6808517"); //Pyromancy_FanTheFlames_Ability
            inflame.GetComponent<AbilityEffectRunAction>().Actions.Actions.Get<ContextActionOnAllUnitsInCombat>().OnlyEnemies = true;
        }

        public static void Veteran_Versatility()
        {
            var versatility = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("7f7d109653f24774939f758d094b8812"); //Veteran_Keystone_Feature
            var icon = versatility.m_Icon;

            // fix weird trigger
            foreach (var comp in versatility.GetComponents<AbilityRuleTriggerInitiator>())
            {
                Collection.RemoveAll(ref comp.Restrictions.Property.Getters, f => f is ContextValueGetter);
            }

            // add new spell trigger
            var spellTrigger = Helper.CreateAbilityRuleTriggerInitiator()
                .Add(new CheckAbilityAttackSpellGetter())
                .Add(Helper.CreateConditional(Operation.And)
                    .Add(Helper.CreateContextConditionHasFact("e3064c5c0c5f494f9e2d5cd3c1dae542"))
                    .Add(Helper.CreateContextConditionHasFact("6a77f341aa4f480ea799a9cf90ea2c5e", true))
                    .IfFalse(Helper.CreateContextActionApplyBuff("2f7145a22ec44fa69b760af7750a467c"))
                    .IfFalse(Helper.CreateContextActionRemoveBuff("6ccaabf55b294788b1ce282066ea48a8"))
                    .IfFalse(Helper.CreateContextActionRemoveBuff("037de94fa4324857955158762ea5992c"))
                    .IfFalse(Helper.CreateContextActionRemoveBuff("3b66a03e3adb449c84f20036e74deada"))
                    .IfFalse(Helper.CreateContextActionRemoveBuff("7e5dc8ae9cfc492f95e45d84b8a418ae"))
                    .IfFalse(Helper.CreateContextActionRemoveBuff("b511876936f04be0af639f20efea6edc"))
                    .IfFalse(Helper.CreateContextActionRemoveBuff("e3064c5c0c5f494f9e2d5cd3c1dae542"))
                    .IfFalse(Helper.CreateContextActionApplyBuff("e3064c5c0c5f494f9e2d5cd3c1dae542"))
            );
            versatility.AddComponents(spellTrigger);

            // show current buff
            foreach (var (guid, name) in new (string, string)[] {
                ("6ccaabf55b294788b1ce282066ea48a8", "Versatility Melee Area"), //Veteran_Keystone_BuffAreaMelee
                ("037de94fa4324857955158762ea5992c", "Versatility Ranged Area"), //Veteran_Keystone_BuffAreaShot
                ("3b66a03e3adb449c84f20036e74deada", "Versatility Ranged Scatter"), //Veteran_Keystone_BuffBurstShot
                ("7e5dc8ae9cfc492f95e45d84b8a418ae", "Versatility Melee Single"), //Veteran_Keystone_BuffSingleMelee
                ("b511876936f04be0af639f20efea6edc", "Versatility Ranged Singleshot"), //Veteran_Keystone_BuffSingleShot
                ("e3064c5c0c5f494f9e2d5cd3c1dae542", "Versatility Misc"), //Veteran_Keystone_BuffUseItems
            })
            {
                var buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(guid);
                buff.m_Flags = 0; // &= ~BlueprintBuff.Flags.HiddenInUi;
                buff.m_DisplayName = name.CreateString();
                buff.m_Description = buff.m_DisplayName;
                buff.m_Icon = icon;
            }
        }

        public static void InfiniteConsumables()
        {
            string[] consumables = [
                "b074d60ee4fc406ba88e113f0cd2ce3a", //AdaptiveAntidot
                "9699621372aa4c3aa4df6b4a1f116db0", //DrugIntoxicatingElixir
                "02e3fe7902f04c2f8bac7c9b15abd8ae", //Drug_AntiToxin
                "6089683abf7f4599b5f3a008ea99f724", //Drug_Apexaliy
                "3194d57e8c404ef68414338ae7af1870", //Drug_Berserk
                "7d3001156fec8ce499be74f5476140a4", //Drug_Bloodshot
                "a8ababfbf25049d8ae884bcb82525b0a", //Drug_Combatpleasure
                "ad4d37b8a1a24666af16f2b9fa09eae6", //Drug_Fireproof
                "3036632a9eb34c8093837ce67a3b9870", //Drug_Flash
                "1843de0c31ca4c5c94b45cf87ff0a41e", //Drug_Furor
                "877f47130a2d43a3aa943dc847dc9235", //Drug_NeuroStimulant
                "b6daa597bb6b5114bb9e21ea1e855792", //Drug_Onslaught
                "c0597f96f77042b4a705aafaf01b76ec", //Drug_PainBlocker
                "58d3b2e71d5245aab60cfe026e3e2343", //Drug_Sharpshot
                "79b1f8fa1fb9463cbb50e00979a9c1f4", //Drug_ShotOfReckless
                "16ccfa01deb148c1a0cee796fe5ea295", //Drug_SplashOfPower
                "17d042939e73b2a42918e8ff1d931103", //Drug_Stimm
                "07e75586610848e7a942c29e51ba5fa5", //ElixirOfWarpNeutrality
                "d8877770478c4a3ea9b64fafee7abd36", //Elusive
                "de5bd3fa3ba642208ede17ed3827e29e", //UncannySkin
                "292221b9b73f487c9299393c537602be", //GrenadeMonofilament
                "7d804b9e661a4fbdb4138c9de4808dda", //Grenade_AeldariPlasma
                "70cef1cfff5d4c91affe098d80ed57b5", //Grenade_Blind
                "ece61084f54a452cb63c135dab7c2b0f", //Grenade_Demoralizing
                "9a7233892a414a52a5cd4fb4367a5d99", //Grenade_Fire
                "a93a71bcb8904f30891a4710af6be8e7", //Grenade_FireQA
                "9637dc3883074c6ca17bb0ba6bee8605", //Grenade_Flashbang
                "0088d2c1ea084c1428266b7ffdeb6ab1", //Grenade_Frag
                "60b61f9c46374afdb76abc9d2f654328", //Grenade_FragQA
                "633143bfc45945e18d0f275c544d82c4", //Grenade_FragT2
                "848349b5906149f2b7dcf599a3b0d87b", //Grenade_GasCloud
                "a87ea3a714c045f4b27072ea1de37a98", //Grenade_Hallucinogen
                "de7ae5185beb489d9981610afd033333", //Grenade_HighWire
                "ba6c9b3434a740a08a0a28ef24dceb3d", //Grenade_HolyFire
                "d089cc4d4d194b85bd3614a855c45f7b", //Grenade_Knockback
                "20650117b63acf740998b602e108c51a", //Grenade_Krak
                "e129ac75f5c84ee0b9bbbe584f99a518", //Grenade_KrakQA
                "f1b22d8810584b1dae358df1b94cfbcd", //Grenade_KrakT1
                "cce8ef29541447d9828dff015136a123", //Grenade_Liquifier
                "947fb06dcd65411e8d991e9987f1f184", //Grenade_Mindscrambler
                "583b587520c14c04b86f7b2277dac4e2", //Grenade_Pungent_smoke
                "a7a3c48f88be440c80a25d4eb259a1b9", //Grenade_Resonance
                "e397cb9f6a1140a3bd9108aa43dc2b16", //Grenade_Slow
                "53a441db74d4461396938c775513dee6", //Grenade_Stun
                "fba70f596450402f86d49e9c8cccea9e", //Grenade_StunQA
                "f23cc7ce1cd84c55890c914a695fc9b5", //Grenade_TearsOfRepentance
                "d1e3d6c62bac4421844c33aaad5f99aa", //Grenade_Tesla
                "705dcc9dda9d429e9de2c8f974e632b6", //Grenade_Toxin
                "1c20a615e0f04591a467b8a62aea1996", //Leprosy
                "dcaab7fe2ed24b8f8ebdecbd35433e57", //Grenade_Warp
                "8b784af230484a90ad801e901a9d0b8e", //Grenade_Wraithbone
                "edf1ca959e0f40f4bf5bb64586f816b9", //Grenade_Xenospasm
                "d878a044b9174fc7b663ba94b0aee377", //BattleStimulator_Medikit
                "5cb792047d8945f8beba608d6f8a828e", //ChirurgeonMedikit_Item
                "4158054f16214c129f2bca3167f892f6", //FullRecoverKit
                "cd2541b15c4249fd974c3a2a0695aa70", //GladiatorsFirstAidKit
                "a676f7b14cf445159680c73184e2aaea", //GladiatorsHealingPotion
                "a1ef13863d0245c6956f84c46f105dcb", //HaemunculiRevivalPotion
                "4241a581631a41938cf45911cdae6cc7", //HealingBattleDanceInfusion
                "63c1e87c8502400f91537ee5e1fb8460", //LargeMedikit
                "56e3576b84384495b9015d5ec88217ac", //Medikit
                "9c64cb49aadd41cb87a7ad563bd53d89", //MedikitQA
                "237e04a301754516933f0eeaabca75da", //OfficersMedikit
                "c47bb956bb7c40d990e49c97c89a4f6d", //ScrapBox
                "0c9d46cdf46d45059fcabc22470b8d51", //ScrapContainer
                "8d9ec005c04a4984ad6032b3f74d84b1", //UpgradedMedikit
            ];

            foreach (string guid in consumables)
            {
                var bp = ResourcesLibrary.TryGetBlueprint(guid);
                if (bp is not BlueprintItemEquipmentUsable item)
                    continue;

                item.RestoreChargesAfterCombat = true;
                item.RemoveFromSlotWhenNoCharges = false;
            }
        }

        #region Menu

        private static void OnGUI(UnityModManager.ModEntry entry)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Unlock Foulstone (experimental)"))
            {
                var ans = ResourcesLibrary.TryGetBlueprint<BlueprintAnswer>("bb727db38a4843b7a848f111ca5e09e7");
                ans.OnSelect.Run();
            }

            GUILayout.EndHorizontal();
        }

        #endregion

        #region Helper

        private static void EnsureLibrary(string modPath, string library)
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith($"{library}, ")))
            {
                PrintDebug($"{library} already loaded.");
                return;
            }

            string path = null;
            Version version = null;
            modPath = new DirectoryInfo(modPath).Parent.FullName;
            PrintDebug($"Looking for {library} in " + modPath);

            foreach (string cPath in Directory.GetFiles(modPath, $"{library}.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var cVersion = new Version(FileVersionInfo.GetVersionInfo(cPath).FileVersion);
                    PrintDebug($"Found: newer={version == null || cVersion > version} version={cVersion} @ {cPath}");
                    if (version == null || cVersion > version)
                    {
                        path = cPath;
                        version = cVersion;
                    }
                }
                catch (Exception) { }
            }

            if (path != null)
            {
                try
                {
                    Print($"Loading {library} " + path);
                    AppDomain.CurrentDomain.Load(File.ReadAllBytes(path));
                }
                catch (Exception) { }
            }
        }

        public static List<MethodInfo> PatchSafe(Type patch)
        {
            try
            {
                Print("Patching " + patch.Name);
                var processor = harmony.CreateClassProcessor(patch);
                return processor.Patch();
            }
            catch (Exception e) { PrintException(e); }
            return null;
        }

        public static bool LoadSafe(Action action)
        {
#if DEBUG
            var watch = Stopwatch.StartNew();
#endif
            string name = action.Method.DeclaringType.Name + "." + action.Method.Name;

            try
            {
                Print($"Loading {name}");
                action();
#if DEBUG
                watch.Stop();
                PrintDebug("Loaded in milliseconds: " + watch.ElapsedMilliseconds);
#endif
                return true;
            }
            catch (Exception e)
            {
#if DEBUG
                watch.Stop();
#endif
                PrintException(e);
                return false;
            }
        }

        #endregion

        #region Log

        [System.Diagnostics.Conditional("DEBUG")]
        internal static void PrintDebug(string msg)
        {
            logger.Log(msg);
        }

        internal static void Print(string msg)
        {
            logger.Log(msg);
        }

        internal static void PrintError(string msg)
        {
            logger.Log("[Exception/Error] " + msg);
        }

        private static int _exceptionCount;
        internal static void PrintException(Exception ex)
        {
            if (_exceptionCount > 1000)
                return;

            logger.LogException(ex);

            _exceptionCount++;
            if (_exceptionCount > 1000)
                logger.Log("-- too many exceptions, future exceptions are suppressed");
        }

        #endregion

        #region Patches

        [HarmonyPatch]
        private static class Patches
        {
            [HarmonyPatch(typeof(MainMenuLoadingScreen), nameof(MainMenuLoadingScreen.EndLoading))]
            [HarmonyPriority(Priority.Normal)]
            [HarmonyPostfix]
            private static void Postfix1()
            {
                try
                {
                    OnBlueprintsLoaded();
                }
                catch (Exception ex) { logger?.LogException(ex); }
            }
        }

        #endregion
    }
}
