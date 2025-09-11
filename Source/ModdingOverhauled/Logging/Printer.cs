using KL.Utils;

namespace ModdingOverhauled.Logging
{
    internal static class Printer
    {
        private static readonly string LogMessage = "[MO]> ";
        public static void Warn(object toLog)
        {
            D.Warn(LogMessage + (toLog?.ToString() ?? "null"));
        }
        public static void Error(object toLog)
        {
            D.Err(LogMessage + (toLog?.ToString() ?? "null"));
        }
    }
}
