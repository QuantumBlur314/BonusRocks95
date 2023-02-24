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

    //ToDo list:  A.) hijack OWRigidBody's UpdteCenterOfMass to tweak ship's center of mass, B.) Increase warp tower tolerances + edit corresponding Nomai text 4TehLulz
    public class BonusRocks95 : ModBehaviour
    {
        public bool bRHolesShrinkStars;
        public bool bRHolesWarpUnmarkedBodies;
        public static List<OWRigidbody> _growQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _growingBody;
        public float _nextGrowCheckTime;
        public SectorDetector _detectorInspector;
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
        public static void GetSectorDetectors( )
        {if (Resources) {}; }

    public static void GrowSun(SectorDetector __instance);
        {  if (SectorDetector._detectorInspector != null)
            { SectorDetector._detectorInspector.OnEnterSector += BonusRocks95.GrowSun; }
        };
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

        {   //Whether sun shrinks in VanishVolumes  (note: method sucks, once I can control sun scale manually, change what part of the base code this affects)
            if (BonusRocks95.Instance.bRHolesShrinkStars)
                return true;

            bool isTheSun = bodyToShrink == Locator._centerOfTheUniverse._staticReferenceFrame;
            return !isTheSun;
        
        }


        [HarmonyPrefix, HarmonyPatch(typeof(PauseMenuManager), nameof(PauseMenuManager.OnDeactivatePauseMenu))]

        private static bool AddSunToCustomGrowQueue(OWRigidbody __instance)
        {
            if (BonusRocks95.Instance.bRHolesShrinkStars || __instance.CompareTag("Probe") || __instance.CompareTag("Player") || __instance.CompareTag("Ship") || __instance.CompareTag("NomaiShuttleBody"))
                return true;  //do nothing if Shrink Stars is enabled, nor if the __instance is any of these losers

            bool itsTheSun = __instance == Locator._centerOfTheUniverse._staticReferenceFrame;
            BonusRocks95.Instance.ModHelper.Console.WriteLine($"Is it the Sun?");

            if (!itsTheSun)
                return true; //if OWRigidbody is the sun, continue running. Otherwise, stop (will probably just stop running if a non-sun rigidbody exists anywhere, dammit)


            __instance.SetLocalScale(Vector3.one * 0.1f);  //idk what this does but WhiteHoleVolume.AddToGrowQueue does it so I will, too

            if (!BonusRocks95._growQueue.Contains(__instance))  //wait, will this target EVERY instance of OWRigidBody, not just the sun?  I only want the sun in this Q_Q
            {
                BonusRocks95._growQueue.Add(__instance);
                BonusRocks95.Instance.ModHelper.Console.WriteLine($"GrowQueue updated:");
                foreach (var tinybodies in BonusRocks95._growQueue) BonusRocks95.Instance.ModHelper.Console.WriteLine(tinybodies.ToString());
            }
            return true;



        }
        private static void CustomFixedUpdate(OWRigidbody _growingBody)  //stolen from WhiteHoleVolume.FixedUpdate then mangled beyond recognition
        {
            if (_growingBody != null)
            {
                BonusRocks95.Instance._growingBody.SetLocalScale(BonusRocks95.Instance._growingBody.GetLocalScale() * 1.05f);
                if (BonusRocks95.Instance._growingBody.GetLocalScale().x >= 1f)
                {
                    BR95FinishGrowing(BonusRocks95.Instance._growingBody);
                    BonusRocks95.Instance._growingBody = null;
                    return;
                }
            }
            else if (BonusRocks95._growQueue.Count > 0)
            {
                if (BonusRocks95._growQueue[0] == null)
                {
                    BonusRocks95._growQueue.RemoveAt(0);

                    return;
                }
                if (Time.time > BonusRocks95.Instance._nextGrowCheckTime)
                {
                    BonusRocks95.Instance._nextGrowCheckTime = Time.time + 1f;
                    BonusRocks95.Instance._growingBody = BonusRocks95._growQueue[0];
                    BonusRocks95._growQueue.RemoveAt(0);

                }
            }
        }

        private static bool BR95FinishGrowing(OWRigidbody body)
        {
            body.SetLocalScale(Vector3.one);
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.Shrink))]
        private static void VanishVolume_LogShrink(VanishVolume __instance, OWRigidbody bodyToShrink)

        {   //Debug: Send the game's list of _shrinkingBodies to the logs to find out if the Sun's stuck in the list
            BonusRocks95.Instance.ModHelper.Console.WriteLine($"_shrinkingBodies list contains:");
            foreach (var stupidbodies in __instance._shrinkingBodies) BonusRocks95.Instance.ModHelper.Console.WriteLine(stupidbodies.ToString());

        }
    }
}