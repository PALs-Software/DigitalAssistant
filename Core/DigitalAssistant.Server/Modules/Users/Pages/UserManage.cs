using BlazorBase.User.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace DigitalAssistant.Server.Modules.Users.Pages;

[Route("/User/Manage")]
[Authorize]
public class UserManage : BaseUserManage
{
}
