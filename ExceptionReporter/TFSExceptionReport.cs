using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using AzureDevOpsTools.Exception.Common;

namespace AzureDevOpsTools.ExceptionReporter
{
    /// <summary>
    /// This class allows you to post exception reports over the internet.
    /// </summary>
    [Serializable]
    internal class TFSExceptionReport
    {
        internal ExceptionEntity ExceptionEntity { get; set; }

        /// <summary>
        /// Create a new exception repoert item.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="reporter"></param>
        /// <param name="username"></param>
        /// <param name="ex"></param>
        /// <param name="version"></param>
        /// <param name="description">Step to reproduce the error.</param>
        public TFSExceptionReport(string applicationName, string reporter, string username, System.Exception ex, string version, string description)
        {
            //ensure contracts.
            Contract.Requires(String.IsNullOrEmpty(applicationName));
            Contract.Requires(String.IsNullOrEmpty(reporter));
            Contract.Requires(String.IsNullOrEmpty(username));
            Contract.Requires(ex != null);

            //ensure contracts.
            Contract.Ensures(ExceptionEntity != null);
            Contract.Ensures(String.IsNullOrEmpty(ExceptionEntity.ApplicationName));
            Contract.Ensures(String.IsNullOrEmpty(ExceptionEntity.Reporter));
            Contract.Ensures(String.IsNullOrEmpty(ExceptionEntity.Username));

            var stackTrace = ExceptionRegistrator.CreateExceptionText(ex);
            var title = GetTitle(ex);
            var message = title;
            var exceptionClass = GetExceptionClass(ex);
            var exceptionMethod = GetExceptionMethod(ex);

            ExceptionEntity = new ExceptionEntity
                                   {
                                       ApplicationName = applicationName,
                                       Reporter = reporter,
                                       Username = username,
                                       Version = version,
                                       ExceptionSource = ex.Source ?? String.Empty,
                                        ExceptionClass = exceptionClass,
                ExceptionMethod = exceptionMethod,
                                       StackTrace = stackTrace,
                                       Comment = description,
                                       ExceptionMessage = message,
                                       ExceptionType = ex.GetType().ToString(),
                                       ExceptionTitle = title
                                   };

        }

        private static string GetExceptionMethod(System.Exception ex)
        {
            string exceptionMethod;
            try
            {
                exceptionMethod = (ex.TargetSite ?? MethodBase.GetCurrentMethod()).Name;
            }
            catch (TargetException)
            {
                exceptionMethod = "N/A";
                ReportLogger.LogExceptionsDuringDelivery(new TargetException("Class is late bound, cannot determine class", ex));
            }
            return exceptionMethod;
        }

        private static string GetExceptionClass(System.Exception ex)
        {
            string exceptionClass;
            try
            {
                var declaring = (ex.TargetSite ?? MethodBase.GetCurrentMethod()).DeclaringType;
                exceptionClass = (declaring == null) ? "Global Method" : declaring.FullName;
            }
            catch (TargetException)
            {
                ReportLogger.LogExceptionsDuringDelivery(new TargetException("Class is late bound cannot determine class", ex));
                exceptionClass = "N/A";
            }
            return exceptionClass;
        }

        private static string GetTitle(System.Exception ex)
        {
            var innerEx = ExceptionRegistrator.GetMostInnerException(ex);
            var splittedTitle = innerEx.StackTrace.Split(
                new[] { " in " }, StringSplitOptions.RemoveEmptyEntries).First().Trim().Split(
                    new[] { " at " }, StringSplitOptions.RemoveEmptyEntries);

            var title = GetFirstLine(splittedTitle).Trim().Split(
                        new[] { "(" }, StringSplitOptions.RemoveEmptyEntries).First().Trim();
            title = innerEx.Message.Trim('.') + (title.Trim().StartsWith("at") ? " " : " at ") + title.Trim();
            title = TFSStringUtil.GenerateValidTFSStringType(title);
            return title;
        }

        private static string GetFirstLine(string[] splittedTitle)
        {

            return splittedTitle.First();
        }

        /// <summary>
        /// Post the exception report to the service.
        /// </summary>
        /// <returns>If Post fails to deliver the report, the reason is represented in the returning exception. If success return value is null</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal System.Exception Post()
        {
            try
            {
                // Check for settings overrides
                var serviceUrl = ServiceSettings.ServiceUrl.OriginalString.ToLower();

                //TODO: CAll service here

            }
            catch (System.Exception e)
            {
                ReportLogger.LogExceptionsDuringDelivery(e);

                //change in behavior: exception is now returned on failure.
                return e;
            }

            return null;
        }
    }
}
