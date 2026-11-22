"""Dump Structure build states from an installed Stationeers game, for re-verifying this mod.

Reads rocketstation_Data/resources.assets (read-only) with UnityPy, generating MonoBehaviour type
trees from the game's own Managed/*.dll, and prints a JSON list of every prefab whose name starts
with PREFIX together with each BuildState's tools, quantities and times.

Setup (once, in a throwaway virtual environment outside the repo):
    python -m venv .venv-tools
    .venv-tools/Scripts/python -m pip install UnityPy TypeTreeGeneratorAPI

Usage:
    .venv-tools/Scripts/python -I tools/dump_frame_states.py "<Stationeers dir>" [PREFIX] > frames.json
    .venv-tools/Scripts/python -I tools/dump_frame_states.py "<Stationeers dir>" Structure --finishing

PREFIX defaults to "StructureFrame". With --finishing, only states using ItemScrewdriver together
with ItemPlasticSheets are printed (one line each) - in game 0.2.6429.27802 that is exactly
StructureFrame state 3.
"""
import json
import os
import sys

import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator


def load_env(game_dir):
    data_dir = os.path.join(game_dir, "rocketstation_Data")
    env = UnityPy.load(os.path.join(data_dir, "resources.assets"))
    assets_file = next(iter(env.files.values()))
    gen = TypeTreeGenerator(assets_file.unity_version)
    managed = os.path.join(data_dir, "Managed")
    # The native generator writes to file descriptor 1 on duplicate assembly identities; send that
    # to stderr so stdout stays valid JSON.
    sys.stdout.flush()
    saved_stdout = os.dup(1)
    os.dup2(2, 1)
    try:
        for name in sorted(os.listdir(managed)):
            if not name.endswith(".dll"):
                continue
            try:
                with open(os.path.join(managed, name), "rb") as fh:
                    gen.load_dll(fh.read())
            except Exception:  # noqa: BLE001 - duplicates are harmless
                pass
    finally:
        os.dup2(saved_stdout, 1)
        os.close(saved_stdout)
    env.typetree_generator = gen
    return assets_file.objects


def dump(objects, prefix):
    def owner_name(path_id):
        try:
            obj = objects[path_id]
            if obj.type.name == "MonoBehaviour":
                go_id = obj.read_typetree()["m_GameObject"]["m_PathID"]
            else:
                go_id = obj.read().m_GameObject.path_id
            return objects[go_id].read().m_Name
        except Exception as exc:  # noqa: BLE001
            return f"<unresolved {path_id}: {exc}>"

    def ref(pptr):
        if not pptr or pptr.get("m_PathID", 0) == 0:
            return None
        if pptr.get("m_FileID", 0) != 0:
            return f"<external {pptr['m_FileID']}:{pptr['m_PathID']}>"
        return owner_name(pptr["m_PathID"])

    result = []
    for obj in objects.values():
        if obj.type.name != "GameObject":
            continue
        go = obj.read()
        if not go.m_Name.startswith(prefix):
            continue
        for comp in go.m_Components:
            ptr = comp.component if hasattr(comp, "component") else comp
            co = objects.get(ptr.path_id)
            if co is None or co.type.name != "MonoBehaviour":
                continue
            try:
                tree = co.read_typetree()
            except Exception:  # noqa: BLE001
                continue
            if "BuildStates" not in tree:
                continue
            states = []
            for i, bs in enumerate(tree["BuildStates"]):
                tool = bs.get("Tool", {})
                states.append({
                    "index": i,
                    "ToolEntry": ref(tool.get("ToolEntry")),
                    "EntryQuantity": tool.get("EntryQuantity"),
                    "ToolEntry2": ref(tool.get("ToolEntry2")),
                    "EntryQuantity2": tool.get("EntryQuantity2"),
                    "EntryTime": tool.get("EntryTime"),
                    "ToolExit": ref(tool.get("ToolExit")),
                    "ExitTime": tool.get("ExitTime"),
                    "BlockAir": bs.get("BlockAir"),
                    "BlockGravity": bs.get("BlockGravity"),
                })
            result.append({"prefab": go.m_Name, "states": states})
    return result


def main():
    if len(sys.argv) < 2:
        print(__doc__, file=sys.stderr)
        return 2
    game_dir = sys.argv[1]
    prefix = sys.argv[2] if len(sys.argv) > 2 and not sys.argv[2].startswith("--") else "StructureFrame"
    data = dump(load_env(game_dir), prefix)
    if "--finishing" in sys.argv:
        print(f"{len(data)} prefab(s) with BuildStates scanned", file=sys.stderr)
        for p in data:
            for s in p["states"]:
                if s["ToolEntry"] == "ItemScrewdriver" and s["ToolEntry2"] == "ItemPlasticSheets":
                    print(f"{p['prefab']} state {s['index']}/{len(p['states']) - 1}: EntryTime={s['EntryTime']}")
    else:
        json.dump(data, sys.stdout, indent=1)
    return 0


if __name__ == "__main__":
    sys.exit(main())
