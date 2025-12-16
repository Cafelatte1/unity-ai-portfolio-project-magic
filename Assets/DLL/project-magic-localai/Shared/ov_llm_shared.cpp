#include "ov_llm_shared.h"
#include <iostream>

namespace ovllm {

    PipelinePtr LoadModel(const std::string& modelPath, const std::string& device) {
        try {
            auto pipeline = std::make_unique<ov::genai::LLMPipeline>(modelPath, device);
            return pipeline;
        }
        catch (const std::exception& e) {
            std::cerr << "[LoadModel Error] " << e.what() << std::endl;
            return nullptr;
        }
    }

    std::string GenerateText(
        ov::genai::LLMPipeline* pipeline, const ov::genai::ChatHistory& chatHistory, const ov::genai::GenerationConfig& generation_config
        ) {
        if (!pipeline) {
            return "[Error] Pipeline is null";
        }

        try {
            auto result = pipeline->generate(chatHistory, generation_config);
            return result.texts.empty() ? "" : result.texts[0];
        }
        catch (const std::exception& e) {
            return std::string("[Generate Error] ") + e.what();
        }
    }

} // namespace ovllm
