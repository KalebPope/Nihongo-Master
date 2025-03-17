using Microsoft.EntityFrameworkCore;
using JapaneseMasterAPI.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace JapaneseMasterAPI.Data
{
        public class JMDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public JMDbContext(DbContextOptions<JMDbContext> options) : base(options)
        {
        }
    }
}
