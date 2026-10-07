# Local model sources and attribution

Qwen3.5-9B, Qwen team: https://huggingface.co/Qwen/Qwen3.5-9B
License: Apache-2.0. A copy is in Qwen3.5-Apache-2.0.txt.

Community quantization by Unsloth; not an official Qwen GGUF release:
https://huggingface.co/unsloth/Qwen3.5-9B-GGUF/tree/3885219b6810b007914f3a7950a8d1b469d598a5
Pinned model card:
https://huggingface.co/unsloth/Qwen3.5-9B-GGUF/blob/3885219b6810b007914f3a7950a8d1b469d598a5/README.md

File: Qwen3.5-9B-UD-IQ3_XXS.gguf
Bytes: 4016235744
SHA256: 40d0f32cd3030b04f0784139a589fb63e876cfbf8667d56311b79783c74fd149

Weights are downloaded separately, never bundled in the Word installer.
The project does not fine-tune these weights. Its changes concern sentence
analysis, protected Word ranges, structured generation, semantic review,
retry handling and CPU memory management. Low-bit quantization may affect
quality. Upstream benchmark claims do not validate this Word application.

llama.cpp CPU runtime: https://github.com/ggml-org/llama.cpp/tree/b11460
MIT copy: llama.cpp-LICENSE.txt. Runtime archive hash is pinned in
build/local-runtime.json. LLVM/OpenMP license files accompany the binaries.
Official Microsoft app-local x64 CRT attribution is in
MICROSOFT-CRT-NOTICE.txt; packaging uses the Visual Studio redistributable files.
