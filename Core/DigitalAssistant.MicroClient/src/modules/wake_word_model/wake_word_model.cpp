#include "wake_word_model.h"

WakeWordModel::WakeWordModel()
{
    error_reporter = new tflite::MicroErrorReporter();
    model = tflite::GetModel(wake_word_recognition_model);
    if (model->version() != TFLITE_SCHEMA_VERSION)
    {
        TF_LITE_REPORT_ERROR(error_reporter,
                             "Model provided is schema version %d not equal "
                             "to supported version %d.",
                             model->version(), TFLITE_SCHEMA_VERSION);
        return;
    }

    resolver = new tflite::MicroMutableOpResolver<10>();
    resolver->AddConv2D();
    resolver->AddMaxPool2D();
    resolver->AddFullyConnected();
    resolver->AddMul();
    resolver->AddAdd();
    resolver->AddLogistic();
    resolver->AddReshape();
    resolver->AddQuantize();
    resolver->AddDequantize();

    interpreter = new tflite::MicroInterpreter(model, *resolver, tensor_arena, TENSOR_ARENA_SIZE, error_reporter);
    TfLiteStatus allocate_status = interpreter->AllocateTensors();
    if (allocate_status != kTfLiteOk)
    {
        TF_LITE_REPORT_ERROR(error_reporter, "AllocateTensors() failed");
        return;
    }
    TF_LITE_REPORT_ERROR(error_reporter, "Arena used bytes %d\n", interpreter->arena_used_bytes());

    input = interpreter->input(0);
    output = interpreter->output(0);
}

float *WakeWordModel::GetInputBuffer()
{
    return input->data.f;
}

float WakeWordModel::Predict()
{
    interpreter->Invoke();
    return output->data.f[0];
}
