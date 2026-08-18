namespace Askii.Common.Security;

/// <summary>
/// Base32 secondo RFC 4648, senza padding.
///
/// Serve perché le app di authenticator scambiano il segreto TOTP in questa
/// codifica: è l'alfabeto che i QR e l'inserimento manuale si aspettano.
/// </summary>
public static class Base32
{
    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(ReadOnlySpan<byte> dati)
    {
        if (dati.IsEmpty) return string.Empty;

        var risultato = new System.Text.StringBuilder((dati.Length * 8 + 4) / 5);
        int buffer = 0, bitDisponibili = 0;

        foreach (var b in dati)
        {
            buffer = (buffer << 8) | b;
            bitDisponibili += 8;

            while (bitDisponibili >= 5)
            {
                risultato.Append(Alfabeto[(buffer >> (bitDisponibili - 5)) & 31]);
                bitDisponibili -= 5;
            }
        }

        // I bit rimasti si completano a destra con zeri.
        if (bitDisponibili > 0)
        {
            risultato.Append(Alfabeto[(buffer << (5 - bitDisponibili)) & 31]);
        }

        return risultato.ToString();
    }

    /// <summary>
    /// Tollerante per come il segreto arriva dagli utenti: ignora spazi,
    /// padding e differenze di maiuscole, perché viene spesso incollato a mano.
    /// </summary>
    public static byte[] Decode(string base32)
    {
        if (string.IsNullOrWhiteSpace(base32)) return [];

        var pulito = base32.Replace(" ", "").Replace("-", "").TrimEnd('=').ToUpperInvariant();

        var byteAttesi = pulito.Length * 5 / 8;
        var risultato = new byte[byteAttesi];

        int buffer = 0, bitDisponibili = 0, scritti = 0;

        foreach (var c in pulito)
        {
            var valore = Alfabeto.IndexOf(c);
            if (valore < 0) throw new FormatException($"Carattere '{c}' non valido in Base32.");

            buffer = (buffer << 5) | valore;
            bitDisponibili += 5;

            if (bitDisponibili >= 8)
            {
                risultato[scritti++] = (byte)((buffer >> (bitDisponibili - 8)) & 0xFF);
                bitDisponibili -= 8;
            }
        }

        return risultato;
    }
}
