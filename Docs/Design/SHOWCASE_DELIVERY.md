# 主线演示交付验证 · 2026-10-29

使用项目声明的 **Unity 6000.3.24f1** 构建 Windows x64，产物为 `Builds/Windows/Starfall.exe`。全部运行时源码编译通过，演示托管规则检查 **7 PASS / 0 FAIL**，既有任务指引纯规则检查 **57 PASS / 0 FAIL**。

## 完整主线

未加速、未跳过 CG 的完整自动演示按约 **480 秒**编排到达结果页。11 章依次展示，五层共 54 个固定矿点全部真实回收，五个坐标与 J01–J08 齐全；四项修船与炸弹制作、三节点与机甲本体、三项隔离检查、原有倒计时撤离、三项主线生态交互和守望者结局全部通过。演示中无死亡回收中断，没有完成支线任务。

锻造进行时暂停检查通过；原故事的 State、Store、存档文件内容、地下状态和镜头、第一人称位置与朝向、俯仰角及撤离计时均能在退出后恢复。运行中未发生未处理异常。

完整运行的原始报告保存在 `QA/Reports/ShowcaseFullFlow-20261029/showcase-report.txt`：**59 PASS / 2 FAIL**。两项失败都是旧截图辅助代码的黑图判定：它只渲染深度最高的武器相机，遗漏主场景，并可能保留临时纹理旧内容。没有将原始报告改写为全通过。

## 截图修复与最终构建

截图辅助代码改为先清黑，再按深度合成全部启用且未绑定其他目标纹理的游戏相机，并恢复原目标。最终重新构建后独立运行生命周期与视觉回归：**30 PASS / 0 FAIL**，进程退出码 0，日志 `SHOWCASE_QA_PASS`。

该回归检查开场 CG 与暂停、地下和第一人称的取消恢复，并拍摄裂隙到达和机甲登场。第一人称画面包含实际三维世界与武器，CG 宽屏区域没有旧地表残留，机甲构图完整；视觉夹具不会击败机甲或代替主线通关验证。新截图位于 `QA/Reports/ShowcaseLifecycle`。

完整主线运行与最终构建之间的 **79 个生产运行时脚本 SHA256 均一致**，变更仅为验证组件。指纹清单保存在完整运行报告目录的 `production-source-hashes.json`。

这些截图不包含 OnGUI 字幕与设置菜单；不能将它们作为全部界面文字、音量或人工战斗手感验收的证明。使用方式见 [主线演示说明](../Gameplay/SHOWCASE.md)。

## 原有主线回归

最终 Windows 构建另行顺序执行 `-campaignQA -guidanceSave`：**176 PASS / 0 FAIL**，进程退出码 0，日志 `CAMPAIGN_QA_PASS`。覆盖原有抓钩、分层解锁、锻造成败、库存与存档、货舱和图纸、修船逃离、区域档案权限、机甲、超时恢复及守望者尾声。使用独立 `QA/GuidanceVerification` 存档；报告位于 `QA/Reports/Campaign/campaign-report.txt`，日志为 `QA/Logs/showcase-campaign-regression.log`。

## 构建修复

移除了原 `Packages/packages-lock.json` 中同值重复的 `com.unity.modules.animation` 键，修复 Unity 包管理器拒绝解析的错误。验证脚本在当前进程缺少标准 Windows `ALLUSERSPROFILE` 环境变量时补充系统公共数据路径，避免 UPM 初始化路径错误；不会修改系统环境变量。
