using System.Collections.Generic;
using UnityEngine;

namespace Main
{
    public static class EmergencyHelper
    {
        /* === Cached References === */
        public static Camera mainCamera;
        public static Transform hud;

        public static Transform currentMeetingAnim;
        public static Transform currentReportAnim;
        public static GameObject meetingBackground;

        public static Transform reportAnimTransform;
        public static GameObject originalReportText;
        public static GameObject originalReportStab;
        public static GameObject clonedReportText;
        public static GameObject clonedReportStab;
        public static Transform killOverlayParent;

        /* === States === */
        public static bool is_meeting_alter_pending;

        /// <summary>
        /// Refresh references every frame (called in ModUpdate.Update).
        /// </summary>
        public static void UpdateReferences()
        {
            // --- Cache Main Camera ---
            if (mainCamera == null)
            {
                var camObj = GameObject.Find("Main Camera");
                if (camObj != null) mainCamera = camObj.GetComponent<Camera>();
            }

            if (mainCamera == null) return;

            // --- Cache HUD ---
            if (hud == null && mainCamera != null)
            {
                var t = mainCamera.transform.Find("Hud");
                if (t != null) hud = t;
            }

            if (hud == null) return;

            // --- Cache Meeting Animations ---
            currentMeetingAnim = null;
            foreach (string path in ModUpdate.emergencyPaths)
            {
                var emergency = hud.Find(path);
                if (emergency != null)
                {
                    currentMeetingAnim = emergency;
                    break;
                }
            }

            // --- Cache Report Animations ---
            currentReportAnim = null;
            foreach (string path in ModUpdate.reportBodyPaths)
            {
                var report = hud.Find(path);
                if (report != null)
                {
                    currentReportAnim = report;
                    break;
                }
            }

            // --- Cache Meeting Background ---
            meetingBackground = null;
            var meetingHubBg = hud.Find("MeetingHub(Clone)/Background");
            if (meetingHubBg != null) meetingBackground = meetingHubBg.gameObject;
        }

        public static void InitializeReportAndMeetingAnimations(
            List<string> reportBodyPaths,
            List<string> emergencyPaths,
            ref Transform reportAnim,
            ref Transform meetingAnim)
        {
            if (hud == null) return;

            // Report
            foreach (string path in reportBodyPaths)
            {
                var reportBodyAnim = hud.Find(path);
                if (reportBodyAnim != null)
                {
                    reportBodyAnim.gameObject.SetActive(false);
                    reportAnim = reportBodyAnim;
                    CacheReportOriginals(reportAnim);
                    break;
                }
            }

            // Meeting
            foreach (string path in emergencyPaths)
            {
                var anim = hud.Find(path);
                if (anim != null)
                {
                    anim.gameObject.SetActive(false);
                    meetingAnim = anim;
                    CacheReportOriginals(meetingAnim);
                    break;
                }
            }
        }

        public static void CheckAndMarkMeetingAnimationPending(ref bool waitForMeetingAnimation)
        {
            if (!waitForMeetingAnimation) return;

            if (IsReportOrMeetingAnimationActive())
            {
                is_meeting_alter_pending = true;
                waitForMeetingAnimation = false;
            }
        }

        public static void InitializeMeetingAndReportObjects()
        {
            if (hud == null) return;

            // Meeting
            if (currentMeetingAnim != null)
            {
                DisableChildren(currentMeetingAnim, "TextBg", "SpeedLines", "yellowtape");
                reportAnimTransform = currentMeetingAnim;
                CacheReportOriginals(currentMeetingAnim);
            }

            // Report
            if (currentReportAnim != null)
            {
                DisableChildren(currentReportAnim, "TextBg", "SpeedLines", "yellowtape");
                reportAnimTransform = currentReportAnim;
                CacheReportOriginals(currentReportAnim);
            }
        }


        public static void StartKillOverlayIfPending(KillOverlayAnimator kill_animator)
        {
            if (kill_animator == null) return;

            kill_animator.StartAnimation(1.9f);
            DisableOriginalReportObjects();

            if (meetingBackground != null)
                meetingBackground.SetActive(false);

            is_meeting_alter_pending = false;
        }

        private static void DisableChildren(Transform parent, params string[] names)
        {
            foreach (var name in names)
            {
                var child = parent.Find(name);
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        public static void SetClonePosition(ref GameObject go, Vector3 localPos)
        {
            if (go == null) return;
            go.transform.localPosition = localPos;
        }

        public static void CacheReportOriginals(Transform reportAnim)
        {
            reportAnimTransform = reportAnim;
            if (reportAnimTransform == null) return;

            var textT = reportAnimTransform.Find("Text (TMP)");
            originalReportText = textT != null ? textT.gameObject : null;

            var stabT = reportAnimTransform.Find("killstabplayerstill");
            originalReportStab = stabT != null ? stabT.gameObject : null;

            if (killOverlayParent == null && hud != null)
                killOverlayParent = hud.Find("KillOverlay/QuadParent");
        }

        public static void SpawnReportClones()
        {
            if (reportAnimTransform == null || hud == null) return;

            bool isMeeting = currentMeetingAnim != null;

            // Text clone
            if (clonedReportText == null && originalReportText != null)
            {
                clonedReportText = UnityEngine.Object.Instantiate(originalReportText, hud, false);
                clonedReportText.SetActive(true);
                originalReportText.SetActive(false);

                clonedReportText.transform.localPosition = new Vector3(0f, -0.2f, -136f);
                clonedReportText.transform.localRotation = Quaternion.identity;
            }

            // Stab clone
            if (clonedReportStab == null && originalReportStab != null)
            {
                clonedReportStab = UnityEngine.Object.Instantiate(originalReportStab, hud, false);
                clonedReportStab.SetActive(true);
                originalReportStab.SetActive(false);

                clonedReportStab.transform.localRotation = Quaternion.identity;

                if (isMeeting)
                {
                    clonedReportStab.transform.localPosition = new Vector3(-0.7f, 1.45f, -135f);
                    clonedReportStab.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
                else
                {
                    clonedReportStab.transform.localPosition = new Vector3(0f, 1f, -135f);
                    clonedReportStab.transform.localScale = Vector3.one;
                }
            }
        }

        public static void DisableOriginalReportObjects()
        {
            if (originalReportText != null) originalReportText.SetActive(false);
            if (originalReportStab != null) originalReportStab.SetActive(false);
        }

        public static void CleanupReportClones()
        {
            if (clonedReportText != null)
            {
                UnityEngine.Object.Destroy(clonedReportText);
                clonedReportText = null;
            }
            if (clonedReportStab != null)
            {
                UnityEngine.Object.Destroy(clonedReportStab);
                clonedReportStab = null;
            }
        }

        public static bool IsReportOrMeetingAnimationActive()
        {
            return currentReportAnim != null || currentMeetingAnim != null;
        }
    }
}
