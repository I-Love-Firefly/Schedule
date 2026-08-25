#include "llama.h"

#include <algorithm>
#include <cstring>
#include <memory>
#include <string>
#include <vector>

#if defined(_WIN32)
#define SCHEDULE_EXPORT __declspec(dllexport)
#else
#define SCHEDULE_EXPORT __attribute__((visibility("default")))
#endif

namespace {
thread_local std::string last_error;

struct Runtime {
    llama_model * model = nullptr;
    int context_size = 4096;
    int threads = 4;
    ~Runtime() { if (model) llama_model_free(model); }
};

void fail(const std::string & message) { last_error = message; }
}

extern "C" SCHEDULE_EXPORT void schedule_ai_backend_init() {
    llama_backend_init();
}

extern "C" SCHEDULE_EXPORT const char * schedule_ai_last_error() {
    return last_error.c_str();
}

extern "C" SCHEDULE_EXPORT void * schedule_ai_create(const char * model_path, int context_size, int threads) {
    last_error.clear();
    if (!model_path || !*model_path) { fail("模型路径为空。"); return nullptr; }
    auto runtime = std::make_unique<Runtime>();
    runtime->context_size = std::max(2048, context_size);
    runtime->threads = std::clamp(threads, 1, 8);
    auto params = llama_model_default_params();
    params.n_gpu_layers = 0;
    runtime->model = llama_model_load_from_file(model_path, params);
    if (!runtime->model) { fail("无法加载 GGUF 模型，请检查文件是否完整且格式正确。"); return nullptr; }
    return runtime.release();
}

extern "C" SCHEDULE_EXPORT int schedule_ai_generate(
        void * pointer, const char * prompt, int max_tokens, char * output, int output_capacity) {
    last_error.clear();
    if (!pointer || !prompt || !output || output_capacity <= 1) { fail("推理参数无效。"); return -1; }
    auto * runtime = static_cast<Runtime *>(pointer);
    const auto * vocab = llama_model_get_vocab(runtime->model);
    const int prompt_count = -llama_tokenize(vocab, prompt, std::strlen(prompt), nullptr, 0, true, true);
    if (prompt_count <= 0) { fail("OCR 提示词无法分词。"); return -1; }
    const int available_tokens = runtime->context_size - prompt_count - 1;
    if (available_tokens < 32) { fail("OCR 内容过长，超出模型上下文。"); return -1; }
    max_tokens = std::min(std::max(max_tokens, 32), available_tokens);

    std::vector<llama_token> tokens(prompt_count);
    if (llama_tokenize(vocab, prompt, std::strlen(prompt), tokens.data(), tokens.size(), true, true) < 0) {
        fail("OCR 提示词分词失败。"); return -1;
    }

    auto ctx_params = llama_context_default_params();
    ctx_params.n_ctx = prompt_count + max_tokens;
    ctx_params.n_batch = prompt_count;
    ctx_params.n_threads = runtime->threads;
    ctx_params.n_threads_batch = runtime->threads;
    ctx_params.no_perf = true;
    llama_context * raw_context = llama_init_from_model(runtime->model, ctx_params);
    if (!raw_context) { fail("无法创建模型上下文，设备可用内存可能不足。"); return -1; }
    std::unique_ptr<llama_context, decltype(&llama_free)> context(raw_context, llama_free);

    auto sampler_params = llama_sampler_chain_default_params();
    sampler_params.no_perf = true;
    llama_sampler * raw_sampler = llama_sampler_chain_init(sampler_params);
    std::unique_ptr<llama_sampler, decltype(&llama_sampler_free)> sampler(raw_sampler, llama_sampler_free);
    llama_sampler_chain_add(sampler.get(), llama_sampler_init_greedy());

    llama_batch batch = llama_batch_get_one(tokens.data(), tokens.size());
    std::string result;
    result.reserve(std::min(output_capacity - 1, 128 * 1024));
    int position = 0;
    for (int generated = 0; generated < max_tokens; ++generated) {
        if (llama_decode(context.get(), batch) != 0) { fail("模型解码失败。"); return -1; }
        position += batch.n_tokens;
        llama_token token = llama_sampler_sample(sampler.get(), context.get(), -1);
        if (llama_vocab_is_eog(vocab, token)) break;
        char piece[512];
        const int piece_size = llama_token_to_piece(vocab, token, piece, sizeof(piece), 0, true);
        if (piece_size < 0) { fail("模型输出无法转换为文本。"); return -1; }
        if (result.size() + piece_size >= static_cast<size_t>(output_capacity)) {
            fail("模型输出超过安全缓冲区。"); return -1;
        }
        result.append(piece, piece_size);
        batch = llama_batch_get_one(&token, 1);
    }

    std::memcpy(output, result.data(), result.size());
    output[result.size()] = '\0';
    return static_cast<int>(result.size());
}

extern "C" SCHEDULE_EXPORT void schedule_ai_destroy(void * pointer) {
    delete static_cast<Runtime *>(pointer);
}
