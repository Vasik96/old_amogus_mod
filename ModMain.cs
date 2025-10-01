using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using InnerNet;
using Rewired;
using Sentry.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Main
{
    [BepInPlugin("com.vasik96.old_amogus", "Old Among Us", "1.0")]
    public class ModEntry : BasePlugin
    {
        private GameObject? updateHandler;

        public override void Load()
        {
            Debug.Log("[Old Among Us] -> Mod loaded.");

            var harmony = new Harmony("com.vasik96.old_amogus");
            harmony.PatchAll();

            ClassInjector.RegisterTypeInIl2Cpp<ModUpdate>();
            SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>(OnSceneLoaded));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                ModManager.Instance.ShowModStamp();
            }
            StartUpdateLoop();
        }

        private void StartUpdateLoop()
        {
            if (updateHandler == null)
            {
                updateHandler = new GameObject("AmongUsMenu");
                updateHandler.AddComponent<ModUpdate>();
                UnityEngine.Object.DontDestroyOnLoad(updateHandler);
            }
        }
    }
    public class ModUpdate : MonoBehaviour
    {
        public static ModUpdate Instance { get; private set; }

        KillOverlayAnimator killAnimation = new KillOverlayAnimator();

        /* OLD MEETING AND REPORT VARIABLES */
        private bool lastActive;
        private Vector3 lastLocalPos, lastLocalScale;
        private Quaternion lastLocalRot;
        private float lastPosChangeTime = 0f;
        private float lastScaleChangeTime = 0f;
        private float lastRotChangeTime = 0f;
        private float lastActiveChangeTime = 0f;

        public bool wait_for_meeting_animation = false;

        public static readonly string[] emergencyPaths = new string[]
        {
        "KillOverlay/EmergencyAnimation(Clone)",
        "KillOverlay/AirshipEmergencyAnimation(Clone)",
        "KillOverlay/FungleEmergencyAnimation(Clone)"
        };


        public static readonly string[] reportBodyPaths = new string[]
        {
        "KillOverlay/ReportBodyAnimation(Clone)",
        "KillOverlay/AirshipReportBodyAnimation(Clone)",
        "KillOverlay/FungleReportBodyAnimation(Clone)"
        };


        void Awake()
        {
            Instance = this;
        }
        void Start()
        {

        }

        void Update()
        {
            if (killAnimation == null)
                killAnimation = new KillOverlayAnimator();

            // Refresh references once per frame
            EmergencyHelper.UpdateReferences();

            EmergencyHelper.CheckAndMarkMeetingAnimationPending(ref wait_for_meeting_animation);

            if (EmergencyHelper.is_meeting_alter_pending)
            {
                EmergencyHelper.InitializeMeetingAndReportObjects();

                EmergencyHelper.StartKillOverlayIfPending(killAnimation);

            }

            if (killAnimation.CurrentPhase == KillOverlayAnimator.AnimationPhase.Paused)
            {
                if (EmergencyHelper.clonedReportText == null && EmergencyHelper.clonedReportStab == null)
                {
                    EmergencyHelper.SpawnReportClones();
                }
            }

            if (killAnimation.CurrentPhase == KillOverlayAnimator.AnimationPhase.Idle ||
                killAnimation.CurrentPhase == KillOverlayAnimator.AnimationPhase.None ||
                killAnimation.CurrentPhase == KillOverlayAnimator.AnimationPhase.Shrinking)
            {
                if (EmergencyHelper.clonedReportText != null || EmergencyHelper.clonedReportStab != null)
                {
                    EmergencyHelper.CleanupReportClones();
                }

                EmergencyHelper.is_meeting_alter_pending = false;
            }

            killAnimation.Update();
        }

    }
}