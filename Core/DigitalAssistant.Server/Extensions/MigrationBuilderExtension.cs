using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitalAssistant.Server.Extensions;

public static class MigrationBuilderExtension
{
    public static string GetMaxStringType(this MigrationBuilder migrationBuilder)
    {
        return migrationBuilder.IsSqlite() ? "text" : "nvarchar(max)";
    }
}
