# 开放素材替换与界面更新

2026-10-23：采用现成开放素材替换常规怪物表现，并重做物品和任务界面。

## 怪物

已导入 Quaternius 的 **Ultimate Monsters** 中七个带骨骼动画的 FBX，以及原始调色板贴图。来源、CC0 许可与 SHA-256 记录在 `Assets/ThirdParty/Quaternius/Monsters/SOURCE.json`，原许可文本一并保留。

| 场合 | 使用的外观 |
| --- | --- |
| 初始地区地面怪 / 飞行怪 | Dino / Dragon |
| 后续地区普通怪 | Dino、Alien、Yeti / Dragon、Glub_Evolved、Dragon_Evolved，按地区与持久 ID 固定分配 |
| 地面 / 飞行精英 | BlueDemon / Dragon_Evolved |
| 磁蚀 / 霜环变体 | Alien / Glub_Evolved，保留变体颜色 |

两类原怪物 Prefab 的路径、根组件与攻击锚点继续使用。只隐藏旧视觉网格并添加新的模型层，不重写关卡、导航或碰撞。天空巨兽与守核机甲暂保留原有外观。

动画层根据现有战斗状态播放待机、移动、攻击、受击和死亡；关闭根运动以继续由导航控制位置，并随游戏暂停。材质支持骨骼渲染器的受击闪光与变体染色。

首次正常打开 Unity 时，会自动导入 FBX、配置 Generic 动画、生成七个 Resources 外观 Prefab 和动画控制器，再原位升级怪物 Prefab。缺少任一生成资源会补建。手动入口为 `Starfall / Monsters / Rebuild CC0 Monster Library`；Windows 构建入口也会先准备这些资源。

2026-10-23 可见性修复：Unity 6000.3 的默认 `BakeMesh` 已包含骨骼渲染器缩放，旧尺寸计算又乘了一次世界矩阵，导致地龙仅约 3 厘米高、飞龙中心低于其悬浮锚点约 2.6 米。现在使用补偿缩放的烘焙结果计算边界，在 Animator 上方的独立 `Artwork fit` 容器调整尺寸和原点，并强制将调色板导入为 2D 纹理。资源版本提升为 v2；正常打开工程及构建都会升级旧资源。

## 界面

采用 Kenney **UI Pack - Sci-Fi**、**Game Icons** 和 **Board Game Icons** 的 22 张面板与图标，三份原始 CC0 许可及来源放在 `Assets/Resources/UI/KenneySciFi`。

- 背包和仓库采用物品格、堆叠数量、容量条和物品详情栏；保留全部存取、拆分、整理、医疗和回收操作。
- 手账采用分类栏、任务或线索列表、详情与进度栏；支持追踪主线、伙伴任务、中继和遗物。
- 调查长文可滚动阅读，修船清单显示真实材料数量与安装进度。
- 剧情弹窗与按钮使用统一科幻皮肤。继续使用原中文字体及 1600×900 逻辑坐标的等比适配。

## 检查与限制

源 FBX 已离线解析检查：七个模型均包含网格、骨骼与动画，单模型约 4,452–7,676 个三角面，8–14 段动画。图标、许可与 Unity GUID 另行检查。

已在 Unity 6000.3.24f1 中重建七种怪物，通过待机、移动、攻击共 84 个骨骼姿态的尺寸、原点、图集和渲染可见性检查，输出七张模型截图。Windows 构建与实际游戏中的 `-monsterVisibilityQA` 也已通过，确认两类怪物的运行时动画及游戏相机可见像素，并输出现场截图。验证入口与报告位置见 `QA/README.md`。物品和任务界面的完整截图套件仍需单独验收。

激活并构建后，可运行 `Starfall.exe -interfaceQA`，在独立验证存档下生成 1600×900 与 1280×720 的背包、仓库、药品详情、任务、线索和委托共 12 张完整界面截图，输出至 `QA/Reports/Interface`。怪物战斗回归仍可使用原 `-creatureQA`、`-lostRealmQA`、`-frontierQA`。
