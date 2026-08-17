using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Askii.Common.Extensions;

public static class EmailStringExtensions
{
    public static string NormalizeEmail(this string str)
    {
        return str.ToLowerInvariant().Trim();
    }

    public static bool IsValidEmail(this string? str)
    {
        if(str is null) return false;
        return MailAddress.TryCreate(str, out _);
    }
}