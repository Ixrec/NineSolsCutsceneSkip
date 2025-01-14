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
    private ConfigEntry<KeyboardShortcut> skipKeybind = null!;

    private Harmony harmony = null!;

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        // Load patches from any class annotated with @HarmonyPatch
        harmony = Harmony.CreateAndPatchAll(typeof(CutsceneSkip).Assembly);

        skipKeybind = Config.Bind("General.SkipKeybind", "SkipKeybind",
            new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl), "Skip Keybind");

        KeybindManager.Add(this, SkipActiveCutsceneOrDialogue, () => skipKeybind.Value);

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    public static SimpleCutsceneManager? activeCutscene = null;

    private void SkipActiveCutsceneOrDialogue() {
        var hengPRFlashback = GameObject.Find("A2_SG4/Logic")?.GetComponent<A2_SG4_Logic>();
        if (hengPRFlashback != null) {
            Log.Info($"Found A2_SG4_Logic a.k.a. Heng Power Reservoir flashback, calling A2_SG4_Logic.TrySkip() as a special case");
            hengPRFlashback.TrySkip();
            return;
        }

        if (activeCutscene != null) {
            Log.Debug($"calling TrySkip() on {activeCutscene.name}");
            AccessTools.Method(typeof(SimpleCutsceneManager), "TrySkip").Invoke(activeCutscene, []);
            activeCutscene = null;

            ToastManager.Toast($"Cutscene Skipped");
            return;
        }
        Log.Debug($"activeCutscene was null. Checking for dialogue next.");

        var dpgo = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/Always Canvas/DialoguePlayer(KeepThisEnable)");
        var dp = dpgo?.GetComponent<DialoguePlayer>();
        if (dp != null) {
            var playingDialogueGraph = AccessTools.FieldRefAccess<DialoguePlayer, DialogueGraph>("playingDialogueGraph").Invoke(dp);
            if (playingDialogueGraph != null) {
                dp.TrySkip();
                ToastManager.Toast($"Dialogue Skipped");
                return;
            }
            Log.Debug($"no dialogue was playing");
        } else {
            Log.Debug($"dp was null");
        }
    }

    private void OnDestroy() {
        // Make sure to clean up resources here to support hot reloading

        harmony.UnpatchSelf();
    }
}