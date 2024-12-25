using HarmonyLib;
using NineSolsAPI;
using System.Collections.Generic;
using UnityEngine;

namespace CutsceneSkip;

[HarmonyPatch]
public class Patches {
    public static string GetFullPath(GameObject go) {
        var transform = go.transform;
        List<string> pathParts = new List<string>();
        while (transform != null) {
            pathParts.Add(transform.name);
            transform = transform.parent;
        }
        pathParts.Reverse();
        return string.Join("/", pathParts);
    }

    private static List<string> skipDenylist = new List<string> {
        // skipping these "cutscenes" leaves enemies in unintended, possibly softlocking places, such as stuck inside walls
        "A1_S2_GameLevel/Room/Prefab/Gameplay2_Alina/Simple Binding Tool/SimpleCutSceneFSM_關門戰開頭演出/FSM Animator/LogicRoot/[CutScene]",
    };

    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "PlayAnimation")]
    private static void SimpleCutsceneManager_PlayAnimation(SimpleCutsceneManager __instance) {
        var goPath = GetFullPath(__instance.gameObject);
        Log.Debug($"SimpleCutsceneManager_PlayAnimation {goPath}");
        if (skipDenylist.Contains(goPath)) {
            Log.Info($"not allowing skip for cutscene {goPath} because it's on the skip denylist");
            return;
        }

        if (__instance.name.EndsWith("_EnterScene"))
            Log.Info($"skipping toast for {__instance.name} because transition 'cutscenes' are typically over before the player can even see the toast");
        else
            ToastManager.Toast($"Press Ctrl+K to Skip This Cutscene");

        CutsceneSkip.activeCutscene = __instance;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "End")]
    private static void SimpleCutsceneManager_End(SimpleCutsceneManager __instance) {
        Log.Debug($"SimpleCutsceneManager_End {__instance.name}");
        if (CutsceneSkip.activeCutscene == __instance)
            CutsceneSkip.activeCutscene = null;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DialoguePlayer), "StartDialogue")]
    private static void DialoguePlayer_StartDialogue(DialoguePlayer __instance) {
        Log.Info($"DialoguePlayer_StartDialogue {__instance.name}");
        ToastManager.Toast($"Press Ctrl+K to Skip This Dialogue");
    }

    // Exploratory patches. These can all be commented out.
    /*
    [HarmonyPatch(typeof(SkippableManager), nameof(SkippableManager.RegisterSkippable))]
    [HarmonyPrefix]
    private static void SkippableManager_RegisterSkippable(SkippableManager __instance, ISkippable skippable) {
        Log.Info($"SkippableManager_RegisterSkippable is MonoBehaviour = {(skippable is MonoBehaviour)}");
        Log.Info($"SkippableManager_RegisterSkippable {(skippable as MonoBehaviour)?.name} - {skippable.GetType()} - {(skippable as MonoBehaviour)?.transform?.parent?.name}");
    }
    [HarmonyPatch(typeof(SkippableManager), nameof(SkippableManager.UnRegisterSkippable))]
    [HarmonyPrefix]
    private static void SkippableManager_UnRegisterSkippable(SkippableManager __instance, ISkippable skippable) {
        Log.Info($"SkippableManager_UnRegisterSkippable is MonoBehaviour = {(skippable is MonoBehaviour)}");
        Log.Info($"SkippableManager_UnRegisterSkippable {(skippable as MonoBehaviour)?.name} - {skippable.GetType()} - {(skippable as MonoBehaviour)?.transform?.parent?.name}");
    }
    [HarmonyPatch(typeof(SkippableManager), nameof(SkippableManager.ClearReference))]
    [HarmonyPrefix]
    private static void SkippableManager_ClearReference(SkippableManager __instance) {
        Log.Info($"SkippableManager_ClearReference");
    }
    [HarmonyPatch(typeof(SkippableManager), nameof(SkippableManager.TrySkip))]
    [HarmonyPrefix]
    private static void SkippableManager_TrySkip(SkippableManager __instance) {
        Log.Info($"SkippableManager_TrySkip");
    }

    [HarmonyPatch(typeof(SimpleCutsceneManager), nameof(SimpleCutsceneManager.Play), [])]
    [HarmonyPrefix]
    private static void SimpleCutsceneManager_Play(SimpleCutsceneManager __instance) {
        Log.Info($"SimpleCutsceneManager_Play 0-ary {__instance.name}");
    }
    [HarmonyPatch(typeof(SimpleCutsceneManager), nameof(SimpleCutsceneManager.Play), [typeof(Action)])]
    [HarmonyPrefix]
    private static void SimpleCutsceneManager_Play(SimpleCutsceneManager __instance, Action callback) {
        Log.Info($"SimpleCutsceneManager_Play 1-ary {__instance.name}");
    }
    [HarmonyPatch(typeof(SimpleCutsceneManager), nameof(SimpleCutsceneManager.Pause))]
    [HarmonyPrefix]
    private static void SimpleCutsceneManager_Pause(SimpleCutsceneManager __instance) {
        Log.Info($"SimpleCutsceneManager_Pause {__instance.name}");
    }
    [HarmonyPatch(typeof(SimpleCutsceneManager), nameof(SimpleCutsceneManager.Resume))]
    [HarmonyPrefix]
    private static void SimpleCutsceneManager_Resume(SimpleCutsceneManager __instance) {
        Log.Info($"SimpleCutsceneManager_Resume {__instance.name}");
    }
    [HarmonyPatch(typeof(SimpleCutsceneManager), "SetPauseLoop")]
    [HarmonyPrefix]
    private static void SimpleCutsceneManager_SetPauseLoop(SimpleCutsceneManager __instance) {
        Log.Info($"SimpleCutsceneManager_SetPauseLoop {__instance.name}");
    }
    //[HarmonyPatch(typeof(SimpleCutsceneManager), "EnterLevelAwake")]
    //[HarmonyPrefix]
    //private static void SimpleCutsceneManager_EnterLevelAwake(SimpleCutsceneManager __instance) {
    //    Log.Info($"SimpleCutsceneManager_EnterLevelAwake {__instance.name}");
    //}
    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "PlayWithoutLockControl")]
    private static void SimpleCutsceneManager_PlayWithoutLockControl(SimpleCutsceneManager __instance) {
        Log.Info($"SimpleCutsceneManager_PlayWithoutLockControl {__instance.name}");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "BeforePlay")]
    private static void SimpleCutsceneManager_BeforePlay(SimpleCutsceneManager __instance) {
        Log.Info($"SimpleCutsceneManager_BeforePlay {__instance.name}");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ICutScene), "PlayCutscene")]
    private static void ICutScene_PlayCutscene(ICutScene __instance) {
        Log.Info($"ICutScene_PlayCutscene {__instance.name}");
    }

    [HarmonyPatch(typeof(BubbleDialogueController), "ShowNode")]
    [HarmonyPrefix]
    private static void BubbleDialogueController_ShowNode(BubbleDialogueController __instance) {
        Log.Info($"BubbleDialogueController_ShowNode {__instance.name}");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DialoguePlayer), "TextProgress", [typeof(bool)])]
    private static void DialoguePlayer_TextProgress(DialoguePlayer __instance, bool BubbleChanged) {
        Log.Info($"DialoguePlayer_TextProgress {__instance.name} {BubbleChanged}");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(DialoguePlayer), "UpdateCharacter")]
    private static void DialoguePlayer_UpdateCharacter(DialoguePlayer __instance) {
        Log.Info($"DialoguePlayer_UpdateCharacter {__instance.name}");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(DialoguePlayer), "EndDialogue")]
    private static void DialoguePlayer_EndDialogue(DialoguePlayer __instance) {
        Log.Info($"DialoguePlayer_EndDialogue {__instance.name}");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(DialoguePlayer), "PlayVoice")]
    private static void DialoguePlayer_PlayVoice(DialoguePlayer __instance) {
        Log.Info($"DialoguePlayer_PlayVoice {__instance.name}");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DialogueBubble), "ShowBubble")]
    private static void DialogueBubble_ShowBubble(DialogueBubble __instance) {
        Log.Info($"DialogueBubble_ShowBubble {__instance.name}");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(DialogueBubble), "ProgressShowText")]
    private static void DialogueBubble_ProgressShowText(DialogueBubble __instance) {
        Log.Info($"DialogueBubble_ProgressShowText {__instance.name}");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(DialogueBubble), "EndProgressText")]
    private static void DialogueBubble_EndProgressText(DialogueBubble __instance) {
        Log.Info($"DialogueBubble_EndProgressText {__instance.name}");
    }
    */
}