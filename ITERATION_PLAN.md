# CAudio 迭代计划

## 当前状态

项目已经完成两轮基础建设：

- 第一轮：完成轻量音频系统的基础设计与运行时代码落地，包括 `AudioManager`、`AudioService`、播放句柄、音源池、音频数据库、Cue 数据模型和基础播放 API。
- 第二轮：补充独立 `AudioCueAsset`、数据库校验、编辑器管理窗口、调试日志设置、Mixer 音量参数、Voice Ducking、更多播放入口与运行时配置能力。

当前系统已经具备可用的基础播放能力，但还需要围绕可维护性、编辑器体验、测试验证、资源加载和示例文档继续打磨。

## 第三轮：稳定性与可验证性

目标：把现有运行时代码从“可用”推进到“可放心接入项目”。

交付物：

- 修正所有中文注释与字符串的编码显示问题，确保源码、README、编辑器文本在 Unity 与 Git 中一致使用 UTF-8。
- 为核心运行时行为补充 EditMode 测试，覆盖数据库查找、重复 Key 校验、随机/权重选择、播放失败结果、音量换算等纯逻辑。
- 梳理 `AudioPlayOptions` 的可变对象使用方式，避免 `PlayAt`、`PlayMusic` 等入口修改调用方传入的 options 后产生副作用。
- 检查延迟播放、淡入淡出、Stop/Release、FollowTarget、ReplaceSameChannel 等状态流，修复容易出现空句柄或残留音源的边界情况。
- 增加一份最小接入示例文档，说明如何创建数据库、配置 Cue、初始化系统、播放/停止音频。

验收标准：

- Unity Console 无编译错误。
- 新增测试通过。
- 同一份 `AudioPlayOptions` 重复传入不同播放入口不会被意外污染。
- 数据库缺失、Key 缺失、Clip 缺失等失败路径都能返回明确 `AudioPlayResult`。
- 开发者能按文档在 5 分钟内完成一次基础音效播放。

## 第四轮：编辑器工作流

目标：让音频配置人员能高效管理 Cue，而不是直接在 Inspector 里翻嵌套字段。

交付物：

- 优化 `AudioDatabaseWindow` 的中文 UI 文案、布局和筛选体验。
- 增加 Cue 的复制、删除确认、快速定位、按通道筛选、按问题筛选。
- 增加一键校验后的可操作反馈，例如选中问题条目、Ping 相关资产、提示重复 Key 来源。
- 为 `AudioCueData` 增加自定义 PropertyDrawer，让 Clip、权重、音量/音调范围、空间化参数更易编辑。
- 增加批量导入入口：从选中的 `AudioClip` 自动创建 Cue 或追加 Clip 选项。

验收标准：

- 常见配置流程不需要手动展开多层序列化字段。
- 重复 Key、空 Clip、空资产引用等问题能在编辑器窗口中快速定位。
- 多个音频文件能批量生成可播放 Cue。

## 第五轮：资源加载与项目集成

目标：让系统可以从直连 Clip 平滑扩展到 Addressables 或项目自定义资源系统。

交付物：

- 完成异步播放 API 设计，例如 `PlayAsync` 或 `PrepareClipAsync`。
- 提供 Addressables 版 `IAudioClipProvider` 示例实现。
- 明确 Clip 生命周期策略：缓存、释放、引用计数或交给外部资源系统管理。
- 增加加载失败、加载取消、播放请求过期等状态结果。
- 支持按数据库默认 Provider 或运行时注入 Provider 切换资源来源。

验收标准：

- 直连 Clip 路径保持兼容。
- Addressables 示例可独立启用，不强迫核心包依赖 Addressables。
- 异步加载失败不会占用 AudioSource 或留下不可停止的句柄。

## 第六轮：音频表现能力

目标：覆盖中小型 Unity 项目常见音频需求。

交付物：

- 音乐 Crossfade 与播放队列。
- Snapshot 或 Mixer 参数过渡接口。
- 通道级暂停/恢复、Mute、Solo。
- Cue Cooldown、最大同时播放数、同 Key 限流策略。
- 随机播放模式扩展：顺序、洗牌、不立即重复。
- 分组 Stop，例如按 Key 前缀、Tag、Channel 或自定义 Group。

验收标准：

- 背景音乐切换无明显爆音或突兀中断。
- 高频音效可通过限流避免瞬间堆叠。
- 音频状态可通过句柄和通道 API 明确控制。

## 第七轮：发布准备

目标：把 CAudio 整理成可以复用、演示和交付的轻量 Unity 音频方案。

交付物：

- 完整 README：功能介绍、安装方式、快速开始、API 示例、常见问题。
- 示例场景：SFX、UI、Music、3D/Follow、Mixer Ducking 各一组演示。
- 包结构整理：运行时、编辑器、示例、测试分层清晰。
- 增加 Assembly Definition，降低编译范围并支持测试程序集。
- 版本号、变更日志、License 和发布说明。

验收标准：

- 新项目导入后能直接打开示例场景体验核心功能。
- 文档覆盖从安装到常见扩展的完整路径。
- 运行时脚本和编辑器脚本通过 asmdef 分离。

## 优先级建议

近期建议优先执行第三轮。原因是当前系统的能力轮廓已经成型，下一步最值得先处理的是稳定性、测试和编码问题。等运行时行为足够稳，再投入编辑器体验和 Addressables 集成，返工成本会低很多。

第三轮可以拆成 5 个任务包：

1. 编码与文案修复。已启动：当前核心脚本、README、快速开始文档和编辑器窗口均按 UTF-8 中文读取正常。
2. 播放入口副作用修复。已完成初版：新增 `AudioPlayOptions.Clone()` / `CopyOrDefault()`，播放入口改为修改副本。
3. 播放生命周期边界检查。已完成初版：音源池复用时重置状态，停止淡出避免重复重入。
4. 核心逻辑测试。已启动：新增 EditMode 测试程序集，覆盖 options、Cue、Database、AudioService 失败路径、冷却、并发限制和异步请求。
5. 快速开始文档。已完成初版：见 `QUICK_START.md`。

当前验证状态：

- `git diff --check` 通过。
- 原项目仍有 Unity 实例打开，批处理测试会被 Unity 拒绝，无法在原路径生成 `TestResults.xml`。
- 已复制 `Assets`、`Packages`、`ProjectSettings` 到临时项目 `C:\Users\Nagisa\AppData\Local\Temp\CAudio_TestRun_20260619_022947` 验证。
- 临时项目 EditMode 测试通过：`TestResults_NoQuit_EditMode.xml` 显示 23 total / 23 passed / 0 failed / 0 skipped。
- Unity Test Framework 1.1.33 下命令行测试不要附加 `-quit`；由 Test Runner 在测试结束后退出 Unity。

已提前推进的后续迭代项：

- 第四轮：新增 Editor asmdef；`AudioDatabaseWindow` 支持通道筛选、复制 Cue、删除确认、从选中 `AudioClip` 批量创建 Cue；新增 `AudioCueData` PropertyDrawer。
- 第五轮：新增 `AudioAsyncPlayRequest`、`AudioService.PlayAsync` / `AudioManager.PlayAsync`；新增可选 Addressables Provider 示例，并通过 asmdef `versionDefines` 避免核心包强依赖 Addressables。
- 第六轮：新增 `CrossfadeMusic`、音乐队列、Mixer 参数过渡、Snapshot 过渡、通道暂停/恢复、Mute、Solo、按 Key 前缀/Group 停止、Cue 冷却、最大同时播放数限制，以及顺序/洗牌/不立即重复选择模式。
- 第七轮：新增 Runtime / Editor / EditMode Tests asmdef，扩展 README 与快速开始文档；新增 `VERSION`、`CHANGELOG.md` 和 `Assets/CAudio/Samples` 示例脚本说明。
