using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Askii.Database.Entities;

public class Options
{
    private readonly IServiceScopeFactory _scopeFactory;
    private List<Option> Opt { get; set; } = new List<Option>();

    // Iniettiamo IServiceScopeFactory invece del DbContext direttamente
    public Options(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task Seed()
    {
        // Creiamo uno scope temporaneo per usare in sicurezza il DbContext
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Opt = await db.Options.AsNoTracking().ToListAsync();

        bool changed = false;

        if(!Opt.Any(x => x.Name == Option.Email.SMTP_USER)) { db.Options.Add(new Option(Option.Email.SMTP_USER)); changed = true; }
        if(!Opt.Any(x => x.Name == Option.Email.SMTP_PASS)) { db.Options.Add(new Option(Option.Email.SMTP_PASS)); changed = true; }
        if(!Opt.Any(x => x.Name == Option.Email.SMTP_PORT)) { db.Options.Add(new Option(Option.Email.SMTP_PORT)); changed = true; }
        if(!Opt.Any(x => x.Name == Option.Email.SMTP_HOST)) { db.Options.Add(new Option(Option.Email.SMTP_HOST)); changed = true; }

        if (changed)
        {
            await db.SaveChangesAsync();
            Opt = await db.Options.AsNoTracking().ToListAsync();
        }
    }

    // Overload per i tipi di valore (int, bool, Enum, ecc.)
    public async Task UpdateOption<T>(string opt, T value) where T : struct
    {
        await UpdateInternal(opt, value.ToString() ?? string.Empty);
    }

    // Overload per le stringhe
    public async Task UpdateOption(string opt, string value)
    {
        await UpdateInternal(opt, value);
    }

    private async Task UpdateInternal(string opt, string stringValue)
    {
        var option = Opt.FirstOrDefault(x => x.Name == opt);
        if(option is not null)
        {
            option.SetValue(stringValue); // Aggiorna la cache in memoria

            // Salva nel database aprendo una connessione temporanea
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Options.Where(x => x.Name == opt)
                .ExecuteUpdateAsync(o => o.SetProperty(x => x.Value, stringValue));    
        }
    }

    public Option? Get(string opt)
    {
        return Opt.FirstOrDefault(x => x.Name == opt);
    }

    // Rimosso "where T : struct" qui, così puoi fare GetValue<string>(...)
    public T? GetValue<T>(string opt) 
    {
        Option? option = Get(opt);
        return option != null ? option.Get<T>() : default;
    }
}

public class Option
{
    public static class Email
    {
        public static readonly string SMTP_USER = "smtp_user";
        public static readonly string SMTP_PASS = "smtp_password";
        public static readonly string SMTP_PORT = "smtp_port";
        public static readonly string SMTP_HOST = "smtp_host";
    }

    public Option() {}
    public Option(string name) { Name = name; }
    public Option(string name, string defaultValue) { Name = name; Value = defaultValue; }
    
    public string Name { get; set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public DateTime LastUpdateUtc {get;set;} = DateTime.UtcNow;

    // Overload per le stringhe (usato da UpdateInternal)
    public void SetValue(string v) 
    {
        Value = v;
        LastUpdateUtc = DateTime.UtcNow;
    }

    public T? Get<T>()
    {
        if(string.IsNullOrEmpty(Value)) return default;

        Type t = typeof(T);

        // Se richiedi direttamente una stringa, restituiscila
        if (t == typeof(string))
        {
            return (T)(object)Value;
        }

        var underType = Nullable.GetUnderlyingType(t);
        if(underType is not null) t = underType;

        if(t.IsEnum) return (T)Enum.Parse(t, Value, true);

        return (T)Convert.ChangeType(Value, t, CultureInfo.InvariantCulture);
    }
}