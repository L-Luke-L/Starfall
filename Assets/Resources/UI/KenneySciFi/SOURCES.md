# Kenney UI artwork

The 22 original PNG files in this folder are a selected subset of three Kenney packs, all licensed under CC0 1.0. The original license text for each pack is included beside the textures.

- [UI Pack: Sci-Fi](https://kenney.nl/assets/ui-pack-sci-fi), version 2.0: panel, inset, button, slot and progress bar textures.
- [Game Icons](https://kenney.nl/assets/game-icons), version 1.0: bag, gear, ammunition bars, medical plus, core target, signal, checkmark, lock, wrench and star icons.
- [Board Game Icons](https://kenney.nl/assets/board-game-icons), version 1.1: book, fuel flame, refined-material stack, mission flag, shield and laboratory flask icons.

`Tools/ImportKenneyUI.py` records the exact upstream paths and creates stable Unity import GUIDs. The textures are tinted and drawn with nine-slice borders at runtime. The Chinese font and existing mineral artwork belong to the original Starfall project and are not taken from these packs.

## Starfall terminal treatment

The runtime skin in `CampaignGameUI` uses coal-blue metal panels, copper-gold selection marks and restrained cyan instrument lights, matching the camp's worn metal, cold equipment lighting and warm sparks. Item rarity colors retain the game's mineral palette. The small panels and buttons use the original CC0 geometry; dividers, keyboard focus outlines, status badges and segmented capacity bars are drawn as UI geometry so text and status colors remain crisp at different resolutions.

All PNGs are imported as ordinary 2D textures (`TextureImporterShape.Texture2D`, serialized value `1`) with alpha transparency, clamp wrapping, bilinear sampling and no mipmaps. They are UI resources rather than sprite-sheet, cube or array textures. No additional asset packs or generated images were added for this treatment.
