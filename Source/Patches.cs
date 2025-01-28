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
        "GameLevel/Room1/SimpleCutSceneFSM/FSM Animator/LogicRoot/[CutScene]", // Yi and Shuanshuan's dinner during intro
        "AG_S2/Room/Prefab/ControlRoom FSM Binding Tool/NPC_AICore_Base/NPC_AICore_Base_FSM/FSM Animator/LogicRoot/[CutScene]AI核心解鎖", // first Ruyi activation after finding nymph
        // skipping this leaves Yi stuck somewhere he can't get out of
        "GameLevel/Room1/SimpleCutSceneFSM_EnterVilliage/FSM Animator/LogicRoot/[CutScene]", // first time walking into PBV during intro
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
        }

        string id = "";
        if (__instance.name.EndsWith("_EnterScene")) {
            Log.Info($"skipping notification for {__instance.name} because transition 'cutscenes' are typically over before the player can even see the toast");
        } else {
            id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Cutscene");
        }

        CutsceneSkip.activeCutscene = (__instance, id);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "End")]
    private static void SimpleCutsceneManager_End(SimpleCutsceneManager __instance) {
        Log.Debug($"SimpleCutsceneManager_End {__instance.name}");
        if (CutsceneSkip.activeCutscene.Item1 == __instance)
            CutsceneSkip.activeCutscene = (null, "");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DialoguePlayer), "StartDialogue")]
    private static void DialoguePlayer_StartDialogue(DialoguePlayer __instance) {
        Log.Debug($"DialoguePlayer_StartDialogue {__instance.name}");
        var id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Dialogue");
        CutsceneSkip.dialogueSkipNotificationId = id;
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
        Log.Debug($"VideoPlayAction_OnStateEnterImplement {goPath}");
        if (skippableVideos.Contains(goPath)) {
            var id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Video");
            CutsceneSkip.activeVideo = (__instance, id);
        }
    }
    [HarmonyPrefix, HarmonyPatch(typeof(VideoPlayAction), "VideoClipDone")]
    private static void VideoPlayAction_VideoClipDone(VideoPlayAction __instance) {
        Log.Debug($"VideoPlayAction_VideoClipDone {__instance.name}");
        if (CutsceneSkip.activeVideo.Item1 == __instance)
            CutsceneSkip.activeVideo = (null, "");
    }

    // The Heng flashback in Power Reservoir got its own special implementation class instead of using SimpleCutsceneManager
    [HarmonyPrefix, HarmonyPatch(typeof(A2_SG4_Logic), "EnterLevelStart")]
    private static void A2_SG4_Logic_EnterLevelStart(A2_SG4_Logic __instance) {
        Log.Info($"A2_SG4_Logic_EnterLevelStart / Heng Power Reservoir flashback");
        var id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Heng Flashback");
        CutsceneSkip.activeA2SG4 = (__instance, id);
    }

    // The Yanlao fight also has a special implementation class not covered by our SimpleCutsceneManager patches
    [HarmonyPrefix, HarmonyPatch(typeof(A4_S5_Logic), "EnterLevelStart")]
    private static void A4_S5_Logic_EnterLevelStart(A4_S5_Logic __instance) {
        Log.Info($"A4_S5_Logic_EnterLevelStart / Sky Rending Claw Pre-Fight Scenes");
        var id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip Pre-Claw Fight Cutscenes");
        CutsceneSkip.activeA4S5 = (__instance, id);
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A4_S5_Logic), "FooGameComplete")]
    private static void A4_S5_Logic_FooGameComplete(A4_S5_Logic __instance) {
        Log.Info($"A4_S5_Logic_FooGameComplete / Sky Rending Claw Post-Fight Scenes");
        if (CutsceneSkip.activeA4S5.Item1 != null) {
            Notifications.CancelNotification(CutsceneSkip.activeA4S5.Item2);
        }
        var id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip Post-Claw Fight Cutscene");
        CutsceneSkip.activeA4S5 = (__instance, id);
    }
}