# 开发验证

版本库只跟踪本说明。`Reports/Latest`、`Reports/History` 和 `Logs` 是本地报告与日志目录，克隆后的工程中可能尚未生成。

验证入口仍保留在 `Assets/Scripts/Verification`，正常试玩不自动执行。运行 `-immersionQA`、`-hardQA`、`-forgeQA` 等开发参数时，工具会在 QA 中重新生成对应套件目录及截图/逐帧录制。这些是可再生文件，完成检查后可删除，保留需要的报告即可。

源码交付包仅包含本说明，不包含验证产物。
