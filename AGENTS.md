# AGENTS.md: guidance for AI and human contributors

## Purpose and scope

This mod changes **one value**: the `EntryTime` of the plastic-sheet + screwdriver finishing build state on steel-frame prefabs. Treat the narrow scope as a hard requirement:

- Do not change other build states, other structures, tool speeds, material quantities (`EntryQuantity*`), deconstruction (`ToolExit`, `ExitTime`), `BlockAir`, `BlockGravity`, or anything to do with the atmosphere.
- Do not add Harmony patches, transpilers or new dependencies unless editing prefab data has become impossible. If that happens, record why in this file first.

## Architecture

```
FasterFrameFinishingMod (MonoBehaviour, LaunchPad Default Entrypoint: OnLoaded(ConfigFile); no game types)
  ├─ Configuration/FinishingConfig   BepInEx ConfigFile binding → FinishingSettings
  └─ Game/FinishingBootstrap         guarded entry into game-typed code (static, owns patcher)
       ├─ Game/GameHooks             Prefab.OnPrefabsLoaded subscribe, "already loaded?" check
       └─ Game/FramePrefabPatcher    reads Structure.AllStructurePrefabs, writes ToolUse.EntryTime
            └─ Core/*                game-independent: matcher, calculator, original-value store
```

| Layer | Rule |
|---|---|
| `Core/` | No Unity, BepInEx or Stationeers references. Everything here is unit-tested and compiled directly into the test project. |
| `Configuration/` | May reference BepInEx only. |
| `Game/` | The only code that references `Assembly-CSharp`. Keep each game access in a small `[MethodImpl(NoInlining)]` method so that a missing member throws at a call site that catches it. |
| Entry class | References no Stationeers types, directly or through fields or locals, so that loading the entry class can never fail because of a game update. It must stay a plain `MonoBehaviour`, **never** a `BaseUnityPlugin`; see below. |

**Lifecycle.** StationeersLaunchPad adds `FasterFrameFinishingMod` as a component and calls `OnLoaded(ConfigFile)`, which binds the config. If `Enabled` is true, `FinishingBootstrap.Start` subscribes `FramePrefabPatcher.ApplySafely` to `Prefab.OnPrefabsLoaded`, and also applies straight away if prefabs are already registered. `ApplySafely` never throws: on error it rolls back.

**Idempotency.** `OriginalValueStore<ToolUse>` records the vanilla `EntryTime` per `ToolUse` object, by reference identity. Every application computes from the recorded vanilla value, so applying twice gives the same result as applying once.

**No OnDestroy restore.** The patcher is held statically and is not tied to the plugin GameObject. Never restore values in `OnDestroy`: if the GameObject LaunchPad attaches the mod to were destroyed early, the mod would silently turn itself off.

## Verified game facts (Stationeers 0.2.6429.27802, Unity 2022.3.62, BepInEx 5.4.23.3)

All of these come from the decompiled installed `Assembly-CSharp.dll` (ILSpy) and the shipped `resources.assets` (UnityPy). Do not add game API names to the code unless you have checked them the same way.

| API | Facts |
|---|---|
| `Assets.Scripts.Objects.Prefab` | `public static event Action OnPrefabsLoaded`, raised once near the end of `static async UniTask LoadAll()`. `LoadAll` returns early if `PrefabsGameObject != null`, so prefabs are registered once per process. `Register` instantiates each source prefab under the inactive `~Prefabs` object, then `RegisterExisting` adds structures to `Structure.AllStructurePrefabs`. `OnPrefabsLoaded` fires before `Thing.OnAllPrefabsLoaded()`. |
| `Assets.Scripts.Objects.Structure : Thing` | `public static readonly List<Structure> AllStructurePrefabs`; `public List<BuildState> BuildStates`; `NextBuildState => BuildStates[CurrentBuildStateIndex + 1]`. |
| `Structure.AttackWith(Attack, bool doAction)` | `value = clamp(tool?.getToolSpeed() ?? 1, 0.2, 5)`. Construction: `Duration = nextBuildState.Tool.EntryTime / value`. Deconstruction: `Duration = Tool.ExitTime / value`. Materials are handled by `HandleToolUse`: `ToolEntry2` (off-hand stackable) consumes `EntryQuantity2`. |
| `Assets.Scripts.Objects.BuildState` | `[Serializable]` class (not a `UnityEngine.Object`). Fields: `public ToolUse Tool`, plus `BlockAir`, `BlockGravity` and others. |
| `ToolUse : ToolBasic` | `[Serializable]`. `ToolBasic`: `Item ToolEntry`, `Item ToolEntry2`, `float EntryTime = 2f`, `int EntryQuantity`, `int EntryQuantity2`. `ToolUse`: `Item ToolExit`, `float ExitTime`, `int ExitQuantity`, `ToolUseType`. |
| `EntryTime` readers | Only `Structure.AttackWith` (build duration) and `Structure.BuildPlacementTime` (state 0). |
| Item classes | `Assets.Scripts.Objects.Items.Screwdriver : ManualTool` and `Assets.Scripts.Objects.Items.PlasticSheet : Stackable`. |
| Instancing | `Thing.Create<T>(prefabHash)` clones the registered prefab copy (`Prefab.Find`). `BuildState` and `ToolUse` are serialised by value, so every world instance has its own copy taken at creation or load time. Saves store only `CurrentBuildStateIndex`. |
| Client hold loop | `InventoryManager`: if `Duration > 0`, the `WaitUntilDone` coroutine runs (progress bar and sounds), then `UseItemComplete`. If `Duration == 0`, `UseItemComplete` is called immediately. This is why the mod's minimum is 0.05 s. |
| Networking | `OnServer.AttackWith` runs the action locally; a client then sends `AttackWithMessage`. On the server, `Process` calls `AttackWith(attack)` with no duration validation. Timing is client-side. |

**Prefab data** (from `resources.assets`; 736 `Structure*` prefabs have `BuildStates`):

| Prefab | States | Finishing stage |
|---|---|---|
| `StructureFrame` | 4: frames 0.5 s; torch + steel sheet 1.0 s; torch + steel sheet 1.0 s; **screwdriver + 1 plastic sheet 2.0 s** (exit: crowbar 3.0 s) | **index 3** |
| `StructureFrameIron` | 3 (same as the old steel layout, with iron sheets) | none |
| `StructureFrameSide`, `StructureFrameCorner`, `StructureFrameCornerCut` | 1 | none |

Exactly one state in the whole game uses `ItemScrewdriver` together with `ItemPlasticSheets`. Plastic sheets also appear with a welding torch (20 states), a crowbar (2) or no tool (30). The matcher therefore requires **both** tool types, a `StructureFrame` name prefix (which excludes `StructurePictureFrame*` and `StructureSuitStorageFrame`), and that the state is the **final** one. It does not hard-code index 3; index 3 is only checked to add a note to the log.

## StationeersLaunchPad integration (verified in installed SLP 1.2.1 and GitHub `main`)

The mod is loaded **only** by StationeersLaunchPad, through its **Default Entrypoint** (`StationeersLaunchPad/Entrypoints/Default.cs`):

- LaunchPad scans every `*.dll` under the mod folder (`Documents/My Games/Stationeers/mods/<mod>/`, or a Workshop item) that has `About/About.xml`.
- For each non-abstract `MonoBehaviour`, it takes the first instance method named `OnLoaded` (`DEFAULT_METHOD_NAME`) whose parameters are distinct types from `ConfigFile`, `List<GameObject>`, `List<Assembly>` and `ModData`. It adds that type as a component and calls the method.
- For a `ConfigFile` parameter, LaunchPad creates `new ConfigFile(Path.Join(Paths.ConfigPath, "<About.ModID>.cfg"), true)` and shows it in its configuration UI. `PluginInfo.ModId` must therefore equal `<ModID>` in `About.xml`; a test enforces this.
- LaunchPad *also* registers every `BaseUnityPlugin` as a separate BepInEx entrypoint (`Entrypoints/BepInEx.cs`). A class that was both would be initialised twice. `LaunchPadEntrypointTests` enforces that there is no `BaseUnityPlugin`.
- Do **not** reference `StationeersLaunchPad.dll`: its README says those classes are not a stable API. The Default Entrypoint is a convention (a method name and parameter types), so the mod needs no LaunchPad assembly at all. If you ever need LaunchPad helpers, use LaunchPadBooster, which has a stable API, and only for a real need.
- **Ordering.** `LaunchPadConfig` runs `StageLoading` (assemblies, then assets, then entrypoints, where `OnLoaded` is called) before `StartGame()` resumes `SplashBehaviour.AwakeCoroutine`, which leads to `WorldManager.LoadGameDataAsync` and `Prefab.LoadAll()`. `OnLoaded` therefore runs before prefabs exist. The "already loaded" path in `FinishingBootstrap` is a safety net only.
- **Local mods folder:** the game reads local mods from `<SavePath>\mods` (`WorkshopUtils.GetLocalDirInfo`). `SavePath` comes from `Documents\My Games\Stationeers\setting.xml`; by default it is `Documents\My Games\Stationeers`. `DeployToGame` reads `<SavePath>` with `XmlPeek`, and `StationeersModsDirectory` overrides it. The player README documents only the default folders (Windows, and Linux/Proton under `compatdata/544550`).
- **Release zip:** Release builds write `artifacts/FasterFrameFinishing-<Version>.zip`. It contains a single top-level `FasterFrameFinishing/` folder (the DLL plus `About/`). Players download this zip from GitHub Releases. Never tell players to copy folders out of the repository or `bin/`. The build fails if the `About.xml` `<Version>` differs from the project `<Version>`.
- `About.xml` declares `<ModSide>Client</ModSide>`, so LaunchPad labels it client-side. That matches the networking analysis.

## Re-verifying after a game update

1. Build and test with the game installed: `dotnet build -c Release` then `dotnet test --solution FasterFrameFinishing.sln -c Release --no-build`. `GameApiCompatibilityTests` fail if any referenced member, or the `EntryTime` read in `AttackWith`, has gone. `LaunchPadEntrypointTests` fail if a LaunchPad update changes the `OnLoaded` convention.
2. Re-extract the prefab data in a throwaway virtual environment outside the repo:
   ```powershell
   python -m venv $env:TEMP\fff-venv
   & $env:TEMP\fff-venv\Scripts\python -m pip install UnityPy TypeTreeGeneratorAPI
   & $env:TEMP\fff-venv\Scripts\python -I tools\dump_frame_states.py "<Stationeers dir>"
   & $env:TEMP\fff-venv\Scripts\python -I tools\dump_frame_states.py "<Stationeers dir>" Structure --finishing
   ```
   Compare the output with `tests/FasterFrameFinishing.Tests/VerifiedPrefabData.cs`, and update both that file and the tables above.
3. To read game code, decompile into a temporary folder **outside the repository** (`dotnet tool install -g ilspycmd`, then `ilspycmd -p -o <tmp> -r <Managed> <Managed>\Assembly-CSharp.dll`). Never commit decompiled code or game binaries.
4. Run the in-game checklist in `docs/TESTING.md` and record the results there.

## Coding conventions

- C# 9 in the mod (`netstandard2.1`, compiled against the game's Mono `mscorlib`/`netstandard`). Avoid `record`, `init` and other features that need runtime types missing from Unity's profile. Tests use the latest C# on .NET 10.
- `TreatWarningsAsErrors` is on. Keep the build at zero warnings.
- Use Unity's overloaded `==` for `UnityEngine.Object` null checks, and plain `?.` only on non-Unity types (`BuildState`, `ToolUse`).
- Every change to game data must go through `FinishingApplier` and `OriginalValueStore`.
- Log at Info exactly what is modified (prefab, state index, tools, old → new). Detailed dumps go behind `DebugLogging`.
- Use `CultureInfo.InvariantCulture` when formatting numbers for logs and config descriptions.
- Add a unit test in `Core` for any new logic. Put game-API assumptions in `GameApiCompatibilityTests`.

## Build facts

- The game path comes from `StationeersDirectory`: the command line, then `Stationeers.User.props`, then `STATIONEERS_DIR`, then the default Steam path. The derived `StationeersRoot` always has a trailing slash.
- Mod references: `mscorlib`, `netstandard`, `System`, `System.Core`, `Assembly-CSharp`, `UnityEngine`, `UnityEngine.CoreModule` and `UniTask` from `rocketstation_Data\Managed`, plus `BepInEx` from `BepInEx\core`. All are `Private=false`, so none is copied to the output. **No Harmony.**
- `global.json` opts in to Microsoft.Testing.Platform (required by the .NET 10 SDK for xUnit v3). Use `dotnet test --solution …` or `dotnet test --project …`.
- CI (`.github/workflows/build.yml`) runs the core tests on hosted runners. The full mod build runs only on an opt-in self-hosted runner that has the game installed.
