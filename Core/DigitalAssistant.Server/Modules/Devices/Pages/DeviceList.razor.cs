using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace DigitalAssistant.Server.Modules.Devices.Pages;

[Route("/Devices")]
[Authorize(Roles = "Admin, User")]
public partial class DeviceList
{

}
