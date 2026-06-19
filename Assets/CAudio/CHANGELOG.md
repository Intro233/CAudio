# 变更日志

## 0.3.0

- 新增 UPM 包元数据，`Assets/CAudio` 可通过 Package Manager 的 Git URL 安装。
- 新增可导入的 `功能展示` 示例，包含 uGUI 控制场景和示例音频。
- 新增 Runtime、Editor、EditMode Tests 程序集定义。
- 新增更安全的 `AudioPlayOptions` 克隆逻辑，避免播放入口修改调用方持有的配置对象。
- 新增 EditMode 测试，覆盖 options、Cue 选择、数据库校验、失败结果、冷却、最大同时播放数、异步请求和分组停止。
- 新增异步播放请求和可选 Addressables Provider 支持。
- 新增音乐交叉淡入淡出、音乐队列、Mixer 参数过渡、Snapshot 过渡、通道暂停/恢复、静音、独奏、按 Key 前缀停止和分组停止。
- 新增 Cue 冷却、最大同时播放数、分组、顺序选择、洗牌选择和避免连续重复选择。
- 优化音频数据库编辑器窗口，支持筛选、问题筛选、复制、删除确认、选中 Clip 导入、拖拽导入、问题定位和自定义 `AudioCueData` 绘制器。
- 修复暂停的非循环音频被误判为播放结束的问题。
- 修复音乐队列中失败条目阻断后续条目的问题。
- 修复淡出时通道/主音量被重复应用的问题。
- 新增音源池上限和 `PoolLimitReached` 播放失败结果。
- 新增可选 Clip 释放生命周期，支持 Addressables 和自定义 Provider。
- 优化校验问题元数据和编辑器侧定位能力。

## 0.2.0

- 新增独立 `AudioCueAsset` 支持。
- 新增数据库校验、调试设置、Mixer 参数映射、Voice Ducking 和更多播放辅助入口。

## 0.1.0

- 新增初版轻量运行时音频系统、数据库、Cue 模型、音源池、播放句柄和基础播放 API。
