using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PropertyManagement.Data;

public class PropertyManagementDbContext(DbContextOptions<PropertyManagementDbContext> options) : IdentityDbContext(options)
{
}