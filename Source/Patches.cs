using HarmonyLib;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Dialogue;
using System;

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
        // skipping this leaves enemies you need to kill stuck inside or behind walls
        "A1_S2_GameLevel/Room/Prefab/Gameplay2_Alina/Simple Binding Tool/SimpleCutSceneFSM_關門戰開頭演出/FSM Animator/LogicRoot/[CutScene]", // AF(E) locked room after upper node
        "GameLevel/Room/Prefab/村民避難所_階段 FSM Object/FSM Animator/View/村民避難所ControlRoom/Phase3(二次入侵)/General FSM A0_S10 二次入侵/FSM Animator/LogicRoot/[CutScene] 戰鬥前", // 2nd PBV invasion in GD
        "GameLevel/Room/Prefab/村民避難所_階段 FSM Object/FSM Animator/View/村民避難所ControlRoom/Phase3(二次入侵)/General FSM A0_S10 二次入侵/FSM Animator/LogicRoot/[CutScene] 戰鬥前二進", // ^ if you die and come back
        // skipping this softlocks immediately
        "A2_S1/Room/Prefab/EnterPyramid_Acting/[CutScene]ActivePyramidAndEnter",
        "A3_S1/Room/Prefab/妹妹回憶_SimpleCutSceneFSM Variant/FSM Animator/LogicRoot/[CutScene]",
        "A4_S4/ZGunAndDoor/Shield Giant Bot Control Provider Variant_Cutscene/Hack Control Monster FSM/FSM Animator/LogicRoot/Cutscene/LogicRoot/[CutScene]",
        "A5_S5/Room/SimpleCutSceneFSM_JieChuan and Jee/FSM Animator/LogicRoot/[CutScene]",
        "GameLevel/Room/Prefab/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/[CutScene] 易公死亡", // = Eigong death, skipping leaves you trapped in her arena
        "A3_S5_BossGouMang_GameLevel/Room/Simple Binding Tool/BossGouMangLogic/[CutScene]/[CutScene]Goumang_Explosion_Drop/[Timeline]Goumang_Explosion_Drop",
        "A5_S2/Room/SimpleCutSceneFSM_A5妹妹回憶/FSM Animator/LogicRoot/[CutScene]",
        "GameLevel/Room1/SimpleCutSceneFSM/FSM Animator/LogicRoot/[CutScene]", // Yi and Shuanshuan's dinner during intro"
        "AG_S2/Room/Prefab/ControlRoom FSM Binding Tool/NPC_AICore_Base/NPC_AICore_Base_FSM/FSM Animator/LogicRoot/[CutScene]AI核心解鎖", // first Ruyi activation after finding nymph
        "A4_S5/Room/TaoChangSniper_SimpleCutSceneFSM/FSM Animator/LogicRoot/[CutScene]", // Yanlao fight teaser on entering BR gauntlet
        // skipping this leaves Yi stuck somewhere he can't get out of
        "GameLevel/Room1/SimpleCutSceneFSM_EnterVilliage/FSM Animator/LogicRoot/[CutScene]", // first time walking into PBV during intro
        "A4_S3/Room/Prefab/CutScene_ChangeScene_FSM Variant/FSM Animator/LogicRoot/[CutScene]EnterScene", // funicular from IW to BR
        "A4_S2/Room/Prefab/ElevatorGroup/CutScene_ChangeScene_FSM Variant 斜坡電梯/FSM Animator/LogicRoot/[CutScene]EnterScene", // funicular from BR to IW
        "A11_S2/CutScene_ChangeScene_FSM Variant/FSM Animator/LogicRoot/[CutScene]EnterScene", // funicular from TRC to TRI
        "A11_S1/Room/CutScene_ChangeScene_FSM Variant 斜坡電梯/FSM Animator/LogicRoot/[CutScene]EnterScene", // funicular from TRI to TRC
        "AG_GoHome/Room/Prefab/SimpleCutSceneFSM_搭公車/FSM Animator/LogicRoot/[CutScene]", // normal ending tram
        "A5_S5/Room/Prefab/[Mech]PodLift FSM 換景Ver (樓上 to 樓下)/[Mech]PodLift FSM/FSM Animator/LogicRoot/[CutScene] ReceiveTeleport_FromBelow", // Shengwu Hall elevator arrival
        // skipping this leaves the camera stuck, not technically a softlock but still unplayable
        "A1_S1_GameLevel/Room/A1_S1_Tutorial_Logic/[CutScene]AfterTutorial_AI_Call/[Timeline]", // the quick tutorial at the start of AF(M)
        // skipping this door opening animation leaves the door closed
        "A4_S3/Room/Prefab/ElementRoom/ElementDoor FSM/ElementDoor FSM/FSM Animator/LogicRoot/[CutScene]Eenter_A4SG4",
        // skipping this entry animation prevents Yi from actually entering the place
        "A7_S1/Room/Prefab/A7_S1_三階段FSM/FSM Animator/Phase2_A7Entry/花入口 FSM Object/FSM Animator/LogicRoot/[CutScene] 進入演出", // Lady E soulscape 1st entrance
        // skipping this prevents Yi from receiving an item / makes a randomizer location perma-missable
        "A2_S5_ BossHorseman_GameLevel/Room/Simple Binding Tool/Boss_SpearHorse_Logic/[CutScene]SpearHorse_End", // Yingzhao drop
        "A0_S6/Room/Prefab/SimpleCutSceneFSM_道長死亡/FSM Animator/LogicRoot/Cutscene_TaoChangPart2", // Yanlao flower
        "AG_S2/Room/Prefab/ControlRoom FSM Binding Tool/NPC_AICore_Base/NPC_AICore_Base_FSM/FSM Animator/LogicRoot/NPC_AICore_FSM/General FSM Object/Animator(FSM)/LogicRoot/[CutScene]原始細胞", // Super Mutant Buster (present Ji's Hair later)
        "AG_S2/Room/Prefab/ControlRoom FSM Binding Tool/NPC_AICore_Base/NPC_AICore_Base_FSM/FSM Animator/LogicRoot/NPC_AICore_FSM/General FSM Object/Animator(FSM)/LogicRoot/[CutScene]血清&原始細胞", // Super Mutant Buster (present both items at once)
        // covered by the special case logic for Yanlao/Claw fight
        "A4_S5/A4_S5_Logic(DisableMeForBossDesign)/CUTSCENE_START",
        "A4_S5/A4_S5_Logic(DisableMeForBossDesign)/CUTSENE_EMERGENCY",
        "A4_S5/A4_S5_Logic(DisableMeForBossDesign)/CUTSCENE_Finish",
        // these "cutscenes" are actual gameplay segments the player is supposed to fight through, so skipping them is out of scope for this mod
        "A11_S2/Room/Prefab/EventBinder/OldBoy FSM Object/FSM Animator/LogicRoot/[CutScene]OldBoyFighting/[Timeline]", // post-PonR hallway
        "A3_S1/Room/Prefab/Gameplay_Boat/Bell_Boat FSM/Boat_Timeline FSM Object  Variant/FSM Animator/View/[CutScene]DragonBoat_Moving/[Timeline]", // LYR dragon boat ride
        "A3_S3/Room/Prefab/Gameplay_Boat/Bell_Boat FSM/Boat_Timeline FSM Object  Variant/FSM Animator/View/[CutScene]DragonBoat_Moving/[Timeline]", // W&OS dragon boat ride
        // these are background dialogue bubbles in Lady E's hot springs, not a useful thing to "skip" and likely confusing
        "A2_Stage_Remake/Room/Prefab/FallingTeleportTrickBackgroundProvider/A7_HotSpring/溫泉場景Setting FSM Object/FSM Animator/View/SPA/PinkSkin/Pink/SimpleCutSceneFSM_八仙無限murmur/FSM Animator/LogicRoot/[CutScene]",
        "A2_Stage_Remake/Room/Prefab/FallingTeleportTrickBackgroundProvider/A7_HotSpring/溫泉場景Setting FSM Object/FSM Animator/View/SPA/PinkSkin/Pink_Odd/SimpleCutSceneFSM_八仙無限murmur/FSM Animator/LogicRoot/[CutScene]",
        "A7_ButterflyTest/Room/Prefab/FallingTeleportTrickBackgroundProvider/A7_HotSpring/溫泉場景Setting FSM Object/FSM Animator/View/SPA/PinkSkin/Pink_Odd/SimpleCutSceneFSM_八仙無限murmur/FSM Animator/LogicRoot/[CutScene]",
        // skipping this either softlocks or prevents Goumang from turning on the lights in her boss arena, depending on technical details; either way it's not worth it
        "A3_S5_BossGouMang_GameLevel/Room/Simple Binding Tool/BossGouMangLogic/[CutScene]/[CutScene]LightUp",
        // Outer Warehouse hack in the lower left corner; skipping this prevents the crates and crushers from moving as intended
        "A4_S1/Room/Prefab/Gameplay_3/左下開電/PowerOnFSM/FSM Animator/LogicRoot/[CutScene FSM]PowerOn/FSM Animator/LogicRoot/[CutScene]",
        // Shuanshuan dinner scene; I previously thought delay was enough but apparently this has to be deny
        "AG_S2/Room/NPCs/議會演出相關Binding/ShanShan 軒軒分身 FSM/FSM Animator/CutScene/[CutScene] 食譜_團圓飯/FSM Animator/LogicRoot/[CutScene]",
        // Power Reservoir control room hack 1st scene. This 1st scene is fine in isolation, but the 2nd scene (showing the Radiant Pagoda) is on a fixed timer, and if you skip
        // the 1st scene it becomes possible to leave the room as the 2nd scene starts playing, which *is* a softlock. The simplest fix is not letting you skip the 1st scene.
        "A2_SG1/Room/Unlock FSM/FSM Animator/LogicRoot/[TimeLine]ControlRoomPowerUp"
    };

    // These cutscenes are only problematic if you skip them *very* early, and we really want them to be skippable,
    // so we introduce an artificial delay to prevent users from hitting the problem
    private static List<string> skip100FDelaylist = new List<string> {
        // softlocks if skipped instantly
        "GameLevel/SimpleCutSceneFSM/FSM Animator/LogicRoot/[CutScene]Altar/[Timeline]Altar", // PBV intro ceremony/harvesting scene
        "AG_S2/Room/NPCs/議會演出相關Binding/ShanShan 軒軒分身 FSM/FSM Animator/CutScene/收到文物演出/[CutsceneFSM] 軒軒收到種子/FSM Animator/LogicRoot/[CutScene]", // Shuanshuan planting the Unknown Seed
        "AG_S2/Room/NPCs/議會演出相關Binding/ShanShan 軒軒分身 FSM/FSM Animator/CutScene/收到文物演出/[CutsceneFSM] 軒軒收到棋盤/FSM Animator/LogicRoot/[CutScene]", // Shuanshuan playing with the Qiankun Board
        "AG_S2/Room/NPCs/議會演出相關Binding/ShanShan 軒軒分身 FSM/FSM Animator/CutScene/收到文物演出/[CutsceneFSM] 軒軒收到古唱片/FSM Animator/LogicRoot/[CutScene]", // listening to the vinyl record w/ Shuanshuan
        "AG_S2/Room/NPCs/議會演出相關Binding/ShanShan 軒軒分身 FSM/FSM Animator/CutScene/收到文物演出/[CutsceneFSM] 軒軒收到名畫作/FSM Animator/LogicRoot/[CutScene]", // admiring the portrait w/ Shuanshuan
        "AG_S2/Room/NPCs/議會演出相關Binding/ShanShan 軒軒分身 FSM/FSM Animator/CutScene/收到文物演出/[CutsceneFSM] 軒軒收到VR/FSM Animator/LogicRoot/[CutScene]", // Shuanshuan playing with the VR Device
        "A2_SG4/Room/妹妹回憶_SimpleCutSceneFSM/FSM Animator/LogicRoot/[CutScene]", // Heng Warehouse flashback
        "VR_TaoChang/Room/SimpleCutSceneFSM_易公後妹妹回憶/FSM Animator/LogicRoot/[CutScene]", // Heng flashback after being trapped in Eigong's soulscape
        // According to MattStrats, the Lady E fight can also lose many of its sound effects if these are skipped early, though I could not reproduce myself
        "P2_R22_Savepoint_GameLevel/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/[CutScene]FirstTimeContact/[Timeline]", // full version for first attempt
        "P2_R22_Savepoint_GameLevel/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/[CutScene]SecondTimeContact/[Timeline]", // quick refight version
    };

    private static List<string> skip200FDelaylist = new List<string> {
        // Eigong fight loses many of its sound effects (including parry!) if these are skipped early
        // I can reproduce the refight scene losing sound effects even with a 100 frame delay. Any value longer than 100 I cannot repro with, so 200 should be adequate.
        "GameLevel/Room/Prefab/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/[CutScene] 一進", // full version for first attempt
        "GameLevel/Room/Prefab/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/[CutScene] 二進", // quick refight version
    };

    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "PlayAnimation")]
    private static void SimpleCutsceneManager_PlayAnimation(SimpleCutsceneManager __instance) {
        var goPath = GetFullPath(__instance.gameObject);
        if (skip100FDelaylist.Contains(goPath) || skip200FDelaylist.Contains(goPath)) {
            return;
        }

        Log.Info($"SimpleCutsceneManager_PlayAnimation {goPath}");
        if (skipDenylist.Contains(goPath)) {
            Log.Info($"not allowing skip for cutscene {goPath} because it's on the skip denylist");
            return;
        }

        if (__instance.name.EndsWith("[TimeLine]CrateEnter_L") || __instance.name.EndsWith("[TimeLine]CrateEnter_R")) {
            Log.Debug($"not allowing skip for {goPath} because all crate exit 'cutscenes' I've tested instantly softlock when skipped");
            return;
        } else if (__instance.name == "[CutScene]調閱報告") {
            Log.Debug($"not allowing skip for {goPath} because all \"[CutScene]調閱報告\" / Eigong lab report cutscenes risk softlocking when skipped");
            return;
        } else if (__instance.name == "[Timeline]EatingBySavePoint") {
            Log.Info($"not allowing skip for {goPath} because skipping a \"[Timeline]EatingBySavePoint\" / root node fusang root cutscene merely deletes the animation without saving any time");
            return;
        }

        string id = "";
        if (__instance.gameObject == GameObject.Find(CutsceneSkip.KuafuEndingChoiceCutsceneGOPath)) {
            Log.Info($"Not prompting player to skip this cutscene because it's in the Kuafu ending choice conversation, where even dialogue skipping softlocks.");
        } else if (__instance.name.EndsWith("_EnterScene")) {
            Log.Info($"skipping notification for {__instance.name} because transition 'cutscenes' are typically over before the player can even see the toast");
        } else {
            id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Cutscene");
        }

        CutsceneSkip.activeCutscene = (__instance, id);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SimpleCutsceneManager), "PlayAnimation")]
    private static async void SimpleCutsceneManager_PlayAnimation_Postfix(SimpleCutsceneManager __instance) {
        var goPath = GetFullPath(__instance.gameObject);
        int? delay = null;
        // completely arbitrary numbers, have not tested how framerate settings affect this
        if (skip100FDelaylist.Contains(goPath)) {
            delay = 100;
        }
        if (skip200FDelaylist.Contains(goPath)) {
            delay = 200;
        }
        if (delay != null) {
            await UniTask.DelayFrame((int)delay);
            Log.Info($"SimpleCutsceneManager_PlayAnimation acting on {goPath} with delay (i.e. Postfix patch + {delay} frame wait) to avoid softlocking");
            var id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Cutscene");
            CutsceneSkip.activeCutscene = (__instance, id);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SimpleCutsceneManager), "End")]
    private static void SimpleCutsceneManager_End(SimpleCutsceneManager __instance) {
        Log.Debug($"SimpleCutsceneManager_End {__instance.name}");
        if (CutsceneSkip.activeCutscene.Item1 == __instance)
            CutsceneSkip.activeCutscene = (null, "");
    }

    public static List<string> dialogueSkipDenylist = new List<string> {
        // Because the LightUp cutscene is nested inside this dialogue, it's possible to softlock by skipping "the dialogue"
        // during the cutscene part. Plus, skipping earlier will prevent the lights from turning on, which is annoying.
        "A3_S5_BossGouMang_GameLevel/Room/Simple Binding Tool/BossGouMangLogic/Start_Dialogue",
        // This dialogue also softlocks if we skip it when it's paused by a mid-dialogue cutscene
        "A2_S1/Room/Prefab/GuideFish_Acting/NPC_GuideFish A2Variant/General FSM Object/Animator(FSM)/LogicRoot/NPC_Talking_Controller/Config/Conversations/[Conversation] StartDialogueGroup(Note)/標記管理者_Dialogue",
    };

    [HarmonyPrefix, HarmonyPatch(typeof(DialoguePlayer), "StartDialogue")]
    private static void DialoguePlayer_StartDialogue(DialoguePlayer __instance, DialogueGraph dialogueGraph, Action callback, bool withBackground) {
        if (CutsceneSkip.KuafuEndingChoiceCutsceneActive()) {
            Log.Info($"Not prompting player to skip this dialogue because it's in the Kuafu ending choice conversation, where even dialogue skipping softlocks.");
            return;
        }

        var graphGoPath = GetFullPath(dialogueGraph.gameObject);
        Log.Debug($"DialoguePlayer_StartDialogue for dialogueGraph {graphGoPath}");
        if (dialogueSkipDenylist.Contains(graphGoPath)) {
            Log.Info($"Not prompting player to skip this dialogue because it's in the skip denylist.");
        } else if (graphGoPath == "A2_SG4/Logic/PhoneActingLoigic(Part1)/妹妹來電1_Dialogue") {
            // Heng flashback special case: Wait until the first dialogue starts before allowing skips and prompting the user
            var id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Heng Flashback");
            CutsceneSkip.activeA2SG4.Item2 = id;
        } else {
            var id = Notifications.AddNotification($"Press {CutsceneSkip.SkipKeybindText()} to Skip This Dialogue");
            CutsceneSkip.dialogueSkipNotificationId = id;
        }
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
        // Don't post a notification at first. We don't want to allow skipping it until the first dialogue starts, since earlier skips are very glitchy.
        CutsceneSkip.activeA2SG4 = (__instance, "");
    }
    [HarmonyPrefix, HarmonyPatch(typeof(A2_SG4_Logic), "OnLevelDestroy")]
    private static void A2_SG4_Logic_OnLevelDestroy(A2_SG4_Logic __instance) {
        Log.Info($"A2_SG4_Logic_OnLevelDestroy / Heng Power Reservoir flashback");
        if (CutsceneSkip.activeA2SG4.Item1 == __instance) {
            Notifications.CancelNotification(CutsceneSkip.activeA2SG4.Item2);
            CutsceneSkip.activeA2SG4 = (null, "");
        }
    }

    // The Yanlao fight also has a special implementation class not covered by our SimpleCutsceneManager patches
    [HarmonyPrefix, HarmonyPatch(typeof(A4_S5_Logic), "EnterLevelStart")]
    private static async void A4_S5_Logic_EnterLevelStart(A4_S5_Logic __instance) {
        Log.Info($"A4_S5_Logic_EnterLevelStart / Sky Rending Claw Pre-Fight Scenes");
        // This also softlocks if you skip it at the earliest possible moment
        await UniTask.DelayFrame(100); // completely arbitrary number, have not tested how framerate settings affect this
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

    // Implement prompts for the 2nd 'unwalk' keybind

    [HarmonyPrefix, HarmonyPatch(typeof(Player), "SetStoryWalk")]
    private static void Player_SetStoryWalk(Player __instance, bool storyWalk, float walkModifier) {
        if (storyWalk) {
            Log.Info($"Player_SetStoryWalk called with storyWalk={storyWalk}, walkModifier={walkModifier}");
            if (CutsceneSkip.storyWalkNotificationId != null) {
                Notifications.CancelNotification(CutsceneSkip.storyWalkNotificationId);
            }
            var id = Notifications.AddNotification($"Press {CutsceneSkip.UnwalkKeybindText()} to Disable 'Story Walk'");
            CutsceneSkip.storyWalkNotificationId = id;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Player), "SetHasHat")]
    private static void Player_SetHasHat(Player __instance, bool hastHat) {
        if (hastHat) {
            Log.Info($"Player_SetHasHat called with hastHat [sic] = {hastHat}");
            if (CutsceneSkip.storyWalkNotificationId != null) {
                Notifications.CancelNotification(CutsceneSkip.storyWalkNotificationId);
            }
            var id = Notifications.AddNotification($"Press {CutsceneSkip.UnwalkKeybindText()} to Doff Yi's Hat");
            CutsceneSkip.storyWalkNotificationId = id;
        }
    }
}