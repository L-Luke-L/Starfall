# 星陨边境 / Starfall

软件工程课程开发项目，使用 Unity 6.3 LTS（6000.3.24f1）。包含地表与地下抓钩、营地锻造、虫洞，以及第一人称断崖盆地战斗和采矿。

## 打开与试玩

- 克隆仓库，或下载并完整解压源码 ZIP。
- 在 Unity Hub 中使用 **Unity 6000.3.24f1** 打开仓库根目录（包含 `Assets`、`Packages`、`ProjectSettings`）。首次打开需等待资源导入。
- 进入 `Assets/Scenes/Scene01_Starfall.unity` 后点击 Play。
- 第一人称场景为 `Assets/Scenes/Scene02_Encounter.unity`，正常流程通过隐藏遭遇或虫洞进入。
- 构建 Windows 版需要安装对应编辑器的 **Windows Build Support (Mono)** 模块，构建完成后运行 `Builds/Windows/Starfall.exe`。

仓库包含完整运行时与编辑器代码、着色器、场景、素材和 `.meta` 引用文件。Windows 程序、Unity 缓存、录制与历史备份由本地生成，不纳入源码版本控制。

## 工程目录

| 目录 | 内容 |
| --- | --- |
| `Assets/Art` | 按场景与功能分类的美术资源 |
| `Assets/Scenes` | 当前两个正式场景 |
| `Assets/Prefabs`、`Assets/Content` | 独立物体、矿物和配置 |
| `Assets/Scripts/World` | 地表、地下与环境表现 |
| `Assets/Scripts/Gameplay` | 物品、库存与通用交互 |
| `Assets/Scripts/Travel` | 遭遇、虫洞与场景切换 |
| `Assets/Scripts/Forge` | 锻造玩法与表现 |
| `Assets/Scripts/Combat` | 第一人称、怪物、战斗与采矿 |
| `Assets/Scripts/Shared` | 共用交互反馈 |
| `Assets/Scripts/Verification` | 开发验证入口 |
| `Assets/Editor`、`Assets/Shaders` | 编辑器工具与着色器 |
| `Docs` | 玩法、设计与维护记录 |
| `Tools` | 源码打包工具 |
| `QA/README.md` | 开发验证使用说明 |

`Library`、`Logs`、`UserSettings` 由 Unity 管理。`Builds`、QA 输出和回退备份仅保存在本地。

## 操作与规则

地表 A/D 移动、E 交互；地下空格下放抓钩。帐篷内选择锻造，空格开始及落锤，连续三次命中产出金属，失误损失当前矿石。

第一人称 WASD 移动、鼠标观察、Shift 奔跑、空格跳跃、左键射击、右键近战、R 换弹、E 拾取或长按采矿。Esc 暂停，F11 全屏。暂停页可关闭声音或减弱镜头反馈。

高难度规则保留；同一轮游戏的虫洞往返保留怪物、矿脉、护甲和弹药状态。关闭游戏后开启新一轮探索，虫洞永久解锁单独保存在本机。

详细说明见 [文档索引](Docs/README.md)。

## 构建、验证与打包

- Unity 菜单 `Starfall / Build Windows` 构建当前场景。
- `Starfall / Create Scene 01` 会重新生成初始场景，不用于日常打开工程。
- 开发验证入口和输出位置见 [QA 说明](QA/README.md)。
- 源码打包：`python Tools/PackageProject.py <输出.zip>`。包含 Assets、Packages、ProjectSettings、Docs、Tools；排除构建产物、缓存、录制和备份。
- 完整分享包：先构建 Windows 版，再运行 `python Tools/PackageShare.py <新输出.zip>`，包含全部源码和可运行游戏。

请将场景、素材及其 `.meta` 文件一同提交，保持 Unity 资源引用。玩法数值以高难度探索说明为准，界面与反馈以沉浸交互说明为准。
