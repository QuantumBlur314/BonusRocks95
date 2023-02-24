using HarmonyLib;
using JetBrains.Annotations;
using OWML.Common;
using OWML.ModHelper;
using OWML.ModHelper.Events;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using UnityEngine;

namespace BonusRocks95
{
    public class BonusRocks95 : ModBehaviour
    {
        public bool bRHolesShrinkStars;
        public bool bRHolesWarpUnmarkedBodies;


        public static BonusRocks95 Instance;
        public void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;
        }

        private void Start()
        {
            BonusRocks95.Instance.ModHelper.Console.WriteLine($"{nameof(BonusRocks95)} rocks into battle 95 times.", MessageType.Success);

            // Example of accessing game code.
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                BonusRocks95.Instance.ModHelper.Console.WriteLine("Another day...", MessageType.Warning);
            };
            BonusRocks95.Instance.ModHelper.Console.WriteLine($"{nameof(BonusRocks95)} settings begin screaming backwards", MessageType.Info);
        }
        public override void Configure(IModConfig config)
        {
            BonusRocks95.Instance.bRHolesShrinkStars = BonusRocks95.Instance.ModHelper.Config.GetSettingsValue<bool>("Shrink Stars");
            ModHelper.Console.WriteLine($"Shrink Stars: {bRHolesShrinkStars}");
        }

        public virtual void 
    }
    [HarmonyPatch]
    public class BonusRocks95PatchClass
    {
        [HarmonyPrefix, HarmonyPatch(typeof(BlackHoleVolume), nameof(BlackHoleVolume.Vanish))]

        //Prevents blackholes from vanishing the Sun - still shrinks tho
        private static bool BlackHoleVolume_Vanish(OWRigidbody bodyToVanish)
        {
            return bodyToVanish != Locator._centerOfTheUniverse._staticReferenceFrame;
        }

    [HarmonyPrefix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.Shrink))]

        private static bool VanishVolume_Shrink(VanishVolume __instance, OWRigidbody bodyToShrink)

        {   //Whether sun shrinks in VanishVolumes
            if (BonusRocks95.Instance.bRHolesShrinkStars)
            { return true; }

            bool isTheSun = bodyToShrink == Locator._centerOfTheUniverse._staticReferenceFrame;
            return !isTheSun;

        }

    [HarmonyPostfix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.Shrink))]
        private static void VanishVolume_LogShrink(VanishVolume __instance, OWRigidbody bodyToShrink)

        {   //Debug: Send the game's list of _shrinkingBodies to the logs to find out if the Sun's stuck in the list
            BonusRocks95.Instance.ModHelper.Console.WriteLine($"_shrinkingBodies list contains:");
            foreach (var stupidbodies in __instance._shrinkingBodies) BonusRocks95.Instance.ModHelper.Console.WriteLine(stupidbodies.ToString());
            
        }
    [HarmonyPatch(typeof(WhiteHoleVolume), nameof(WhiteHoleVolume.AddToGrowQueue))]
        private static void WhiteHoleVolume_LogGrow(WhiteHoleVolume __instance, OWRigidbody bodyToGrow)

        {   //Debug: Log the game's list of _bodiesToGrow whenever AddToGrowQueue happens, to find out why the sun's not growing back
            BonusRocks95.Instance.ModHelper.Console.WriteLine($"_growQueue list contains:");
            foreach (var tinybodies in __instance._growQueue) BonusRocks95.Instance.ModHelper.Console.WriteLine(tinybodies.ToString());
        }
    }
}