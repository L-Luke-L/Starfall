**星界任务指引实施与验证说明**

日期：2026-10-25（Asia/Shanghai）。已按批准方案接入功能并完成自动回归；真人新玩家验收尚未开展。

**交付内容**

- 原启动目录已同步完整任务指引构建，启动 [Starfall.exe](D:/Starfall/Builds/Windows/Starfall.exe)。同步前的旧版本保存在 [备份目录](D:/Starfall/Builds/Windows-before-guidance-20261025-2d808849)，139 个游戏构建文件已逐个校验，操作说明按原启动目录更新；记录见 [standard-player-sync.json](D:/Starfall/QA/Reports/GuidanceDelivery/standard-player-sync.json)。
- 独立交付目录：[GuidanceWindows](D:/Starfall/Builds/GuidanceWindows)，启动 [Starfall.exe](D:/Starfall/Builds/GuidanceWindows/Starfall.exe)。
- 完整便携包：[Starfall-Guidance-Windows-x64.zip](D:/Starfall/Builds/Starfall-Guidance-Windows-x64.zip)。运行与分享需保留整个目录。
- 玩法说明：[任务指引](D:/Starfall/Docs/Gameplay/QUEST_GUIDANCE.md)。设计依据：[批准方案](D:/Starfall/Docs/Design/QUEST_GUIDANCE_PLAN.md)。

从原启动目录 Builds/Windows/Starfall.exe 再次执行指引专项：102 PASS / 0 FAIL，退出 0，成功标记存在。记录见 [standard-player-runtime.json](D:/Starfall/QA/Reports/GuidanceDelivery/standard-player-runtime.json)。

**玩家可见变化**

左上角显示任务、下一步、目的地、距离与操作。屏内标记和屏外箭头接入实际设备位置；跨区先指向信标，再说明进入目标区域的操作。上下层使用现有导航数据与船坞实体坡道；无完整路径时说明当前阻碍。基础指引从开局可用，Q 扫描沿用原解锁条件。

J 手账默认定位当前任务，提供主线与修船、伙伴、探索、建设与制作、委托、物资回收六类任务。任务条目显示当前步骤、前置、操作、完成清单、材料库存与回收缺口；存档入口保留。暂停页及操作说明页可切换详细、关闭和简洁模式，偏好保存在本机。

已接入六条伙伴支线、三处中继、五件遗物及归航星图、五区勘测、三处遗迹、六项委托、四项建设、模块与战术制作、回收箱、修船起飞和核心结局。委托达标继续引导领奖，遗物收齐继续引导复原；前置完成后自动接回原任务。遗迹的详细帮助由玩家主动展开，逐级提供规则、目标和下一次操作。

任务解析复用原库存、知识、计数、区域和结局事实。完成与发奖仍走原业务。没有加入自动行走、自动交互、自动起飞或自动引爆。

**自动验证**

| 检查 | 最终结果 | 证据 |
| --- | --- | --- |
| 纯规则与全部运行时代码编译 | 57 PASS / 0 FAIL | `Tools/TestGuidance.ps1 -CompileAll` |
| 指引专项 | 102 PASS / 0 FAIL，退出 0 | `QA/Logs/guidance-verified-guidanceQA.log` |
| 原主线回归 | 176 PASS / 0 FAIL，退出 0 | `QA/Logs/guidance-verified-campaignQA.log` |
| 原失落星界回归 | 144 PASS / 0 FAIL，退出 0 | `QA/Logs/guidance-verified-lostRealmQA.log` |
| 原余烬重建回归 | 237 PASS / 0 FAIL，退出 0 | `QA/Logs/guidance-verified-frontierQA.log` |

上述运行日志均含各套件的成功标记。机器可读汇总：[guidance-verified-results.json](D:/Starfall/QA/Reports/GuidanceDelivery/guidance-verified-results.json)。原始报告在构建副本的 `QA/Reports` 中。纯规则检查会在专项里再次执行，表中项目存在覆盖重叠。

从最终交付目录再次启动 EXE，指引专项为 102 PASS / 0 FAIL，退出 0。报告：[guidance-report.txt](D:/Starfall/QA/Reports/Guidance/guidance-report.txt)。交付目录的 139 个构建文件与验证构建逐个 SHA256 相同；压缩包的 140 个文件（含操作说明）逐个解压流校验一致。[打包校验](D:/Starfall/QA/Reports/GuidanceDelivery/package-verification.json)记录文件数与散列。

交付脚本程序集 SHA256：`A9968E7BF87FD97E4F9ACB882DB5912F4D7C4676646E5A1F640AB3668094D181`。

专项覆盖实际矿井/货舱/绞盘/动态勘测落点、真实互动推进、跨区信标、暂停、缺省字段恢复、追踪保存、实际船坞坡道入口、上层遗物目的地及上坡后导航点前进。原有套件继续覆盖实际人物碰撞移动、机关、中继暂停、库存、死亡回收、修船和双结局。

界面截图覆盖 1280×720 手账、1600×900 任务区与船坞、1920×1080 任务区。截图来自完整游戏帧，包含 OnGUI；采集时检查黑帧并重试。已检查任务文字、标记与准星/弹药区域的关系。截图与自动移动检查分别提供界面和运行时证据。

截图入口：[900p 任务区](D:/Starfall/QA/Reports/Guidance/battlefield-guidance-900p.png)、[720p 手账](D:/Starfall/QA/Reports/Guidance/journal-guidance-720p.png)、[1080p 任务区](D:/Starfall/QA/Reports/Guidance/battlefield-guidance-1080p.png)、[船坞上层目标](D:/Starfall/QA/Reports/Guidance/shipyard-upper-guidance-900p.png)。四张均已逐张查看。

**独立审阅与修复**

独立只读审阅发现两项重要问题：成品锭不足时遗漏死亡箱原矿、锻造委托错误限定紫铜。已增加复现检查，再修正为“取回原矿 → 锻造”和“优先使用任一种可用原矿”。同时补齐已知组件来源耗尽后的下一地区，以及战斗委托避开当前空区域的步骤。

坡道回退原先持续指向最低边缘；按玩家实际影响将该项从审阅的 Minor 提升为 Important，补充实际场景复现。现在进入坡道后，标记按玩家在实体坡道上的位置向前推进。

修复前的完整专项复现为 96 PASS / 6 FAIL，修复后为 102 PASS / 0 FAIL，并重新运行三套原有回归。红阶段证据：[guidance-review-red-results.json](D:/Starfall/QA/Reports/GuidanceDelivery/guidance-review-red-results.json)。没有遗留已确认的重要审阅问题，也没有延期的 Minor 项。

**执行取舍与范围**

- 用户已批准方案，在当前会话直接执行，没有重复请求设计或执行方式批准。
- 保留用户指定的 D:/Starfall 工作区及现有矿井并行修改；不创建分支、不提交、不进行其他 Git 写操作。通过基线副本和独立构建目录隔离验证；交付包含当前工作区已有的矿井改动。
- 本机已有 Unity 2022.3.35f1，实际可运行包由该版本编译。源工程的 Unity 6000.3.24f1 声明保持不变，没有安装引擎或重建生成场景。6000.3.24f1 的编辑器实测仍未完成。
- 采用实体场景根节点与实际网格定位船坞坡道；在路径不可用时保留方向补充和明确阻碍，不声明某条路线安全。
- 原有资源总量、怪物强度、核心结局后关闭星界的规则沿用原设计；审阅排除的矿井改动由本任务保留，原回归结果覆盖了当前合并工作区。
- M2 与最终验收中的真人新玩家条件尚未验证。代码和自动检查覆盖先完成，不能据此宣称所有里程碑的人测门槛已经通过。

**仍需人工确认**

按批准方案，至少三名未参与开发、未读外部攻略的玩家，需要在无人解释路线与操作的条件下完成指定任务。完整从开局到结局的连续游玩、长期手感和精细 GPU 性能也未由本次自动检查替代。暂停页设置的实际鼠标操作与跨启动体验可一并纳入这轮人测。
