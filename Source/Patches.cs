﻿using HarmonyLib;
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
        // skipping this leaves enemies in unintended, possibly softlocking places, such as stuck inside walls
        "A1_S2_GameLevel/Room/Prefab/Gameplay2_Alina/Simple Binding Tool/SimpleCutSceneFSM_關門戰開頭演出/FSM Animator/LogicRoot/[CutScene]",
        // skipping this softlocks immediately
        "A2_S1/Room/Prefab/EnterPyramid_Acting/[CutScene]ActivePyramidAndEnter",
        "A3_S1/Room/Prefab/妹妹回憶_SimpleCutSceneFSM Variant/FSM Animator/LogicRoot/[CutScene]",
        "A4_S4/ZGunAndDoor/Shield Giant Bot Control Provider Variant_Cutscene/Hack Control Monster FSM/FSM Animator/LogicRoot/Cutscene/LogicRoot/[CutScene]",
        "A5_S5/Room/SimpleCutSceneFSM_JieChuan and Jee/FSM Animator/LogicRoot/[CutScene]",
        "AG_S2/Room/NPCs/議會演出相關Binding/ShanShan 軒軒分身 FSM/FSM Animator/CutScene/[CutScene] 食譜_團圓飯/FSM Animator/LogicRoot/[CutScene]",
        "GameLevel/Room/Prefab/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/[CutScene] 易公死亡", // = Eigong death, skipping leaves you trapped in her arena
        "A3_S5_BossGouMang_GameLevel/Room/Simple Binding Tool/BossGouMangLogic/[CutScene]/[CutScene]Goumang_Explosion_Drop/[Timeline]Goumang_Explosion_Drop",
        "A5_S2/Room/SimpleCutSceneFSM_A5妹妹回憶/FSM Animator/LogicRoot/[CutScene]",
        // skipping this leaves Yi stuck somewhere he can't get out of
        "A4_S3/Room/Prefab/CutScene_ChangeScene_FSM Variant/FSM Animator/LogicRoot/[CutScene]EnterScene", // funicular into BR
        "A11_S2/CutScene_ChangeScene_FSM Variant/FSM Animator/LogicRoot/[CutScene]EnterScene", // funicular into TRI
        "AG_GoHome/Room/Prefab/SimpleCutSceneFSM_搭公車/FSM Animator/LogicRoot/[CutScene]", // normal ending tram
        // skipping this leaves the camera stuck, not technically a softlock but still unplayable
        "A1_S1_GameLevel/Room/A1_S1_Tutorial_Logic/[CutScene]AfterTutorial_AI_Call/[Timeline]",
        // skipping this door opening animation leaves the door closed
        "A4_S3/Room/Prefab/ElementRoom/ElementDoor FSM/ElementDoor FSM/FSM Animator/LogicRoot/[CutScene]Eenter_A4SG4",
        // skipping this prevents a boss from dropping an item, i.e. breaks a randomizer location
        "A2_S5_ BossHorseman_GameLevel/Room/Simple Binding Tool/Boss_SpearHorse_Logic/[CutScene]SpearHorse_End",
        "A0_S6/Room/Prefab/SimpleCutSceneFSM_道長死亡/FSM Animator/LogicRoot/Cutscene_TaoChangPart2",
        // covered by the special case logic for Yanlao/Claw fight
        "A4_S5/A4_S5_Logic(DisableMeForBossDesign)/CUTSCENE_START",
        "A4_S5/A4_S5_Logic(DisableMeForBossDesign)/CUTSENE_EMERGENCY",
        "A4_S5/A4_S5_Logic(DisableMeForBossDesign)/CUTSCENE_Finish",
        // skips the post-PonR hallway, including all of the actual fighting, which is out of scope for this mod
        "A11_S2/Room/Prefab/EventBinder/OldBoy FSM Object/FSM Animator/LogicRoot/[CutScene]OldBoyFighting/[Timeline]",
    };

    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "PlayAnimation")]
    private static void SimpleCutsceneManager_PlayAnimation(SimpleCutsceneManager __instance) {
        var goPath = GetFullPath(__instance.gameObject);
        Log.Debug($"SimpleCutsceneManager_PlayAnimation {goPath}");
        if (skipDenylist.Contains(goPath)) {
            Log.Info($"not allowing skip for cutscene {goPath} because it's on the skip denylist");
            return;
        }

        if (__instance.name.EndsWith("[TimeLine]CrateEnter_L") || __instance.name.EndsWith("[TimeLine]CrateEnter_R")) {
            Log.Info($"not allowing skip for {goPath} because all crate exit 'cutscenes' I've tested instantly softlock when skipped");
            return;
        } else if (__instance.name == "[CutScene]調閱報告") {
            Log.Info($"not allowing skip for {goPath} because all \"[CutScene]調閱報告\" / Eigong lab report cutscenes risk softlocking when skipped");
            return;
        } else if (__instance.name.EndsWith("_EnterScene")) {
            Log.Info($"skipping toast for {__instance.name} because transition 'cutscenes' are typically over before the player can even see the toast");
        } else {
            ToastManager.Toast($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Cutscene");
        }

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
        ToastManager.Toast($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Dialogue");
    }

    // The credits videos aren't skippable, and the intro video is both vanilla skippable and not even a VideoPlayAction.
    // So with only 2 known video cutscenes that I wanted to and can skip, an allowlist seemed better than a denylist.
    private static HashSet<string> skippableVideos = new HashSet<string> {
        // true ending - Yi shooting the Rhizomatic Arrow
        "GameLevel/Room/Prefab/SimpleCutSceneFSM_結局_大爆炸/--[States]/FSM/[State] PlayCutSceneEnd/[Action] VideoPlayAction",
        // Heng flashback after Lady E fight - Yi's first fusang revival
        "A7_S6_Memory_Butterfly_CutScene_GameLevel/A7_S6_Cutscene FSM/--[States]/FSM/[State] PlayingVideo/[Action] VideoPlayAction",
    };

    [HarmonyPrefix, HarmonyPatch(typeof(VideoPlayAction), "OnStateEnterImplement")]
    private static void VideoPlayAction_OnStateEnterImplement(VideoPlayAction __instance) {
        var goPath = GetFullPath(__instance.gameObject);
        Log.Info($"VideoPlayAction_OnStateEnterImplement {goPath}");
        if (skippableVideos.Contains(goPath)) {
            CutsceneSkip.activeVideo = __instance;
            ToastManager.Toast($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Video");
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(VideoPlayAction), "VideoClipDone")]
    private static void VideoPlayAction_VideoClipDone(VideoPlayAction __instance) {
        Log.Debug($"VideoPlayAction_VideoClipDone {__instance.name}");
        if (CutsceneSkip.activeVideo == __instance)
            CutsceneSkip.activeVideo = null;
    }

    // Special cases

    [HarmonyPrefix, HarmonyPatch(typeof(A2_SG4_Logic), "EnterLevelStart")]
    private static void A2_SG4_Logic_EnterLevelStart(A2_SG4_Logic __instance) {
        Log.Info($"A2_SG4_Logic_EnterLevelStart {__instance.name}");
        ToastManager.Toast($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Heng Flashback");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(A4_S5_Logic), "EnterLevelStart")]
    private static void A4_S5_Logic_EnterLevelStart(A4_S5_Logic __instance) {
        Log.Info($"A4_S5_Logic_EnterLevelStart {__instance.name}");
        ToastManager.Toast($"Press {CutsceneSkip.SkipKeybindText()} to Skip Pre-Claw Fight Cutscenes");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A4_S5_Logic), "FooGameComplete")]
    private static void A4_S5_Logic_FooGameComplete(A4_S5_Logic __instance) {
        Log.Info($"A4_S5_Logic_FooGameComplete {__instance.name}");
        ToastManager.Toast($"Press {CutsceneSkip.SkipKeybindText()} to Skip Post-Claw Fight Cutscene");
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