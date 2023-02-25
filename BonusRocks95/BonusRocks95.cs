using HarmonyLib;
using JetBrains.Annotations;
using OWML.Common;
using OWML.ModHelper;
using OWML.ModHelper.Events;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;

namespace BonusRocks95
{

    //ToDo list:  A.) hijack OWRigidBody's UpdteCenterOfMass to tweak ship's center of mass, B.) Increase warp tower tolerances + edit corresponding Nomai text 4TehLulz
    public class BonusRocks95 : ModBehaviour
    {
        public bool bRHolesShrinkStars;
        public bool bRHolesWarpUnmarkedBodies;
        public static List<OWRigidbody> _bR95growQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _growingBody;
        public float _nextGrowCheckTime;
        public Key Big;
        public bool BigBubbon;
        public Key Small;
        public bool SmallBubbon;

        public static BonusRocks95 Instance;



        public void Awake()
        {


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;

        }

        private void Start()
        {
            // Example of accessing game code.
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                BonusRocks95.Instance.ModHelper.Console.WriteLine("Another day...", MessageType.Warning);
            };
            BonusRocks95.Instance.ModHelper.Console.WriteLine($"{nameof(BonusRocks95)} begins screaming backwards", MessageType.Info);
        }
        public override void Configure(IModConfig config)
        {
            BonusRocks95.Instance.bRHolesShrinkStars = BonusRocks95.Instance.ModHelper.Config.GetSettingsValue<bool>("Shrink Stars");
            ModHelper.Console.WriteLine($"Shrink Stars: {bRHolesShrinkStars}");

            Big = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Big Your Ball"));
            ModHelper.Console.WriteLine($"Button of Big a Ball: {Big}");
            Small = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Small Your Ball"));
            ModHelper.Console.WriteLine($"Button of Small a Ball: {Small}");

        }

        private void Update()
        {
            if (!OWInput.IsInputMode(InputMode.Menu))   //Keybinding code lovingly stolen from BlackHolePortalGun by NagelId, who added keybinding to BHPG specifically because I suggested it.
            {
                BigBubbon = Keyboard.current[Big].wasPressedThisFrame; ;
                SmallBubbon = Keyboard.current[Small].wasPressedThisFrame;   //BHPG listened for .wasReleasedThisFrame here; if this doesn't work, just do that
            }

            if (BigBubbon)
            {

                AddSunToCustomGrowQueue(Locator._centerOfTheUniverse._staticReferenceFrame);
                BonusRocks95.Instance.ModHelper.Console.WriteLine($"GrowQueue updated:");
                foreach (var tinybodies in BonusRocks95._bR95growQueue) BonusRocks95.Instance.ModHelper.Console.WriteLine(tinybodies.ToString());
                CustomFixedUpdate();
            }
        }
        private void AddSunToCustomGrowQueue(OWRigidbody bodyToGrow)
        {
            bodyToGrow.SetLocalScale(Vector3.one * 0.1f);  //idk what this does but WhiteHoleVolume.AddToGrowQueue does it so I will, too

            if (!BonusRocks95._bR95growQueue.Contains(bodyToGrow))
            {
                BonusRocks95._bR95growQueue.Add(bodyToGrow);
            };



        }
        private void CustomFixedUpdate()  //stolen from WhiteHoleVolume.FixedUpdate then mangled beyond recognition
        {
            if (_growingBody != null)
            {
                _growingBody.SetLocalScale(_growingBody.GetLocalScale() * 1.05f);
                if (_growingBody.GetLocalScale().x >= 1f)
                {
                    BR95FinishGrowing(_growingBody);
                    _growingBody = null;
                    return;
                }
            }
            else if (BonusRocks95._bR95growQueue.Count > 0)
            {
                if (BonusRocks95._bR95growQueue[0] == null)
                {
                    BonusRocks95._bR95growQueue.RemoveAt(0);


                    return;
                }
                if (Time.time > _nextGrowCheckTime)
                {
                    _nextGrowCheckTime = Time.time + 1f;
                    _growingBody = BonusRocks95._bR95growQueue[0];
                    BonusRocks95._bR95growQueue.RemoveAt(0);

                }
            }
        }
        private void BR95FinishGrowing(OWRigidbody body)
        {
            body.SetLocalScale(Vector3.one);
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

            [HarmonyPostfix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.Shrink))]
            private static void VanishVolume_LogShrink(VanishVolume __instance, OWRigidbody bodyToShrink)

            {   //Debug: Send the game's list of _shrinkingBodies to the logs to find out if the Sun's stuck in the list
                BonusRocks95.Instance.ModHelper.Console.WriteLine($"_shrinkingBodies list contains:");
                foreach (var stupidbodies in __instance._shrinkingBodies) BonusRocks95.Instance.ModHelper.Console.WriteLine(stupidbodies.ToString());

            }
        }
    }
}