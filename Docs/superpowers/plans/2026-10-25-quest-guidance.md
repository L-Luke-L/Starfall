# 星界任务指引 Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement task by task. 当前批准涵盖设计实施，直接在本会话执行，不再重复请求批准。

**Goal:** 让不了解玩法的玩家通过分步目标、导航与操作提示完成星界任务。

**Architecture:** 纯任务解析器从现有 CampaignState 与目录配置推导下一步；运行时适配场景对象、路径与真实交互；现有手账负责任务选择和详细帮助。任务完成和奖励仍由原业务系统处理。

**Tech Stack:** C#、Unity；源码保留 6000.3.24f1，使用本机 2022.3.35f1 在独立构建副本验证，注明版本差异。

**Spec:** D:/Starfall/Docs/Design/QUEST_GUIDANCE_PLAN.md

## Global Constraints

- 默认详细指引，提供详细/简洁/关闭设置，基础导航无需扫描能力。
- 一次追踪一个任务；撤离和正在进行的中继临时优先，结束后回到原任务。
- 保留旧追踪键和旧存档；不复制完成记录、不更改发奖、不自动交互或确认结局。
- 不安装 Unity、不运行场景重建、不进行 Git 写操作；保留现有矿井切换等并行修改。
- 本次继续在用户指定 D:/Starfall 工作区实现；测试和构建使用本任务独立目录，不覆盖正在运行的构建。

## Review Focus

- 已完成前置、乱序探索、旧任务键和无效键应产生可执行的下一步。
- 核心路线不应依赖飞船修复；材料来源不得把仓库外物资当成可立即使用。
- 任务达标待领取和遗物收齐待复原必须继续引导。
- 跨区域、楼层、已消失对象与无路径应避免错误标记或空坐标。
- 暂停、死亡、读档与计时挑战不得改变原业务规则或写入正常测试存档。

## Task 1: 纯任务规则与自动检查

**Files:** CampaignGuidance.cs、GuidanceRuleChecks.cs、Tools/TestGuidance.ps1、Tools/GuidanceTestMain.cs。

**Interfaces:** `CampaignGuidance.Resolve(CampaignState, GuidanceContext)` 返回当前生效 `GuidanceStep`；`Describe(CampaignState, GuidanceContext, string)` 描述指定任务；`TaskKeys(CampaignState, GuidanceContext, int)` 返回已开放任务键。`GuidanceContext` 提供目录与瞬时挑战状态，不持久化副本。

- [x] 写真实 CampaignState 输入的行为检查，覆盖开局、伙伴前置、主线分支、待领取、遗物复原、旧键、材料缺口、计时挑战。
- [x] 运行本地 .NET 检查，确认空解析器产生预期行为失败。
- [x] 实现纯解析器与任务目录，运行检查至全通过。

## Task 2: 运行时、目标和导航

**Files:** CampaignGuidanceRuntime.cs、CampaignGuidanceTargets.cs、CampaignData.cs、CampaignStore.cs、CampaignRuntime.cs、CampaignRegion.cs。

**Interfaces:** `CampaignRuntime.CurrentGuidance`、`GetGuidanceContext()`、`InvalidateGuidance()`；区域 `TryGuidancePoint(string, out Vector3)`、`PreferGuidanceFocus()`。目标用区域编号与稳定业务键解析，实际 Transform 由当前区域提供。

- [x] 添加独立 `-guidanceQA` 的实际运行时检查，验证基础指引、绞盘切换、勘测实际落点、跨区域信标、暂停与存档。
- [x] 实现缓存、目标注册适配、屏内/屏外标记、路径拐点、跨区与上下层路线、指引偏好与教程/卡住帮助。
- [x] 验证目标实际位置和交互对象一致，不对距离变化保存，不改变 Near 和原交互条件。

## Task 3: HUD、手账与全任务接入

**Files:** CampaignGuidanceView.cs、CampaignView.cs、CampaignVoyages.cs、CampaignFrontierView.cs。

**Interfaces:** `DrawGuidanceHud()`、`DrawGuidanceJournal()`、`DrawGuidanceSettings(Rect)`；旧 `TrackObjective(string)` 统一到新规则，完成后回到保存的主线方向。

- [x] 接入三至四行任务区、任务分类、追踪/取消、规则/详细解谜帮助、真实材料来源和操作复习。
- [x] 覆盖全部伙伴、中继、遗物、勘测、遗迹、委托、建设、模块/制作、死亡回收、修船与核心终局。
- [x] 检查 720p、900p、1080p 的界面布局，保留准星、生命、弹药和倒计时空间。

## Task 4: 构建、回归与交付

**Files:** GuidanceVerification.cs、Tools/BuildGuidance.ps1、玩法/QA 文档与本计划执行记录。

- [x] 在独立 Builds/GuidanceWorkspace-20261025 副本构建，不重写源工程版本或场景。
- [x] 运行指引专项及 campaign/lostRealm/frontier 三套原有回归，记录真实结果。
- [x] 检查实际屏幕截图与日志；独立审阅当前改动，修复重要问题并重跑对应检查。
- [x] 完成可运行包、操作说明和验证报告；明确尚需真人新玩家验收的项目。

## Execution record

Ruling: 用户已批准完整计划，采用本会话直接实施；不重复发起执行方式或设计批准。

Ruling: 工作区已有并行矿井修改，用户历史偏好禁止未授权 Git 写操作；保留原地修改，使用独立构建目录与基线副本隔离验证。

Ruling: 暂无 Unity 6000.3.24f1，沿用本机已有 2022.3.35f1 的构建副本做可执行验证；源工程版本不变，最终报告注明该边界。

Task 1: complete — 纯规则从 36 FAIL / 2 PASS 开始，扩展到 51 PASS / 0 FAIL；检查使用真实 CampaignState、配方、目录类型和解析器，无模拟任务实现。

Task 2: runtime RED — guidance-red2 构建退出 0，专项运行 8 FAIL；缺项包括真实矿井/货舱/绞盘/勘测/跨区目标与旧字段恢复。第一次接入后 guidance-green 专项退出 0。

Task 3: implemented — 分步 HUD、任务分类、当前任务定位、材料与步骤清单、解谜逐级帮助、暂停页偏好和操作复习已接入。720p 手账可读；首次 900p 截图出现驱动黑帧，加入像素有效性检查后如实报 FAIL，继续修正。

Task 4: ongoing — guidance-layout 实际检查发现船坞坡道位于场景独立根节点且网格名含小写 ramp，已按实际场景来源修正。独立审阅首次因模型额度失败，尚无审阅结论；后续使用可用模型重新执行同一只读审阅。

Final review: 独立只读审阅已完成，无 Critical、两项 Important（原矿回收箱、锻造委托），并报告坡道回退 Minor。

Ruling: 坡道回退会在 NavMesh 路径不可用时持续把已上坡的玩家指向身后，按实际影响提升为 Important，在同一修复轮处理；若判断错误，成本为一段局部坡道目标计算，原有移动与交互规则不变。

Final: fixed 原矿死亡箱 → 锻造、任意可用原矿锻造委托 — 新增规则检查 RED→GREEN。补充组件来源耗尽时推进下一地区，以及战斗目标为空时切换区域的可执行链。

Final: fixed 坡道导航点向前推进 — 真实船坞场景检查 RED→GREEN。修复前专项 96 PASS / 6 FAIL，修复后 102 PASS / 0 FAIL；全部运行时编译与纯规则 57 PASS / 0 FAIL。

Task 2: complete — 最终实际专项通过，跨区、动态锚点、旧字段、暂停与坡道均有运行证据。

Task 3: complete — 四张 720p/900p/1080p 全界面截图均拒绝黑帧并逐张查看；新手真人体验独立列为待项。

Task 4: complete — 最终构建退出 0；campaign 176、lostRealm 144、frontier 237 项全部通过、退出 0。交付目录再次运行专项 102 PASS / 0 FAIL，139 个构建文件、140 个压缩包条目逐个校验通过。无 Git 写操作。

Ruling: 人测由真实新玩家完成，当前只完成代码、自动运行与截图检查；M2 和最终人测门槛仍待开展，不以自动通过替代。这一取舍的成本是交付后仍需按原标准完成无口头提示的试玩。

Final deferred minors: none。原有资源平衡、敌人强度和终局关闭星界规则保持原设计；并行矿井工作未重写。
