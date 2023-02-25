using Epic.OnlineServices;
using HarmonyLib;
using JetBrains.Annotations;
using OWML.Common;
using OWML.ModHelper;
using OWML.ModHelper.Events;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
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
        public static List<AstroObject> _AllAstroObjectsListWhy = new();  //UNTIL I FIGURE OUT HOW NOT TO DO THIS
        public static List<AstroObject> _VanishBlacklist = new();   //The goal: Add sun to blacklist by default, or any AstroObject.Type.Star then other stuff 
        public static List<AstroObject> _ShrinkBlacklist = new();   //ALSO THIS IS SUPPOSED TO SAVE POOR PLANETS FROM ENDING UP PERMASHRUNK      
        public bool bRHolesShrinkStars;                    //Toggles whether stars are on the shrink blacklist
        public bool bRHolesWarpUnmarkedBodies;
        public static List<OWRigidbody> _bR95growQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _bR95growingBody;
        public float _bR95nextGrowCheckTime;
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
                if (loadScene != OWScene.SolarSystem) return;                    //If the loaded scene isn't SolarSystem, disregard the rest of this method
                _AllAstroObjectsListWhy = Resources.FindObjectsOfTypeAll<AstroObject>().ToList();    //fills _AllAstroObjectsListWhy list with every astroobject (why
                                                                                                     
                StarsOnVanishBlacklist();                 //places stars on vanish blacklist

                ModHelper.Console.WriteLine($"AstroObjects on VanishBlacklist:", MessageType.Success);

                foreach (var astralobject in BonusRocks95._VanishBlacklist)                   //for each astral object in the _VanishBlacklist,
                {
                    if (astralobject != null)
                    {
                        BonusRocks95.Instance.ModHelper.Console.WriteLine(astralobject.ToString());         //Prints the occupants of _warpTweakQueue to the logs
                    }
                }
            };
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
        private void Update()   //Keybinding code lovingly stolen from BlackHolePortalGun by NagelId, who added keybinding to BHPG specifically because I suggested it.
        {
            if (!OWInput.IsInputMode(InputMode.Menu))                //if the player isn't in the menu (RECOMMEND THIS TO BLOCKS MOD PERSON)
            {
                BigBubbon = Keyboard.current[Big].wasPressedThisFrame; ;
                SmallBubbon = Keyboard.current[Small].wasPressedThisFrame;   //BHPG listened for .wasReleasedThisFrame here; if this doesn't work, just do that
            }

            if (BigBubbon && !BonusRocks95._bR95growQueue.Contains(Locator._centerOfTheUniverse._staticReferenceFrame))
            {

                AddToCustomGrowQueue(Locator._centerOfTheUniverse._staticReferenceFrame);
                BonusRocks95.Instance.ModHelper.Console.WriteLine($"GrowQueue updated:");
                foreach (var tinybodies in BonusRocks95._bR95growQueue)                   //for each tinybody object in the _bR95growQueue,
                {
                    if (tinybodies != null)
                    {
                        BonusRocks95.Instance.ModHelper.Console.WriteLine(tinybodies.ToString());         //Prints the occupants of _bR95growQueue to the logs
                    }
                };
            }
        }
        private void AddToCustomGrowQueue(OWRigidbody bodyToGrow)
        {
            bodyToGrow.SetLocalScale(Vector3.one * 0.1f);  //call AddSunToCustomGrowQueue(your preferred body here) to shrink it to 0.1x its current size, then watch it grow (why tho)

            if (!BonusRocks95._bR95growQueue.Contains(bodyToGrow))
            {
                BonusRocks95._bR95growQueue.Add(bodyToGrow);

            };
        }
        private static void StarsOnVanishBlacklist()                    //Blacklists all AstroObjects of type "Star" from being warped
        {
            foreach (var ofallthefellas in BonusRocks95._AllAstroObjectsListWhy)
            {
                if (ofallthefellas != null && ofallthefellas.GetAstroObjectType() == AstroObject.Type.Star)
                {
                    if (!BonusRocks95._VanishBlacklist.Contains(ofallthefellas))
                        BonusRocks95._VanishBlacklist.Add(ofallthefellas);
                };
            }
        }
        private void FixedUpdate()  //stolen from WhiteHoleVolume.FixedUpdate then mangled beyond recognition
        {
            if (_bR95growingBody != null)
            {
                _bR95growingBody.SetLocalScale(_bR95growingBody.GetLocalScale() * 1.05f);
                if (_bR95growingBody.GetLocalScale().x >= 1f)
                {
                    BR95FinishGrowing(_bR95growingBody);
                    _bR95growingBody = null;
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
                if (Time.time > _bR95nextGrowCheckTime)
                {
                    _bR95nextGrowCheckTime = Time.time + 1f;
                    _bR95growingBody = BonusRocks95._bR95growQueue[0];
                    BonusRocks95._bR95growQueue.RemoveAt(0);
                }
            }
        }
        private void BR95FinishGrowing(OWRigidbody body)  //When BR95FinishGrowing() is called, it will expect whatever's in its parentheses to be OWRigidbody, and will treat them as "body"
        {
            body.SetLocalScale(Vector3.one);
        }

        [HarmonyPatch]
        public class BonusRocks95PatchClass
        {

            [HarmonyPrefix, HarmonyPatch(typeof(BlackHoleVolume), nameof(BlackHoleVolume.Vanish))]

            //Prevents blackholes from vanishing the Sun - still shrinks tho
            private static bool DontVanishBlacklistedBodies(AstroObject bodyToVanish)
            {
                if (BonusRocks95._VanishBlacklist.Contains(bodyToVanish))
                { return true; }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.Shrink))]

            private static bool VanishVolume_Shrink(VanishVolume __instance, OWRigidbody bodyToShrink)

            {   //Whether sun shrinks in VanishVolumes  (note: method sucks, once I can control sun scale manually, change what part of the base code this affects)
                if (BonusRocks95.Instance.bRHolesShrinkStars)
                    return true;

                bool isTheSun = bodyToShrink == Locator._centerOfTheUniverse._staticReferenceFrame;  //at the moment this seems to prevent other things (such as BH fragments) from shrinking in VanishVolumes.  Fix this
                return !isTheSun;

            }

            [HarmonyPostfix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.Shrink))]
            private static void VanishVolume_LogShrink(VanishVolume __instance)

            {   //Debug: Send the game's list of _shrinkingBodies to the logs to find out if the Sun's stuck in the list
                BonusRocks95.Instance.ModHelper.Console.WriteLine($"_shrinkingBodies list contains:");
                foreach (var stupidbodies in __instance._shrinkingBodies) BonusRocks95.Instance.ModHelper.Console.WriteLine(stupidbodies.ToString());

            }


        }
    }
}