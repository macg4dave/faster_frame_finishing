# Faster Frame Finishing

![Faster Frame Finishing](docs/images/banner.png)

> 🎮 **Just want to play?** Subscribe on the **Steam Workshop** and follow the install steps on the Workshop page. Everything below is for people building the mod from source.

A small [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad) mod for **Stationeers** that speeds up the **plastic-sheet finishing stage of steel frames**: the fourth construction step, where you hold a screwdriver and apply one plastic sheet.

- **5× faster by default.** Vanilla is 2.0 s; with the mod it is 0.4 s, before the game's own tool-speed modifiers.
- **Configurable** speed multiplier, where `1.0` is vanilla, plus an optional instant-finish mode.
- **Only that one stage changes.** Placement, the two steel-sheet welding stages, deconstruction, materials, atmosphere sealing, gravity blocking and every other structure keep vanilla behaviour.
- **No Harmony patches.** The mod changes one verified field on one prefab, once, at load time.
- **Loaded by StationeersLaunchPad** through its native Default Entrypoint (`OnLoaded(ConfigFile)`). It is not a BepInEx plugin, and you can edit its settings in the LaunchPad window.
- **Client-side.** The server does not need it installed (see [Multiplayer](#multiplayer-and-dedicated-servers)).

> Verified against Stationeers **0.2.6429.27802** (Unity 2022.3.62, BepInEx 5.4.23.3, StationeersLaunchPad). See [What has been tested](#what-has-and-has-not-been-tested).

---

## What it changes, exactly

The steel frame prefab (`StructureFrame`) has four build states in this game version:

| # | Tool (active hand) | Material (off hand) | Vanilla time | Changed? |
|---|---|---|---|---|
| 0 | Steel Frames kit | – | 0.5 s | no |
| 1 | Welding Torch | 1× Steel Sheets | 1.0 s | no |
| 2 | Welding Torch | 1× Steel Sheets | 1.0 s | no |
| **3** | **Screwdriver** | **1× Plastic Sheets** | **2.0 s** | **yes: 2.0 s ÷ SpeedMultiplier** |

The mod writes only `BuildStates[3].Tool.EntryTime` on the registered `StructureFrame` prefab. The game calculates the hold duration as `EntryTime / toolSpeed`, so suit, hygiene and mood speed modifiers still apply on top.

Other frame variants in this game version (`StructureFrameIron`, `StructureFrameSide`, `StructureFrameCorner`, `StructureFrameCornerCut`) have no plastic finishing stage. The mod detects them and leaves them alone. If a future update adds the same stage to another `StructureFrame*` variant, the mod picks it up automatically.

Each time the game starts, the mod logs exactly what it changed:

```
[Info   :Faster Frame Finishing] StructureFrame build state 3 (ItemScrewdriver + 1x ItemPlasticSheets): EntryTime 2s -> 0.4s (x5; final state, as expected).
[Info   :Faster Frame Finishing] Frame finishing modified on 1 prefab(s); 4 other frame variant(s) left unchanged.
```

## Installation

**Requirements**
- Stationeers with [BepInEx 5.4](https://github.com/BepInEx/BepInEx/releases) and [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad/releases) installed. Follow LaunchPad's own installation guide.
- LaunchPad is **required**. The mod has no BepInEx plugin class, so BepInEx alone will not load it, and placing it in `BepInEx\plugins` does nothing.

**Install**
1. Copy the `FasterFrameFinishing` folder, containing `FasterFrameFinishing.dll` and `About/About.xml`, to
   `%USERPROFILE%\Documents\My Games\Stationeers\mods\FasterFrameFinishing\`.
   If you build from source, `-p:DeployToGame=true` does this for you.
2. Start the game. If the mod is unticked in the LaunchPad window on the loading screen, tick it.
3. To check that it loaded, look in `<Stationeers>\BepInEx\LogOutput.log` for `Faster Frame Finishing 1.0.0 loaded via StationeersLaunchPad`.

## Removal

1. Untick the mod in the LaunchPad window, or delete `Documents\My Games\Stationeers\mods\FasterFrameFinishing\`.
2. Optionally delete `<Stationeers>\BepInEx\config\stationeers.fasterframefinishing.cfg`.

That fully reverts the change. The mod writes nothing to save files, because build-state timings are not saved; they come from prefabs every time the game starts. Saves made with the mod load normally without it.

To turn the mod off without uninstalling, set `Enabled = false` and restart.

## Configuration

On first launch, LaunchPad passes the mod a BepInEx `ConfigFile` named after the `About.xml` ModID: `<Stationeers>\BepInEx\config\stationeers.fasterframefinishing.cfg`. You can edit it in the **LaunchPad window**, which you open by clicking the LaunchPad box on the loading screen, or in any text editor. The complete generated file is in [docs/stationeers.fasterframefinishing.cfg](docs/stationeers.fasterframefinishing.cfg).

```ini
[Frame Finishing]
Enabled = true
SpeedMultiplier = 5
InstantFinish = false
DebugLogging = false
```

| Setting | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | `false` keeps vanilla timing, and the mod modifies nothing. |
| `SpeedMultiplier` | `5` | `1.0` is vanilla. `5` means 2.0 s → 0.4 s. Values below 1 slow the stage down. Accepted range is **0.1–100**. BepInEx clamps out-of-range values, and invalid values (NaN, 0, negative) fall back to 5 with a warning in the log. |
| `InstantFinish` | `false` | Finishes almost instantly (0.05 s) whatever the multiplier. You still hold the screwdriver, and one plastic sheet is still consumed. |
| `DebugLogging` | `false` | Logs every steel-frame prefab's build states and the matching decisions. |

**All settings need a game restart.** Frame timings are copied from the prefab into each frame when that frame is created or loaded, so a change made mid-session would only affect some frames. The mod therefore applies its settings once, at startup. If you change a value while the game is running, a warning is logged as a reminder.

The shortest duration the mod will write is 0.05 s. A duration of exactly 0 would bypass the game's normal hold-to-construct routine, which handles the progress bar, sounds and completion. The minimum keeps that routine intact.

## Multiplayer and dedicated servers

**Install it on the clients that want faster finishing. The server does not need it.**

This comes from the game code, not from testing:

- The client calculates the construction duration locally. The player's client calls `Structure.AttackWith(..., doAction: false)` to get the duration, runs the progress bar itself, and only then sends `AttackWithMessage` to the server.
- The server applies the completed action without checking how long it took. `AttackWithMessage.Process` calls `AttackWith` directly, and `CompletionRatio` is not used to validate construction.
- Build-state timings are never sent over the network.

As a result:

| Setup | Effect |
|---|---|
| Single player | Works. |
| Listen-server host with the mod | Faster for the host only. |
| Client with the mod, server without | Faster for that client. |
| Client without the mod | Vanilla for that client, whatever the server has. |
| Dedicated server with the mod | No effect on anyone (harmless). |

Material use, atmosphere and structure state are handled by the server exactly as in vanilla. Mixed installs cause no desync. One consequence: a player with the mod finishes frames faster than players without it. Server operators who care about fairness should treat it as a client-side quality-of-life mod.

`About.xml` declares `<ModSide>Client</ModSide>`, so LaunchPad shows it as client-side.

## Building (Windows)

**Prerequisites**

- [.NET SDK 10](https://dotnet.microsoft.com/download). This repository pins it in `global.json`. The mod itself targets `netstandard2.1` and compiles against the game's own Mono libraries.
- A local Stationeers install with BepInEx 5 and StationeersLaunchPad. The build references the game's assemblies directly; they are never copied into or committed to this repository.

**Point the build at your game**, using any one of these:

```powershell
# a) per-machine file (git-ignored)
Copy-Item Stationeers.User.props.example Stationeers.User.props   # then edit the path

# b) environment variable
$env:STATIONEERS_DIR = 'F:\SteamLibrary\steamapps\common\Stationeers'

# c) command line
dotnet build -p:StationeersDirectory='F:\SteamLibrary\steamapps\common\Stationeers'
```

If none is set, the default `C:\Program Files (x86)\Steam\steamapps\common\Stationeers` is used. If a required assembly is missing, the build stops with an error naming the file it expected.

**Build and test**

```powershell
dotnet build FasterFrameFinishing.sln -c Release
dotnet test --solution FasterFrameFinishing.sln -c Release --no-build
```

The output is a ready-to-install folder at `src\FasterFrameFinishing\bin\Release\package\FasterFrameFinishing\`.
To copy it straight into your LaunchPad mods folder, add `-p:DeployToGame=true`.

## What has and has not been tested

**Automated (run locally against game 0.2.6429.27802: 66 passed, 1 skipped (the reminder to set the GitHub link))**

| Requirement | How it is checked |
|---|---|
| Fourth stage detected | Matcher tests use build-state data extracted from the shipped `resources.assets`. Exactly one state among all 736 structure prefabs matches. |
| Multiplier applied correctly | Calculator tests (×1, ×2, ×5, ×10, ×0.5, floor, instant). |
| Earlier stages keep vanilla timing | The matcher selects only the final screwdriver + plastic state, and plastic sheets used with other tools never match. |
| Disabled means vanilla | Calculator, applier and real BepInEx config-binding tests. |
| Missing or changed game API fails safely | `GameApiCompatibilityTests` check the installed `Assembly-CSharp.dll` for every member the plugin references. They also confirm from the IL that `Structure.AttackWith` still reads `EntryTime` and calls `getToolSpeed()`. At runtime, any failure is caught, logged and rolled back. |
| No compounding on repeated initialisation | Applier/store tests: applying 10 times gives the same result as once, and restore is exact. |
| Other frame variants handled | Iron, side, corner and corner-cut frames are all checked to be left unmodified. |
| LaunchPad integration | `LaunchPadEntrypointTests`: the built DLL exposes exactly one Default Entrypoint (`MonoBehaviour.OnLoaded(ConfigFile)`) under LaunchPad's rules, and no `BaseUnityPlugin` that would cause double initialisation. It does not reference `StationeersLaunchPad.dll`. The installed LaunchPad still uses `OnLoaded`. `About.xml` ModID and Version match the code. |
| Configuration file and validation | The real `FinishingConfig` is bound to BepInEx's real `ConfigFile`: defaults, generated text and clamping. |

Game API and config-binding tests run only when a local game install is found; otherwise they are skipped (as in CI).

**Not yet tested in a game session.** The steps below are in [docs/TESTING.md](docs/TESTING.md):

- The log lines appear at startup, and the finishing stage takes about 0.4 s in play.
- One plastic sheet is consumed and the screwdriver interaction is unchanged. This follows from the code, since the mod does not touch quantities or tools, but it has not been observed in game.
- Multiplayer behaviour as described above.

## Compatibility notes and limitations

- Other mods that change `StructureFrame` build-state 3 `EntryTime` will conflict, and whichever runs last wins. Mods that change other fields, such as FriendlyFrames (`BlockGravity`, `ToolExit`), are compatible.
- Mods that scale tool speed globally stack multiplicatively with this one. The game also clamps tool speed to 0.2–5.
- If a game update changes the frame layout, the mod logs a warning and leaves frames vanilla rather than guessing. Examples: the stage is no longer the final state, more than one such stage exists, or no frame has one. Re-check with the steps in [AGENTS.md](AGENTS.md#re-verifying-after-a-game-update).
- Frames that already exist in a world use the speed configured at startup. Restart after changing settings.

## Publishing to the Steam Workshop

The game uploads the mod folder itself. The Workshop page text comes from `<Description>` in [About/About.xml](src/FasterFrameFinishing/About/About.xml) (Steam BBCode). The preview image is `About/Preview.png`, and the change note is `<ChangeLog>`. The in-game mod list shows `<InGameDescription>` instead, because the game cannot display BBCode lists or tables.

1. **Once only:** replace `YOUR-GITHUB-NAME` in `About.xml` with your real GitHub URL. Until you do, the test `GitHub_link_is_not_a_placeholder` shows as skipped, as a reminder.
2. Build the mod and copy it to your local mods folder:
   `dotnet build src/FasterFrameFinishing -c Release -p:DeployToGame=true`
3. Start Stationeers and go to **main menu → Workshop**. Select **Faster Frame Finishing** and click **Publish**.
4. After the first publish, the game writes the new `<WorkshopHandle>` into the deployed copy of `About.xml`, in `Documents\My Games\Stationeers\mods\FasterFrameFinishing\About\About.xml`. **Copy that line into `src/FasterFrameFinishing/About/About.xml`**, so that later publishes update the same Workshop item instead of creating a new one.
5. For updates: bump `<Version>` in `About.xml` and `PluginInfo.Version` (a test checks that they match), update `<ChangeLog>`, then repeat steps 2–3.

The artwork is original. To regenerate it, run `python -I tools/make_art.py <out_dir>`; it needs Pillow.

## Repository layout

```
src/FasterFrameFinishing/        LaunchPad mod (netstandard2.1)
  FasterFrameFinishingMod.cs     LaunchPad Default Entrypoint: config, start, error containment
  Configuration/                 BepInEx config binding
  Core/                          game-independent logic (matching, timing, idempotency)
  Game/                          the only code that touches Stationeers types
  About/About.xml                LaunchPad metadata + Steam Workshop text
  About/Preview.png              Steam Workshop preview image (512×512)
tests/FasterFrameFinishing.Tests xUnit v3 tests (.NET 10)
tools/dump_frame_states.py       re-extracts build-state data from the installed game
tools/make_art.py                regenerates Preview.png and the README banner
docs/                            generated config sample, in-game test checklist, images
```

## Credits

Written independently. The approach of editing prefabs on `Prefab.OnPrefabsLoaded` was informed by [cherrydev/StationeersFriendlyFramesMod](https://github.com/cherrydev/StationeersFriendlyFramesMod), [StationeersModding/ExamplePatchMod](https://github.com/StationeersModding/ExamplePatchMod) and [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad). Stationeers is © RocketWerkz; no game files are included.
