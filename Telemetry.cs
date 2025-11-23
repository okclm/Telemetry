using MelonLoader;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection;
using System.Collections;
using Il2Cpp;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Il2CppSystem.Net.ServicePointManager;
using Scene = UnityEngine.SceneManagement;
using System.Xml.Linq;
using MelonLoader.Utils;
using Il2CppTLD.Stats;

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
 */

namespace Telemetry
{
    public class TelemetryMain : MelonMod
    {
        // *** Stuff for capturing player position every waitTime seconds ***
        // private float waitTime = 10.0f; // This needs to be a parameter controlled in the options menu.
        private float timer = 0.0f;

        // *** Stuff for capturing player position when distance from previous position is greater than distance threshold ***
        public static Vector3 previousPosition = new Vector3(0, 0, 0); // This tracks the previous player x,z position logged.

        // Are we in the game nenu?
        public static bool inMenu = true;

        //internal const string LOG_FILE_FORMAT_VERSION_NUMBER = "1.0";             // The version # of the log file format.  This is used to determine if the log file format has changed and we need to update the code to read it.
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

        private static void LogData(string text)
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
            // Only the data for sceen and player position does not start with a semi-colon.

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
                 //LogMessage($"; GameManager.GetWeatherComponent().IsIndoorEnvironment()=" + GameManager.GetWeatherComponent().IsIndoorEnvironment());
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
                if ((timer > Settings.waitTime) || (howFar > Settings.distanceThreshold) || InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.captureKey))
                {
                    // Are we here because the distance threshold was met or because the user pressed the capture key?
                    // Might add an indicator in the output data file reflecting that.  Might.
                    string triggerCode = "K";   // Default is we are here because of a keypress.
                    if (howFar > Settings.distanceThreshold) { triggerCode = "D"; }  // We are here because the distance threshold was exceeded.
                    if (timer > Settings.waitTime) { triggerCode = "T"; }            // We are here because the waittime threshold was exceeded.

                    // Deterine IRL time.  We use this to timestamp the data with the current IRL time.
                    string irlDateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

                    // Determine the hours played.  This is a float and we can use it as a timestamp for the data.
                    float gameTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();

                    // Get the player position
                    Vector3 playerPos = GameManager.GetVpFPSPlayer().transform.position;    // Copy of player position coords

                    // Get the camera position and angle
                    Vector3 cameraPosition = GameManager.GetMainCamera().transform.position;
                    Vector2 cameraAngleElevation = GameManager.GetVpFPSCamera().Angle;

                    // Get the current weather stage name
                    Weather weatherComponent = GameManager.GetWeatherComponent();
                    WeatherStage ws = weatherComponent.GetWeatherStage();
                    string weatherStageName = weatherComponent.GetWeatherStageDisplayName(ws);

                    float wct = weatherComponent.GetCurrentTemperature();
                    float wctwwc = weatherComponent.GetCurrentTemperatureWithWindchill();
                    float wcwc = weatherComponent.GetCurrentWindchill();

                    // Various ways to get the current save name and game id
                    //string currentSaveName = SaveGameSystem.GetCurrentSaveName();   // Example: "sandbox27"
                    //string csn = SaveGameSystem.m_CurrentSaveName;                  // Example: "sandbox27"
                    //uint cgi = SaveGameSystem.m_CurrentGameId;                      // Example: 27

                    // TODO Make sure the user-defined save name is valid.  Replace any illega filename characters with an underscore.
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
                        LogData(";");
                        LogData(";                                          ----- player -----   ------ camera ------ - camera-");
                        LogData(";                                          -----position-----   ------position------ - angle   ---Weather--  -Current- -Current-   -Current Temp  -");
                        LogData(";     irlDateTime  | gameTime  |sceneName |   x   |  y  |  z   |  x    |  y  |  z   | x  | y  |--- Stage -- |-- Temp - -WindChill- -With WindChill-  triggerCode        ;comment");
                        //                               07/18/2025 06:56:56|0.028248226|LakeRegion|1019.26|26.55|444.12|1019.26|28.30|444.12|0.00|0.00|Partly Cloudy|-18.02 | -8.14| -26.16|D

                        LogData(";");
                    }


                    LogData(irlDateTime +
                        $"|" + gameTime +
                        $"|" + Scene.SceneManager.GetActiveScene().name +
                        $"|{playerPos.x:F2}|{playerPos.y:F2}|{playerPos.z:F2}" +
                        $"|{cameraPosition.x:F2}|{cameraPosition.y:F2}|{cameraPosition.z:F2}" +
                        $"|{cameraAngleElevation.x:F2}|{cameraAngleElevation.y:F2}" +
                        $"|{ws,2}" +
                        // $"|{weatherStageName}" +
                        $"|{wct:F2}" +
                        $"|{wcwc:F2}" +
                        $"|{wctwwc:F2}" +
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
    }
}
