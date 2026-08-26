# 云端 DeepSeek 课程表识别

## 运行顺序

1. Android 端始终先运行离线 ML Kit OCR，得到文字块和归一化坐标。
2. 开启“云端优先”时，APP 通过 HTTPS 将完整图片和 OCR 坐标发送到自有网关。
3. 网关调用腾讯云 TokenHub 的 `deepseek/deepseek-v4-flash-vision-exp`。
4. 如果视觉模型暂不可用，网关改用 `deepseek-v4-flash` 处理 OCR 坐标文本。
5. 如果网关、网络或全部云端模型不可用，APP 自动进入原有离线混合识别流程。
6. 云端 JSON 必须经过本地字段、OCR 证据、时间和网格校验；未通过质量闸门的结果只能在用户逐项确认后写库。

## 密钥边界

- TokenHub API Key 只存放在服务器的 `Server/ScheduleAiGateway/.env`。
- `.env` 已加入 `.gitignore`，不得复制到客户端资源、源码、日志或 GitHub。
- APP 只知道自有 HTTPS 网关地址，不知道 TokenHub Key。
- Key 应限定为课程表所用的 DeepSeek 推理服务，并设置 token 总额度、告警与轮换周期。

## 数据处理

网关不持久化图片和 OCR 内容。正常日志只记录请求 ID、模型名和耗时。APP 页面明确提示云端模式会上传课程表，用户可以随时关闭开关恢复完全离线处理。

公网发布前仍需补充正式的用户身份认证或 Android 应用完整性校验。当前网关的 IP 限流和每日总限额主要用于内测及成本兜底，不能替代生产身份认证。
