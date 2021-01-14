using System;
using System.Text.RegularExpressions;

namespace DL444.Ucqu.Library.Client
{
    internal static class MatchExtension
    {
        public static string FirstGroupValue(this Match match) => match.Groups[1].Value;
    }
}
