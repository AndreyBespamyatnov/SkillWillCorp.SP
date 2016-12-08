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


        /// <summary>Задает сообщению(message) нужный формат сообщение об ошибке и возвращает токен
        /// </summary>
        /// <param name="message">Текст ошибки, понятный даже консультантам</param>
        /// <param name="exceptionMessage">Полное сообщение исключения</param>
        /// <param name="traceSeverity">Задает уровень данных трассировки, который записывается в файл журнала трассировки.</param>
        /// <returns></returns>
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

        /// <summary>Печатает сообщение об ошибке и возвращает токен
        /// </summary>
        /// <param name="message">Текст ошибки, понятный даже консультантам</param>
        /// <param name="ex">Полное сообщение исключения</param>
        /// <param name="traceSeverity">Задает уровень данных трассировки, который записывается в файл журнала трассировки.</param>
        /// <returns></returns>
        public static Guid WriteError(string message, Exception ex, TraceSeverity traceSeverity)
        {
            return Logger.WriteError(message, Logger.GetMessageFromException(ex), traceSeverity);
        }

        /// <summary>Печатает сообщение указанного формата с подставленными аргументами 
        /// и возвращает токен
        /// </summary>
        /// <param name="formatWithFormat">Формат, возвращаемого сообщения</param>
        /// <param name="args">Аргументы для подстановки</param>
        /// <returns></returns>
        public static Guid WriteMessage(string formatWithFormat, params object[] args)
        {
            return WriteMessage(String.Format(formatWithFormat, args));
        }

        /// <summary>Печатает сообщение и возвращает токен
        /// </summary>
        /// <param name="message">Сообщение для печати</param>
        /// <returns></returns>
        public static Guid WriteMessage(string message)
        {
            SPDiagnosticsCategory category = new SPDiagnosticsCategory(Logger.CATEGORY_MESSAGE, TraceSeverity.Medium, EventSeverity.Information);
            SPDiagnosticsArea area = new SPDiagnosticsArea(Logger.AREA, new SPDiagnosticsCategory[] { category });
            SPDiagnosticsService.Local.WriteTrace(0, area.Categories[Logger.CATEGORY_MESSAGE], TraceSeverity.Medium, message);
            return Logger.GetCurrentCorrelationToken();
        }

        /// <summary>Генерирует и возвращает сообщение по указанном исключению
        /// </summary>
        /// <param name="ex">Исключение для извлечения сообщения из него</param>
        /// <returns></returns>
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

        /// <summary>Возвращает текущий токен корреляции
        /// </summary>
        /// <returns></returns>
        public static Guid GetCurrentCorrelationToken()
        {
            Guid correlationToken = Guid.Empty;
            EventActivityIdControl(EVENT_ACTIVITY_CTRL_GET_ID, ref correlationToken);
            return correlationToken;
        }
    }
}
