using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DigitalAssistant.Client.Modules.Audio.Windows;

public class WindowsAudioDeviceService : IAudioDeviceService
{
    #region Members
    protected MMDeviceEnumerator DeviceEnumerator = new();
    #endregion

    #region Get Device By Settings

    public MMDevice? GetOutputDevice(ClientSettings settings)
    {
        return GetDevice(DataFlow.Render, settings);
    }

    public MMDevice? GetInputDevice(ClientSettings settings)
    {
        return GetDevice(DataFlow.Capture, settings);
    }

    public MMDevice? GetDevice(DataFlow dataFlow, ClientSettings settings)
    {
        var settingsDeviceId = dataFlow == DataFlow.Render ? settings.OutputDeviceId : settings.InputDeviceId;
        if (String.IsNullOrEmpty(settingsDeviceId))
            return GetDefaultDevice(dataFlow);

        var device = GetDevice(dataFlow, settingsDeviceId);
        if (device != null)
            return device;

        return GetDefaultDevice(dataFlow);
    }
    #endregion

    #region Get Device

    public MMDevice? GetOutputDevice(string deviceId)
    {
        return DeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).FirstOrDefault(entry => entry.ID == deviceId);
    }

    public MMDevice? GetInputDevice(string deviceId)
    {
        return DeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).FirstOrDefault(entry => entry.ID == deviceId);
    }

    public MMDevice? GetDevice(DataFlow dataFlow, string deviceId)
    {
        return DeviceEnumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active).FirstOrDefault(entry => entry.ID == deviceId);
    }
    #endregion

    #region Get Default Device

    public MMDevice? GetDefaultOutputDevice()
    {
        return GetDefaultDevice(DataFlow.Render);
    }

    public MMDevice? GetDefaultInputDevice()
    {
        return GetDefaultDevice(DataFlow.Capture);
    }

    public MMDevice? GetDefaultDevice(DataFlow dataFlow)
    {
        if (!DeviceEnumerator.HasDefaultAudioEndpoint(dataFlow, Role.Console))
            return null;

        return DeviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Console);
    }

    #endregion

    #region Get Devices
    public List<(string? Id, string Name)> GetOutputDevices()
    {
        return GetDevices(DataFlow.Render);
    }

    public List<(string? Id, string Name)> GetInputDevices()
    {
        return GetDevices(DataFlow.Capture);
    }

    protected List<(string? Id, string Name)> GetDevices(DataFlow dataFlow)
    {
        var devices = new List<(string? Id, string Name)>
        {
            (null, "System")
        };
        devices.AddRange(DeviceEnumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active).Select(entry => ((string?)entry.ID, entry.FriendlyName)));
        return devices;
    }
    #endregion

    #region MISC
    public int? GetDeviceNumberFromWasabiInputDevice(MMDevice inputDevice)
    {
        var normalizedWasabiDeviceName = inputDevice.FriendlyName.Replace("(", "").Replace(")", "").Replace(" ", "").ToLower();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            var normalizedWaveInDeviceName = capabilities.ProductName.Replace("(", "").Replace(")", "").Replace(" ", "").ToLower();
            if (normalizedWaveInDeviceName == normalizedWasabiDeviceName)
                return i;
        }

        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            var normalizedWaveInDeviceName = capabilities.ProductName.Replace("(", "").Replace(")", "").Replace(" ", "").ToLower();
            if (normalizedWasabiDeviceName.Contains(normalizedWaveInDeviceName))
                return i;
        }

        return null;
    }
    #endregion
}
