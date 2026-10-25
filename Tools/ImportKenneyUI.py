"""Copy the small, licensed UI subset used by CampaignGameUI, with stable metas."""
from pathlib import Path
import shutil
import uuid
import re

root=Path(__file__).resolve().parents[1]
destination=root/'Assets/Resources/UI/KenneySciFi'
destination.mkdir(parents=True,exist_ok=True)
files={
 'Panel':('KenneySciFi','PNG/Extra/Double/panel_rectangle_screws.png'),
 'Inset':('KenneySciFi','PNG/Extra/Double/panel_rectangle.png'),
 'Slot':('KenneySciFi','PNG/Extra/Double/button_square.png'),
 'Button':('KenneySciFi','PNG/Extra/Double/button_rectangle.png'),
 'ProgressBack':('KenneySciFi','PNG/Extra/Double/bar_shadow_square_small.png'),
 'ProgressFill':('KenneySciFi','PNG/Blue/Double/bar_square_small.png'),
 'IconBag':('KenneyGameIcons','PNG/White/2x/shoppingBasket.png'),
 'IconGear':('KenneyGameIcons','PNG/White/2x/gear.png'),
 'IconAmmo':('KenneyGameIcons','PNG/White/2x/barsVertical.png'),
 'IconMedkit':('KenneyGameIcons','PNG/White/2x/plus.png'),
 'IconCore':('KenneyGameIcons','PNG/White/2x/target.png'),
 'IconDecoy':('KenneyGameIcons','PNG/White/2x/signal3.png'),
 'IconCheck':('KenneyGameIcons','PNG/White/2x/checkmark.png'),
 'IconLock':('KenneyGameIcons','PNG/White/2x/locked.png'),
 'IconWrench':('KenneyGameIcons','PNG/White/2x/wrench.png'),
 'IconStar':('KenneyGameIcons','PNG/White/2x/star.png'),
 'IconBook':('KenneyBoardIcons','PNG/Double (128px)/book_open.png'),
 'IconFuel':('KenneyBoardIcons','PNG/Double (128px)/fire.png'),
 'IconIngots':('KenneyBoardIcons','PNG/Double (128px)/cards_stack.png'),
 'IconFlag':('KenneyBoardIcons','PNG/Double (128px)/flag_square.png'),
 'IconShield':('KenneyBoardIcons','PNG/Double (128px)/shield.png'),
 'IconFlask':('KenneyBoardIcons','PNG/Double (128px)/flask_full.png'),
}
for name,(pack,relative) in files.items():
 target=destination/(name+'.png')
 shutil.copy2(root/'QA/Downloads'/pack/relative,target)
 meta=target.with_suffix('.png.meta')
 if not meta.exists():
  guid=uuid.uuid5(uuid.NAMESPACE_URL,'https://kenney.nl/starfall-ui/'+name).hex
  meta.write_text(f'''fileFormatVersion: 2
guid: {guid}
TextureImporter:
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureType: 0
  textureShape: 1
  maxTextureSize: 2048
  alphaUsage: 1
  alphaIsTransparency: 1
  textureCompression: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    overridden: 0
''',encoding='utf-8')
 # Unity serializes TextureImporterShape.Texture2D as 1. Preserve existing GUIDs
 # while correcting any old cube/array import; the file's alpha is not its shape.
 text=meta.read_text(encoding='utf-8')
 normalized=re.sub(r'(?m)^  textureShape: .*$', '  textureShape: 1', text)
 if normalized==text and '  textureShape:' not in text:
  normalized=text.replace('  textureType: 0\n','  textureType: 0\n  textureShape: 1\n')
 if normalized!=text:meta.write_text(normalized,encoding='utf-8')
for pack,license in [('KenneySciFi','License.txt'),('KenneyGameIcons','license.txt'),('KenneyBoardIcons','License.txt')]:
 shutil.copy2(root/'QA/Downloads'/pack/license,destination/(pack+'-LICENSE.txt'))
print(f'Imported {len(files)} Kenney CC0 UI textures into {destination}')
