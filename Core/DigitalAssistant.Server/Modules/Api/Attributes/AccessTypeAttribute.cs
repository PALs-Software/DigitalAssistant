using DigitalAssistant.Server.Modules.Api.Enums;

namespace DigitalAssistant.Server.Modules.Api.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class AccessTypeAttribute : Attribute
{
    public AccessTypeAttribute(params AccessTokenType[] accessTypes)
    {
        Types = accessTypes.ToList();
    }

    public List<AccessTokenType> Types { get; set; }
}
