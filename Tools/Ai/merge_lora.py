"""Merge a PEFT LoRA checkpoint into a text or vision model for GGUF conversion."""

from __future__ import annotations

import argparse
from pathlib import Path

import torch
from peft import PeftModel
from transformers import AutoConfig, AutoModelForCausalLM, AutoModelForImageTextToText, AutoProcessor, AutoTokenizer


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", required=True, type=Path)
    parser.add_argument("--adapter", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()

    args.output.mkdir(parents=True, exist_ok=True)
    config = AutoConfig.from_pretrained(args.model, trust_remote_code=True)
    text_only = any(name.endswith("ForCausalLM") for name in getattr(config, "architectures", []))
    model_class = AutoModelForCausalLM if text_only else AutoModelForImageTextToText
    model = model_class.from_pretrained(
        args.model,
        torch_dtype=torch.bfloat16,
        trust_remote_code=True,
        low_cpu_mem_usage=True,
        device_map="cpu",
    )
    model = PeftModel.from_pretrained(model, args.adapter)
    model = model.merge_and_unload(progressbar=True, safe_merge=True)
    model.save_pretrained(args.output, safe_serialization=True, max_shard_size="4GB")

    if text_only:
        AutoTokenizer.from_pretrained(args.model, trust_remote_code=True).save_pretrained(args.output)
    else:
        AutoProcessor.from_pretrained(args.model, trust_remote_code=True).save_pretrained(args.output)
    print(f"Merged model written to {args.output.resolve()}")


if __name__ == "__main__":
    main()
