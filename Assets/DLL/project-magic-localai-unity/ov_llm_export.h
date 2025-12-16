#pragma once
#include <cstddef>

#if defined(_WIN32) || defined(_WIN64)
#define OVLLM_EXPORT extern "C" __declspec(dllexport)
#else
#define OVLLM_EXPORT extern "C"
#endif

// ---------------------------
// Unity DLL API
// ---------------------------
 
// modelPath : OpenVINO LLM Path (UTF-8)
// device    : "CPU", "GPU", "NPU"
OVLLM_EXPORT void* OV_LoadModel(const char* modelPath, const char* device);

//   Ex) [
//          {"role":"user","content":"Hello"},
//          {"role":"assistant","content":"Hi!"}
//        ]
OVLLM_EXPORT const char* OV_Inference(void* pipelinePtr, const char* messagesJson, const char* toolsJson);

OVLLM_EXPORT void  OV_Release(void* pipelinePtr);

OVLLM_EXPORT void  OV_FreeString(const char* str);
