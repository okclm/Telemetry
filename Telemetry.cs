using HarmonyLib;
using Il2Cpp;
using Il2CppInterop;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppTLD.Logging;
using Il2CppTLD.Stats;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Playables;
using static Il2CppSystem.Net.ServicePointManager;
using Scene = UnityEngine.SceneManagement;

/*
 * Stuff you need to know:
 * X is x, Z is z, and Y is elevation.  Y is elevation!?
 * 
 * Weather stuff:
 * GetWeatherStage() returns an enum (WeatherStage):
 * Weather.GetWeatherStageDisplayName(GetWeatherStage()) returns a string name for the enum value.  i.e "Partly Cloudy"
 * 
 * Writing message to the HUD:
 * HUDMessage.AddMessage("string") will display a message on the player's HUD.
 * 
 * Adding glyphs like the down arrow to strings:
 * // direct glyph (editor must be UTF‑8 / font supports glyph)
    string s1 = "Down arrow: ↓";

    // Unicode escape (works regardless of editor glyph support)
    string s2 = "Down arrow: \u2193";

    // 8‑digit escape (same result)
    string s3 = "Down arrow: \U00002193";

    // triangle alternative
    string s4 = "Down triangle: \u25BC";

    // char literal
    char down = '\u2193';

// Example usage with HUDMessage
HUDMessage.AddMessage("Press " + s1);
 */

namespace Telemetry
{
    public class TelemetryMain : MelonMod
    {
        // Weather tracking stuff
        private const string HARMONY_ID_WEATHER = "OKCLM.tld.weathersethook";   // Used in Harmony instance creation.
        private static WeatherStage lastStage = WeatherStage.Undefined;         // Keep track of last weather stage seen.
        public static bool weatherStageChanged = false;                         // Has the weather stage changed since last check? 

        // Wind tracking stuff
        private const string HARMONY_ID_WIND = "OKCLM.tld.windsethook";         // Used in Harmony instance creation.
        private static WindStrength? lastWindStrength = null;                   // Keep track of last wind strength seen.
        public static float? lastWindMPH = null;                                // Keep track of last wind speed seen.
        public static float? lastWindPhaseDurationHours = null;                 // Keep track of last wind phase duration seen.
        public static bool windStrengthChanged = false;                         // Has the wind strength changed since last check? 

        // *** Stuff for capturing telemtry data every waitTime seconds ***
        // private float waitTime = 10.0f; // This is a parameter controlled in the options menu.
        public static float timer = 0.0f;

        // *** Stuff for capturing telemtry data when player's distance from previous position is greater than distance threshold ***
        public static Vector3 previousPosition = new Vector3(0, 0, 0); // This tracks the previous player x,z position logged.

        // Are we in the game menu?
        public static bool inMenu = true;

        public const string MOD_VERSION_NUMBER = "Version 1.1 - 12/16/2025";      // The version # of the mod.
        //internal const string LOG_FILE_FORMAT_VERSION_NUMBER = "1.0";           // The version # of the log file format.  This is used to determine if the log file format has changed and we need to update the code to read it.
        internal const string DEFAULT_FILE_NAME = "Telemetry.log";                // The log file is written in the MODS folder for TLD  (i.e. D:\Program Files (x86)\Steam\steamapps\common\TheLongDark\Mods)
        internal const string FILE_NAME_DESMOS2D = "Telemetry_Desmos2D.log";      // The log file is written in the MODS folder for TLD  (i.e. D:\Program Files (x86)\Steam\steamapps\common\TheLongDark\Mods)
        internal const string FILE_NAME_DESMOS3D = "Telemetry_Desmos3D.log";      // The log file is written in the MODS folder for TLD  (i.e. D:\Program Files (x86)\Steam\steamapps\common\TheLongDark\Mods)

        static string Log_File_Format_Version_Number = "1.1";   // The version # of the log file format.  This is used to determine if the log file format has changed and we need to update the code to read it.
        static string telemetry_filename = "Telemetry.log";     // This is the telemetry log file.

        //private static string GetFilePath() => Path.GetFullPath(Path.Combine(MelonEnvironment.ModsDirectory, FILE_NAME));
        private static string GetFilePath() => Path.GetFullPath(Path.Combine(MelonEnvironment.ModsDirectory, telemetry_filename));
        private static string GetDesmos2DFilePath() => Path.GetFullPath(Path.Combine(MelonEnvironment.ModsDirectory, FILE_NAME_DESMOS2D));
        private static string GetDesmos3DFilePath() => Path.GetFullPath(Path.Combine(MelonEnvironment.ModsDirectory, FILE_NAME_DESMOS3D));

        //internal static void CreateLogFile() => File.Create(GetFilePath());

        public static void LogData(string text)
        {
            if (text is null) return;
            File.AppendAllLines(GetFilePath(), new string[] { text });
        }

        private static void LogDesmos2DData(string text)
        {
            if (text is null) return;
            File.AppendAllLines(GetDesmos2DFilePath(), new string[] { text });
        }

        private static void LogDesmos3DData(string text)
        {
            if (text is null) return;
            File.AppendAllLines(GetDesmos3DFilePath(), new string[] { text });
        }

        // alsoGenerateDesmosData

        public static void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null)
        {
            // *** This if DEBUG statement will enable all LogMessage text going to the MelonLoader logger.
            // *** Otherwise, we just log to the Telemetry log file.
            #if DEBUG
                // Use static MelonLogger so this method can be called from static contexts (Harmony hooks, etc.)
                MelonLogger.Msg("." + caller + "." + lineNumber + ": " + message);
            #else
                // In release, write lightweight telemetry to file so logs are still available without requiring Melon instance.
                try
                {
                    LogData(";" + caller + "." + lineNumber + ": " + message);
                }
                catch
                {
                    // swallow — don't let logging break the game
                }
            #endif

            //#if DEBUG
            //    LoggerInstance.Msg("." + caller + "." + lineNumber + ": " + message);
            //#endif
        }

        public static string SanitizeFileName(string filename)
        {
            // Get a list of invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();

            // Remove any invalid characters from the input
            var sanitized = new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());

            return sanitized;
        }

        public override void OnInitializeMelon()
        {
            // Debug break: in DEBUG builds this will prompt to attach a debugger (Launch) then break.
            // Replace or remove the #if block if you want this in release.

            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch(); // prompts to attach a debugger
            //            }
            //            else
            //            {
            //                Debugger.Break(); // break into already attached debugger
            //            }
            //#endif

            LogMessage("Starting Telemetry Mod: " + MOD_VERSION_NUMBER);
            LogMessage("Initializing Melon.");

            var harmonyWeather = new HarmonyLib.Harmony(HARMONY_ID_WEATHER);
            LogMessage("Using AccessTools for Weather.Update() hook...");
            harmonyWeather.Patch(
                AccessTools.Method(typeof(Weather), "Update"),
                prefix: new HarmonyMethod(typeof(TelemetryMain), nameof(OnWeatherUpdate))
            );

            var harmonyWind = new HarmonyLib.Harmony(HARMONY_ID_WIND);
            LogMessage("Using AccessTools for Wind.Update() hook...");
            harmonyWind.Patch(
                AccessTools.Method(typeof(Wind), "Update"),
                postfix: new HarmonyMethod(typeof(TelemetryMain), nameof(OnWindUpdate))
            );

            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / distanceThreshold={Settings.distanceThreshold:F2}]");
            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / Settings.enableTelemetryDataCapture={Settings.enableTelemetryDataCapture}]");

            // Can we get the player current position
            if (GameManager.GetVpFPSPlayer())
            {
                // Let's remember the current position so we can calculate distance from it.
                previousPosition = GameManager.GetVpFPSPlayer().transform.position;
               //  LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / previousPosition.X={previousPosition.x:F2} / previousPosition.Y={previousPosition.y:F2} / previousPosition.Z={previousPosition.z:F2}]");
            }

            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / Calling Telemetry.Settings.OnLoad()");
            Telemetry.Settings.OnLoad();
            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / Back from calling Telemetry.Settings.OnLoad()");
            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / Settings.enableTelemetryDataCapture={Settings.enableTelemetryDataCapture}]");

            // LogData("; Starting Telemetry Version: " + MOD_VERSION_NUMBER);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / Enter OnSceneWasLoaded. Settings.enableTelemetryDataCapture={Settings.enableTelemetryDataCapture}]");
            
            if (sceneName.Contains("MainMenu"))
            {
                //SCRIPT_InterfaceManager/_GUI_Common/Camera/Anchor/Panel_OptionsMenu/Pages/ModSettings/GameObject/ScrollPanel/Offset/

                inMenu = true;
                LogMessage("Menu Scene " + sceneName + " was loaded.");
            }
            else if (!sceneName.Contains("_SANDBOX_") && (sceneName.Contains("_SANDBOX")))
            {
                inMenu = false;
                LogMessage("Sandbox Scene " + sceneName + " was loaded.");

                if (GameManager.GetVpFPSPlayer())
                {
                    // Reset trigger counters on a scene change...
                    // Note: This means that distance travelled and wait time are counted per-scene.
                    // The Weather Stage and Wind Strength don't change on a scene change.  So maybe don't reset them?
                    LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $"] / Enter OnSceneWasLoaded. Resetting telemetry data capture triggers.");

                    previousPosition = GameManager.GetVpFPSPlayer().transform.position; // Player current position
                    timer = timer - Settings.waitTime;  // Reset timer to (near) zero.

                    // Don't reset weather and wind tracking on scene change.
                    //lastStage = WeatherStage.Undefined; // Reset last weather stage seen.
                    //lastWindStrength = null;         // Reset last wind strength seen.
                }

                if (Settings.enableTelemetryHUDDisplay) HUDMessage.AddMessage("Sandbox Scene " + sceneName + " was loaded.",false,true);
            }
            else
            {
                LogMessage("Uninteresting scene " + sceneName + " was loaded.");
            }
        }


        public override void OnUpdate()
        {
            // If we can, log player position
            // todo: Need a throttle here.  Time?
            // Ok, removed time throttle and replaced with a distance throttle.
            // If the player has moved further than distanceThreshold (10.0 by default) then we record the current scene and the player coordinates.
            // Note the inMenu bool should track if we are in the game (false) or in the menu (true).
            // Also note that the logged data starts with a semi-colon to indicate "comment" that we can ignore later.
            // Only the telemetry data lines do not start with a semi-colon.

            timer += Time.deltaTime;

            // LogMessage("OnUpdate called...");    // Warning: Verbose logging!

            // if (GameManager.GetVpFPSPlayer() && (timer > waitTime))

            // Are we enabled?
            // LogMessage($"; enableTelemetryDataCapture=" + enableTelemetryDataCapture);
            if (!Settings.enableTelemetryDataCapture) // Not enabled.  Nothing to do here.
            {
                // LogMessage($"; Telemetry not enabled.  No action taken.");
                return; 
            }

            // Are we indoors and that is enabled?
            // LogMessage($"; onlyOutdoors=" + onlyOutdoors);
            if (Settings.onlyOutdoors)
            {
                // LogMessage($"; Only processing telemetry when outdoors.");
                // LogMessage($"; GameManager.GetWeatherComponent().IsIndoorEnvironment()=" + GameManager.GetWeatherComponent().IsIndoorEnvironment());
                if (GameManager.GetWeatherComponent().IsIndoorEnvironment())
                {
                    // LogMessage($"; Telemetry not enabled for indoors.  No action taken.");
                    return;     // Only enabled for outdoors and we are indoors.  So nothing to do here.
                }
            }

            // The lastWindStrength variable is set in the OnWindUpdate() hook.
            // If it has not been set yet, we are too early.
            // So do nothing at this time.
            if (!lastWindStrength.HasValue)
            {
                // LogMessage($"; Wind strength not yet initialized.");
                // LogMessage($"; Detected wind strength <null> likely due to start up timing.  No action taken.");
                return; // Wind strength not yet initialized.  Nothing to do here.
            }

            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / OnUpdate. inMenu={inMenu}]");

            if (GameManager.GetVpFPSPlayer() && (!inMenu))
            {
                // Calculate distance travelled by player
                float howFar = GetDistanceToPlayer();

                // Determine the hours played.  This is a float and we can use it as a timestamp for the data.
                float gameTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                // Sometimes the gameTime is zero here.  Seen most often when entering a new scene (indoors).  Check for gameTime zero and skip logging if so.
                if (gameTime == 0f)
                {
                    LogMessage($"; Detected gameTime of zero likely due to scene change timing.  No action taken.");
                    return;     // When gameTime is zero, nothing to do here.
                }

                // Have we moved far enough to do something?
                // Or, did the user press the capture telemetry key?
                // Or did the waitTime elapse?
                if ((Settings.enableTelemetryTimeDataCapture && (timer > Settings.waitTime)) || 
                    (Settings.enableTelemetryDistanceDataCapture && (howFar > Settings.distanceThreshold)) ||
                    (Settings.enableTelemetryWeatherChangeDataCapture && (weatherStageChanged == true)) ||
                    (Settings.enableTelemetryWindStrengthChangeDataCapture && (windStrengthChanged == true)) ||
                    InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.captureKey))
                {
                    // Are we here because the distance threshold was met or because the user pressed the capture key?
                    string triggerCode = "K";   // Default is we are here because of a keypress.
                    if (howFar > Settings.distanceThreshold) { triggerCode = "D"; }  // We are here because the distance threshold was exceeded.
                    if (timer > Settings.waitTime) { triggerCode = "T"; }            // We are here because the waittime threshold was exceeded.
                    if (weatherStageChanged == true) { triggerCode = "W"; }          // We are here because the weather stage changed.
                    if (windStrengthChanged == true) { triggerCode = "w"; }          // We are here because the wind changed.

                    // Deterine IRL time.  We use this to timestamp the data with the current IRL time.
                    string irlDateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

                    // Moved to above to address gameTime zero issue.
                    // Determine the hours played.  This is a float and we can use it as a timestamp for the data.
                    //float gameTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();

                    // Determine current scene name
                    string? sceneName = Scene.SceneManager.GetActiveScene().name;

                    // Get the player position
                    Vector3 playerPos = GameManager.GetVpFPSPlayer().transform.position;    // Copy of player position coords

                    // Get the camera position and angle
                    Vector3 cameraPosition = GameManager.GetMainCamera().transform.position;
                    Vector2 cameraAngleElevation = GameManager.GetVpFPSCamera().Angle;

                    // Get the current weather telemetry data
                    Weather weatherComponent = GameManager.GetWeatherComponent();
                    WeatherStage weatherStage = weatherComponent.GetWeatherStage();
                    string weatherStageName = weatherComponent.GetWeatherStageDisplayName(weatherStage);
                    // float x = weatherComponent.GetDuration(weatherComponent);

                    // WeatherSet.GetName(weatherComponent.GetCurrentWeatherSet());  // How to access the current weather set name without parsing it from the debug text.

                    // ** Example weather debug text (weatherDebugText).  Note it is multi-line and verbose.: **
                    // Morning To Midday (51.59%)
                    // Weather Set: LightFog_cannery. 4.27hrs. (61.6 %)
                    // 00 >> LightFog.tr: 100.0 %. 2.63 / 4.27hrs
                    // Custom Type Name: 
                    // Wind Speed Base: 6.2 MPH.Target Wind Speed: 7.6 MPH.Angle Base: 314.6
                    // Wind Actual Speed: 7.6 MPH.Actual Angle: 311.2
                    // Player Speed: 0
                    // Player Wind Angle: 0.0
                    // Wwise WindIntensity: 10
                    // Wwise GustStrength: 6
                    // Local snow depth: 0.2355731
                    // Aurora alpha: 0
                    // Time Since Last Blizzard(4): 24.09688 

                    // This is an example of the weather debug text broken into lines.  Note this shows multiple weather stages (lines 2 and 3).
                    //	[0]	"Midday To Afternoon (35.35%)"
                    //  [1] "Weather Set: HeavySnow_cannery. 4.73hrs. (39.3 %)"
                    //  [2] "00 >> LightSnow. tr: 100.0 %. 1.86/2.39hrs"
                    //  [3] "01___HeavySnow. tr: 0.0 %. 0.00/2.34hrs"
                    //  [4] " Custom Type Name: "
                    //  [5] "Wind Speed Base: 24.3 MPH. Target Wind Speed: 26.5 MPH. Angle Base: 160.6"
                    //  [6] "Wind Actual Speed: 26.5 MPH. Actual Angle: 160.6"
                    //  [7] "Player Speed: 0"
                    //  [8] "Player Wind Angle: 0.0"
                    //  [9] "Wwise WindIntensity: 36"
                    //  [10]    "Wwise GustStrength: 10"
                    //  [11]    "Local snow depth: 0.01834451"
                    //  [12]    "Aurora alpha: 0"
                    //  [13]    "Time Since Last Blizzard (48): 76.62036"

                    // Let's talk "stats"...
                    // StatsManager statsManager = GameManager.GetStatsManager();

                    string weatherDebugText = Weather.GetDebugWeatherText();
                    string? weatherSetLine = GetLineStartingWith(weatherDebugText, "Weather Set");
                    string? weatherSetValueText = GetTextAfterSeparator(weatherSetLine, ':');
                    weatherSetValueText = GetTextBeforeSeparator(weatherSetValueText, '.');
                    float? weatherSetDurationHours = null;  // Duration of current weather set in hours.   Default value is null (undefined)

                    string? after = GetTextAfterSeparator(weatherSetLine, ':'); // e.g. " Blizzard_ashcanyon. 8.21hrs."
                    if (after != null)
                    {
                        var m = Regex.Match(after, @"(\d+(\.\d+)?)\s*hrs", RegexOptions.IgnoreCase);
                        if (m.Success && float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float hours))
                        {
                            // hours is the duration of the current weather set (in hours)
                            weatherSetDurationHours = hours;
                        }
                    }

                    //  [2] "00 >> LightSnow. tr: 100.0 %. 1.86/2.39hrs"
                    float? weatherStageDurationHours = null;  // Duration of current weather stage in hours.   Default value is null (undefined)
                    weatherSetLine = GetLineContaining(weatherDebugText, ">>");
                    after = GetTextAfterSeparator(weatherSetLine, '/'); // e.g. "2.39hrs"
                    if (after != null)
                    {
                        var m = Regex.Match(after, @"(\d+(\.\d+)?)\s*hrs", RegexOptions.IgnoreCase);
                        if (m.Success && float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float hours))
                        {
                            // hours is the duration of the current weather stage (in hours)
                            weatherStageDurationHours = hours;
                        }
                    }

                    float weatherCurrentTemperature = weatherComponent.GetCurrentTemperature();
                    float weatherCurrentTemperatureWithoutHeatSources = weatherComponent.GetCurrentTemperatureWithoutHeatSources();
                    float weatherCurrentWindchill = weatherComponent.GetCurrentWindchill();
                    // float weatherCurrentTemperatureWithWindchill = weatherComponent.GetCurrentTemperatureWithWindchill();  // Nope. This includes heat sources.
                    float weatherCurrentTemperatureWithWindchill = weatherCurrentTemperatureWithoutHeatSources + weatherCurrentWindchill;  // Calculate this to avoid the heat sources being included.

                    // Let's talk Wind...
                    // LogAndShowWindFromSettings();
                    // float? windPhaseDurationHours = null;                     // m_PhaseDurationHours


        // Various ways to get the current save name and game id
        //string currentSaveName = SaveGameSystem.GetCurrentSaveName();   // Example: "sandbox27"
        //string csn = SaveGameSystem.m_CurrentSaveName;                  // Example: "sandbox27"
        //uint cgi = SaveGameSystem.m_CurrentGameId;                      // Example: 27

        // TODO Make sure the user-defined save name is valid.  Replace any illegal filename characters with an underscore.
        string ssUDF = SanitizeFileName(SaveGameSystem.GetNewestSaveSlotForActiveGame().m_UserDefinedName);   // Example: "FAR TERRITORY"

                    // How to get at the maps?
                    // Il2Cpp.MapDetailManager.GetName(GameManager.GetMapDetailManager());
                    // LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / Il2Cpp.MapDetailManager.GetName(GameManager.GetMapDetailManager())={Il2Cpp.MapDetailManager.GetName(GameManager.GetMapDetailManager())}");
                    // MapDetail mapDetail = GameManager.GetMapDetailManager().GetComponent<MapDetail>(); //   __instance.gameObject.GetComponent<MapDetail>();
                    // MapDetail xmapDetail = GameManager.GetMapDetailManager().GetComponent<MapDetail>();
                    // LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / xmapDetail={xmapDetail.ToString}");

                    //if (xmapDetail != null)
                    //{
                    //    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / xmapDetail={xmapDetail.ToString}");
                    //}
                    //else
                    //{
                    //    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / xmapDetail=NULL");
                    //}

                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / currentSaveName={currentSaveName}");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / csn={csn}");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / cgi={cgi.ToString()}");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / ssUDF={ssUDF}");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / triggerCode={triggerCode}");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / cameraPosition={cameraPosition.ToString("F1")}");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / cameraAngleElevation={cameraAngleElevation.ToString("F1")}");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / distanceThreshold={Settings.distanceThreshold:F2}");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / howFar={howFar:F2}");
                    // LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" *** Travelled farther than distance threshold! ***");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / PlayerPosition.X={playerPos.x:F2} / PlayerPosition.Y={playerPos.y:F2} / PlayerPosition.Z={playerPos.z:F2}]");
                    //LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / previousPosition.X={previousPosition.x:F2} / previousPosition.Y={previousPosition.y:F2} / previousPosition.Z={previousPosition.z:F2}]");
                    // LogMessage($"; GameManager.GetTimeOfDayComponent().GetTODHours(Time.deltaTime)=" + GameManager.GetTimeOfDayComponent().GetTODHours(Time.deltaTime));
                    // LogMessage($"; GameManager.GetTimeOfDayComponent().FormatTime(GameManager.GetTimeOfDayComponent().GetHour(), GameManager.GetTimeOfDayComponent().GetMinutes())=" + GameManager.GetTimeOfDayComponent().FormatTime(GameManager.GetTimeOfDayComponent().GetHour(), GameManager.GetTimeOfDayComponent().GetMinutes()));
                    // LogMessage($"; GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused()=" + GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused());

                    //                    Vector3 pos = GameManager.GetVpFPSPlayer().transform.position;

                    //                                                -- player --     -- camera --      - camera-
                    //                                                --position--     --position--      - angle -   ---Weather---
                    //     irlDateTime     gameTime     sceneName     x    y     z     x    y     z      x       y   ---Stage  ---    triggerCode        comment
                    // 09/10/2024 16:57:07|3038.889|MiningRegionMine|9.32|1.44|240.52|9.32|3.19|240.52|202.44|-32.76|Partly Cloudy|K	               ; Blocked path can be cleared with hatchet
                    //
                    // 

                    //Quaternion sun;
                    //sun = Quaternion.AngleAxis(GameManager.GetWeatherComponent().GetRotationDegreesForRegion(GameManager.m_ActiveScene) - 90f, Vector3.up);
                    // sun.

                    // Update the telemetry filename with the current user-defined save name.
                    telemetry_filename = ssUDF.Replace(" ", "_") + "_" + DEFAULT_FILE_NAME;

                    if (File.Exists(GetFilePath()) == false)
                    {
                        // If the data file does not exist, Write a comment as the 1st line of the file with useful information (i.e. file format version #).
                        LogData("; Telemetry data file format version: " + Log_File_Format_Version_Number);
                        LogData("; Telemetry Mod: " + MOD_VERSION_NUMBER);
                        //LogData(";");
                        //LogData(";  ** Testing detection of a wind change. **");
                        LogData(";");
                        //LogData(";                                          ----- player -----   ------ camera ------ - camera-");
                        //LogData(";                                          -----position-----   ------position------ - angle   ---Weather--  ---Weather--  -Current- -Current-   -Current Temp  -");
                        //LogData(";     irlDateTime  | gameTime  |sceneName |   x   |  y  |  z   |  x    |  y  |  z   | x  | y  |---- Set --- |--- Stage -- |-- Temp - -WindChill- -With WindChill-  triggerCode        ;comment");

                        //LogData(";    irlDateTime   |   gameTime   |sceneName");
                        //LogData(";         ↓        |       ↓      | ↓");
                        //11/25/2025 08:17:08|179.1017456055|CanneryRegion|-357.67|31.81|-519.39|-357.67|33.56|-519.39|187.15|-22.70|LightFog_cannery|LightFog|-13.43|0.00|-13.43|D

                        //LogData(";         \u2193   |     \u2193 |   \u2193");


                        // LogData(";");

                        LogData("; Fields are separated by the \"|\" character:");
                        LogData(";   irlDateTime: Real-world date and time when the data was recorded (MM/DD/YYYY HH:MM:SS)");
                        LogData(";   gameTime: In-game hours played (float)");
                        LogData(";   sceneName: Current game scene name");
                        LogData(";   playerPosition (x, y, z): Player's position coordinates in the game world");
                        LogData(";   cameraPosition (x, y, z): Camera's position coordinates in the game world");
                        LogData(";   cameraAngleElevation (x, y): Camera's angle and elevation");
                        LogData(";   weatherSet: Current weather set");
                        LogData(";   weatherSetDurationHours: Duration of current weather set in hours");
                        LogData(";   weatherStage: Current weather stage");
                        LogData(";   weatherStageDurationHours: Duration of current weather stage in hours");
                        LogData(";   windStrength: Current wind strength (Calm, SlightlyWindy, Windy, VeryWindy, Blizzard)");
                        LogData(";   windSpeedMPH: Current wind speed in MPH");
                        LogData(";   windPhaseDurationHours: Current wind strength duration in hours");
                        //LogData(";   weatherStageName: Current weather stage name");
                        //LogData(";   weatherCurrentTemperature: Current temperature in the game world");
                        LogData(";   weatherCurrentTemperatureWithoutHeatSources: Current temperature (C) without any heat sources in the game world");
                        LogData(";   weatherCurrentWindchill: Current wind chill temperature (C) in the game world");
                        LogData(";   weatherCurrentTemperatureWithWindchill: Current temperature (C) with wind chill factored in");
                        LogData(";   triggerCode: Code indicating what triggered the data capture (T=Time, D=Distance, K=Keypress, W=Weather change, w=Wind change)");
                        LogData(";");
                    }


                    LogData(irlDateTime +
                        $"|{gameTime:F10}" +
                        $"|{sceneName,-3}" +
                        $"|{playerPos.x:F2}|{playerPos.y:F2}|{playerPos.z:F2}" +
                        $"|{cameraPosition.x:F2}|{cameraPosition.y:F2}|{cameraPosition.z:F2}" +
                        $"|{cameraAngleElevation.x:F2}|{cameraAngleElevation.y:F2}" +
                        $"|{weatherSetValueText}" +
                        $"|{(weatherSetDurationHours.HasValue ? weatherSetDurationHours.Value.ToString() : "<null>"),2}" +
                        $"|{weatherStage,2}" +
                        $"|{(weatherStageDurationHours.HasValue ? weatherStageDurationHours.Value.ToString() : "<null>"),2}" +
                        $"|{(lastWindStrength.HasValue ? lastWindStrength.Value.ToString() : "<null>"),2}" + 
                        $"|{(lastWindMPH.HasValue ? lastWindMPH.Value.ToString() : "<null>"),2}" + 
                        $"|{(lastWindPhaseDurationHours.HasValue ? lastWindPhaseDurationHours.Value.ToString() : "<null>"),2}" + 
                        // $"|{weatherDebugText}" +
                        //$"|{weatherStageName}" +
                        //$"|{weatherCurrentTemperature:F2}" +
                        $"|{weatherCurrentTemperatureWithoutHeatSources:F2}" +
                        $"|{weatherCurrentWindchill:F2}" +
                        $"|{weatherCurrentTemperatureWithWindchill:F2}" +
                        $"|{triggerCode}");


                    if (Settings.alsoGenerateDesmos2DData)
                    {
                        LogDesmos2DData($"{playerPos.x:F2}\t{playerPos.z:F2}");
                    }

                    if (Settings.alsoGenerateDesmos3DData)
                    {
                        LogDesmos3DData($"{playerPos.x:F2}\t{playerPos.y:F2}\t{playerPos.z:F2}");
                    }

                    // Reset the previous position to the current position
                    previousPosition = playerPos;

                    // Reset the wait timer to (near) zero.  Subtracting the waitTime is more accurate over time than resetting to zero.
                    timer = timer - Settings.waitTime;

                    // Reset the weather stage changed flag to false
                    weatherStageChanged = false;

                    // Reset the wind strength changed flag to false
                    windStrengthChanged = false;
                }

                //SaveGameSystem.GetNewestSaveSlotForActiveGame();
            }

        }

        // Helper function to calculate the distance the player has moved since the last logged position.
        private float GetDistanceToPlayer()
        {
            if (GameManager.GetVpFPSPlayer())
            {
                float dist = Vector3.Distance(previousPosition, GameManager.GetVpFPSPlayer().gameObject.transform.position);
                // LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" Distance traveled={dist:F2}]");
                return dist;
            }

            return 0.0f;
        }

        // Helper methods to split the multi-line Weather Debug Text string and pick lines by index or by prefix, then extract text after a separator.
        // Example usage (inside OnUpdate)
        // string wdt = Weather.GetDebugWeatherText();

        // Get the 5th line (zero-based)
        // string? fifthLine = GetLineAt(wdt, 4);

        // Get the line that starts with "Wind Actual Speed"
        // string? windLine = GetLineStartingWith(wdt, "Wind Actual Speed");

        // Get the value after the colon for that line
        // string? windValueText = GetTextAfterSeparator(windLine);

        // If you need a numeric part, use a regex (fully-qualified to avoid extra using)
        // var match = System.Text.RegularExpressions.Regex.Match(windLine ?? "", @"Wind Actual Speed:\s*([\d\.]+)");
        // if (match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float windSpeed))
        // {
        //   windSpeed parsed
        // }

        private static string? GetLineAt(string multiLine, int index)
        {
            if (string.IsNullOrEmpty(multiLine)) return null;
            var lines = multiLine.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (index < 0 || index >= lines.Length) return null;
            return lines[index].Trim();
        }

        private static string? GetLineStartingWith(string multiLine, string prefix, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (string.IsNullOrEmpty(multiLine) || prefix is null) return null;
            var lines = multiLine.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith(prefix, comparison));
        }

        private static string? GetTextAfterSeparator(string line, char separator = ':')
        {
            if (string.IsNullOrEmpty(line)) return null;
            int ix = line.IndexOf(separator);
            if (ix < 0) return null;
            return line[(ix + 1)..].Trim();
        }

        private static string? GetTextBeforeSeparator(string line, char separator = '.')
        {
            if (string.IsNullOrEmpty(line)) return null;
            int ix = line.IndexOf(separator);
            if (ix < 0) return null;
            return line.Substring(0, ix).Trim();
        }

        private static string? GetLineContaining(string multiLine, string target, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (string.IsNullOrEmpty(multiLine) || target is null)
                return null;

            return multiLine
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.Contains(target, comparison));
        }

        // Helper methods to track and log the current weather stage changes

        // Runs every frame but only logs when the weather stage changes
        private static bool OnWeatherUpdate(Weather __instance)
        {
            // LogMessage("Checking if Weather changed...");

            WeatherStage current = __instance.GetWeatherStage();
            if (!inMenu && (current != lastStage))
            {
                weatherStageChanged = true;
                LogMessage($"Weather stage change detected.  Last stage=\"{lastStage}\", New stage=\"{current}\"");

                lastStage = current;
                LogCurrentWeather("Stage change");
            }
            return true;
        }

        // The Weather Stage logging method
        private static void LogCurrentWeather(string source)
        {
            try
            {
                string debugText = Weather.GetDebugWeatherText();
                string setName = "Unknown";

                // Parse "Weather Set: Blizzard_ashcanyon. 8.21hrs."
                int pos = 0;
                while (pos < debugText.Length)
                {
                    int nextLine = debugText.IndexOf('\n', pos);
                    if (nextLine == -1) nextLine = debugText.Length;

                    string line = debugText.Substring(pos, nextLine - pos);
                    if (line.Contains("Weather Set"))
                    {
                        int colon = line.IndexOf(':');
                        if (colon >= 0)
                        {
                            string rest = line.Substring(colon + 1).Trim();
                            int dot = rest.IndexOf('.');
                            setName = dot > 0 ? rest.Substring(0, dot).Trim() : rest.Trim();
                        }
                        break;
                    }
                    pos = nextLine + 1;
                }

                var w = GameManager.GetWeatherComponent();
                string stage = w.GetWeatherStageDisplayName(w.GetWeatherStage());
                float feelsLike = w.GetCurrentTemperatureWithoutHeatSources() + w.GetCurrentWindchill();
                float day = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();

                //MelonLogger.Msg($"[WEATHER CHANGE - {source}] {DateTime.Now:HH:mm:ss} | Day {day:F1} | {GameManager.m_ActiveScene}");
                //MelonLogger.Msg($"   → {setName} ({stage}) → Feels like {feelsLike:F1}°C");

                LogMessage($"[WEATHER CHANGE - {source}] {DateTime.Now:HH:mm:ss} | Day {day:F1} | {GameManager.m_ActiveScene}");
                LogMessage($"   → {setName} ({stage}) → Feels like {feelsLike:F1}°C");

                if (Settings.enableTelemetryHUDDisplay)
                {
                    HUDMessage.AddMessage($"Weather → {setName} ({stage}) → Feels like {feelsLike:F1}°C", 5f, true);
                }
            }
            catch (Exception e)
            {
                //MelonLogger.Error("Weather hook error: " + e.Message);
                LogMessage("LogCurrentWeather hook error: " + e.Message);
            }
        }

        // Clean up Harmony Weather patches on mod unload
        public override void OnDeinitializeMelon()
        {
            HarmonyLib.Harmony.UnpatchID(HARMONY_ID_WEATHER);
            LogMessage("OnWeatherUpdate unloaded cleanly.");

            HarmonyLib.Harmony.UnpatchID(HARMONY_ID_WIND);
            LogMessage("OnWindUpdate unloaded cleanly.");
        }

        // Helpers to read wind values and match a WindSettings entry.
        // Call `LogAndShowWindFromSettings()` (or call `GetWindInfoFromSettings()` directly) from `OnUpdate()` or a debug key handler.
        public static (Il2Cpp.WindSettings? settings, float baseMPH, float actualMPH, float baseAngleDeg, float angleDeg) GetWindInfoFromSettings()
        {
            // Try the GameManager helper first, fallback to a scene lookup.
            Il2Cpp.Wind wind = null;
            try { wind = GameManager.GetWindComponent(); } catch { }
            if (wind == null)
            {
                try { wind = UnityEngine.Object.FindObjectOfType<Il2Cpp.Wind>(); } catch { }
            }

            if (wind == null)
            {
                LogMessage("GetWindInfoFromSettings: Wind component not found");
                return (null, 0f, 0f, 0f, 0f);
            }

            // Read numeric values the Wind class exposes.
            float baseMPH = 0f, actualMPH = 0f, baseAngle = 0f, angle = 0f, maxMPH = 0f;
            try
            {
                baseMPH = wind.m_CurrentMPH_Base;
                actualMPH = wind.m_CurrentMPH;
                baseAngle = wind.m_CurrentAngleDeg_Base;
                angle = wind.m_CurrentAngleDeg;
                maxMPH = wind.m_MaxWindMPH;
            }
            catch (Exception ex)
            {
                LogMessage("GetWindInfoFromSettings read numeric fields failed: " + ex.Message);
            }

            // Attempt to find a WindSettings whose m_VelocityRange contains the base speed.
            try
            {
                var settingsArray = wind.m_WindSettings;
                if (settingsArray != null)
                {
                    for (int i = 0; i < settingsArray.Length; i++)
                    {
                        var s = settingsArray[i];
                        if (s == null) continue;
                        // m_VelocityRange.x = min, .y = max (convention)
                        var vr = s.m_VelocityRange;
                        LogMessage($"Inspecting WindSettings #{i+1}. \"{s.name}\": range=({vr.x:F8}, {vr.y:F8}) * maxMPH={maxMPH:F8} => ({vr.x * maxMPH:F8}, {vr.y * maxMPH:F8}), baseMPH={baseMPH:F8},  actualMPH={actualMPH:F8}");
                        if (actualMPH >= (vr.x * maxMPH) && actualMPH <= (vr.y * maxMPH))
                        {
                            return (s, baseMPH, actualMPH, baseAngle, angle);
                        }
                    }

                    // Not found by range.  Could we match based on a pair of adjacent settings?
                    // Consider the case where the baseMPH is between two ranges.
                    // #1. "Calm":          range=(0.0460, 0.0770) * maxMPH=85.0000 => (3.9100, 6.5450),   baseMPH=9.1307 is above Calm max of 6.5450
                    // #2. "SlightlyWindy": range=(0.1176, 0.1530) * maxMPH=85.0000 => (10.0000, 13.0050), baseMPH=9.1307 is below SlightlyWindy min of 10.0000
                    // #3. "Windy":         range=(0.2118, 0.3850) * maxMPH=85.0000 => (18.0000, 32.7250), baseMPH=9.1307
                    // #4. "VeryWindy":     range=(0.4118, 0.6150) * maxMPH=85.0000 => (35.0000, 52.2750), baseMPH=9.1307
                    // #5. "Blizzard":      range=(1.0000, 1.0000) * maxMPH=85.0000 => (85.0000, 85.0000), baseMPH=9.1307
                    // This is probably the case where the wind is changing and is between two states.  In the above example, 
                    // the baseMPH=9.1307 is between Calm max of 6.5450 and SlightlyWindy min of 10.0000.  Perhaps we call this "Calm..SlightlyWindy"?
                    // That would be a bit messy because we are returning a single WindSettings object.  So for now we will just log a message and return null.

                    if (settingsArray.Length > 0)
                    {
                        LogMessage("Unable to find velocity based on range.");
                        //return (settingsArray[0], baseMPH, actualMPH, baseAngle, angle);
                        return (null, baseMPH, actualMPH, baseAngle, angle);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("GetWindInfoFromSettings inspect settings failed: " + ex.Message);
            }

            return (null, baseMPH, actualMPH, baseAngle, angle);
        }

        // Convenience: log and show wind values (and matched WindSettings name if found)
        public static void LogAndShowWindFromSettings()
        {
            var info = GetWindInfoFromSettings();
            string settingsName = "<none>";
            if (info.settings != null)
            {
                try { settingsName = info.settings.name ?? "<unnamed>"; } catch { settingsName = "<err>"; }
            }
            // Todo: The settingName value is not correct.  Investigate.  Fixed.  Needed to use the min/max times the maxMPH wind value.
            string msg = $"Wind (settingsName={settingsName}) base={info.baseMPH:F1} MPH (angle {info.baseAngleDeg:F0}°) actual={info.actualMPH:F1} MPH (angle {info.angleDeg:F0}°)";
            LogMessage(msg);
            if (Settings.enableTelemetryHUDDisplay) try { HUDMessage.AddMessage(msg, 4f, true); } catch { /* HUD may be unavailable */ }
        }

        // Logs wind settings info (for debugging)
        public static void LogWindSettings(Il2Cpp.Wind wind)
        {
            if (wind == null)
            {
                LogMessage("Wind component null.  No action.");
                return;
            }

            // Attempt to log WindSettings for each wind strength (Calm, SlightlyWindy, Windy, VeryWindy, Blizzard).
            // "name"
            // "m_VelocityRange"
            // "m_GustinessRange"
            // "m_LateralBlusterRange"
            // "m_VerticalBlusterRange"
            // "m_RTPC_Range"
            // "m_ClothRandomRange"

            try
            {
                var settingsArray = wind.m_WindSettings;
                if (settingsArray != null)
                {
                    for (int i = 0; i < settingsArray.Length; i++)
                    {
                        var s = settingsArray[i];
                        if (s == null) continue;
                        // m_VelocityRange.x = min, .y = max (convention)
                        var vr = s.m_VelocityRange;
                        var gr = s.m_GustinessRange;
                        var lbr = s.m_LateralBlusterRange;
                        var vbr = s.m_VerticalBlusterRange;
                        var rtpcr = s.m_RTPC_Range;
                        var crr = s.m_ClothRandomRange;
                        // LogMessage($"Wind setting #{i + 1}. \"{s.name}\": range=({vr.x:F8}, {vr.y:F8}) * maxMPH={maxMPH:F8} => ({vr.x * maxMPH:F8}, {vr.y * maxMPH:F8}), baseMPH={baseMPH:F8},  actualMPH={actualMPH:F8}");
                        LogMessage($"Wind setting #{i + 1}. \"{s.name}\": m_VelocityRange=({vr.x:F8}, {vr.y:F8}), " +
                                   $"m_GustinessRange=({gr.x:F8}, {gr.y:F8}), " +
                                   $"m_LateralBlusterRange=({lbr.x:F8}, {lbr.y:F8}), " +
                                   $"m_VerticalBlusterRange=({vbr.x:F8}, {vbr.y:F8}), " +
                                   $"m_RTPC_Range=({rtpcr.x:F8}, {rtpcr.y:F8}), " +
                                   $"m_ClothRandomRange=({crr.x:F8}, {crr.y:F8}), {crr.z:F8})");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Inspect Wind settings failed: " + ex.Message);
            }
        }

        // Add this Harmony postfix handler (paste with the other helper methods)
        private static void OnWindUpdate(Wind __instance)
        {
            try
            {
                if (__instance == null) return;
                if (inMenu) return; // avoid noise while in menus
                if (lastWindStrength.HasValue && __instance.m_CurrentStrength.Equals(lastWindStrength.Value))
                {
                    //LogMessage($"Current wind (m_CurrentStrength={__instance.m_CurrentStrength}) no change from lastWindStrength: {lastWindStrength.Value}");
                    return; // no change
                }

                // Need to understand what the m_PhaseDurationHours field means and when it changes.
                // Can the Wind Strength remain the same while m_PhaseDurationHours changes?
                // I would think the wind duration would remain the same until the strength changes.
                LogMessage($"Current wind (m_CurrentStrength): {__instance.m_CurrentStrength}");
                LogMessage($"Current wind (m_PhaseDurationHours): {__instance.m_PhaseDurationHours}");
                LogMessage($"Current wind (m_PhaseElapsedTODSeconds): {__instance.m_PhaseElapsedTODSeconds}");
                LogMessage($"Current wind (m_CurrentMPH_Base): {__instance.m_CurrentMPH_Base}");
                LogMessage($"Current wind (m_CurrentMPH): {__instance.m_CurrentMPH}");
                LogMessage($"Current wind (m_ActiveSettings):");
                LogMessage($"Current wind   (m_ActiveSettings.m_Angle): {__instance.m_ActiveSettings.m_Angle}");
                LogMessage($"Current wind   (m_ActiveSettings.m_Velocity): {__instance.m_ActiveSettings.m_Velocity}");
                LogMessage($"Current wind   (m_ActiveSettings.m_Gustiness): {__instance.m_ActiveSettings.m_Gustiness}");
                LogMessage($"Current wind   (m_ActiveSettings.m_LateralBluster): {__instance.m_ActiveSettings.m_LateralBluster}");
                LogMessage($"Current wind   (m_ActiveSettings.m_VerticalBluster): {__instance.m_ActiveSettings.m_VerticalBluster}");
                //LogMessage($"Current wind   (m_ActiveSettings.ToString): {__instance.m_ActiveSettings.ToString}");

                LogMessage($"Current wind (m_HoursBetweenWindChangeMin): {__instance.m_HoursBetweenWindChangeMin}");
                LogMessage($"Current wind (m_HoursBetweenWindChangeMax): {__instance.m_HoursBetweenWindChangeMax}");
                LogMessage($"Current wind (m_HoursForTransitionMin): {__instance.m_HoursForTransitionMin}");
                LogMessage($"Current wind (m_HoursForTransitionMax): {__instance.m_HoursForTransitionMax}");
                LogMessage($"Current wind (m_LateralBluster): {__instance.m_LateralBluster}");
                LogMessage($"Current wind (m_LateralBluster_Limit): {__instance.m_LateralBluster_Limit}");
                LogMessage($"Current wind (m_VerticalBluster): {__instance.m_VerticalBluster}");
                LogMessage($"Current wind (m_VerticalBluster_Limit): {__instance.m_VerticalBluster_Limit}");
                LogMessage($"Current wind (m_LockedWindSpeed): {__instance.m_LockedWindSpeed}");
                LogMessage($"Current wind (m_MaxWindMPH): {__instance.m_MaxWindMPH}");
                LogMessage($"Current wind (m_NeverCalmWind): {__instance.m_NeverCalmWind}");

                if (__instance.m_SourceSettings==null)
                {
                    LogMessage($"Current wind (m_SourceSettings): NULL");
                }
                else
                {
                    LogMessage($"Current wind (m_SourceSettings):");
                    LogMessage($"Current wind   (m_SourceSettings.m_Angle): {__instance.m_SourceSettings.m_Angle}");
                    LogMessage($"Current wind   (m_SourceSettings.m_Velocity): {__instance.m_SourceSettings.m_Velocity}");
                    LogMessage($"Current wind   (m_SourceSettings.m_Gustiness): {__instance.m_SourceSettings.m_Gustiness}");
                    LogMessage($"Current wind   (m_SourceSettings.m_LateralBluster): {__instance.m_SourceSettings.m_LateralBluster}");
                    LogMessage($"Current wind   (m_SourceSettings.m_VerticalBluster): {__instance.m_SourceSettings.m_VerticalBluster}");
                    //LogMessage($"Current wind   (m_SourceSettings.ToString): {__instance.m_SourceSettings.ToString}");
                }

                LogMessage($"Current wind (m_StartHasBeenCalled): {__instance.m_StartHasBeenCalled}");

                if (__instance.m_TargetSettings == null)
                {
                    LogMessage($"Current wind (m_TargetSettings): NULL");
                }
                else
                {
                    LogMessage($"Current wind (m_TargetSettings):");
                    LogMessage($"Current wind   (m_TargetSettings.m_Angle): {__instance.m_TargetSettings.m_Angle}");
                    LogMessage($"Current wind   (m_TargetSettings.m_Velocity): {__instance.m_TargetSettings.m_Velocity}");
                    LogMessage($"Current wind   (m_TargetSettings.m_Gustiness): {__instance.m_TargetSettings.m_Gustiness}");
                    LogMessage($"Current wind   (m_TargetSettings.m_LateralBluster): {__instance.m_TargetSettings.m_LateralBluster}");
                    LogMessage($"Current wind   (m_TargetSettings.m_VerticalBluster): {__instance.m_TargetSettings.m_VerticalBluster}");
                    //LogMessage($"Current wind   (m_TargetSettings.ToString): {__instance.m_TargetSettings.ToString}");
                }

                LogMessage($"Current wind (m_TransitionTimeTODSeconds): {__instance.m_TransitionTimeTODSeconds}");
                LogMessage($"Current wind (m_WindChill): {__instance.m_WindChill}");
                //LogMessage($"Current wind (m_WindKillers): {__instance.m_WindKillers}");  // I think this is about colliders that stop the wind from impacting the player...
                //LogMessage($"Current wind (m_WindZoneTransform): {__instance.m_WindZoneTransform}");  // Transform of the WindZone game object?
                LogMessage($"Current wind (GetSpeedMPH_Base): {__instance.GetSpeedMPH_Base()}");

                // LogMessage($"Current wind (m_WindSettings): {__instance.m_WindSettings[0].m_VelocityRange}");  // This is an array of records
                LogWindSettings(__instance);

                //if (lastWindPhaseDurationHours.HasValue && __instance.m_PhaseDurationHours.Equals(lastWindPhaseDurationHours.Value))
                //{
                //    return; // no change
                //}

                // Wind strength has changed!  Read current wind state.
                var currentStrength = __instance.m_CurrentStrength;
                float currentMPH = __instance.m_CurrentMPH;
                float? currentWindPhaseDurationHours = __instance.m_PhaseDurationHours;

                // Strength change (e.g. Calm -> Blizzard) - use enum/struct equality
                if (!lastWindStrength.HasValue || !currentStrength.Equals(lastWindStrength.Value))
                {
                    LogMessage($"Wind strength change: {(lastWindStrength.HasValue ? lastWindStrength.Value.ToString() : "<null>")} -> {currentStrength}");
                    if (Settings.enableTelemetryHUDDisplay) try { HUDMessage.AddMessage($"Wind: {lastWindStrength?.ToString() ?? "<null>"} → {currentStrength}", 4f, true); } catch { }

                    // Show detailed wind numbers and matched settings
                    // *************** (What happens if we don't do this?) ***************
                    // LogAndShowWindFromSettings();

                    windStrengthChanged = true; // set Wind trigger flag to true for telemetry logging
                }
                //else
                //{
                //    // Minor speed drift detection (threshold to avoid tiny noise)
                //    if (Math.Abs(currentMPH - lastWindMPH) > 0.25f)
                //    {
                //        LogMessage($"Wind speed change: {lastWindMPH:F4} -> {currentMPH:F4} MPH");
                //    }
                //}

                // Update trackers
                lastWindStrength = currentStrength;
                lastWindMPH = currentMPH;
                lastWindPhaseDurationHours = currentWindPhaseDurationHours;
            }
            catch (Exception ex)
            {
                LogMessage("OnWindUpdate error: " + ex.Message);
            }
        }
    }
}
