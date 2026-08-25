#!/usr/bin/env python3
"""Load a quantized schedule model through the same C ABI used by Android."""

from __future__ import annotations

import argparse
import ctypes
import os
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--library", type=Path, required=True)
    parser.add_argument("--model", type=Path, required=True)
    args = parser.parse_args()

    library = args.library.resolve()
    if os.name == "nt":
        os.add_dll_directory(str(library.parent))
    native = ctypes.CDLL(str(library))
    native.schedule_ai_create.argtypes = [ctypes.c_char_p, ctypes.c_int, ctypes.c_int]
    native.schedule_ai_create.restype = ctypes.c_void_p
    native.schedule_ai_generate.argtypes = [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_int, ctypes.c_void_p, ctypes.c_int]
    native.schedule_ai_generate.restype = ctypes.c_int
    native.schedule_ai_destroy.argtypes = [ctypes.c_void_p]
    native.schedule_ai_backend_init()

    handle = native.schedule_ai_create(str(args.model.resolve()).encode("utf-8"), 2048, 6)
    if not handle:
        raise RuntimeError("Native runtime could not load the GGUF model")
    try:
        prompt = (
            '<|im_start|>user\nReturn only {"ok":true}.<|im_end|>\n'
            '<|im_start|>assistant\n<think>\n\n</think>\n\n'
        ).encode("utf-8")
        output = ctypes.create_string_buffer(64 * 1024)
        written = native.schedule_ai_generate(handle, prompt, 64, output, len(output))
        if written < 0:
            raise RuntimeError("Native generation failed")
        print(output.raw[:written].decode("utf-8", errors="replace"))
        print(f"native_smoke_ok bytes={written}")
    finally:
        native.schedule_ai_destroy(handle)


if __name__ == "__main__":
    main()
