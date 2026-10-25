# 开发验证

## 怪物模型可见性

Unity Editor 可直接运行 `-batchmode -projectPath <工程路径> -executeMethod OpenMonsterVisibilityVerification.Diagnose -logFile <日志路径>`，输出七种源 FBX 与 Resources 怪物 prefab 在 Idle 起始姿态下的真实几何尺寸、renderer bounds、逐层缩放及两种 BakeMesh 结果，保存到 `QA/Reports/MonsterVisibility/diagnostics.txt`。该入口只诊断，便于重现模型缩小或偏移；可使用 `-nographics`。

修复后运行 `-batchmode -projectPath <工程路径> -executeMethod OpenMonsterVisibilityVerification.Run -logFile <日志路径>`；需要图形设备，不添加 `-nographics`。覆盖七种 Resources 模型的 Idle、Move、Attack 各四个时刻，共 84 个骨骼姿态；独立烘焙几何检查地龙 3.05 米参考高度、飞龙最高 1.7 米/最宽 6.4 米的 Idle 适配、动画尺寸及原点偏移、2D 图集和着色器有效性，并用固定 9 米视野检查实际渲染像素。动画姿态允许自然伸展，参考适配尺寸在 Idle 起始帧严格检查。Unity 6000.3 的 `BakeMesh(false)` 已包含 renderer 缩放，量测使用无缩放世界矩阵，并与 `BakeMesh(true)` 加完整世界矩阵核对，避免重复缩放。

输出 `QA/Reports/MonsterVisibility/visibility-report.txt` 和七张模型 PNG；日志标记 `STARFALL_MONSTER_VISIBILITY_PASS` 或 `STARFALL_MONSTER_VISIBILITY_FAIL`。两个入口均不加载游戏场景或读写正常存档，成功退出码为 0，检查失败/缺少资源/运行异常为 1；也可在菜单 `Starfall/Monsters` 中运行诊断与验证。失败时每种模型保留首个错误，继续检查其余模型。

构建后运行 `Starfall.exe -monsterVisibilityQA`，在真实盆地场景检查地面与飞行怪的运行时外观、实时 Idle/Move 动画与实际尺寸。使用游戏相机的 RenderTexture 输出 `gameplay.png`、`ground.png`、`flying.png`；比较隐藏模型前后的画面像素，确认模型确实出现在相机画面中，兼容隐藏窗口。报告同目录下的 `gameplay-report.txt`，日志以 `STARFALL_MONSTER_VISIBILITY_QA_PASS` 或 `STARFALL_MONSTER_VISIBILITY_QA_FAIL` 结束，退出码为 0 或 2。此参数不读取或写入正常剧情存档。

2026-10-23：修复前七种模型均因图集缺失而未通过验证；独立几何诊断还复现了重复缩放及错误偏移。修复后七种模型、84 个姿态检查通过；Windows 构建成功，`-monsterVisibilityQA` 实际运行通过。首次旧战斗回归的弹丸命中检查未通过；2026-10-24 排除了自动瞬移残留速度，并补修弹丸碰撞和冲刺复位后，当前 `-creatureQA` 全部 31 项通过。最新报告为 `QA/creatures-v10/creature-report.json`。

## 物品与任务界面

运行 `-interfaceQA -force-d3d11 -force-d3d11-bitblt-model -screen-fullscreen 0 -screen-width 960 -screen-height 720` 使用独立验证存档，检查 Kenney 皮肤、物品筛选、双向及部分转移、整理、拆分、满仓回滚、仓库权限、药品、追踪、存档和暂停恢复。以 1600×900、1280×720、960×720、1920×800 捕获背包、第二页、仓库、药品详情、任务、线索、滚动末尾和委托共 32 张完整界面截图。此套件使用 `ScreenCapture` 包含 OnGUI；普通相机截图不包含这些界面。通过最新帧色相标记、文字/颜色内容和画面指纹拒绝黑帧、空帧及重复旧帧，并在正式截图前预热隐藏窗口的交换链。输出 `QA/Reports/Interface`，日志以 `STARFALL_INTERFACE_QA_PASS` 或 `STARFALL_INTERFACE_QA_FAIL` 结束。不要与其他 QA 参数混用。

界面画风与功能说明见 `Docs/Design/INTERFACE_REFINEMENT.md`。首次接入时的截图验证只检查尺寸，可能把黑帧当作通过；当前版本要求实际界面内容通过检查。

2026-10-24：当前界面 95 项检查通过，四种比例的 32 张截图均包含有效的最新 OnGUI 内容。隐藏窗口首帧可缺少 Repaint；多尺寸预热后再验收。普通模态遮罩会压暗独立的 QA 色标，检测保留最小色度并核对归一化色相，不接受纯黑或低色度假帧。上述 Direct3D 参数仅用于自动截图，不改变正常游戏的图形配置。

## 存档与战斗核查

Editor 使用 `-batchmode -nographics -projectPath <工程路径> -executeMethod CampaignDataIntegrityVerification.Run -logFile <日志路径>`，检查正常/旧版存档、损坏手账/维修/锻造/嵌套记录、备份恢复和损坏原件保留；生成 `QA/Reports/DataIntegrity/data-integrity-report.txt`，日志标记 `STARFALL_DATA_INTEGRITY_PASS` 或 `STARFALL_DATA_INTEGRITY_FAIL`。

`-creatureQA` 增加无来源弹丸、同帧单次结算、初始重叠接触点和冲刺复位检查。`QA/creatures-v10/combat-diagnostics.txt` 记录飞行怪的预测瞄点、玩家速度和真实碰撞对象，便于区分地形遮挡与命中逻辑。

## 余烬重建

运行 `-frontierQA` 使用独立验证存档，输出 `QA/Reports/Frontier/frontier-report.txt` 和 1600×900 相机截图。覆盖四项建设、背包扩容/死亡回收、旧版二号存档兼容与历史补记、真实矿点与锻造委托、五区勘测、松键/移动/暂停中断、三种物理机关、错误灯序列重试、两种变体预警、击杀与图鉴、战术投放/数量/冷却/暂停/墙体遮挡、满仓奖励进入回收箱、重复领取及非法存档。使用人物实际碰撞控制器沿导航路走到调查点、遗迹支路和返回入口。后期材料与定位可由套件预置，不代表人工通关。

新套件与 `-campaignQA`、`-lostRealmQA` 分别运行；日志以 `FRONTIER_QA_PASS` 或 `FRONTIER_QA_FAIL` 结束。正常存档仍在 `CampaignV1`，不由验证套件读写。

## 剧情版本

运行 Windows 程序时添加 `-campaignQA`，使用独立测试存档；不会读写正常故事的 `CampaignV1`。输出为 `QA/Reports/Campaign/campaign-report.txt` 和流程截图。

覆盖容器容量/事务回滚、真实抓钩、分层解锁、真实 QTE 成败、首区两只地龙、满包剧情领取、手枪换弹、手动医疗、致死回收、多死亡箱、存档及备份恢复、制作修船、剧情权限、双结局、机甲节点与实际范围攻击、倒计时恢复及三个生态任务。后期阶段用预置材料和位置缩短测试准备；这不替代完整的人工作战平衡测试。

图形截图需要正常呈现游戏窗口；隐藏窗口会让部分 Windows 驱动返回黑色截图。日志中以 `CAMPAIGN_QA_PASS` 或 `CAMPAIGN_QA_FAIL` 标记结果。

## 失落星界扩展

运行参数 `-lostRealmQA` 使用同一独立测试存档，进入五个专属场景，检查入口到主路与支线调查点的导航、旧存档备份迁移、无法到达的回收箱物资保全、伙伴和任务、重复物资箱、两种武器的弹药消耗与换弹、区域事件、模块制作与槽位、读档、精英和场景返回后的对象数量。测试从游戏相机直接渲染 1600×900 截图；输出到 `QA/Reports/LostRealms`。日志以 `LOST_REALM_QA_PASS` 或 `LOST_REALM_QA_FAIL` 结尾。

帧时间记录采用同一机器和分辨率下的裁剪均值，用于发现明显退化。隐藏窗口下可能受到 60 FPS 上限影响，精确 GPU 性能分析及战斗手感仍需前台人工游玩。

旧版套件保留：带其他 `*QA` 参数时不加载新剧情运行器，以验证旧系统接口兼容。

## 输出管理

扩展套件还检查中继的超时重试与一次性奖励、暂停冻结、震荡脉冲的体力/冷却/墙体遮挡、精英招式顺序及反击窗口、人物碰撞控制器实际沿坡道走上船坞甲板、上层遗物与掉落交互、观测台、伙伴三秒治疗和模块非法存档。主线套件继续单独运行。

版本库只跟踪本说明。`Reports/Latest`、`Reports/History` 和 `Logs` 是本地报告与日志目录，克隆后的工程中可能尚未生成。

验证入口仍保留在 `Assets/Scripts/Verification`，正常试玩不自动执行。运行 `-immersionQA`、`-hardQA`、`-forgeQA` 等开发参数时，工具会在 QA 中重新生成对应套件目录及截图/逐帧录制。这些是可再生文件，完成检查后可删除，保留需要的报告即可。

源码交付包仅包含本说明，不包含验证产物。
