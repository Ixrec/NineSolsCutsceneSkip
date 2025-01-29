using BepInEx;
using BepInEx.Configuration;
using Dialogue;
using HarmonyLib;
using NineSolsAPI;
using UnityEngine;

namespace CutsceneSkip;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class CutsceneSkip : BaseUnityPlugin {
    // https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/4_configuration.html
    private static ConfigEntry<KeyboardShortcut> skipKeybind = null!;

    public static string SkipKeybindText() {
        return skipKeybind.Value.Serialize();
    }

    private Harmony harmony = null!;

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        // Load patches from any class annotated with @HarmonyPatch
        harmony = Harmony.CreateAndPatchAll(typeof(CutsceneSkip).Assembly);

        skipKeybind = Config.Bind("", "Skip Keybind",
            new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl), "The keyboard shortcut to actually skip cutscenes and dialogue.");

        KeybindManager.Add(this, SkipActiveCutsceneOrDialogue, () => skipKeybind.Value);

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        Notifications.Awake();
    }

    public static (A2_SG4_Logic?, string) activeA2SG4 = (null, "");
    public static (A4_S5_Logic?, string) activeA4S5 = (null, "");
    public static string dialogueSkipNotificationId = "";
    public static (SimpleCutsceneManager?, string) activeCutscene = (null, "");
    public static (VideoPlayAction?, string) activeVideo = (null, "");

    private void SkipActiveCutsceneOrDialogue() {
        if (activeA2SG4.Item1 != null) {
            var hengPRFlashback = activeA2SG4.Item1;

            var done = AccessTools.FieldRefAccess<A2_SG4_Logic, bool>("_done").Invoke(hengPRFlashback);
            if (done) {
                Log.Info($"A2_SG4_Logic's _done flag is already set. Player must have skipped too early. Resetting _done flag back to false.");
                AccessTools.FieldRefAccess<A2_SG4_Logic, bool>("_done").Invoke(hengPRFlashback) = false;

                var s2ConnectionTouched = AccessTools.FieldRefAccess<SceneConnectionPoint, bool>("touchedChangeSceneTrigger").Invoke(hengPRFlashback.connectionToS2);
                if (s2ConnectionTouched) {
                    Log.Info($"A2_SG4_Logic::connectionToS2's touchedChangeSceneTrigger flag is also already set. Also resetting that touchedChangeSceneTrigger flag back to false.");
                    AccessTools.FieldRefAccess<SceneConnectionPoint, bool>("touchedChangeSceneTrigger").Invoke(hengPRFlashback.connectionToS2) = false;
                }
                var s3ConnectionTouched = AccessTools.FieldRefAccess<SceneConnectionPoint, bool>("touchedChangeSceneTrigger").Invoke(hengPRFlashback.connectionToS3);
                if (s3ConnectionTouched) {
                    Log.Info($"A2_SG4_Logic::connectionToS3's touchedChangeSceneTrigger flag is also already set. Also resetting that touchedChangeSceneTrigger flag back to false.");
                    AccessTools.FieldRefAccess<SceneConnectionPoint, bool>("touchedChangeSceneTrigger").Invoke(hengPRFlashback.connectionToS3) = false;
                }
            }

            Log.Info($"Found A2_SG4_Logic a.k.a. Heng Power Reservoir flashback, calling A2_SG4_Logic.TrySkip() as a special case");
            hengPRFlashback.TrySkip();
            Notifications.CancelNotification(activeA2SG4.Item2);

            if (done) {
                activeA2SG4.Item1 = null; // don't keep repeating this if the player is mashing, that also softlocks
            }
            return;
        }

        if (activeA4S5.Item1 != null) {
            var yl = activeA4S5.Item1;
            if (yl.BossKilled.CurrentValue) {
                Log.Info($"Found A4_S5_Logic a.k.a. Sky Rending Claw fight. Claw already killed. Applying special case logic to skip post-fight scene.");
                yl.FinishCutscene.TrySkip();
            } else {
                if (yl.GianMechClawMonsterBase.gameObject.activeSelf) {
                    Log.Info($"Found A4_S5_Logic a.k.a. Sky Rending Claw fight. Claw not yet killed. But claw is already active, so trying to skip this now would just softlock. Doing nothing.");
                    return;
                }
                Log.Info($"Found A4_S5_Logic a.k.a. Sky Rending Claw fight. Claw not yet killed. Applying special case logic to skip pre-fight scenes.");
                var ylmc = GameObject.Find("A4_S5/A4_S5_Logic(DisableMeForBossDesign)/CUTSCENE_START/MangaView_OriginalPrefab/MANGACanvas");
                ylmc.SetActive(false);

                yl.BeforeMangaBubble.TrySkip();
                yl.BubbleDialogue.TrySkip();
                yl.TrySkip();
            }
            Notifications.CancelNotification(activeA4S5.Item2);
            return;
        }

        var dpgo = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/Always Canvas/DialoguePlayer(KeepThisEnable)");
        var dp = dpgo?.GetComponent<DialoguePlayer>();
        if (dp != null) {
            var playingDialogueGraph = AccessTools.FieldRefAccess<DialoguePlayer, DialogueGraph>("playingDialogueGraph").Invoke(dp);
            if (playingDialogueGraph != null) {
                Log.Info($"calling DialoguePlayer.playingDialogueGraph.TrySkip()");
                dp.TrySkip();
                if (dialogueSkipNotificationId != "") {
                    Notifications.CancelNotification(dialogueSkipNotificationId);
                    dialogueSkipNotificationId = "";
                }
                return;
            }
        }

        if (activeCutscene.Item1 != null) {
            var scm = activeCutscene.Item1;
            Log.Info($"calling TrySkip() on {scm.name}");
            AccessTools.Method(typeof(SimpleCutsceneManager), "TrySkip").Invoke(scm, []);
            if (AccessTools.FieldRefAccess<SimpleCutsceneManager, bool>("isMangaPauseing").Invoke(scm)) {
                Log.Info($"also calling Resume() since it was 'manga paused'");
                AccessTools.Method(typeof(SimpleCutsceneManager), "Resume").Invoke(scm, []);
            }
            Notifications.CancelNotification(activeCutscene.Item2);
            activeCutscene = (null, "");
            return;
        }

        if (activeVideo.Item1 != null) {
            var vpa = activeVideo.Item1;
            Log.Info($"calling TrySkip() on {vpa.name}");
            AccessTools.Method(typeof(VideoPlayAction), "TrySkip").Invoke(vpa, []);
            Notifications.CancelNotification(activeVideo.Item2);
            activeVideo = (null, "");
            return;
        }
    }

    private void Update() {
        Notifications.Update();
    }

    private void OnDestroy() {
        // Make sure to clean up resources here to support hot reloading

        harmony.UnpatchSelf();
        Notifications.OnDestroy();
    }
}