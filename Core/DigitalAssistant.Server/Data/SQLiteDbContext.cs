using Microsoft.EntityFrameworkCore;

namespace DigitalAssistant.Server.Data;

public class SQLiteDbContext(DbContextOptions options) : ApplicationDbContext(options)
{
}
