#pragma once

enum class TcpMessageType
{
    AvailableClientToSetup,
    SetupClientWithServer,
    Authentication,
    AudioData,
    StopSendingAudioData,
    Action,
    TransferAudioDevices,
    UpdateClientSettings
};