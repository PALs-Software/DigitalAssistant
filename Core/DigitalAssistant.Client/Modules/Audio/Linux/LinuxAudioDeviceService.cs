using System.Diagnostics;
using System.Text.RegularExpressions;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using NAudio.CoreAudioApi;

namespace DigitalAssistant.Client.Modules.Audio.Linux;

public partial class LinuxAudioDeviceService : IAudioDeviceService
{
    #region Members
    [GeneratedRegex("(.*)(\n    .*)+")]
    protected static partial Regex DeviceInfoRegex();
    #endregion

    #region Get Device By Settings

    public string? GetOutputDevice(ClientSettings settings)
    {
        return GetDevice(DataFlow.Render, settings);
    }

    public string? GetInputDevice(ClientSettings settings)
    {
        return GetDevice(DataFlow.Capture, settings);
    }

    public string? GetDevice(DataFlow dataFlow, ClientSettings settings)
    {
        var deviceId = dataFlow == DataFlow.Render ? settings.OutputDeviceId : settings.InputDeviceId;
        if (String.IsNullOrEmpty(deviceId))
            return null;

        return GetDevice(dataFlow, deviceId);
    }

    public string? GetDevice(DataFlow dataFlow, string id)
    {
        var devices = GetDevices(dataFlow);
        var device = devices.Where(entry => entry.Id == id).FirstOrDefault();
        if (device == default)
            return null;

        return device.Id;
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

    public List<(string? Id, string Name)> GetDevices(DataFlow dataFlow)
    {
        var process = new Process();
        process.StartInfo.FileName = dataFlow == DataFlow.Render ? "aplay" : "arecord";
        process.StartInfo.Arguments = "-L";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;

        process.Start();
        var devicesString = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var devices = new List<(string? Id, string Name)>
        {
            (null, "System")
        };
        var matches = DeviceInfoRegex().Matches(devicesString);
        foreach (Match match in matches)
        {
            if (match.Groups.Count != 3)
                continue;

            if (match.Groups[1].Value == "null")
                continue;

            var values = match.Groups[0].Value.Split(Environment.NewLine);
            for (int i = 0; i < values.Length; i++)
                values[i] = values[i].Trim();
            var name = String.Join(", ", values[1..]);

            devices.Add((match.Groups[1].Value, name));
        }

        return devices;
    }

    #endregion
}
