namespace DigitalAssistant.Base.ClientServerConnection;

public enum TcpMessageType
{
    Authentication,
    AudioData,
    Action,
    TransferAudioDevices,
    UpdateClientSettings
}
