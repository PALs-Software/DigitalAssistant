using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.Abstractions.CRUD.Extensions;
using BlazorBase.Abstractions.General.Extensions;
using BlazorBase.CRUD.Extensions;
using BlazorBase.CRUD.Models;
using BlazorBase.MessageHandling.Interfaces;
using DigitalAssistant.Server.Modules.Api.Enums;
using DigitalAssistant.Server.Modules.Api.Services;
using DigitalAssistant.Server.Modules.CacheModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DigitalAssistant.Server.Modules.Api.Models;

[Route("/AccessTokens")]
[Index(nameof(TokenHash))]
[Index(nameof(CreatedOn))]
[Authorize(Roles = "Admin")]
public partial class AccessToken : ExtendedInformationBaseModel
{
    #region Properties

    [Key]
    public Guid Id { get; set; }

    [Visible]
    [Required]
    [DisplayKey]
    [StringLength(250)]
    public string Name { get; set; } = null!;

    [Visible]
    [Required]
    public AccessTokenType Type { get; set; }

    [StringLength(128)]
    public string TokenHash { get; set; } = null!;

    [Visible]
    [Required]
    [PresentationDataType(PresentationDataType.DateTime)]
    public DateTime ValidUntil { get; set; }

    #endregion

    #region CRUD

    public override Task OnCreateNewEntryInstance(OnCreateNewEntryInstanceArgs args)
    {
        if (args.Model is AccessToken accessToken && accessToken.ValidUntil == DateTime.MinValue)
            accessToken.ValidUntil = DateTime.Now.Date.AddYears(30);

        return base.OnCreateNewEntryInstance(args);
    }

    public override Task OnAfterAddEntry(OnAfterAddEntryArgs args)
    {
        var token = TokenService.GenerateRandomToken(128); // 128 characters long - 1024 bit strong access token
        TokenHash = $"Bearer {token}".CreateSHA512Hash();

        args.EventServices.ServiceProvider.GetRequiredService<IMessageHandler>().ShowMessage(
            args.EventServices.Localizer["Created a new access token"],
            args.EventServices.Localizer["You have successfully added a new access token. The token value is: {0}", token]
        );

        return base.OnAfterAddEntry(args);
    }

    public override async Task OnAfterCardSaveChanges(OnAfterCardSaveChangesArgs args)
    {
        var untrackedAccessToken = await args.EventServices.DbContext.FirstAsync<AccessToken>(entry => entry.Id == Id, asNoTracking: true);
        Cache.ApiCache.AccessTokens[TokenHash] = untrackedAccessToken;

        await base.OnAfterCardSaveChanges(args);
    }

    public override Task OnAfterDbContextDeletedEntry(OnAfterDbContextDeletedEntryArgs args)
    {
        Cache.ApiCache.AccessTokens.Remove(TokenHash, out _);
        return base.OnAfterDbContextDeletedEntry(args);
    }

    #endregion

}