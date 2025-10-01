using HarmonyLib;
using Main;
using UnityEngine;

namespace old_amogus_mod
{
    public class Patches
    {
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        public static class MeetingHud_Start_Patch
        {
            public static void Postfix()
            {
                Debug.Log(">>> MEETING STARTED <<<");

                Debug.Log("Waiting for meeting/report animation to spawn...");
                ModUpdate.Instance.wait_for_meeting_animation = true;
                
            }
        }
    }
}
