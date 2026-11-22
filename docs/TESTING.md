# Test strategy

Testing is split into **automated** checks, which need no game session, and **in-game** checks, which need a person playing. Nothing in the in-game section has been run unless a result has been filled in.

## 1. Automated

```powershell
dotnet build FasterFrameFinishing.sln -c Release
dotnet test --solution FasterFrameFinishing.sln -c Release --no-build
```

| Test class | Needs game install | Covers |
|---|---|---|
| `FinishingStageMatcherTests` | no | Detects the fourth stage. Leaves other frame variants and non-frame prefabs alone. Skips unexpected layouts. |
| `FinishingTimeTests` | no | Multiplier maths, instant mode, minimum duration, disabled mode, input validation. |
| `IdempotencyTests` | no | Repeated application doesn't compound. Restore is exact. Keys use reference identity. |
| `WorkshopReadinessTests` | no | Workshop description is at most 8,000 characters and its BBCode is balanced. Install steps are present. The in-game description has no BBCode the game shows raw. `Preview.png` is a square PNG under 1 MB. A reminder (shown as a skip) appears while the GitHub link is still a placeholder. |
| `ConfigBindingTests` | BepInEx.dll | Default values, generated `.cfg` text, clamping of out-of-range values, disabled config. |
| `GameApiCompatibilityTests` | Assembly-CSharp.dll | Every game member the mod references exists. `AttackWith` still reads `EntryTime`. |
| `LaunchPadEntrypointTests` | built mod; StationeersLaunchPad.dll for one check | Exactly one LaunchPad Default Entrypoint (`OnLoaded(ConfigFile)`) and no `BaseUnityPlugin`. No references to Harmony or `StationeersLaunchPad.dll`. The installed LaunchPad still uses `OnLoaded`. `About.xml` matches the code constants. |

The fixture data in `VerifiedPrefabData.cs` was extracted from the game's `resources.assets` with `tools/dump_frame_states.py`. After a game update, run that tool again and update the fixtures if anything changed.

**Last local run:** game 0.2.6429.27802, .NET SDK 10.0.404. 66 passed, 0 failed, 1 skipped. The skip is the intentional reminder to replace the GitHub placeholder link. With no game install (as in CI), the 5 game API tests and 4 LaunchPad tests are skipped, and the config tests are not compiled.

## 2. In-game checklist (manual)

Set up a creative or test world that has steel frames, steel sheets, plastic sheets, a welding torch, a screwdriver and a crowbar.

| # | Check | Expected | Result |
|---|---|---|---|
| 1 | Install into `Documents/My Games/Stationeers/mods`, start the game, and confirm the mod is listed and ticked in the LaunchPad window. Search `BepInEx/LogOutput.log` for `Faster Frame Finishing`. | It logs `loaded via StationeersLaunchPad`, then `StructureFrame build state 3 (ItemScrewdriver + 1x ItemPlasticSheets): EntryTime 2s -> 0.4s` and `4 other frame variant(s) left unchanged`. No errors. | not run |
| 2 | Place a steel frame, then weld 2 steel sheets. | Each stage takes the vanilla time: 0.5 s, then 1.0 s, then 1.0 s. | not run |
| 3 | Hold a screwdriver with plastic sheets in the off hand and finish the frame. | About 0.4 s (in a suit, normal mood). Screwdriver sound and progress bar as in vanilla. | not run |
| 4 | Count plastic sheets before and after step 3. | Exactly one consumed. | not run |
| 5 | Deconstruct the finished frame with a crowbar. | Vanilla 3.0 s. The plastic sheet is returned as in vanilla. | not run |
| 6 | Build an iron frame. | Vanilla behaviour, with 3 stages. | not run |
| 7 | Set `InstantFinish = true` and restart. Repeat step 3. | Completes almost immediately. One sheet consumed. | not run |
| 8 | Set `Enabled = false` and restart. Repeat step 3. | Vanilla 2.0 s. Log says `Disabled in config`. | not run |
| 9 | Pick each option (2x, 3x, 4x, 5x, 10x) in the **Speed Multiplier** drop-down and restart each time. | The log shows `2s -> 1s`, `0.667s`, `0.5s`, `0.4s` and `0.2s` respectively. | not run |
| 10a | Open main menu → Workshop → Faster Frame Finishing. | No `thumb.png` alert appears. The tidy in-game description is shown, not raw `[h1]` tags. The picture is not stretched. **Speed Multiplier** is a drop-down offering 2x, 3x, 4x, 5x and 10x. | not run |
| 10 | Set `SpeedMultiplier = 7` or `abc` in the cfg file and restart. | The mod falls back to 5x (with a warning for `7`), and the file is rewritten to `x5`. The game does not crash. | not run |
| 11 | Set `DebugLogging = true` and restart. | All 5 `StructureFrame*` prefabs and their states are listed. | not run |
| 12 | Multiplayer: host without the mod, client with it. | The client finishes in about 0.4 s and the server accepts it. The sheet is consumed on the server. | not run |
| 13 | Multiplayer: client without the mod. | Vanilla 2.0 s for that client. | not run |
| 14 | Load a save that has finished frames, then uninstall the mod and load it again. | The save loads normally both times. | not run |

## 3. Fail-safe behaviour (by design, from code review)

- If a referenced game member is missing at runtime, the call into `FinishingBootstrap` throws `MissingMemberException` or `TypeLoadException`. `FasterFrameFinishingMod.OnLoaded` catches it and logs an error. Nothing is modified.
- If an exception happens while prefabs are being modified, `FramePrefabPatcher.ApplySafely` catches it and restores every value it had already changed.
- If the stage is not found, or appears more than once, or is not the final state, the mod logs a warning and leaves that prefab vanilla.
