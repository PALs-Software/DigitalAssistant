# Initial Setup

After starting the server application for the first time, the website can be accessed under the specified url and port. For the initial setup, you are guided through a setup wizard in which the admin account and the basic settings are defined.

# Setup
The setup includes all the basic settings of the digital assistant

## Speech Recognition Model
In order to execute the user's voice commands, they must first be converted into text form. Digital Assistant uses customized speech recognition models from [Whisper](https://github.com/openai/whisper) to do this. The exact model used can be selected in the "Speech Recognition Model" tab of the setup. The models are then downloaded and loaded into the RAM. 

Select the models based on your server's hardware, as the model can consume a lot of compute power and RAM. Note the amount of RAM required for each model.

#### Text-To-Speech models:
- ~20-200 MB RAM
  
#### Speech Recognition models:
- Tiny: ~1 GB
- Base: ~1 GB
- Small: ~2 GB
- Medium: ~5 GB
- Large: ~10 GB

> [!NOTE]
> For executing the models with the gpu, CUDA is required and must be installed separately. Further information for the installation can be found in the following links:
[ONNX Cuda Version Overview](https://onnxruntime.ai/docs/execution-providers/CUDA-ExecutionProvider.html) / [Windows](https://docs.nvidia.com/cuda/cuda-installation-guide-microsoft-windows/index.html) / [Linux](https://docs.nvidia.com/cuda/cuda-installation-guide-linux/) / [MAC](https://docs.nvidia.com/cuda/archive/10.1/cuda-installation-guide-mac-os-x/index.html)

## Text to Speech Model
In order for the voice assistant to give you answers, the formulated text answers must be converted into speech. In the setup, you can select a model of your choice according to language and quality.

## Interpreter
The interpreter is responsible for deriving a corresponding command from your requests and executing it. To do this, select the language in which you want to write your requests.

## Recommendend Settings

#### Speech Recognition models

Raspberry Pi 4/5:
- Model: Base or Small (Depending of the available RAM)
- Precision: FP32

Server/Desktop Computer (>=8GB RAM):
- Model: Medium
- Precision: FP32

#### Text to Speech Model
Can be selected as desired, as they do not consume much memory.

> [!NOTE]
> In the German-speaking area, the voice "Thorsten" is the best choice

# Add additional users
1. Select the **Users** tab in the administration sidebar tab.
2. Choose the plus icon to add a new user.
3. Fill in the necessary fields.
   - E-Mail
   - Username
4. Specify the user role.
5. Click on the **Change password** action to set an initial password that the user can later change themselves.
6. The user can manage there user profile by opening the user's sidebar by clicking on the profile image in the top right corner and then selecting **Manage Profile**.

# Create Backup
1. Select the **Setup** tab in the administration sidebar tab.
2. Press the action **Create & Download Website Backup**.

# Troubleshooting

## General
In most cases, the logs describe a more detailed error message and how to deal with it, so read them carefully. 

Under Windows, detailed logs can be found in the "Event Viewer"

## I have selected a model that needs more RAM than i have in the setup and now the server application no longer starts and I can no longer change it

1. Open a file explorer and go to your installation folder of the server application.
2. Open the file **appsettings.json**.
3. Set the **PreventLoadingAiModels** to **true**.
4. Launch the server application.
5. Change the selected models in the setup.
6. Set the **PreventLoadingAiModels** to **false**.
7. Restart the server application.