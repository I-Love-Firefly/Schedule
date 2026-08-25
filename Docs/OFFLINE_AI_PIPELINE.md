# Android 离线课程表 AI 管线

## 用户流程

1. 首次使用时，在“从截图导入课表”页面选择 `MiniCPM5-1B-Schedule-Q4_K_M.gguf` 安装到应用私有目录。
2. 选择完整课程表截图。ML Kit 中文 OCR 和后续 MiniCPM5 推理均在手机本地完成。
3. 应用展示识别结果。只有 AI 与几何校验同时通过时，“确认并写入”按钮才可用。
4. 模型安装后，识别过程不需要网络；模型不随 APK 打包，避免 APK 增加约 656 MiB。

当前训练模型位于本机（构建产物不提交 Git）：

`artifacts/ai-export/MiniCPM5-1B-Schedule-Q4_K_M.gguf`

- 大小：688,065,920 字节
- SHA-256：`220A3FC977A3230BEA020F961035BB9B1341F455ACB81F577BAE474AE7268136`

## 开发者构建

训练配置与数据工具在 `Tools/Ai/`。Android C ABI 包装器在 `Native/ScheduleAi/`，运行时使用 OpenBMB 官方 `llama.cpp-omni` 源码构建。

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Ai/build_android_runtime.ps1
dotnet test Tests/Schedule2.0.ParserTests.csproj
dotnet build Schedule2.0.csproj -f net10.0-android -c Release
```

构建脚本把 `libschedule_ai.so` 复制到 `Platforms/Android/jniLibs/arm64-v8a/`，MAUI 项目会把它打入 APK。运行时采用贪心解码，最大 2048 个输出 token；即使输出被截断，托管层也只接收其中完整且通过校验的课程对象。

## 数据路径

```text
截图
  -> 离线 ML Kit OCR（文字 + bounding box + 网格线）
  -> 坐标归一化提示词
  -> MiniCPM5-1B Q4_K_M（固定 JSON）
  -> 截断恢复 / 去重 / OCR 原文校验
  -> 几何解析纠正星期和时间
  -> 写库质量闸门
  -> 用户确认后写入 SQLite
```

原适配器导入功能仍保留，便于已有学校继续使用；截图识别路径不依赖学校网页结构。
