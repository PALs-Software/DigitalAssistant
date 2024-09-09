using BlazorBase.Server.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Collections.Concurrent;

namespace DigitalAssistant.Server.Modules.Users;

public class ExtendedCircuitHandlerService : BaseCircuitHandlerService
{
    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }   
}
