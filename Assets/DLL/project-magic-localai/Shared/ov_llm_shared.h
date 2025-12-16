#pragma once
#include <string>
#include <memory>
#include "openvino/genai/llm_pipeline.hpp"

namespace ovllm {

	using PipelinePtr = std::unique_ptr<ov::genai::LLMPipeline>;

	PipelinePtr LoadModel(const std::string& modelPath, const std::string& device = "CPU");

	std::string GenerateText(ov::genai::LLMPipeline* pipeline, const ov::genai::ChatHistory& chatHistory, const ov::genai::GenerationConfig& generation_config);

}
