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
using Epic.OnlineServices.Stats;

namespace BonusRocks95
{

    //ToDo list:  A.) hijack OWRigidBody's UpdteCenterOfMass to tweak ship's center of mass, B.) Increase warp tower tolerances + edit corresponding Nomai text 4TehLulz
    public class BonusRocks95 : ModBehaviour
    {
        public static List<OWRigidbody> _filteredBodies = new();
        public static List<OWRigidbody> _stars = new();   //The goal: Add sun to blacklist by default, or any AstroObject.Type.Star then other stuff 
        public bool bRShrinkStars;                    //Toggles whether stars are on the shrink blacklist
        public bool bRPlanetsDontSlurp;             //Toggles whether 
        public static List<OWRigidbody> _bR95growQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _bR95growingBody;
        public float _bR95nextGrowCheckTime;

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
                    List_Stars();                           //ALSO MAKE A LIST OF BODIES TO BLACKLIST
                    List_FilteredBodies();
                });                                                  //any Star-type AstroBodies in _AllAstroObjectsListWhy get put on the 
            };
        }
        public override void Configure(IModConfig config)
        {
            Instance.bRShrinkStars = Instance.ModHelper.Config.GetSettingsValue<bool>("Shrink Stars");
            Instance.bRPlanetsDontSlurp = Instance.ModHelper.Config.GetSettingsValue<bool>("Immunize Major Bodies Against VanishVolumes");
            //UpdateBlacklist();          //BLACKLIST NO LONGER NECESSARY, JUST SHRINK SUNS WITH TOGGLE
            List_FilteredBodies();
        }

        private void List_FilteredBodies()                         //Making it case-by-case means I might not need this blacklist at all, but it will be checking every time a collision occurs.  idk
        {
            _filteredBodies = Resources.FindObjectsOfTypeAll<AstroObject>().
                Where(IsImmuneToVanish).Select(x => x.GetAttachedOWRigidbody()).ToList();  //Finds AstroObjects of any type specified in ApplyFilter, then spits out the attached rigidbody to _VanishBlacklist,
            ModHelper.Console.WriteLine("Updated _filteredBodies list:");
            foreach (var filteredBody in _filteredBodies)
            {
                if (filteredBody != null)
                {
                    ModHelper.Console.WriteLine(filteredBody.ToString());
                }
            }
        }

        private bool IsImmuneToVanish(AstroObject astroObject)                //Asks if astroObject is a star/center (or a planet/moon if WarpBodies is on); returns true if star, (and if planet) (THANKS XEN)
        {
            if (astroObject != null)
            {
                var filter = astroObject.GetAstroObjectType() == AstroObject.Type.Star
                || Locator._centerOfTheUniverse._staticReferenceFrame;

                if (Instance.bRPlanetsDontSlurp) filter =
                        filter                                                           //Yes, it's either a star
                        || astroObject.GetAstroObjectType() == AstroObject.Type.Planet  //or a planet,
                        || astroObject.GetAstroObjectType() == AstroObject.Type.Moon;  //or a moon, don't warp it
                return filter;                                                        //Don't warp it ("don't" means it returns true, as in "It's immune to vanishing" = true)
            }
            { return false; }                                                       //Otherwise, no, it's not immune, slurp to your dark bottomless heart's content
        }

        private void List_Stars()
        {
            _stars = Resources.FindObjectsOfTypeAll<AstroObject>().Where(StarCenterDetector)?.Select(x => x.GetAttachedOWRigidbody()).ToList();
            ModHelper.Console.WriteLine("Stars Found:");
            foreach (var unvanishable in _stars)
            {
                if (unvanishable != null)
                {
                    Instance.ModHelper.Console.WriteLine(unvanishable.ToString());
                }
            }                                                                               //gets attached OWRigidbodies for each
        }

        private bool StarHasSmallRigidbody(OWRigidbody rigidStar)
        {
            var theAstro = rigidStar.GetRequiredComponentInChildren<AstroObject>();
            if (rigidStar?._scaleRoot != null && StarCenterDetector(theAstro))
            {
                float itsSize = (float)(rigidStar?.GetLocalScale().x);
                return (itsSize < 1f);
            }
            return false;
        }

        private bool StarCenterDetector(AstroObject testIfStar)  //Asks if ORWigidbody's AstroObject type to see if it's a star.  Answer "yes" or "no", do you don't you, will you won't you, answer yes or no?
        {
            return testIfStar.GetAstroObjectType() == AstroObject.Type.Star;
        }

        public void Update()   //Keybinding code lovingly stolen from BlackHolePortalGun by NagelId, who added keybinding to BHPG specifically because I suggested it.
        {

            if (!bRShrinkStars)  //GOAL: stop looking for the sun in growqueue, although don't add anything not already there
            {
                //add everything in the shrink blacklist to customgrowqueue

                foreach (var star in _stars)                   //for each tinybody object in the _bR95growQueue,
                {
                    if (star != null && StarHasSmallRigidbody(star) && !_bR95growQueue.Contains(star))
                    {
                        Instance.ModHelper.Console.WriteLine("Growing Stars...");
                        //{ _bR95growQueue.Add(star); }
                        _bR95growQueue = _stars;
                    }
                };
            }
            foreach (var normalStar in _stars)
                if (bRShrinkStars && !StarHasSmallRigidbody(normalStar))
                {
                    normalStar.SetLocalScale(Vector3.one * 0.1f);
                    Instance.ModHelper.Console.WriteLine("Shrinking Stars...");
                }
        }

        //GROWQUEUE NONSENSE:

        private void FixedUpdate()  //stolen from WhiteHoleVolume.FixedUpdate then mangled beyond recognition
        {
            if (_bR95growingBody != null)
            {
                _bR95growingBody.SetLocalScale(_bR95growingBody.GetLocalScale() * 1.05f);
                if (_bR95growingBody.GetLocalScale().x >= 1f)
                {
                    _bR95growingBody.SetLocalScale(Vector3.one); ; //BR95FinishGrowing(_bR95growingBody);  //Absorbed BR95FinishGrowing, since it's literally a single line without the relativePosition stuff
                    _bR95growingBody = null;
                    return;
                }
            }
            else if (_bR95growQueue.Count > 0)
            {
                if (_bR95growQueue[0] == null)
                {
                    _bR95growQueue.RemoveAt(0);
                    return;
                }
                if (Time.time > _bR95nextGrowCheckTime)
                {
                    _bR95nextGrowCheckTime = Time.time + 1f;
                    _bR95growingBody = _bR95growQueue[0];
                    _bR95growQueue.RemoveAt(0);
                }
            }
        }

        private static bool PleaseMaamMayIPrivateMessageYou(Collider bumpedCollider)
        {
            {
                if (bumpedCollider.attachedRigidbody != null)  //The crime about to be committed was first performed by LocoChoco in Slate's Shipyard.  I desecrate in the craters of giants
                {
                    var bodyThatMightEnter = bumpedCollider.attachedRigidbody.GetComponent<AstroObject>();  //
                    if (bodyThatMightEnter == null)  //if the AstroObject of the hitCollider's attached rigidbody is null,
                    {
                        bodyThatMightEnter = bumpedCollider.attachedRigidbody.GetComponentInChildren<AstroObject>(); //dig for the AstroObject in the rigidbody's children instead
                    }
                    if (bodyThatMightEnter != null && Instance.IsImmuneToVanish(bodyThatMightEnter)) //if the code-roomba found an AstroObject attached to bodyThatMightEnter,
                    {
                        Instance.ModHelper.Console.WriteLine($"Prevented {bumpedCollider?.GetAttachedOWRigidbody()?.ToString()} from vanishing");
                        return false;
                    }
                }
                return true;
            }
        }

        //private void BR95FinishGrowing(OWRigidbody body)  //When BR95FinishGrowing() is called, it will expect whatever's in its parentheses to be OWRigidbody, and will treat them as "body"
        //{
        // body.SetLocalScale(Vector3.one);      //note how, when you're doing something TO a class, it's the called parameter, then .MethodYouWantToExertOnIt(valuesThatTweakMethod)
        //}
        //JUST PATCH OnTriggerEnter AND BE DONE WITH IT ALREADY
        [HarmonyPatch]
        public class BonusRocks95PatchClass
        {
            [HarmonyPrefix, HarmonyPatch(typeof(VanishVolume), nameof(VanishVolume.OnTriggerEnter))]
            private static bool MayItEnter(Collider hitCollider, VanishVolume __instance)    //PARAMETER MUST BE NAMED SAME AS BASE-GAME, DINGUS
            {
                if (hitCollider.attachedRigidbody != null)  //The crime about to be committed was first performed by LocoChoco in Slate's Shipyard.  I desecrate in the craters of giants
                {
                    var bodyThatMightEnter = hitCollider.GetAttachedOWRigidbody();  //
                    if (bodyThatMightEnter != null && _filteredBodies.Contains(bodyThatMightEnter)) //  PUT IMMUNETOVANISH IN PleaseMaam Method ; if the code-roomba found an AstroObject attached to bodyThatMightEnter,
                    {
                        Instance.ModHelper.Console.WriteLine($"Prevented {hitCollider?.GetAttachedOWRigidbody()?.ToString()} from triggering {__instance} volume");
                        return false;
                    }
                    Instance.ModHelper.Console?.WriteLine($"Let {hitCollider?.GetAttachedOWRigidbody()?.ToString()} through {__instance}");
                }
                return true;
            }
        }
    }
}

