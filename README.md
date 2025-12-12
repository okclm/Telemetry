# Telemetry

### What is it?

This project is a MelonLoader-based mod for [The Long Dark](https://www.thelongdark.com).
A video game developed by [Hinterland Games](https://hinterlandgames.com/).

### Features

It collects various telemetry data while playing for use outside of the game. The collected data includes:
- irlDateTime: Real-world date and time when the data was recorded (MM/DD/YYYY HH:MM:SS)
- gameTime: In-game hours played (float)
- sceneName: Current game scene name
- playerPosition (x, y, z): Player's position coordinates in the game world
- cameraPosition (x, y, z): Camera's position coordinates in the game world
- cameraAngleElevation (x, y): Camera's angle and elevation
- weatherSet: Current weather set
- weatherSetDurationHours: Duration of current weather set in hours
- weatherStage: Current weather stage
- windStrength: Current wind strength (Calm, SlightlyWindy, Windy, VeryWindy, Blizzard)
- windSpeedMPH: Current wind speed in MPH
- windPhaseDurationHours: Current wind strength duration in hours
- weatherCurrentTemperatureWithoutHeatSources: Current temperature (C) without heat sources
- weatherCurrentWindchill: Current wind chill temperature (C)
- weatherCurrentTemperatureWithWindchill: Current temperature (C) with wind chill factored in
- triggerCode: Code indicating what triggered the data capture (T=Time, D=Distance, K=Keypress, W=Weather Change, w=Wind Change)

Data is captured periodically based on various triggers:
- When the player presses a specific key
- At regular game time intervals in seconds
- When the player moves a certain distance in meters
- When there is a change in the current weather set or stage
- When there is a change in the current wind strength

The telemetry data is saved to a text file located in the game's Mods folder.
The filename is formatted as the name of the current game save followed by "_Telemetry.log".  Any illegal characters are removed and blank characters are replaced with underscores.
For example, if the name of the current game save is "FAR TERRITORY?", the telemetry data will be saved in "FAR_TERRITORY_Telemetry.log"

### Options

The mod includes configurable options that can be adjusted through a settings menu in-game.
<img width="1920" height="1080" alt="image" src="Options Screenshot.png" />

### Installation

<!-- >- **Install** [[ModSettings](https://github.com/DigitalzombieTLD/ModSettings/releases/tag/v2.0)] **and it's dependencies.** -->
- **Install** [[ModSettings](https://github.com/DigitalzombieTLD/ModSettings/releases/latest)] **and it's dependencies.**
- **Drop the** **.dll** **file into your mods folder**.
- **Enjoy**!