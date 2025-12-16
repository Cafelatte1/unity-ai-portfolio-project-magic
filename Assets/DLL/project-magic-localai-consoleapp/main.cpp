#include <iostream>
#include <filesystem>
#include <fstream>
#include <string>
#include "openvino/genai/llm_pipeline.hpp"
#include "ov_llm_shared.h"

namespace fs = std::filesystem;
using namespace ov::genai;

inline std::string to_utf8(const char8_t* s)
{
	return std::string(reinterpret_cast<const char*>(s));
}

static std::string modelPath = "../Models/qwen3-4b-it-ir/";
static std::string debugQuery = to_utf8(u8"3 더하기 5는 뭐야?");

JsonContainer build_calculate_schema() {
	JsonContainer tool = JsonContainer::object();

	JsonContainer fn = JsonContainer::object();
	fn["name"] = "calculate";
	fn["description"] = to_utf8(u8"두 숫자와 연산자를 받아 계산합니다");

	JsonContainer params = JsonContainer::object();
	params["type"] = "object";

	JsonContainer props = JsonContainer::object();
	props["a"] = JsonContainer({ {"type", "number"} });
	props["b"] = JsonContainer({ {"type", "number"} });
	props["op"] = JsonContainer({ {"type", "string"} });

	params["properties"] = props;

	JsonContainer required = JsonContainer::array();
	required.push_back("a");
	required.push_back("b");
	required.push_back("op");
	params["required"] = required;

	fn["parameters"] = params;

	tool["type"] = "function";
	tool["function"] = fn;

	return tool;
}


int main(int argc, char* argv[]) {
	std::cout << "Load Model" << std::endl;
	auto pipeline = ovllm::LoadModel(modelPath, "GPU");
	if (!pipeline) {
		std::cout << "파이프라인 로드 실패\n";
		return -1;
	}

	std::cout << "Create Chat History" << std::endl;
	JsonContainer messages = JsonContainer::array();

	if (modelPath.find("vlm") != std::string::npos) {
		// for vision-language models
		JsonContainer message = JsonContainer::object();
		message["role"] = "user";
		JsonContainer content = JsonContainer::array();
		JsonContainer contentText = JsonContainer::object();
		contentText["type"] = "text";
		contentText["text"] = debugQuery;
		content.push_back(contentText);
		message["content"] = content;
		messages.push_back(message);
	}
	else {
		// for test-only models
		JsonContainer message = JsonContainer::object();
		message["role"] = "user";
		message["content"] = debugQuery;
		messages.push_back(message);
	}
	
	ChatHistory chatHistory(messages);
	
	std::cout << "Add Tools" << std::endl;
	JsonContainer tools = JsonContainer::array();
	JsonContainer tool_calculate = build_calculate_schema();
	tools.push_back(tool_calculate);
	chatHistory.set_tools(tools);
	
	std::cout << "Chat History Messages" << std::endl;
	std::cout << chatHistory.get_messages().to_json_string() << std::endl;
	std::cout << "Tools" << std::endl;
	std::cout << chatHistory.get_tools().to_json_string() << std::endl;

	std::cout << "Setup Generation Config" << std::endl;
	ov::genai::GenerationConfig generation_config;
	generation_config.apply_chat_template = true;
	generation_config.max_new_tokens = 300;
	generation_config.do_sample = true;
	generation_config.temperature = 0.5f;
	generation_config.top_p = 0.9f;
	generation_config.top_k = 40;
	generation_config.rng_seed = 42;

	std::cout << "Generate Text" << std::endl;
	std::string gened_text = ovllm::GenerateText(pipeline.get(), chatHistory, generation_config);
	std::cout << "gened_text: " + gened_text << std::endl;

	std::cout << "Save to output.txt" << std::endl;
	std::ofstream out("output.txt");
	out << gened_text;
	out.close();
	return 0;
}