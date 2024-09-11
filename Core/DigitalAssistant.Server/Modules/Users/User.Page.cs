using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.Abstractions.CRUD.Structures;
using BlazorBase.CRUD.Models;
using BlazorBase.MessageHandling.Enum;
using BlazorBase.MessageHandling.Interfaces;
using BlazorBase.User.Attributes;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Users;

public partial class User
{
    public override Task<List<PageActionGroup>?> GeneratePageActionGroupsAsync(EventServices eventServices)
    {
        return Task.FromResult<List<PageActionGroup>?>(
        [
            new PageActionGroup()
            {
                Caption = PageActionGroup.DefaultGroups.Process,
                VisibleInGUITypes = [GUIType.Card],
                PageActions =
                [
                    new PageAction()
                    {
                        Caption = "ChangePassword",
                        ToolTip = "ChangePasswordToolTip",
                        Image = FontAwesomeIcons.Key,
                        VisibleInGUITypes = [GUIType.Card],
                        Action = (source, eventServices, model) =>
                        {
                            ArgumentNullException.ThrowIfNull(model);
                            var user = (User)model;
                            var localizer = eventServices.Localizer;
                            if (user.IdentityUserId == null)
                                throw new CRUDException(localizer["IdentityUserIdNullErr"]);

                            var messageHandler = eventServices.ServiceProvider.GetRequiredService<IMessageHandler>();
                            messageHandler.ShowTextInputDialog(localizer["PasswordChangeRequestTitle"],
                                message: String.Empty,
                                textInputCaption: localizer["PasswordChangeInputCaption"],
                                maskText: true, 
                                onClosing: async (closingArgs, dialogResult, textResult) =>{
                                if (dialogResult != ConfirmDialogResult.Confirmed)
                                    return;

                                var userService = eventServices.ServiceProvider.GetRequiredService<UserService>();
                                var userManager = eventServices.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                                var passwordValidationLocalizer = eventServices.ServiceProvider.GetRequiredService<IStringLocalizer<IdentityPasswordLengthValidationAttribute>>();

                                var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
                                ArgumentNullException.ThrowIfNull(identityUser);
                                ArgumentNullException.ThrowIfNull(textResult.Text);

                                await userManager.RemovePasswordAsync(identityUser);
                                var result = await userManager.AddPasswordAsync(identityUser, textResult.Text);
                                if (result.Succeeded)
                                    messageHandler.ShowMessage(localizer["PasswordChangeSuccessfulTitle"], localizer["PasswordChangeSuccessfulMessage", user.UserName], MessageType.Success);
                                else
                                {
                                    List<string> errors = [];
                                    foreach (var error in result.Errors)
                                    {
                                        string errorMessage = error.Code switch
                                        {
                                            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => (string)passwordValidationLocalizer[error.Code, userManager.Options.Password.RequiredUniqueChars],
                                            nameof(IdentityErrorDescriber.PasswordTooShort) => (string)passwordValidationLocalizer[error.Code, userManager.Options.Password.RequiredLength],
                                            _ => (string)passwordValidationLocalizer[error.Code],
                                        };
                                        errors.Add(errorMessage);
                                    }

                                    throw new CRUDException(String.Join(Environment.NewLine, errors));
                                }
                            });

                            return Task.CompletedTask;
                        }
                    }
                ]
            }
        ]);
    }
}
