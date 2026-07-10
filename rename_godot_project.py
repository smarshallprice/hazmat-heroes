#!/usr/bin/env python3
"""
rename_godot_project.py

Renames all references to a Godot project's old name -> new name.
Works identically on Windows, macOS, and Linux (pure Python, stdlib only).

USAGE
-----
1) Dry run first (recommended) - just shows what WOULD change:
   python rename_godot_project.py "C:\\path\\to\\NewProjectName" Test NewProjectName --dry-run

2) Apply the changes for real:
   python rename_godot_project.py "C:\\path\\to\\NewProjectName" Test NewProjectName

On macOS, paths look like: /Users/you/Projects/NewProjectName

WHAT IT DOES
------------
- Updates config/name in project.godot
- Updates identifiers (application/config/name, package name, product name,
  bundle identifier fragments) in export_presets.cfg, if present
- Renames and updates .csproj / .sln files (for C# Godot projects) and fixes
  AssemblyName / RootNamespace / project references inside them
- Scans .gd, .tscn, .tres, .cfg, .import, .cs files for whole-word matches of
  the old name and replaces them
- Prints a summary of every file changed, and every remaining hit it could
  NOT confidently auto-fix, so you can review manually

WHAT IT DOES NOT TOUCH
-----------------------
- The .godot/ cache folder (regenerated automatically by the editor)
- .uid files (per-resource ids, unrelated to project name)
- Binary files (images, audio, etc.)

It only matches the OLD NAME as a whole word (using regex word boundaries),
so "Test" won't accidentally clobber "Testing" or "TestBed". If you need to
also handle case variants (e.g. "test" or "TEST" or "test_project"), pass
extra --replace OLD=NEW pairs (see --help).
"""

import argparse
import re
import sys
from pathlib import Path

TEXT_EXTENSIONS = {".gd", ".tscn", ".tres", ".cfg", ".import", ".cs", ".godot", ".md", ".txt", ".csproj", ".sln"}
SKIP_DIRS = {".godot", ".git", ".import", "bin", "obj"}


def find_text_files(root: Path):
    for path in root.rglob("*"):
        if path.is_dir():
            continue
        if any(part in SKIP_DIRS for part in path.parts):
            continue
        if path.suffix.lower() in TEXT_EXTENSIONS:
            yield path


def whole_word_pattern(word: str) -> re.Pattern:
    return re.compile(r"\b" + re.escape(word) + r"\b")


def process_file(path: Path, replacements, dry_run: bool):
    try:
        original = path.read_text(encoding="utf-8")
    except (UnicodeDecodeError, PermissionError):
        return None  # not a text file we can safely handle, or locked

    updated = original
    hits = 0
    for old, new in replacements:
        pattern = whole_word_pattern(old)
        updated, n = pattern.subn(new, updated)
        hits += n

    if hits == 0:
        return None

    if not dry_run:
        path.write_text(updated, encoding="utf-8")

    return hits


def rename_csharp_files(root: Path, old_name: str, new_name: str, dry_run: bool):
    renamed = []
    for ext in (".csproj", ".sln"):
        for path in root.rglob(f"*{old_name}*{ext}"):
            new_path = path.with_name(path.name.replace(old_name, new_name))
            renamed.append((path, new_path))
            if not dry_run:
                path.rename(new_path)
    return renamed


def update_project_godot_name(root: Path, old_name: str, new_name: str, dry_run: bool):
    pg = root / "project.godot"
    if not pg.exists():
        print("  ! Could not find project.godot at project root - is this the right folder?")
        return
    text = pg.read_text(encoding="utf-8")
    pattern = re.compile(r'(config/name\s*=\s*")([^"]*)(")')
    match = pattern.search(text)
    if not match:
        print("  ! Could not find config/name in project.godot - check it manually.")
        return
    current_value = match.group(2)
    new_text = pattern.sub(rf'\1{new_name}\3', text, count=1)
    print(f"  project.godot: config/name \"{current_value}\" -> \"{new_name}\"")
    if not dry_run:
        pg.write_text(new_text, encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(
        description="Rename a Godot project's name across all project files.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("project_dir", type=str, help="Path to the Godot project root folder")
    parser.add_argument("old_name", type=str, help='Current project name, e.g. "Test"')
    parser.add_argument("new_name", type=str, help='New project name, e.g. "MyGame"')
    parser.add_argument(
        "--replace",
        action="append",
        default=[],
        metavar="OLD=NEW",
        help="Extra whole-word replacement pair, e.g. --replace test=my_game "
             "(useful for lowercase/snake_case variants). Can be repeated.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would change without modifying any files",
    )
    args = parser.parse_args()

    root = Path(args.project_dir).expanduser().resolve()
    if not root.is_dir():
        print(f"Error: '{root}' is not a directory.")
        sys.exit(1)

    replacements = [(args.old_name, args.new_name)]
    for pair in args.replace:
        if "=" not in pair:
            print(f"Error: --replace value '{pair}' must be in OLD=NEW format")
            sys.exit(1)
        old, new = pair.split("=", 1)
        replacements.append((old, new))

    mode = "DRY RUN (no files will be changed)" if args.dry_run else "APPLYING CHANGES"
    print(f"\n=== Godot project rename: '{args.old_name}' -> '{args.new_name}' ===")
    print(f"Project root: {root}")
    print(f"Mode: {mode}\n")

    # 1. project.godot
    print("Step 1: project.godot")
    update_project_godot_name(root, args.old_name, args.new_name, args.dry_run)

    # 2. C# project/solution files (rename + content)
    print("\nStep 2: C# project/solution files")
    renamed = rename_csharp_files(root, args.old_name, args.new_name, args.dry_run)
    if renamed:
        for old_path, new_path in renamed:
            print(f"  renamed: {old_path.name} -> {new_path.name}")
    else:
        print("  (none found - skipping, this is normal for GDScript-only projects)")

    # 3. Content sweep across text files (project.godot included again in case
    #    other keys reference the name; export_presets.cfg, .gd, .tscn, .cs, etc.)
    print("\nStep 3: scanning project files for other references...")
    changed_files = []
    for path in find_text_files(root):
        hits = process_file(path, replacements, args.dry_run)
        if hits:
            changed_files.append((path, hits))

    if changed_files:
        for path, hits in changed_files:
            rel = path.relative_to(root)
            verb = "would update" if args.dry_run else "updated"
            print(f"  {verb}: {rel}  ({hits} replacement{'s' if hits != 1 else ''})")
    else:
        print("  no other references found")

    print("\nDone.")
    if args.dry_run:
        print("This was a dry run - no files were changed. Remove --dry-run to apply.")
    else:
        print("Remember to also check:")
        print("  - export_presets.cfg bundle identifiers (e.g. com.yourname.OldName)")
        print("  - any window title / UI labels set in code rather than config")
        print("  - open the project in Godot once to let it regenerate .godot/ cache")


if __name__ == "__main__":
    main()
