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

    /* TODO:
     * - https://github.com/asd9176506911298/NineSolsPlugin/blob/e15b3d4797d5b89785553be03c7480341889eb5e/Plugin.cs#L1543-L1560
     * is the "skip all skippables" code in Cheat Menu, link this when explaining how this mod is different
     * 
     * - entering AFD triggers A1_S3_GameLevel/Room/Prefab/CrateEntering_Logic/[TimeLine]CrateEnter_R despite Yi not actually entering a crate???
     * 
     * - A2_SG4 / PR Heng mostly works, but skipping it during the "call" start can lead to the call open sound looping forever
     * leads to a softlock when another call tries to start
     * 
     * - the sequence where Yi reactivates FSP may also be broken by this, but that could've been debug mod teleports
     * 
     * - yuki's Cheat Menu already has LCtrl to skip stuff, see https://github.com/asd9176506911298/NineSolsPlugin/blob/e15b3d4797d5b89785553be03c7480341889eb5e/Plugin.cs#L1543-L1560
     *      impl appears to be TrySkip()ing all ISkippables, so for "cutscenes" that's the same as what I'm doing
     * 
     * - skipping the sanctum entry scene leaves Yi's movement speed un-slowed inside, despite the slow walk animation getting used
     */

    private static List<string> skipDenylist = new List<string> {
        // skipping this leaves enemies in unintended, possibly softlocking places, such as stuck inside walls
        "A1_S2_GameLevel/Room/Prefab/Gameplay2_Alina/Simple Binding Tool/SimpleCutSceneFSM_關門戰開頭演出/FSM Animator/LogicRoot/[CutScene]",
        // skipping this softlocks immediately
        "A2_S1/Room/Prefab/EnterPyramid_Acting/[CutScene]ActivePyramidAndEnter",
        "A3_S1/Room/Prefab/妹妹回憶_SimpleCutSceneFSM Variant/FSM Animator/LogicRoot/[CutScene]",
        "A4_S4/ZGunAndDoor/Shield Giant Bot Control Provider Variant_Cutscene/Hack Control Monster FSM/FSM Animator/LogicRoot/Cutscene/LogicRoot/[CutScene]",
        "A5_S5/Room/SimpleCutSceneFSM_JieChuan and Jee/FSM Animator/LogicRoot/[CutScene]",
        "AG_S2/Room/NPCs/議會演出相關Binding/ShanShan 軒軒分身 FSM/FSM Animator/CutScene/[CutScene] 食譜_團圓飯/FSM Animator/LogicRoot/[CutScene]",
        // skipping this leaves Yi stuck somewhere he can't get out of
        "A4_S3/Room/Prefab/CutScene_ChangeScene_FSM Variant/FSM Animator/LogicRoot/[CutScene]EnterScene", // funicular into BR
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
    };

    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "PlayAnimation")]
    private static void SimpleCutsceneManager_PlayAnimation(SimpleCutsceneManager __instance) {
        var goPath = GetFullPath(__instance.gameObject);
        Log.Info($"SimpleCutsceneManager_PlayAnimation {goPath}");
        if (skipDenylist.Contains(goPath)) {
            Log.Info($"not allowing skip for cutscene {goPath} because it's on the skip denylist");
            return;
        }

        if (__instance.name.EndsWith("[TimeLine]CrateEnter_L") || __instance.name.EndsWith("[TimeLine]CrateEnter_R")) {
            Log.Info($"not allowing skip for {goPath} because all crate exit 'cutscenes' I've tested instantly softlock when skipped");
            return;
        } else if (__instance.name.EndsWith("_EnterScene")) {
            Log.Info($"skipping toast for {__instance.name} because transition 'cutscenes' are typically over before the player can even see the toast");
        } else {
            ToastManager.Toast($"Press Ctrl+K to Skip This Cutscene");
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
        ToastManager.Toast($"Press Ctrl+K to Skip This Dialogue");
    }

    // Special cases

    [HarmonyPrefix, HarmonyPatch(typeof(A2_SG4_Logic), "EnterLevelStart")]
    private static void A2_SG4_Logic_EnterLevelStart(A2_SG4_Logic __instance) {
        Log.Info($"A2_SG4_Logic_EnterLevelStart {__instance.name}");
        ToastManager.Toast($"Press Ctrl+K to Skip This Heng Flashback");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(A4_S5_Logic), "EnterLevelStart")]
    private static void A4_S5_Logic_EnterLevelStart(A4_S5_Logic __instance) {
        Log.Info($"A4_S5_Logic_EnterLevelStart {__instance.name}");
        ToastManager.Toast($"Press Ctrl+K to Skip Pre-Claw Fight Cutscenes");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A4_S5_Logic), "FooGameComplete")]
    private static void A4_S5_Logic_FooGameComplete(A4_S5_Logic __instance) {
        Log.Info($"A4_S5_Logic_FooGameComplete {__instance.name}");
        ToastManager.Toast($"Press Ctrl+K to Skip Post-Claw Fight Cutscene");
    }

    // Exploratory patches. These can all be commented out.

    // begin *_Logic classes

    [HarmonyPrefix, HarmonyPatch(typeof(AG_S4_Logic), "Start")]
    private static void AG_S4_Logic_Start(AG_S4_Logic __instance) {
        Log.Info($"!!! AG_S4_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AG_S3_Logic), "Start")]
    private static void AG_S3_Logic_Start(AG_S3_Logic __instance) {
        Log.Info($"!!! AG_S3_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A1_S7_Logic), "Start")]
    private static void A1_S7_Logic_Start(A1_S7_Logic __instance) {
        Log.Info($"!!! A1_S7_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A1_S7_Logic), "BackToMainMenu")]
    private static void A1_S7_Logic_BackToMainMenu(A1_S7_Logic __instance) {
        Log.Info($"!!! A1_S7_Logic_BackToMainMenu {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S63_Logic), "Start")]
    private static void AA_S63_Logic_Start(AA_S63_Logic __instance) {
        Log.Info($"!!! AA_S63_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S61_Logic), "Start")]
    private static void AA_S61_Logic_Start(AA_S61_Logic __instance) {
        Log.Info($"!!! AA_S61_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S65_Logic), "Start")]
    private static void AA_S65_Logic_Start(AA_S65_Logic __instance) {
        Log.Info($"!!! AA_S65_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S41_Logic), "Start")]
    private static void AA_S41_Logic_Start(AA_S41_Logic __instance) {
        Log.Info($"!!! AA_S41_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S57_Logic), "Start")]
    private static void AA_S57_Logic_Start(AA_S57_Logic __instance) {
        Log.Info($"!!! AA_S57_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S78_Logic), "Start")]
    private static void AA_S78_Logic_Start(AA_S78_Logic __instance) {
        Log.Info($"!!! AA_S78_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S77_Logic), "Start")]
    private static void AA_S77_Logic_Start(AA_S77_Logic __instance) {
        Log.Info($"!!! AA_S77_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A5_S4A_Logic), "Start")]
    private static void A5_S4A_Logic_Start(A5_S4A_Logic __instance) {
        Log.Info($"!!! A5_S4A_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S81_Logic), "Start")]
    private static void AA_S81_Logic_Start(AA_S81_Logic __instance) {
        Log.Info($"!!! AA_S81_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A5_S4c_Logic), "Start")]
    private static void A5_S4c_Logic_Start(A5_S4c_Logic __instance) {
        Log.Info($"!!! A5_S4c_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A4_SG1_Logic), "Start")]
    private static void A4_SG1_Logic_Start(A4_SG1_Logic __instance) {
        Log.Info($"!!! A4_SG1_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S42_Logic), "Start")]
    private static void AA_S42_Logic_Start(AA_S42_Logic __instance) {
        Log.Info($"!!! AA_S42_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AA_S66_Logic), "Start")]
    private static void AA_S66_Logic_Start(AA_S66_Logic __instance) {
        Log.Info($"!!! AA_S66_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(Memory_Logic), "Start")]
    private static void Memory_Logic_Start(Memory_Logic __instance) {
        Log.Info($"!!! Memory_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A1_S1_Tutorial_Logic), "InitializeTutorial")]
    private static void A1_S1_Tutorial_Logic_InitializeTutorial(A1_S1_Tutorial_Logic __instance) {
        Log.Info($"!!! A1_S1_Tutorial_Logic_InitializeTutorial {__instance.name} !!!");
    }
    // confirmed A1_S1_Tutorial_Logic is for the parry+talisman+heal tutorial right after saving Shuanshuan
    [HarmonyPrefix, HarmonyPatch(typeof(A1_S1_Tutorial_Logic), "TutorialEnd")]
    private static void A1_S1_Tutorial_Logic_TutorialEnd(A1_S1_Tutorial_Logic __instance) {
        Log.Info($"!!! A1_S1_Tutorial_Logic_TutorialEnd {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(VR_A5_2_PistonRow_Logic), "Start")]
    private static void VR_A5_2_PistonRow_Logic_Start(VR_A5_2_PistonRow_Logic __instance) {
        Log.Info($"!!! VR_A5_2_PistonRow_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AG_S2_GetButterFly_Logic), "LearnAbility")]
    private static void AG_S2_GetButterFly_Logic_LearnAbility(AG_S2_GetButterFly_Logic __instance) {
        Log.Info($"!!! AG_S2_GetButterFly_Logic_LearnAbility {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(P3_R6_FactorySwitch_Logic), "Start")]
    private static void P3_R6_FactorySwitch_Logic_Start(P3_R6_FactorySwitch_Logic __instance) {
        Log.Info($"!!! P3_R6_FactorySwitch_Logic_Start {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(P3_R6_FactorySwitch_Logic), "FactorySwitchOn")]
    private static void P3_R6_FactorySwitch_Logic_FactorySwitchOn(P3_R6_FactorySwitch_Logic __instance) {
        Log.Info($"!!! P3_R6_FactorySwitch_Logic_FactorySwitchOn {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(Boss_SpearHorse_Logic_Demo), "Start")]
    private static void Boss_SpearHorse_Logic_Demo_Start(Boss_SpearHorse_Logic_Demo __instance) {
        Log.Info($"!!! Boss_SpearHorse_Logic_Demo_Start {__instance.name} !!!");
    }

    // ISkippables
    // Yanlao???
    [HarmonyPrefix, HarmonyPatch(typeof(A4_S5_Logic), "EnterLevelStart")]
    private static void A4_S5_Logic_EnterLevelStart(A4_S5_Logic __instance) {
        Log.Info($"!!! A4_S5_Logic_EnterLevelStart {__instance.name} !!!");
        ToastManager.Toast($"!!! A4_S5_Logic_EnterLevelStart");
    }

    // IResetters
    [HarmonyPrefix, HarmonyPatch(typeof(A1_S1_Tutorial_Logic), "EnterLevelReset")]
    private static void A1_S1_Tutorial_Logic_EnterLevelReset(A1_S1_Tutorial_Logic __instance) {
        Log.Info($"!!! A1_S1_Tutorial_Logic_EnterLevelReset {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A1_S1_Tutorial_Logic), "ExitLevelAndDestroy")]
    private static void A1_S1_Tutorial_Logic_ExitLevelAndDestroy(A1_S1_Tutorial_Logic __instance) {
        Log.Info($"!!! A1_S1_Tutorial_Logic_ExitLevelAndDestroy {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A4_S1_PistonRow_Logic), "EnterLevelReset")]
    private static void A4_S1_PistonRow_Logic_EnterLevelReset(A4_S1_PistonRow_Logic __instance) {
        Log.Info($"!!! A4_S1_PistonRow_Logic_EnterLevelReset {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A4_S1_PistonRow_Logic), "ExitLevelAndDestroy")]
    private static void A4_S1_PistonRow_Logic_ExitLevelAndDestroy(A4_S1_PistonRow_Logic __instance) {
        Log.Info($"!!! A4_S1_PistonRow_Logic_ExitLevelAndDestroy {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(Boss_SpearHorse_Logic), "CheckBossKilled")]
    private static void Boss_SpearHorse_Logic_CheckBossKilled(Boss_SpearHorse_Logic __instance) {
        Log.Info($"!!! Boss_SpearHorse_Logic_CheckBossKilled {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(Boss_SpearHorse_Logic), "ExitLevelAndDestroy")]
    private static void Boss_SpearHorse_Logic_ExitLevelAndDestroy(Boss_SpearHorse_Logic __instance) {
        Log.Info($"!!! Boss_SpearHorse_Logic_ExitLevelAndDestroy {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A2_S5_Leaderboard_Logic), "EnterLevelReset")]
    private static void A2_S5_Leaderboard_Logic_EnterLevelReset(A2_S5_Leaderboard_Logic __instance) {
        Log.Info($"!!! A2_S5_Leaderboard_Logic_EnterLevelReset {__instance.name} !!!");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A2_S5_Leaderboard_Logic), "ExitLevelAndDestroy")]
    private static void A2_S5_Leaderboard_Logic_ExitLevelAndDestroy(A2_S5_Leaderboard_Logic __instance) {
        Log.Info($"!!! A2_S5_Leaderboard_Logic_ExitLevelAndDestroy {__instance.name} !!!");
    }

    // end *_Logic classes

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