using BlazorBase.Files.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace DigitalAssistant.Server.Modules.Files;

[Route("/ServerFiles")]
[Authorize(Roles = "Admin")]
public partial class ServerFile : BaseFile
{
}