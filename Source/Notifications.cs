using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

namespace CutsceneSkip;

internal class Notifications {
    private static Canvas CanvasComponent = null!;
    private static TextMeshProUGUI TextComponent = null;

    /*
     * Lifecycle methods called from our main file / BaseUnityPlugin class
     */
    public static void Awake() {
        var fullscreenCanvasObject = new GameObject("NineSolsAPI-FullscreenCanvas");
        RCGLifeCycle.DontDestroyForever(fullscreenCanvasObject);

        CanvasComponent = fullscreenCanvasObject.AddComponent<Canvas>();
        CanvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;

        TextComponent = fullscreenCanvasObject.AddComponent<TextMeshProUGUI>();
        TextComponent.alignment = TextAlignmentOptions.BottomRight;
        TextComponent.fontSize = 20;
        TextComponent.color = Color.white;
    }
    public static void Update() {
        UpdateText();
    }
    public static void OnDestroy() {
        UnityEngine.Object.Destroy(CanvasComponent.gameObject);
    }

    /*
     * Internal state
     */

    private static bool isDirty = false;

    private struct Notification {
        public string id;
        public DateTimeOffset timestamp;
        public string displayText;
    }

    private static List<Notification> notificationStack = new List<Notification>();

    private static readonly TimeSpan expiry = TimeSpan.FromSeconds(10);

    public static void UpdateText() {
        var now = DateTimeOffset.UtcNow;
        var removedCount = notificationStack.RemoveAll(notification => (now - notification.timestamp) > expiry);
        isDirty |= (removedCount > 0);

        if (isDirty) {
            //var oldText = TextComponent.text;
            TextComponent.text = string.Join('\n', notificationStack.Select(n => n.displayText));
            //Log.Info($"Notifications.UpdateText() changed from text={oldText} to text={TextComponent.text}");
            isDirty = false;
        }
    }

    /*
     * Public API for other classes
     */

    // returns id to use for cancellation later
    // for now we want all notifications to expire after a similar duration, so no argument for that
    public static string AddNotification(string displayText) {
        Log.Info($"Notifications.AddNotification({displayText})");
        // format string "N" means no separators/enclosers/hexadecimal, just 32 digits
        var id = Guid.NewGuid().ToString("N");

        notificationStack.Add(new Notification {
            id = id,
            timestamp = DateTimeOffset.UtcNow,
            displayText = displayText,
        });

        isDirty = true;
        return id;
    }

    public static void CancelNotification(string id) {
        Log.Info($"Notifications.CancelNotification({id})");
        if (id != null && id != "") {
            var removedCount = notificationStack.RemoveAll(x => x.id == id);
            isDirty |= (removedCount > 0);
        }
    }
}
