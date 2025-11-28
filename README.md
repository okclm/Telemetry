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
- weatherStage: Current weather stage
- weatherCurrentTemperatureWithoutHeatSources: Current temperature (C) without heat sources in the game world
- weatherCurrentWindchill: Current wind chill temperature (C) in the game world
- weatherCurrentTemperatureWithWindchill: Current temperature (C) with wind chill factored in
- triggerCode: Code indicating what triggered the data capture (T=Time, D=Distance, K=Keypress, W=Weather Change)

Data is captured periodically based on various triggers:
- At regular game time intervals (every 60 seconds by default)
- When the player moves a certain distance (10 meters by default)
- When the player presses a specific key (Numeric keypad zero by default)
- When there is a change in weather conditions (Weather Set or Stage)

The telemtry data is saved to a text file located in the mod's directory within the game's Mods folder.
The filename is formatted as the name of the current game save followed by "_Telemetry.log".  Any illegal characters in the save name are replaced with underscores.
For example, if the name of the current game save is "FAR TERRITORY", the telemetry data will be saved in "FAR_TERRITORY_Telemetry.log"

### Options

The mod includes configurable options that can be adjusted through a settings menu in-game.
<img width="1920" height="1080" alt="image" src="Options Screenshot.png" />

### Installation

<!-- >- **Install** [[ModSettings](https://github.com/DigitalzombieTLD/ModSettings/releases/tag/v2.0)] **and it's dependencies.** -->
- **Install** [[ModSettings](https://github.com/DigitalzombieTLD/ModSettings/releases/latest)] **and it's dependencies.**
- **Drop the** **.dll** **file into your mods folder**.
- **Enjoy**!