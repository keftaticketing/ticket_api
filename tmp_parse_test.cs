using System;
using System.Globalization;
var s1 = "2026-06-21T08:00:00+03:00";
var s2 = "2026-06-21T08:00:00 03:00";
Console.WriteLine("s1 TryParse: " + DateTimeOffset.TryParse(s1, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d1) + " -> " + d1);
Console.WriteLine("s2 TryParse: " + DateTimeOffset.TryParse(s2, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d2) + " -> " + d2);
Console.WriteLine("s1 DateOnly exact: " + DateOnly.TryParseExact(s1, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var do1));
Console.WriteLine("s2 DateOnly exact: " + DateOnly.TryParseExact(s2, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var do2));
