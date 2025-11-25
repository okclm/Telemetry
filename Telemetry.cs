using Il2Cpp;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppTLD.Stats;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using UnityEngine;
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
        // *** Stuff for capturing telemtry data every waitTime seconds ***
        // private float waitTime = 10.0f; // This is a parameter controlled in the options menu.
        public static float timer = 0.0f;

        // *** Stuff for capturing telemtry data when player's distance from previous position is greater than distance threshold ***
        public static Vector3 previousPosition = new Vector3(0, 0, 0); // This tracks the previous player x,z position logged.

        // Are we in the game menu?
        public static bool inMenu = true;

        public const string MOD_VERSION_NUMBER = "Version 1.1 - 11/25/2025";    // The version # of the mod.
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

        public void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null)
        {
            // *** This if DEBUG statement will enable all LogMessage text going to the MelonLoader logger.
            // *** Otherwise, we just log to the Telemetry log file.
            #if DEBUG
                LoggerInstance.Msg("." + caller + "." + lineNumber + ": " + message);
            #endif
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
            LogMessage("Initializing Melon.");
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
            
            // Reset the odometer on a scene change.  Player distance travelled in the new scene is zero.
            if (GameManager.GetVpFPSPlayer())
            {
                previousPosition = GameManager.GetVpFPSPlayer().transform.position;
                LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / Enter OnSceneWasLoaded. Resetting previousPosition to {previousPosition}]");
            }

            if (sceneName.Contains("MainMenu"))
            {
                //SCRIPT_InterfaceManager/_GUI_Common/Camera/Anchor/Panel_OptionsMenu/Pages/ModSettings/GameObject/ScrollPanel/Offset/

                inMenu = true;
                LogMessage("Menu Scene " + sceneName + " was loaded.");
            }
            else if (sceneName.Contains("SANDBOX"))
            {
                inMenu = false;
                LogMessage("Sandbox Scene " + sceneName + " was loaded.");
                // HUDMessage.AddMessage("Sandbox Scene " + sceneName + " was loaded.",false,true);
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

            // LogMessage($"[" + Scene.SceneManager.GetActiveScene().name + $" / OnUpdate. inMenu={inMenu}]");

            if (GameManager.GetVpFPSPlayer() && (!inMenu))
            {
                // Calculate distance travelled by player
                float howFar = GetDistanceToPlayer();

                // Have we moved far enough to do something?
                // Or, did the user press the capture telemetry key?
                // Or did the waitTime elapse?
                if ((Settings.enableTelemetryTimeDataCapture && (timer > Settings.waitTime)) || (Settings.enableTelemetryDistanceDataCapture && (howFar > Settings.distanceThreshold)) || InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.captureKey))
                {
                    // Are we here because the distance threshold was met or because the user pressed the capture key?
                    string triggerCode = "K";   // Default is we are here because of a keypress.
                    if (howFar > Settings.distanceThreshold) { triggerCode = "D"; }  // We are here because the distance threshold was exceeded.
                    if (timer > Settings.waitTime) { triggerCode = "T"; }            // We are here because the waittime threshold was exceeded.

                    // Deterine IRL time.  We use this to timestamp the data with the current IRL time.
                    string irlDateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

                    // Determine the hours played.  This is a float and we can use it as a timestamp for the data.
                    float gameTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();

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

                    // ** Example weather debug text (wdt).  Note it is multi-line and verbose.: **
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

                    string weatherDebugText = Weather.GetDebugWeatherText();
                    string? weatherSetLine = GetLineStartingWith(weatherDebugText, "Weather Set");
                    string? weatherSetValueText = GetTextAfterSeparator(weatherSetLine, ':');
                    weatherSetValueText = GetTextBeforeSeparator(weatherSetValueText, '.');

                    float weatherCurrentTemperature = weatherComponent.GetCurrentTemperature();
                    float weatherCurrentTemperatureWithWindchill = weatherComponent.GetCurrentTemperatureWithWindchill();
                    float weatherCurrentWindchill = weatherComponent.GetCurrentWindchill();

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
                    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / ssUDF={ssUDF}");
                    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / triggerCode={triggerCode}");
                    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / cameraPosition={cameraPosition.ToString("F1")}");
                    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / cameraAngleElevation={cameraAngleElevation.ToString("F1")}");
                    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / distanceThreshold={Settings.distanceThreshold:F2}");
                    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / howFar={howFar:F2}");
                    // LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" *** Travelled farther than distance threshold! ***");
                    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / PlayerPosition.X={playerPos.x:F2} / PlayerPosition.Y={playerPos.y:F2} / PlayerPosition.Z={playerPos.z:F2}]");
                    LogMessage($";[" + Scene.SceneManager.GetActiveScene().name + $" / previousPosition.X={previousPosition.x:F2} / previousPosition.Y={previousPosition.y:F2} / previousPosition.Z={previousPosition.z:F2}]");
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
                        LogData("; Telemetry Mod Version: " + MOD_VERSION_NUMBER);
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
                        LogData(";   weatherSet: Current weather setting");
                        LogData(";   weatherCurrentTemperature: Current temperature in the game world");
                        LogData(";   weatherCurrentWindchill: Current wind chill in the game world");
                        LogData(";   weatherCurrentTemperatureWithWindchill: Current temperature with wind chill factored in");
                        LogData(";   triggerCode: Code indicating what triggered the data capture (T=Time, D=Distance, K=Keypress)");
                        LogData(";");
                    }


                    LogData(irlDateTime +
                        $"|{gameTime:F10}" +
                        $"|{sceneName,-3}" +
                        $"|{playerPos.x:F2}|{playerPos.y:F2}|{playerPos.z:F2}" +
                        $"|{cameraPosition.x:F2}|{cameraPosition.y:F2}|{cameraPosition.z:F2}" +
                        $"|{cameraAngleElevation.x:F2}|{cameraAngleElevation.y:F2}" +
                        $"|{weatherSetValueText}" +
                        // $"|{weatherStage,2}" +
                        // $"|{weatherDebugText}" +
                        // $"|{weatherStageName}" +
                        $"|{weatherCurrentTemperature:F2}" +
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
                }

                //SaveGameSystem.GetNewestSaveSlotForActiveGame();
            }

        }

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
    }
}
