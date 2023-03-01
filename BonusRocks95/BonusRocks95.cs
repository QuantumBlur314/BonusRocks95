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
using System;
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

        public static List<OWRigidbody> _VanishBlacklist = new();   //The goal: Add sun to blacklist by default, or any AstroObject.Type.Star then other stuff 
        public static List<OWRigidbody> _ShrinkBlacklist = new();   //SHOULDN'T NEED; ONLY ACTIVE WHEN       
        public bool bRShrinkStars;                    //Toggles whether stars are on the shrink blacklist
        public bool bRHolesWarpUnmarkedBodies;             //Toggles whether 
        public static List<OWRigidbody> _bR95growQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _bR95growingBody;
        public float _bR95nextGrowCheckTime;
        public Key Big;                                                 //Grows all OWRigidbodies on _VanishBlacklist to normal size
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


                ModHelper.Events.Unity.FireOnNextUpdate(() =>
                {
                    UpdateBlacklist();
                });                                                     //any Star-type AstroBodies in _AllAstroObjectsListWhy get put on the _VanishBlacklist

                ModHelper.Console.WriteLine($"RIGIDBODIES on VanishBlacklist:", MessageType.Success);

                foreach (var rigidbody in BonusRocks95._VanishBlacklist)                   //for each astral object in the _AllAstroObjectsListWhy
                {
                    if (rigidbody != null)
                    {
                        BonusRocks95.Instance.ModHelper.Console.WriteLine(rigidbody.ToString());         //Prints the occupants to the logs
                    }
                }
            };
        }
        public override void Configure(IModConfig config)
        {
            BonusRocks95.Instance.bRShrinkStars = BonusRocks95.Instance.ModHelper.Config.GetSettingsValue<bool>("Shrink Stars");

            BonusRocks95.Instance.bRHolesWarpUnmarkedBodies = BonusRocks95.Instance.ModHelper.Config.GetSettingsValue<bool>("Stunlock Unwitting AstroObjects");


            Big = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Big Your Ball"));
            Small = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Small Your Ball"));

            UpdateBlacklist();
        }
        private void Update()   //Keybinding code lovingly stolen from BlackHolePortalGun by NagelId, who added keybinding to BHPG specifically because I suggested it.
        {
            if (!OWInput.IsInputMode(InputMode.Menu))                //if the player isn't in the menu (RECOMMEND THIS TO BLOCKS MOD PERSON)
            {
                BigBubbon = Keyboard.current[Big].wasPressedThisFrame; ;         //GOAL: 
                SmallBubbon = Keyboard.current[Small].wasPressedThisFrame;   //BHPG listened for .wasReleasedThisFrame here; if this doesn't work, just do that
            }
            if (BigBubbon)  //GOAL: stop looking for the sun in growqueue, although don't add anything not already there
            {
                FillCustomGrowQueue();                //add everything in the shrink blacklist to customgrowqueue
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
        //IF bRHolesShrinkStars IS ACTIVE, JUST SHRINK ALL STARS IMMEDIATELY.  GET RID OF THE VANISH PATCH
        //PATCH BLACK HOLE INTERACTIONS HIGHER UP TO NIP THEM IN THE BUD, LET PLANET WARP TOGGLE ACCESS THAT
        private void FillCustomGrowQueue()
        {
            _bR95growQueue = Resources.FindObjectsOfTypeAll<OWRigidbody>().
                     Where(RigidBodyIsSmall)?.Select(x => x.GetAttachedOWRigidbody()).ToList();
        }
        private bool RigidBodyIsSmall(OWRigidbody oWRigidbody)
        {
            var isTiny = oWRigidbody?.GetLocalScale().x < 1f;
            return isTiny;
        }
        private void UpdateBlacklist()
        {
            _VanishBlacklist = Resources.FindObjectsOfTypeAll<AstroObject>().
                Where(AstrObjFilter).Select(x => x.GetAttachedOWRigidbody()).ToList();  //Finds AstroObjects of any type specified in ApplyFilter, then spits out the attached rigidbody to _VanishBlacklist,
            foreach (var unvanishable in BonusRocks95._VanishBlacklist)
            {
                if (unvanishable != null)
                {
                    BonusRocks95.Instance.ModHelper.Console.WriteLine(unvanishable.ToString());
                }
            }                                                                               //gets attached OWRigidbodies for each , then puts them into _VanishBlacklist
        }
        private bool AstrObjFilter(AstroObject astroObject)                //When called, returns any AstroObject that fits its filters (MAYBE MAKE A DIFFERENT APPLYFILTER , THANKS XEN)
        {
            var filter = astroObject.GetAstroObjectType() == AstroObject.Type.Star
                || Locator._centerOfTheUniverse._staticReferenceFrame;

            if (BonusRocks95.Instance.bRHolesWarpUnmarkedBodies) filter = filter
                    || astroObject.GetAstroObjectType() == AstroObject.Type.Planet
                    || astroObject.GetAstroObjectType() == AstroObject.Type.Moon;
            return filter;
        }

        private bool StarCenterDetector(AstroObject isStar)
        { return isStar.GetAstroObjectType() == AstroObject.Type.Star || Locator._centerOfTheUniverse._staticReferenceFrame; }

        //GROWQUEUE NONSENSE:
        private void AddToCustomGrowQueue(OWRigidbody bodyToGrow)
        {

            {
                bodyToGrow.SetLocalScale(Vector3.one * 0.1f);  //call AddSunToCustomGrowQueue(your preferred body here) to shrink it to 0.1x its current size, then watch it grow (why tho)
                if (!BonusRocks95._bR95growQueue.Contains(bodyToGrow) && RigidBodyIsSmall(bodyToGrow))
                { BonusRocks95._bR95growQueue.Add(bodyToGrow); }
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
            private static bool DontVanishBlacklistedBodies(OWRigidbody bodyToVanish)
            {
                return !BonusRocks95._VanishBlacklist.Contains(bodyToVanish);     //No longer messy, thanks Xen!  Now bodyToVanish is already an OWRigidbody
            }

            [HarmonyPrefix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.Shrink))]

            private static bool VanishVolume_Shrink(OWRigidbody bodyToShrink)        //if "Stunlock AstroObjects" is active, let them go all the way through (unless they're stars/etc).  If false, don't shrink them or vanish them or anything

            {   //Whether sun shrinks in VanishVolumes  (note: method sucks, once I can control sun scale manually, change what part of the base code this affects)
                if (BonusRocks95.Instance.bRShrinkStars && BonusRocks95.Instance.bRHolesWarpUnmarkedBodies && bodyToShrink != null)  //If stars are ok to shrink, and Unmarked Bodies are getting warped anyway...
                { return true; }
                if (!BonusRocks95.Instance.bRShrinkStars && BonusRocks95.Instance.bRHolesWarpUnmarkedBodies)  //If "Shrink Stars" is false, but "Warp unmarked bodieS" is true
                { return !BonusRocks95.Instance.StarCenterDetector(bodyToShrink.GetRequiredComponent<AstroObject>()); }
                if (!BonusRocks95.Instance.bRShrinkStars && !BonusRocks95.Instance.bRHolesWarpUnmarkedBodies)       //"Shrink Stars" and "Warp unmarked bodies" both false
                { return !BonusRocks95.Instance.AstrObjFilter(bodyToShrink.GetRequiredComponent<AstroObject>()); }

                //GOAL: if StunlockAstrObjects is false, don't shrink anything on the _VanishBlacklist UNLESS ShrinkStars is active, then make an exception for stars
                { return true; }  

                                   

            }


        }
    }
}