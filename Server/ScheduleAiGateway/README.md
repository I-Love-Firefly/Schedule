# Schedule AI Gateway

The Android app sends a timetable image plus on-device OCR coordinates to this gateway. The gateway keeps the Tencent Cloud TokenHub key server-side and calls DeepSeek V4 Flash Vision. If the vision endpoint is temporarily unavailable, it falls back to DeepSeek V4 Flash using OCR layout text only.

## Server configuration

1. Copy `.env.example` to `.env` on the server only.
2. Set `TOKENHUB_API_KEY` to a TokenHub API key. Never commit `.env`.
3. Ensure Tencent Lighthouse firewall allows TCP ports 80 and 443.
4. Run `docker compose up -d --build`.
5. Verify `https://schedule-ai.42-193-179-91.sslip.io/health` returns `{"ok":true}`.

The service does not persist images or OCR text. Logs contain only request IDs, model names, elapsed times, and sanitized error messages. Production release should add account authentication or app attestation in addition to the included IP/global rate limits.
