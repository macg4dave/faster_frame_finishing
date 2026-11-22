# Faster Frame Finishing

![Faster Frame Finishing banner](docs/images/banner.png)

Faster Frame Finishing is a small quality-of-life mod for **Stationeers** that speeds up the final step of building a steel frame: applying a plastic sheet with a screwdriver.

By default, that step is **5× faster**, dropping from **2 seconds to 0.4 seconds**. You can choose your own speed or make it almost instant.

## Installation

The mod uses **StationeersLaunchPad**. You will need **BepInEx 5.4** and **StationeersLaunchPad** installed first. These are shared modding tools, so you may already have them.

### Steam Workshop

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) and [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad#installation), following the LaunchPad installation instructions.
2. When the mod is available on the Stationeers Steam Workshop, subscribe to **Faster Frame Finishing**.
3. Start Stationeers. On the loading screen, click the **LaunchPad** box and make sure **Faster Frame Finishing** is ticked.

### Manual installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) and [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad#installation) as described above.
2. Download **`FasterFrameFinishing-1.1.0.zip`** from the [Releases page](https://github.com/macg4dave/faster_frame_finishing/releases).
   Download the zip file, not the green **Code** button. That button gives you the source code, which the game can't use.
3. Unzip it into your Stationeers `mods` folder (see below).
4. Start Stationeers. On the loading screen, click the **LaunchPad** box and make sure **Faster Frame Finishing** is listed and ticked.

#### Windows

The default `mods` folder is:

```text
%USERPROFILE%\Documents\My Games\Stationeers\mods
```

Paste that line into the address bar of File Explorer and press **Enter** to open it. Then open the zip file and drag the **`FasterFrameFinishing`** folder from inside it into the `mods` folder.

> Avoid **Extract All…** with its default settings. Windows adds an extra folder named after the zip, which gives you `mods\FasterFrameFinishing-1.1.0\FasterFrameFinishing\`.

#### Linux / Steam Deck (Proton)

Stationeers runs through Proton, so the `mods` folder is inside the game's Proton prefix:

```text
~/.local/share/Steam/steamapps/compatdata/544550/pfx/drive_c/users/steamuser/Documents/My Games/Stationeers/mods
```

Unzip from a terminal:

```bash
unzip FasterFrameFinishing-1.1.0.zip -d ~/.local/share/Steam/steamapps/compatdata/544550/pfx/drive_c/users/steamuser/Documents/My\ Games/Stationeers/mods/
```

If Stationeers is installed in a different Steam library, replace `~/.local/share/Steam` with that library's folder.

#### Check the result

If a `mods` folder doesn't exist yet, start the game once and it will be created, or create it yourself. When everything is in place, it should look like this:

```text
mods/
└── FasterFrameFinishing/
    ├── FasterFrameFinishing.dll
    └── About/
        ├── About.xml
        └── Preview.png
```


> **Important:** This is a LaunchPad mod, not a standalone BepInEx plugin. Putting its DLL in `BepInEx\plugins` will not load it.

## Change the speed

Open **Stationeers → main menu → Workshop → Faster Frame Finishing → Mod Configuration**. You can also edit the settings in the LaunchPad window while the game loads.

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Turn the mod on or off. Off restores normal finishing speed. |
| `Speed Multiplier` | `5x` | Choose from the drop-down list: **2x**, **3x**, **4x**, **5x** or **10x**. For normal speed, turn `Enabled` off. |
| `InstantFinish` | `false` | Make the finishing step almost instant (0.05 seconds), regardless of the multiplier. |
| `DebugLogging` | `false` | Write extra diagnostic details, useful when reporting a problem. |

## Multiplayer

Faster Frame Finishing is designed as a **client-side mod**. Each player who wants faster finishing installs it on their own game; the server and other players do not need it.

Players without the mod keep their usual finishing speed. Material consumption and the finished frame remain the same.

Multiplayer behaviour has been checked against the game code but **has not yet been confirmed in a live multiplayer session**.

## Saves and compatibility

- **No save-game changes.** The mod only adjusts construction timing when the game starts, so existing worlds do not need converting.
- **Safe to disable or remove.** Frames and materials are unchanged.
- **Other construction steps stay normal.** Welding, placing frames and deconstruction are unaffected.
- **Possible mod conflicts.** Another mod that changes the same steel-frame finishing time may override this one, or vice versa.
- **Game updates.** If Stationeers changes the expected finishing step, the mod is designed to leave the original timing alone rather than modify the wrong step.

The mod has passed automated checks against **Stationeers 0.2.6429.27802**, Feedback is welcome.

## Troubleshooting

**The mod is not showing up**

Check that BepInEx and StationeersLaunchPad are installed, and that the mod is enabled in the LaunchPad window. If you installed it manually, compare your `mods` folder with the layout under [Check the result](#check-the-result). The most common mistake is an extra folder level, such as `mods\FasterFrameFinishing-1.1.0\FasterFrameFinishing\`.

**The finishing speed has not changed**

Check that `Enabled` is `true`, then restart the game. Settings only take effect after a restart. Also check for another mod that changes frame construction.

**I want to check whether the mod loaded**

Look in the game's `BepInEx\LogOutput.log` for a message mentioning **Faster Frame Finishing**. If you're reporting a problem, enable `DebugLogging`, restart and attach the relevant log.

Found an issue? [Report it on GitHub](https://github.com/macg4dave/faster_frame_finishing/issues).

## Uninstall

If you installed through Steam Workshop, unsubscribe and restart the game. Alternatively, untick the mod in LaunchPad to disable it.

For a manual installation, delete the `FasterFrameFinishing` folder from the `mods` folder you installed it into (see [Manual installation](#manual-installation)).

You can also delete its settings file from `BepInEx\config` if you no longer want to keep your preferences. Your saved worlds do not depend on the mod.

---

## Contributing

Bug reports, feedback, translations and pull requests are welcome. Please [open an issue](https://github.com/macg4dave/faster_frame_finishing/issues) to describe a problem or proposed change. If you want to work on the code, read [AGENTS.md](AGENTS.md) and the [testing notes](docs/TESTING.md) before submitting a pull request.

[Source code](https://github.com/macg4dave/faster_frame_finishing) · [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad)

*Stationeers is developed by RocketWerkz. Faster Frame Finishing is an independent community mod.*
