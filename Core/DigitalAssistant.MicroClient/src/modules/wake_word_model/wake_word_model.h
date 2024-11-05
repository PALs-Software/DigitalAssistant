#pragma once
#include "tensorflow/lite/micro/all_ops_resolver.h"
#include "tensorflow/lite/micro/micro_error_reporter.h"
#include "tensorflow/lite/micro/micro_interpreter.h"
#include "tensorflow/lite/schema/schema_generated.h"
#include "tensorflow/lite/version.h"

#include "config.h"
#include "model.h"

class WakeWordModel
{
public:
    WakeWordModel();
    
    float Predict();
    float *GetInputBuffer();

private:
    tflite::MicroMutableOpResolver<10> *resolver;
    const tflite::Model *model;
    tflite::MicroInterpreter *interpreter;
    tflite::ErrorReporter* error_reporter = nullptr;
    TfLiteTensor *input;
    TfLiteTensor *output;
    uint8_t tensor_arena[TENSOR_ARENA_SIZE];
};