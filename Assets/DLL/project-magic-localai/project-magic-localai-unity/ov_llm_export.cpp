#include "ov_llm_export.h"
#include "ov_llm_shared.h"

#include <string>
#include <cstdlib>
#include <iostream>
#include <exception>
#include "openvino/genai/json_container.hpp"

using ov::genai::JsonContainer;
using ov::genai::ChatHistory;
using ov::genai::GenerationConfig;
using ov::genai::SchedulerConfig;

// ---------------------------
// Helper Functions
// ---------------------------

static const char* dup_string(const std::string& s)
{
#if defined(_MSC_VER)
    return _strdup(s.c_str());
#else
    return strdup(s.c_str());
#endif
}

static ChatHistory CreateChatHistoryFromJson(const char* messagesJson, const char* toolsJson)
{
    ChatHistory chatHistory;

    // --- Handle messages ---
    JsonContainer msgRoot = JsonContainer::from_json_string(messagesJson);

    if (msgRoot.is_array()) {
        chatHistory = ChatHistory(msgRoot);
    }
    else {
        chatHistory.push_back(msgRoot);
    }

    // --- Handle tools (optional) ---
    if ((toolsJson != nullptr) && (strlen(toolsJson) > 0))
    {
        JsonContainer toolsRoot = JsonContainer::from_json_string(toolsJson);

        if (toolsRoot.is_array()) {
            chatHistory.set_tools(toolsRoot);
        }
        else {
            JsonContainer arr = JsonContainer::array();
            arr.push_back(toolsRoot);
            chatHistory.set_tools(arr);
        }
    }

    return chatHistory;
}

// ---------------------------
// DLL Export
// ---------------------------

OVLLM_EXPORT void* OV_LoadModel(const char* modelPath, const char* device)
{
    if (!modelPath) {
        std::cerr << "[LLM_LoadModel] modelPath is null\n";
        return nullptr;
    }

    try {
        std::string model(modelPath);
        std::string dev = device ? std::string(device) : std::string("CPU");

        auto pipeline = ovllm::LoadModel(model, dev);
        if (!pipeline) {
            return nullptr;
        }

        return reinterpret_cast<void*>(pipeline.release());
    }
    catch (const std::exception& e) {
        std::cerr << "[LLM_LoadModel] Exception: " << e.what() << std::endl;
        return nullptr;
    }
}

OVLLM_EXPORT const char* OV_Inference(void* pipelinePtr, const char* messagesJson, const char* toolsJson)
{
    if (!pipelinePtr) {
        return dup_string("[Error] Pipeline is null");
    }
    if (!messagesJson) {
        return dup_string("[Error] chatHistoryJson is null");
    }

    auto pipeline = reinterpret_cast<ov::genai::LLMPipeline*>(pipelinePtr);

    try {
        ChatHistory chatHistory = CreateChatHistoryFromJson(messagesJson, toolsJson);

        ov::AnyMap configs;
        GenerationConfig generation_config;
        generation_config.apply_chat_template = true;
        generation_config.max_new_tokens = 300;
        generation_config.do_sample = true;
        generation_config.temperature = 0.5f;
        generation_config.top_p = 0.9f;
        generation_config.top_k = 40;
        configs["generation_config"] = generation_config;
        SchedulerConfig scheduler_config;
        scheduler_config.cache_size = 2;
        configs["scheduler_config"] = scheduler_config;

        std::string result = ovllm::GenerateText(pipeline, chatHistory, configs);
        return dup_string(result);
    }
    catch (const std::exception& e) {
        std::string msg = std::string("[Generate Error] ") + e.what();
        return dup_string(msg);
    }
}

OVLLM_EXPORT void OV_Release(void* pipelinePtr)
{
    if (!pipelinePtr) return;

    auto pipeline = reinterpret_cast<ov::genai::LLMPipeline*>(pipelinePtr);
    delete pipeline;
}

OVLLM_EXPORT void OV_FreeString(const char* str)
{
    if (!str) return;
    free((void*)str);
}
