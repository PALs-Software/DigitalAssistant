namespace DigitalAssistant.Client.Modules.General;

public static class EnvironmentInformations
{
    public static bool ApplicationRunsInDockerContainer { get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; } }
}
