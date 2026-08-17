using Askii.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Askii.Database;

public static class DbIniializer
{
    public static async Task Init(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<AppDbContext>();
            var config = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DbInitializer");

            await db.Database.MigrateAsync();

            // Impostazioni a livello di file database (persistenti)
            await db.Database.ExecuteSqlRawAsync(@"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA temp_store = MEMORY;
            ");

            if(await db.Users.AnyAsync())
            {
                logger.LogInformation("Superadmin gia presente, db inizializzato");
                return;
            }

            var adminEmail = config["InitialAdmin:Email"];
            var adminPassword = config["InitialAdmin:Password"];
            var firstName = config["InitialAdmin:FirstName"];
            var lastName = config["InitialAdmin:LastName"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("ATTENZIONE: Nessun utente nel DB e le variabili 'InitialAdmin' non sono configurate. Impossibile creare l'amministratore iniziale.");
                return;
            }

            var admin = User.CreateSuperAdmin(adminEmail, adminPassword, firstName, lastName);
            db.Users.Add(admin);
            await db.SaveChangesAsync();

            logger.LogInformation("Super admin creato, db OK!");
                
        }
    }
}