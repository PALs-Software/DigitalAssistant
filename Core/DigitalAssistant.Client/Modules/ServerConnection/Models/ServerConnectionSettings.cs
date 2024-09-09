﻿namespace DigitalAssistant.Client.Modules.ServerConnection.Models;

public class ServerConnectionSettings
{
    public string? ServerName { get; set; }
    public int ServerPort { get; set; }
    public string? ServerAccessToken { get; set; }
    public bool IgnoreServerCertificateErrors { get; set; }
}
