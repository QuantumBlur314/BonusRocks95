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


    public class BonusRocks95 : ModBehaviour
    {
        public static List<OWRigidbody> _filteredBodies = new();  
        public static List<OWRigidbody> _starsAndCenters = new();
        public bool bRShrinkStars;                    //Toggles whether stars are on the shrink blacklist
        public bool bRPlanetsDontSlurp;             //Toggles whether 
        public static List<OWRigidbody> _bR95growQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _bR95growingBody;
        public float _bR95nextGrowCheckTime;
//WarpWindowTweaks
        public float newWarpPadWindow;
        public static List<NomaiWarpTransmitter> _windowTweakList = new();

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

                List_WarpTransmitters();
                ModHelper.Events.Unity.FireOnNextUpdate(
                    () =>
                {
                    List_Stars();
                    List_FilteredBodies();
                }
                );                                             
            };
        }



        public override void Configure(IModConfig config)
        {
            bRShrinkStars = config.GetSettingsValue<bool>("Shrink Stars");
            bRPlanetsDontSlurp = config.GetSettingsValue<bool>("Protect Planets/Moons Against Stunlock");
            newWarpPadWindow = config.GetSettingsValue<float>("Warp Pad Window (default 5)");

            List_FilteredBodies(); 
            UpdateWindows();


        }

        private void List_FilteredBodies()   //Finds all AstroObjects meeting the criteria of IsImmuneToVanish, finds their rigidbodies, and spits them into the _filteredBodies list
        {
            _filteredBodies = Resources.FindObjectsOfTypeAll<AstroObject>().
                Where(IsImmuneToVanish).Select(x => x.GetAttachedOWRigidbody()).ToList();  //Thanks to Xen12 for introducing me to this sorcery, I still barely understand it tho
            ModHelper.Console.WriteLine("Updated _filteredBodies list:");
            foreach (var filteredBody in _filteredBodies)
            {
                if (filteredBody != null)
                {
                    ModHelper.Console.WriteLine(filteredBody.ToString(),MessageType.Debug);
                }
            }
        }

        private bool IsImmuneToVanish(AstroObject astroObject)                //Asks if astroObject is a star/center (or a planet/moon if Stunlock protection is on); returns true if star, (and if planet)
        {
            if (astroObject != null)
            {
                var filter = astroObject.GetAstroObjectType() == AstroObject.Type.Star  
                || Locator._centerOfTheUniverse._staticReferenceFrame;

                if (Instance.bRPlanetsDontSlurp) filter =
                        filter                                                           //Yes, it's a star/centerOfTheUniverse (for all you "put a planet at the center of my system" freaks out there)
                        || astroObject.GetAstroObjectType() == AstroObject.Type.Planet  //or a planet,
                        || astroObject.GetAstroObjectType() == AstroObject.Type.Moon;  //or a moon
                return filter;                                                        //Don't warp it ("don't" means it returns true, as in "It's immune to vanishing" = true)
            }
            { return false; }                                                       //Otherwise, no, it's not immune, slurp to your dark bottomless heart's content
        }

        private void List_Stars()
        {
            _starsAndCenters = Resources.FindObjectsOfTypeAll<AstroObject>().Where(StarCenterDetector)?.Select(x => x.GetAttachedOWRigidbody()).ToList();
            ModHelper.Console.WriteLine("Stars Found",MessageType.Info);
            foreach (var unvanishable in _starsAndCenters)
            {
                if (unvanishable != null)
                {
                    Instance.ModHelper.Console.WriteLine(unvanishable.ToString());
                }
            }
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
                foreach (var starSlashCenter in _starsAndCenters)                   //for each tinybody object in the _bR95growQueue,
                {
                    if (starSlashCenter != null && StarHasSmallRigidbody(starSlashCenter) && !_bR95growQueue.Contains(starSlashCenter))
                    {
                        Instance.ModHelper.Console.WriteLine("Growing Stars...", MessageType.Debug);
                        _bR95growQueue = _starsAndCenters;            //sends them to the growQueue
                    }
                };
            }
            foreach (var normalStar in _starsAndCenters)                     //All stars/centers instantly become small, without ceremony, without animation.  Might make this a method, might not idfk
                if (bRShrinkStars && !StarHasSmallRigidbody(normalStar))
                {
                    normalStar.SetLocalScale(Vector3.one * 0.1f);                      
                    Instance.ModHelper.Console.WriteLine("Shrinking Stars...",MessageType.Debug);
                }
        }

        private void List_WarpTransmitters()  //Finds NomaiWarpTransmitters in scene, puts them into the _warpTweakQueue list, and prints their names to debug logs
        {
            _windowTweakList = Resources.FindObjectsOfTypeAll<NomaiWarpTransmitter>().ToList();
            ModHelper.Console.WriteLine("Transmitters Found", MessageType.Info);
            foreach (var platforms in _windowTweakList)
            {
                if (platforms != null)
                {
                    Instance.ModHelper.Console.WriteLine(platforms.ToString(), MessageType.Debug);
                }
            }
        }

        private void UpdateWindows()    //plugs your newWarpPadWindow value into each NomaiWarpTransmitter._alignmentWindow
        {
            foreach (var stupidpads in _windowTweakList)
            {
                if (stupidpads != null)
                {
                    stupidpads._alignmentWindow = Instance.newWarpPadWindow;
                }
            }
            Instance.ModHelper.Console.WriteLine($"Warp Pad Windows updated to: {newWarpPadWindow}",MessageType.Success);
        }

        //GROWQUEUE NONSENSE:

        private void FixedUpdate()  //ripped from WhiteHoleVolume.FixedUpdate then mangled beyond recognition
        {
            if (_bR95growingBody != null)
            {
                _bR95growingBody.SetLocalScale(_bR95growingBody.GetLocalScale() * 1.05f);
                if (_bR95growingBody.GetLocalScale().x >= 1f)
                {
                    _bR95growingBody.SetLocalScale(Vector3.one); ; //Absorbed FinishGrowing, since without the relativePosition stuff, it's literally just a single line
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
                if (hitCollider.attachedRigidbody != null)
                {
                    var bodyThatMightEnter = hitCollider.GetAttachedOWRigidbody();
                    if (bodyThatMightEnter != null && _filteredBodies.Contains(bodyThatMightEnter))
                    {
                        Instance.ModHelper.Console.WriteLine($"Prevented {hitCollider?.GetAttachedOWRigidbody()?.ToString()} from triggering {__instance} volume",MessageType.Debug);
                        return false;
                    }
                    Instance.ModHelper.Console?.WriteLine($"Let {hitCollider?.GetAttachedOWRigidbody()?.ToString()} through {__instance}",MessageType.Debug);
                }
                return true;
            }
        }
    }
}

