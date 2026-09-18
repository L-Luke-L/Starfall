# 场景结构与可编辑元素

## 已搭建内容

地表的实际画幅为 32 × 18 世界单位，地面位于 y=-3.8，星空约占 71%。星空包含流动银河、610 颗独立闪烁星光、周期流星和远景卫星。地表有三层山脊、岩石、营地、独立补给箱和灯、矿井、绞盘、人物及右侧燃烧飞船；火焰、烟、余烬、船体热反射、焦土接触层均为独立对象，随残骸父对象移动。银河有三层独立的流动尘埃，流星的头部、渐隐尾迹与光晕分别渲染。

地下视角与地表处于同一连续世界：只移动相机，保留矿井、矿石与交互状态。最终相机 y=-8，矿口完整位于界面上方以内。地下已重新搭建连续地层、分层玄武岩壁、浅井衬壁、独立矿脉凹槽与 12 个矿物样本。样本外观与隐藏的修复材料、怪物遭遇、剧情事件配置分离；隐藏怪物已接入断崖盆地战斗；剧情事件保留接口。详见 `UNDERGROUND_DESIGN.md`。当前地下玩法是抓钩勘探，不含另一个可行走地下关卡。

16:9 固定电影构图；宽屏、窄屏使用黑边，保证矿口与操作界面保持完整。

## 独立元素与后续交互

- `Assets/Prefabs/Camp.prefab`：营地。
- `Assets/Prefabs/Mine.prefab`：矿井、绞盘锚点、独立抓钩与缆绳。
- `Assets/Prefabs/Wreck.prefab`：新版低矮残骸、嵌入式火源、遮挡船壳、烟雾、余烬、接触阴影、焦土与散落碎片。
- `Assets/Prefabs/Astronaut.prefab`：人物与脚下阴影。
- `Assets/Prefabs/Buried sample *.prefab`：独立矿物样本和未绑定的内容接口。
- 场景 Hierarchy 按 Cosmos / Distant landscape / Surface / Interactive sites / Underground / Atmosphere 分组；没有整张合并的场景贴图。
- `Interactable` 提供种类、交互半径、描述和 `onInteract`（Inspector 中的 UnityEvent）接口，可挂剧情、背包、任务或新关卡逻辑。
- `SceneDirector` Inspector 可调行走速度、边界、镜头终点、切换时长、摆角、绳长和收放速度。
- 动态光影是 2D 精灵效果，未依赖 URP 或外部美术插件。

## 素材

`Assets/Art` 中的 Camp / MineRig / WreckGrounded-v2 / Astronaut PNG 使用内置 image_gen 分别生成，保留透明通道，互为独立文件；提示词见 `Assets/Art/GENERATION_PROMPTS.md` 和 `Assets/Art/WRECK_V2_PROMPT.md`。各道具内部的装甲、帆布等细节属于其美术贴图；需要单独动画的火焰、缆绳和抓钩已拆为独立对象。

地表岩石、星球和发光贴图由 `WorldArt` 生成；新版地下岩壁和矿石透明素材在 `Assets/Art/Underground`，地层底色使用 `StratifiedRock` 材质；银河使用三层 `NebulaStrand` 动态材质；`LivingNebula` 仅提供底色。独立精灵可直接替换为后续正式美术资源。人物目前采用单帧角色加位移步态，尚未制作逐帧跑步动画。

