using Microsoft.EntityFrameworkCore;

namespace DigitalAssistant.Server.Data;

public class MSSQLDbContext(DbContextOptions options) : ApplicationDbContext(options)
{
}
