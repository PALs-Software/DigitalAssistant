namespace DigitalAssistant.Base.ClientServerConnection;

public enum TcpMessageType
{
    AvailableClientToSetup,
    SetupClientWithServer,
    Authentication,
    AudioData,
    Action,
    TransferAudioDevices,
    UpdateClientSettings
}
