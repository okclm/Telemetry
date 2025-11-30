using UnityEngine;
using ModSettings;
using MelonLoader;
using System.Diagnostics.Contracts;
using System.Reflection;
using Il2Cpp;
using Telemetry;
using System.Runtime.CompilerServices;
using System.Data.SqlTypes;


namespace Telemetry
{
    internal class TelemetrySettings : JsonModSettings
    {     
		// [Section("General (Version 1.1) - 11/25/2025")]
		[Section("General (" + TelemetryMain.MOD_VERSION_NUMBER + ")")]

        [Name("Enable telemetry data capture")]
        [Description("Enable/Disable telemetry data capture")]
        public bool enableTelemetryDataCapture = false;

        [Name("Capture Telemetry Key")]
        [Description("Which key you press to manually capture telemetry data")]
        public KeyCode captureKey = KeyCode.Keypad0;

        [Name("Only capture telemetry data outdoors")]
        [Description("Only enables telemetry data capture while outdoors")]
        public bool onlyOutdoors = true;

        [Name("Generate Desmos 2D telemetry data")]
        [Description("Enables Desmos 2D compatible telemetry data for use with Desmos graphing calculator (https://www.desmos.com/calculator)")]
        public bool alsoGenerateDesmos2DData = true;

        [Name("Generate Desmos 3D telemetry data")]
        [Description("Enables Desmos 3D compatible telemetry data for use with Desmos graphing calculator (https://www.desmos.com/3d)")]
        public bool alsoGenerateDesmos3DData = true;

        [Name("Enable telemetry distance capture trigger")]
        [Description("Enable/Disable telemetry distance data capture")]
        public bool enableTelemetryDistanceDataCapture = true;

        [Name("Distance threshhold (meters)")]
        [Description("Telemetry data captured after player travels this distance")]
        [Slider(0, 100)]
        public int distanceThreshold = 10;

        [Name("Enable telemetry time capture trigger")]
        [Description("Enable/Disable telemetry time data capture")]
        public bool enableTelemetryTimeDataCapture = true;

        [Name("Wait time threshhold (seconds)")]
        [Description("Telemetry data captured after wait time passes")]
        [Slider(60, 86400)]
        public int waitTime = 60;

        [Name("Enable Weather change capture trigger")]
        [Description("Enable/Disable Weather change data capture")]
        public bool enableTelemetryWeatherChangeDataCapture = true;

        [Name("Enable HUD display messages")]
        [Description("Enable/Disable Telemetry informational messages on HUD display")]
        public bool enableTelemetryHUDDisplay = true;

        [Name("Enable Wind change capture trigger")]
        [Description("Enable/Disable Wind change data capture")]
        public bool enableTelemetryWindStrengthChangeDataCapture = true;


        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
        }

        protected override void OnConfirm()
        {
            base.OnConfirm();

            Settings.distanceThreshold = distanceThreshold;
            Settings.waitTime = waitTime;
            Settings.enableTelemetryDataCapture = enableTelemetryDataCapture;
            Settings.onlyOutdoors = onlyOutdoors;
            Settings.alsoGenerateDesmos2DData = alsoGenerateDesmos2DData;
            Settings.alsoGenerateDesmos3DData = alsoGenerateDesmos3DData;
            Settings.enableTelemetryTimeDataCapture = enableTelemetryTimeDataCapture;
            Settings.enableTelemetryDistanceDataCapture = enableTelemetryDistanceDataCapture;
            Settings.enableTelemetryWeatherChangeDataCapture = enableTelemetryWeatherChangeDataCapture;
            Settings.enableTelemetryHUDDisplay = enableTelemetryHUDDisplay;
            Settings.enableTelemetryWindStrengthChangeDataCapture = enableTelemetryWindStrengthChangeDataCapture;

            /*
            TelemetryMain.distanceThreshold = distanceThreshold;
            TelemetryMain.enableTelemetryDataCapture = enableTelemetryDataCapture;
            TelemetryMain.onlyOutdoors = onlyOutdoors;
            */

            // Reset the odometer.  Set the previous position to the player current position
            TelemetryMain.previousPosition = GameManager.GetVpFPSPlayer().transform.position;

            // Reset the wait timer to (near) zero.  Subtracting the waitTime is more accurate over time than resetting to zero.
            TelemetryMain.timer = TelemetryMain.timer - Settings.waitTime;

        }
    }

     internal static class Settings
    {
        public static TelemetrySettings options;

        public static float distanceThreshold = 10.0f;    // This is the distance required to log the current player position.
        public static int waitTime = 60;                  // This is the wait time in seconds required to log the current player position.
        public static bool enableTelemetryDataCapture = false;
        public static bool onlyOutdoors = true;
        public static bool alsoGenerateDesmos2DData = true;
        public static bool alsoGenerateDesmos3DData = true;
        public static bool enableTelemetryTimeDataCapture = true;
        public static bool enableTelemetryDistanceDataCapture = true;
        public static bool enableTelemetryWeatherChangeDataCapture = true;
        public static bool enableTelemetryHUDDisplay = true;
        public static bool enableTelemetryWindStrengthChangeDataCapture = true;

        public static void OnLoad()
        {
            options = new TelemetrySettings();
            options.AddToModSettings("Telemetry Capture");

            // Initialize option variables
            distanceThreshold = options.distanceThreshold;
            waitTime = options.waitTime;
            enableTelemetryDataCapture = options.enableTelemetryDataCapture;
            onlyOutdoors = options.onlyOutdoors;
            alsoGenerateDesmos2DData = options.alsoGenerateDesmos2DData;
            alsoGenerateDesmos3DData = options.alsoGenerateDesmos3DData;
            enableTelemetryTimeDataCapture = options.enableTelemetryTimeDataCapture;
            enableTelemetryDistanceDataCapture = options.enableTelemetryDistanceDataCapture;
            enableTelemetryWeatherChangeDataCapture = options.enableTelemetryWeatherChangeDataCapture;
            enableTelemetryHUDDisplay = options.enableTelemetryHUDDisplay;
            enableTelemetryWindStrengthChangeDataCapture = options.enableTelemetryWindStrengthChangeDataCapture;
        }
    }
}
