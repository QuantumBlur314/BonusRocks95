using HarmonyLib;
using JetBrains.Annotations;
using OWML.Common;
using OWML.ModHelper;
using OWML.ModHelper.Events;
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

        public void Start()
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
    }

    [HarmonyPatch]
    public class BonusRocks95PatchClass
    {
        [HarmonyPrefix, HarmonyPatch(typeof(BlackHoleVolume), nameof(BlackHoleVolume.Vanish))]

        public static bool BlackHoleVolume_Vanish(OWRigidbody bodyToVanish)
        {
            return bodyToVanish != Locator._centerOfTheUniverse._staticReferenceFrame;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.Shrink))]
        public static bool VanishVolume_Shrink(OWRigidbody bodyToShrink)
        {
            if (BonusRocks95.Instance.bRHolesShrinkStars)
            { return true; }

            bool isTheSun = bodyToShrink == Locator._centerOfTheUniverse._staticReferenceFrame;
            return !isTheSun;

        }
 

    
    }
}