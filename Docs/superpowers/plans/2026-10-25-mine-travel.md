# Mine Travel Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan in this session. Steps use checkbox syntax for tracking.

**Goal:** 将中央矿井改为 0.5 秒可打断过渡，并立即处理相关操作。

**Architecture:** SceneDirector 统一完成/取消镜头过渡，旧协程通过版本号失效。CampaignRuntime 在面板和手动保存前完成过渡，恢复游戏时取消旧过渡。

**Tech Stack:** C#、Unity；源码保留 Unity 6000.3.24f1，使用已有 2022.3.35f1 构建副本验证并注明版本差异。

**Spec:** ../specs/2026-10-25-mine-travel-design.md

## Global Constraints

- 不进行 Git 写操作；在用户指定工作区修改，不创建分支或 worktree。
- 不重建生成场景，仅更新现有场景时长；不安装 Unity。
- 保留抓钩、库存、矿层、遭遇和钻机规则；不改变存档结构。

## Review Focus

- 同一帧重复 E 或空格不得重复切换、放钩。
- 暂停时不得跳过并执行游戏操作。
- 面板暂停时间后不得遗留半途镜头。
- 读档/新故事后旧协程不得覆盖镜头。
- 抓钩回收时所有进出井入口都应受同一限制。

## Task 1: Short, interruptible mine travel

**Files:** Modify SceneDirector.cs, CampaignRuntime.cs, CampaignView.cs, Scene01_Starfall.unity, CampaignVerification.cs; create MineTravelVerification.cs and meta; update gameplay/QA docs.

**Interfaces:** SceneDirector.CompleteTravel(): bool finishes an active transition exactly once; SceneDirector.CancelTravel(): void invalidates old camera work and restores the committed endpoint. Existing Interact/Cast/Move consume the appropriate travel input. MineTravelVerification.Run(CampaignRuntime, SceneDirector, Action<string,bool>): IEnumerator runs real runtime checks in the isolated campaign verification save directory.

- [x] Add runtime regression checks for timed travel, E skip, cast once, return movement, modal completion, pause, restore/new-game cancellation and zero-duration travel; run focused suite against old behavior and retain the expected failures.
- [x] Implement 0.5-second defaults, idempotent completion/cancellation, input response, clickable skip prompt and campaign integration; sync serialized scene duration.
- [x] Add actual hook-return speed checks, then apply a 2x empty-return multiplier while preserving loaded return and cast speeds.
- [x] Rebuild in existing build workspace and run focused plus full campaign suites. Expected: no FAIL and process exit 0. Inspect compiler/runtime logs and git diff --check.
- [x] Review final diff, update player instructions and record evidence plus pending manual acceptance. Refresh local Windows build/package if verification succeeds.

## Execution record

Ruling: 用户已选定上一轮完整短过渡提案，直接执行，避免重复确认。现有 main 工作区由用户指定；不做 Git 写操作。

Ruling: 用户追加空钩回收提速，采用可配置 2 倍系数；带货和放钩参数保持原值。

RED evidence: 2022.3.35f1 构建成功，旧实现 focused suite 16 FAIL、6 PASS，退出码 2；失败包含过渡时长、E/空格/面板响应、暂停输入、返程移动、取消旧协程和空钩速度。日志 QA/Logs/mine-travel-red-runtime.log。

Review: 初版专项 22/22、完整主线 170/170 通过。独立复核发现 Esc 暂停后 B/J/F5 仍会完成镜头，补充暂停面板、直接完成、保存稳定状态以及最后一帧暂停的回归检查，再执行修复与完整验证。

Pause RED: 暂停补充检查 4 FAIL，日志 QA/Logs/mine-travel-pause-red-runtime.log。修复统一完成入口的暂停限制，并让已到结束帧的协程等到恢复再完成；保留暂停下打开菜单的现有行为。

Workspace note: Docs/Design/QUEST_GUIDANCE_PLAN.md 在执行期间由其他工作新增，不属于本次改动，保持原样。

Final evidence: 独立复核确认暂停修复无进一步实质问题。最终构建退出码 0，完整主线 176 PASS / 0 FAIL，运行退出码 0；日志 QA/Logs/mine-travel-final-build.log 和 QA/Logs/mine-travel-final-campaign.log。源工程与构建副本的六个变更源码/场景文件哈希一致，git diff --check 通过。

Delivery: Builds/Windows/Starfall.exe 已更新，139 个构建文件与测试副本逐文件 SHA256 一致。新压缩包 Builds/Starfall-Windows-x64-MineTravel.zip 共 140 个文件，全部与试玩目录校验一致；保留原压缩包。验证元数据 QA/mine-travel-verification-20261025.json，文本结果 QA/Reports/mine-travel-final-report.txt。

Limits: 使用现有 Unity 2022.3.35f1 构建，源工程版本仍为 6000.3.24f1。测试退出时仍有既有的 RenderTexture.active 释放提示，无失败断言或异常。真实键鼠输入、跳过提示点击、镜头舒适度和完整人工通关尚未验收。不进行 Git 写操作。
