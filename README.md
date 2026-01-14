# Sorbet 1600 LTD Tuner

A MSCLoader mod for **My Winter Car** that adds an in-game tuning UI for the Sorbet 1600 LTD daily driver.

![MSCLoader](https://img.shields.io/badge/MSCLoader-Compatible-green)
![Version](https://img.shields.io/badge/Version-1.0.0-blue)

## Features

*   **Engine Tuning**: Adjust power and torque multipliers (up to 2x stock)
*   **Forced Induction**: Add turbo boost (0-20 PSI)
*   **Nitrous Oxide**: 5 shots of nitrous per session
*   **Transmission**: Adjust final drive, gear ratios, and enable Launch Control
*   **Drivetrain Swap**: Switch between FWD (Stock), RWD, and AWD
*   **Suspension/Handling**: Adjust mass, center of mass, and grip
*   **Brakes**: Tune brake force and bias
*   **Save/Load**: Save your tuning setups to presets
*   **Undo/Redo**: Safely experiment with undo/redo functionality

## Installation

### Prerequisites
1. [MelonLoader](https://melonwiki.xyz/) installed in your My Winter Car game folder

### Install the Mod
1. Download `SorbetTuner.dll` from releases
2. Copy to `My Winter Car/Mods/` folder
3. Launch the game

## Building from Source

### Requirements
- Visual Studio 2019+ or Rider
- .NET Framework 3.5

### Steps
1. Open `SorbetTuner.csproj`
2. **Update the `<GamePath>` in the .csproj** to point to your My Winter Car installation
3. Build the project (`dotnet build` or via IDE)
4. The DLL will automatically copy to your Mods folder

```xml
<!-- In SorbetTuner.csproj, update this line: -->
<GamePath>C:\Program Files (x86)\Steam\steamapps\common\My Winter Car</GamePath>
```

## Usage

1. Launch the game and load your save
2. Press **F8** to open the tuning menu
3. Adjust sliders to your liking
4. Click **WRITE TO ECU** to apply tuning
5. Save presets for later use!

## Controls

| Key | Action |
|-----|--------|
| F8 | Toggle tuning menu |
| F9 | Quick apply tuning |
| F10 | Reset to stock |
| Ctrl+Z | Undo last change |
| N | Activate nitrous (when charged) |
| Mouse | Adjust sliders, click tabs |

## Presets

Presets are saved to: `My Winter Car/Mods/SorbetTuner/Presets/`

You can share preset `.json` files with friends!

## Troubleshooting

### "Car not found"
- Make sure you're in the game world (not main menu)
- Click "RE-SCAN FOR Sorbet" in the Misc tab
- Check MSCLoader console for debug messages
- Ensure the mod loaded (check MSCLoader console on startup)
- Try pressing F8 multiple times
- Make sure no other mod is using F8

## Credits

- Made with ❤️ for the My Winter Car community
- Uses [MSCLoader](https://github.com/piotrulos/MSCloader)

## License

MIT License - Feel free to modify and share!
