# Faster Frame Finishing

![Faster Frame Finishing banner](docs/images/banner.png)

A Stationeers mod that speeds up welding sheets onto iron and steel frames, and the plastic-sheet finish on steel frames. The default is 5x faster.

| Step | Normal | With the mod (5x) |
| --- | --- | --- |
| Weld iron or steel sheet (two per frame) | 1.0 s each | 0.2 s each |
| Plastic finish (steel frames) | 2.0 s | 0.4 s |

Speed options are 2x, 3x, 4x, 5x and 10x, plus an optional instant mode. Placing frames, taking frames apart and all other structures are unchanged.

## Requirements

- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases)
- [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad#installation)

This is a LaunchPad mod, not a standalone BepInEx plugin. Putting its DLL in `BepInEx\plugins` will not load it.

## Installation

### Steam Workshop

1. Install BepInEx and StationeersLaunchPad by following the [StationeersLaunchPad installation guide](https://github.com/StationeersLaunchPad/StationeersLaunchPad#installation).
2. Subscribe to **Faster Frame Finishing** on the Steam Workshop.
3. Start Stationeers. On the loading screen, click the **LaunchPad** box and check that **Faster Frame Finishing** is ticked.

### Manual installation

1. Install BepInEx and StationeersLaunchPad as above.
2. Download **`FasterFrameFinishing-1.2.0.zip`** from the [Releases page](https://github.com/macg4dave/faster_frame_finishing/releases). Use the zip file, not the **Code** button, which downloads the source code.
3. Put the `FasterFrameFinishing` folder from the zip into your Stationeers `mods` folder (see below).
4. Start Stationeers. On the loading screen, click the **LaunchPad** box and check that **Faster Frame Finishing** is ticked.

#### Windows

The default `mods` folder is:

```text
%USERPROFILE%\Documents\My Games\Stationeers\mods
```

Paste that path into the File Explorer address bar to open it. Then open the zip and drag the `FasterFrameFinishing` folder into the `mods` folder.

Do not use **Extract All…** with its default settings. It adds an extra folder named after the zip (`mods\FasterFrameFinishing-1.2.0\FasterFrameFinishing\`).

#### Linux / Steam Deck (Proton)

The `mods` folder is inside the game's Proton prefix:

```text
~/.local/share/Steam/steamapps/compatdata/544550/pfx/drive_c/users/steamuser/Documents/My Games/Stationeers/mods
```

```bash
unzip FasterFrameFinishing-1.2.0.zip -d ~/.local/share/Steam/steamapps/compatdata/544550/pfx/drive_c/users/steamuser/Documents/My\ Games/Stationeers/mods/
```

If Stationeers is in a different Steam library, replace `~/.local/share/Steam` with that library's folder.

#### Folder layout

The game creates the `mods` folder on first start; you can also create it yourself. The result should be:

```text
mods/
└── FasterFrameFinishing/
    ├── FasterFrameFinishing.dll
    └── About/
        ├── About.xml
        ├── Preview.png
        └── thumb.png
```

## Settings

Settings are in **main menu > Workshop > Faster Frame Finishing > Mod Configuration**, or in the LaunchPad window while the game is loading. Restart the game after changing a setting.

| Setting | Default | Description |
| --- | --- | --- |
| Enabled | On | Turns the mod on or off. Off means normal speed. |
| Speed Multiplier | 5x | How much faster welding and finishing are: 2x, 3x, 4x, 5x or 10x. |
| Instant Mode | Off | Makes welding and finishing almost instant (0.05 s), whatever the speed setting. |
| Debug Logging | Off | Writes extra details to the log file, for bug reports. |

## Multiplayer

The mod is client-side. Each player who wants faster frame building installs it; the server does not need it. Players without the mod keep the normal speed, and materials are used and frames seal exactly as normal for everyone.

This is based on the game code and has not yet been tested in a live multiplayer session.

## Saves and compatibility

- Nothing is stored in your save. The mod can be added or removed at any time.
- Placing frames, taking frames apart and all other structures are unchanged.
- Another mod that changes frame welding or finishing times may conflict with this one.
- If a game update changes how frames are built, the mod leaves frames at normal speed until it is updated.
- Tested with automated checks against Stationeers 0.2.6429.27802.

## Troubleshooting

**The mod does not appear**

Check that BepInEx and StationeersLaunchPad are installed, and that the mod is ticked in the LaunchPad window. For a manual install, compare your `mods` folder with the [folder layout](#folder-layout). A common cause is an extra folder level, such as `mods\FasterFrameFinishing-1.2.0\FasterFrameFinishing\`.

**Welding or finishing is not faster**

Check that **Enabled** is on, then restart the game. Settings only take effect after a restart. Another mod that changes frame construction can also override this one.

**Checking that the mod loaded**

Search the game's `BepInEx\LogOutput.log` for **Faster Frame Finishing**. For bug reports, turn on **Debug Logging**, restart the game and attach the log.

## Uninstalling

- **Steam Workshop:** unsubscribe and restart the game.
- **Manual install:** delete the `FasterFrameFinishing` folder from your `mods` folder.

You can also delete `stationeers.fasterframefinishing.cfg` from `BepInEx\config` to remove the saved settings. Saves are not affected.

---

## Contributing

Bug reports and pull requests are welcome on [GitHub](https://github.com/macg4dave/faster_frame_finishing/issues). Before working on the code, read [AGENTS.md](AGENTS.md) and the [testing notes](docs/TESTING.md).

[Source code](https://github.com/macg4dave/faster_frame_finishing) · [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad)

*Stationeers is developed by RocketWerkz. Faster Frame Finishing is an independent community mod.*
