"""Package source assets, settings, documentation and tools; exclude local caches and QA captures."""
from pathlib import Path
import argparse
import zipfile

project = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('destination', type=Path, help='Output .zip path')
args = parser.parse_args()
destination = args.destination.resolve()
if destination.suffix.lower() != '.zip':
    raise SystemExit('Destination must be a .zip file')
destination.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for name in ('Assets', 'Packages', 'ProjectSettings', 'Docs', 'Tools'):
        for file in sorted((project/name).rglob('*')):
            if file.is_file() and '__pycache__' not in file.parts and file != destination:
                archive.write(file, file.relative_to(project))
    for name in ('README.md', '.gitignore', '.gitattributes'):
        archive.write(project/name, name)
    archive.write(project/'QA/README.md', 'QA/README.md')
with zipfile.ZipFile(destination) as archive:
    assert archive.testzip() is None
    for required in ('Assets/Scenes/Scene01_Starfall.unity', 'Assets/Scenes/Scene02_Encounter.unity', 'Docs/README.md'):
        assert required in archive.namelist(), required
print(f'Verified source package: {destination} ({destination.stat().st_size / 1024**2:.2f} MiB)')
