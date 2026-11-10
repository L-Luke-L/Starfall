"""Check serialized Unity GUID references without changing project assets."""
import argparse
import json
import re
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    report_path = args.report or root / "QA/Reports/ProjectAudit/asset-references.json"
    guid_pattern = re.compile(r"\bguid:\s*([a-fA-F0-9]{32})\b|\bGUID:([a-fA-F0-9]{32})\b")
    meta_pattern = re.compile(r"^guid:\s*([a-fA-F0-9]{32})\s*$", re.MULTILINE)
    owners = defaultdict(list)
    project_guids = defaultdict(list)
    orphan_meta = []
    invalid_meta = []
    for base in (root / "Assets", root / "Packages", root / "Library/PackageCache"):
        if not base.exists():
            continue
        for meta in base.rglob("*.meta"):
            match = meta_pattern.search(meta.read_text(encoding="utf-8-sig", errors="replace"))
            relative = meta.relative_to(root).as_posix()
            if not match:
                invalid_meta.append(relative)
                continue
            guid = match.group(1).lower()
            owners[guid].append(relative)
            if base == root / "Assets":
                project_guids[guid].append(relative)
                if not meta.with_suffix("").exists():
                    orphan_meta.append(relative)
    missing = []
    scanned_files = references = builtins = 0
    # Include all YAML assets as well as assembly definition GUID references.
    extensions = {".unity", ".prefab", ".asset", ".mat", ".controller", ".overridecontroller",
                  ".anim", ".mask", ".playable", ".rendertexture", ".lighting", ".meta",
                  ".asmdef", ".asmref"}
    for base in (root / "Assets", root / "ProjectSettings"):
        for path in base.rglob("*"):
            if not path.is_file() or path.suffix.lower() not in extensions:
                continue
            content = path.read_text(encoding="utf-8-sig", errors="replace")
            scanned_files += 1
            for number, line in enumerate(content.splitlines(), 1):
                # A meta's own identity is not an asset reference.
                if path.suffix == ".meta" and meta_pattern.fullmatch(line):
                    continue
                for match in guid_pattern.finditer(line):
                    guid = (match.group(1) or match.group(2)).lower()
                    references += 1
                    if guid.startswith("0000000000000000"):
                        builtins += 1
                    elif guid not in owners:
                        missing.append({"file": path.relative_to(root).as_posix(), "line": number, "guid": guid})
    duplicates = {guid: paths for guid, paths in project_guids.items() if len(paths) > 1}
    result = {"generatedUTC": datetime.now(timezone.utc).isoformat(), "root": str(root),
              "scannedFiles": scanned_files, "references": references, "builtinReferences": builtins,
              "projectGUIDs": len(project_guids), "missingReferences": missing,
              "duplicateProjectGUIDs": duplicates, "orphanMeta": orphan_meta,
              "invalidMeta": invalid_meta,
              "scope": "Serialized GUID presence; does not validate fileID, animation state, shader output or runtime-generated content."}
    result["passed"] = not (missing or duplicates or orphan_meta or invalid_meta)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"{scanned_files} files, {references} references, {len(missing)} missing, "
          f"{len(duplicates)} duplicate GUIDs, {len(orphan_meta)} orphan metadata, {len(invalid_meta)} invalid metadata")
    print(report_path)
    raise SystemExit(0 if result["passed"] else 2)


if __name__ == "__main__":
    main()
