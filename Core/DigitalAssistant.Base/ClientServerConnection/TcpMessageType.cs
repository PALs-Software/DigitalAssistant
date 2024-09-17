namespace DigitalAssistant.Base.ClientServerConnection;

public enum TcpMessageType
{
    AvailableClientToSetup,
    SetupClientWithServer,
    Authentication,
    AudioData,
    StopSendingAudioData,
    Action,
    TransferAudioDevices,
    UpdateClientSettings
}
