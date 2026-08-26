# Schedule AI Gateway

The Android app sends a timetable image plus on-device OCR coordinates to this gateway. The gateway keeps the official DeepSeek API key server-side and calls `deepseek-v4-flash-vision-exp` at `api.deepseek.com`. If the vision request is temporarily unavailable, it falls back to `deepseek-v4-flash` using OCR layout text only. The existing on-device pipeline remains the final fallback when the gateway itself is unavailable.

## Server configuration

1. Copy `.env.example` to `.env` on the server only.
2. Set `DEEPSEEK_API_KEY` to an official DeepSeek API key. Never commit `.env`, paste the key into source code, or ship it in the APK.
3. Ensure Tencent Lighthouse firewall allows TCP ports 80 and 443.
4. Run `docker compose up -d --build`.
5. Verify `https://schedule-ai.42-193-179-91.sslip.io/health` returns `{"ok":true}`.

The service sends the image to DeepSeek with `detail: original`, requests JSON mode, validates and normalizes the returned schedule before replying to the app, and does not persist images or OCR text. Logs contain only request IDs, model names, elapsed times, and sanitized error messages. Production release should add account authentication or app attestation in addition to the included IP/global rate limits.

## Data path

`Android app -> this gateway -> official DeepSeek API -> this gateway -> Android app`

The API key exists only in the gateway container environment. The app receives structured course JSON, shows a review screen, and writes to SQLite only after its existing local evidence checks or explicit user confirmation.
