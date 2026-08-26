#!/usr/bin/env python3
from pathlib import Path
import shutil
import subprocess
import sys

site = Path("/etc/nginx/sites-available/default")
snippet_path = Path(__file__).with_name("nginx-location.conf")
backup = site.with_name("default.bak-schedule-ai")
marker = "    listen [::]:443 ssl ipv6only=on;"
route_marker = "location /schedule-ai/"

original = site.read_text(encoding="utf-8")
if route_marker not in original:
    if original.count(marker) != 1:
        raise SystemExit("Expected exactly one HTTPS insertion marker; no changes made.")
    snippet = snippet_path.read_text(encoding="utf-8").rstrip()
    if not backup.exists():
        shutil.copy2(site, backup)
    site.write_text(original.replace(marker, f"{snippet}\n\n{marker}", 1), encoding="utf-8")

test = subprocess.run(["nginx", "-t"], text=True)
if test.returncode != 0:
    if backup.exists():
        shutil.copy2(backup, site)
        subprocess.run(["nginx", "-t"], check=False)
    raise SystemExit("Nginx validation failed; the previous configuration was restored.")

subprocess.run(["systemctl", "reload", "nginx"], check=True)
print("NGINX_ROUTE_READY")
