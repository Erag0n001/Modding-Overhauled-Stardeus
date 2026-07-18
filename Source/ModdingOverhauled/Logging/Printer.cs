using System.Diagnostics;
using KL.Utils;

namespace ModdingOverhauled.Logging
{
    internal static class Printer
    {
        private const string LogMessage = "[MO]> ";
#if DEBUG
        private const string LogMessageDebug = "[MO|Debug]> ";
#endif
        public static void Warn(object toLog)
        {
            D.Warn(LogMessage + (toLog?.ToString() ?? "null"));
        }
        public static void Error(object toLog)
        {
            D.Err(LogMessage + (toLog?.ToString() ?? "null"));
        }

        [Conditional("DEBUG")]
        public static void Debug(object toLog) {
            D.Warn(LogMessageDebug + (toLog?.ToString() ?? "null"));
        }
    }
}
