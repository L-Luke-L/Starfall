"""Create and verify a portable Windows player + complete Unity source bundle."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import zipfile

project = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('destination', type=Path)
args = parser.parse_args()
destination = args.destination.resolve()
if destination.suffix.lower() != '.zip':
    raise SystemExit('Destination must be a .zip file.')
destination.parent.mkdir(parents=True, exist_ok=True)
version = re.search(r'^m_EditorVersion: (.+)$', (project/'ProjectSettings/ProjectVersion.txt').read_text(), re.M).group(1)
player = project/'Builds/Windows'
for required in ('Starfall.exe', 'UnityPlayer.dll', 'Starfall_Data/Managed/Assembly-CSharp.dll'):
    assert (player/required).is_file(), required

files = {}
for folder in ('Assets', 'Packages', 'ProjectSettings', 'Tools'):
    for file in (project/folder).rglob('*'):
        if file.is_file() and '__pycache__' not in file.parts:
            files['Starfall/UnityProject/'+file.relative_to(project).as_posix()] = file
for folder in ('Docs',):
    for file in (project/folder).rglob('*'):
        if file.is_file():files['Starfall/UnityProject/'+file.relative_to(project).as_posix()] = file
files['Starfall/UnityProject/.gitignore'] = project/'.gitignore'
files['Starfall/UnityProject/.gitattributes'] = project/'.gitattributes'
files['Starfall/UnityProject/QA/README.md'] = project/'QA/README.md'
for file in player.rglob('*'):
    if file.is_file():files['Starfall/WindowsGame/'+file.relative_to(player).as_posix()] = file

source_readme = f'''# Starfall / 星陨边境

完整 Unity 工程，编辑器版本 {version}。

## 打开

在 Unity Hub 中选择添加磁盘上的项目，选择本文件所在的 UnityProject 文件夹。安装匹配版本编辑器及 Windows Build Support (Mono)。首次打开需等待 Unity 导入资源。

主场景：Assets/Scenes/Scene01_Starfall.unity。
五个独立星界：Assets/Scenes/Scene03_WreckRift.unity 至 Scene07_Core.unity；Scene02_Encounter 是独立盆地预览。
Unity 菜单 Starfall / Build Windows 可构建七个场景。Create Scene 01 会重新生成初始场景，日常编辑无需点击。

## 目录

- Assets：全部 C# 源码、编辑器工具、着色器、场景、预制体与美术资源，保留全部 .meta 引用。
- Packages、ProjectSettings：依赖与项目设置。
- Docs：玩法与设计说明，见 [文档索引](Docs/README.md)。
- Tools：源码和完整分享包的打包脚本，使用 Python 3 标准库。
- QA：开发验证说明。代码在 Assets/Scripts/Verification，正常试玩不运行验证。

Unity 会重新生成 Library 等本地缓存。打包不依赖原作者电脑的盘符或快捷方式。

直接试玩请使用上一级的 WindowsGame/Starfall.exe 或 StartGame.cmd。
'''
guide = f'''星陨边境 · 完整分享包

一、直接试玩（Windows 64 位）
1. 先把整个压缩包完整解压到一个文件夹。
2. 双击 Starfall 文件夹内的 StartGame.cmd，或打开 WindowsGame/Starfall.exe。
3. 请保留 WindowsGame 内所有文件；不要只发送或单独移动 exe。
直接试玩无需安装 Unity。

二、查看全部代码 / 继续开发
使用 Unity Hub 安装 Unity {version}，需要重新构建 Windows 游戏时同时安装 Windows Build Support (Mono)。
在 Unity Hub 中添加 UnityProject 文件夹，等待第一次资源导入完成。
打开 Assets/Scenes/Scene01_Starfall.unity 后点击 Play。
全部运行时和编辑器 C# 代码在 Assets/Scripts 和 Assets/Editor；着色器在 Assets/Shaders。
工程包含所有素材、场景、预制体、.meta 文件、依赖和项目设置。
玩法文档在 UnityProject/Docs，构建与打包说明见 UnityProject/README.md。

三、操作
地表：A/D 移动，E 交互；地下：空格下放抓钩。
锻造：空格开始/落锤，三次命中产出金属，失误损失当前矿石。
第一人称：WASD 移动，鼠标观察，Shift 奔跑，空格跳跃，左键射击，右键近战，R 换弹，1/2/3 切枪，Q 扫描，左 Ctrl 闪避，解锁后 F 震荡脉冲，E 拾取/长按采矿。
Esc 暂停，F11 全屏。暂停页可查看操作、关闭声音或减弱镜头反馈。

四、当前状态
包含五层矿井、背包仓库、制作锻造、五个独立星界区域、三位营地伙伴、六条支线、三种枪械、四种装备模块、两只精英、死亡回收、剧情手账、修船和双结局。
初次试玩先回收裂纹紫铜，再在矿口按 E 主动进入遗骸。取得货舱装置并在入口校准后可自由使用营地虫洞。
B 背包、J 手账、F5 保存。新剧情跨启动保存全部进度，旧剧情存档自动备份后迁移，旧试玩记录单独保留。
完整规则与原型范围见 UnityProject/Docs/Gameplay/CAMPAIGN.md。本轮中继、伙伴交流与观测台见 VOYAGES.md。

五、内容说明
本包同时包含可运行程序与完整源码。旧演示、历史备份、录制帧、开发日志和 Unity 本地缓存未打包。
SHA256SUMS.json 记录包内文件校验值。分享给他人时发送整个 ZIP 即可。
'''
generated = {
    'Starfall/先看这里.txt': guide.replace('\n', '\r\n').encode('utf-8-sig'),
    'Starfall/StartGame.cmd': b'@echo off\r\ncd /d "%~dp0WindowsGame"\r\nstart "" "Starfall.exe" -force-d3d11\r\n',
    'Starfall/UnityProject/README.md': source_readme.encode('utf-8'),
}
hashes = {}
with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for name, file in sorted(files.items()):
        archive.write(file, name)
        hashes[name] = hashlib.sha256(file.read_bytes()).hexdigest()
    for name, data in generated.items():
        archive.writestr(name, data)
        hashes[name] = hashlib.sha256(data).hexdigest()
    archive.writestr('Starfall/SHA256SUMS.json', json.dumps(hashes, ensure_ascii=False, indent=2).encode('utf-8'))

with zipfile.ZipFile(destination) as archive:
    assert archive.testzip() is None
    for name, expected in hashes.items():
        assert hashlib.sha256(archive.read(name)).hexdigest() == expected, name
    for file in (project/'Assets').rglob('*'):
        if file.is_file():assert 'Starfall/UnityProject/'+file.relative_to(project).as_posix() in hashes

checksum = hashlib.sha256(destination.read_bytes()).hexdigest()
report = {
    'passed': True, 'unityVersion': version, 'filesVerified': len(hashes),
    'csharpSourceFiles': sum(name.endswith('.cs') for name in hashes),
    'shaderSourceFiles': sum(name.endswith(('.shader', '.cginc')) for name in hashes),
    'playerFiles': sum(name.startswith('Starfall/WindowsGame/') for name in hashes),
    'archiveBytes': destination.stat().st_size, 'archiveSha256': checksum,
}
reports = project/'QA/Reports/Latest'
reports.mkdir(parents=True, exist_ok=True)
(reports/'share-package.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
