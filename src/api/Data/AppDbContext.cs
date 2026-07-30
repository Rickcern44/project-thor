using Microsoft.EntityFrameworkCore;

namespace ProjectThor.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
