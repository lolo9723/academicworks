# Local model sources and attribution

Default: Qwen3-4B-Instruct-2507, Qwen team, Apache-2.0.
Original: https://huggingface.co/Qwen/Qwen3-4B-Instruct-2507
Community quantization: https://huggingface.co/unsloth/Qwen3-4B-Instruct-2507-GGUF/tree/a06e946bb6b655725eafa393f4a9745d460374c9
File: Qwen3-4B-Instruct-2507-Q4_K_M.gguf
Bytes: 2497281120
SHA256: 3605803b982cb64aead44f6c1b2ae36e3acdb41d8e46c8a94c6533bc4c67e597
License copy: Qwen3-Apache-2.0.txt.

Research profiles also pinned in LocalModelStore.cs:
Qwen3.5-4B Q4_K_M, revision e87f176479d0855a907a41277aca2f8ee7a09523:
https://huggingface.co/unsloth/Qwen3.5-4B-GGUF
Qwen3.5-9B UD-IQ3_XXS, revision 3885219b6810b007914f3a7950a8d1b469d598a5:
https://huggingface.co/unsloth/Qwen3.5-9B-GGUF
Both Apache-2.0; copy: Qwen3.5-Apache-2.0.txt.
These profiles can be tested with --qwen35 / --qwen9. They do not change
Word's default model. A larger model is not automatically better.

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
