using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.SharePoint.Administration;

namespace SkillWillCorp.SP.Offices
{
    public static class Logger
    {
        [DllImport("advapi32.dll")]
        private static extern uint EventActivityIdControl(uint controlCode, ref Guid activityId);
        private const uint EVENT_ACTIVITY_CTRL_GET_ID = 1;
        private const string AREA = "SkillWillCorp";
        private const string CATEGORY_ERROR = "SWCError";
        private const string CATEGORY_MESSAGE = "SWCMessage";


        /// <summary>
        /// Specifies the message (message) the desired format error message and returns a token
        /// </summary>
        /// <param name="message">The simple message text</param>
        /// <param name="exceptionMessage">Full message text</param>
        /// <param name="traceSeverity">Specifies the level of trace data that is written to the trace log file.</param>
        public static Guid WriteError(string message, string exceptionMessage, TraceSeverity traceSeverity)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            message = string.Format("SkillWillMessage: {0};", (string.IsNullOrEmpty(message) == false ? message : "Неизвестная ошибка")) +
                 exceptionMessage;

            SPDiagnosticsCategory category = new SPDiagnosticsCategory(Logger.CATEGORY_ERROR, TraceSeverity.High, EventSeverity.Error);
            SPDiagnosticsArea area = new SPDiagnosticsArea(Logger.AREA, new SPDiagnosticsCategory[] { category });
            SPDiagnosticsService.Local.WriteTrace(0, area.Categories[Logger.CATEGORY_ERROR], traceSeverity, message);
           
            return Logger.GetCurrentCorrelationToken();
        }

        /// <summary>
        /// It prints an error message and returns a token
        /// </summary>
        /// <param name="message">The simple message text</param>
        /// <param name="ex">Full message text</param>
        /// <param name="traceSeverity">Specifies the level of trace data that is written to the trace log file.</param>
        public static Guid WriteError(string message, Exception ex, TraceSeverity traceSeverity)
        {
            return Logger.WriteError(message, Logger.GetMessageFromException(ex), traceSeverity);
        }

        /// <summary>
        /// Prints a message of the specified format with the substituted arguments and returns a token
        /// </summary>
        /// <param name="formatWithFormat">The format of the returned message</param>
        /// <param name="args">Arguments for substitution</param>
        public static Guid WriteMessage(string formatWithFormat, params object[] args)
        {
            return WriteMessage(String.Format(formatWithFormat, args));
        }

        /// <summary>
        /// Print the message and returns a token
        /// </summary>
        /// <param name="message">Message to print</param>
        public static Guid WriteMessage(string message)
        {
            var category = new SPDiagnosticsCategory(Logger.CATEGORY_MESSAGE, TraceSeverity.Medium, EventSeverity.Information);
            var area = new SPDiagnosticsArea(Logger.AREA, new SPDiagnosticsCategory[] { category });
            SPDiagnosticsService.Local.WriteTrace(0, area.Categories[Logger.CATEGORY_MESSAGE], TraceSeverity.Medium, message);
            return Logger.GetCurrentCorrelationToken();
        }

        /// <summary>
        /// Generates and returns a message to the specified exception
        /// </summary>
        /// <param name="ex">An exception to retrieve a message from him</param>
        public static string GetMessageFromException(Exception ex)
        {
            return (ex != null
                ? string.Format("ExceptionMessage: {0}; {1}StackTrace {2};",
                    ex.Message,
                    (ex.InnerException != null
                        ? string.Format(" InnerExceptionMessage: {0}; ", ex.InnerException.Message)
                        : string.Empty),
                    ex.StackTrace)
                : string.Empty);
        }

        /// <summary>
        /// Returns the current correlation token
        /// </summary>
        public static Guid GetCurrentCorrelationToken()
        {
            Guid correlationToken = Guid.Empty;
            EventActivityIdControl(EVENT_ACTIVITY_CTRL_GET_ID, ref correlationToken);
            return correlationToken;
        }
    }
}
