using System;

namespace Osiris.Exception.Reporter
{

    /// <summary>
    /// Delegate used to decorate an exception with a description, the name of the reporter and the application name.
    /// </summary>
    /// <param name="description">A description of the exception and the steps needed to reproduce the error</param>
    public delegate void ReportException(string description);

    /// <summary>
    /// this interface represents the instance for 
    /// </summary>
    public interface IReportForm
    {
        /// <summary>
        /// Register all exception events which will automatically trigger a callback.
        /// </summary>
        /// <param name="callback">All caught exceptions shall be forwarded to the provided callback.</param>
        void RegisterExceptionEvents(Func<System.Exception, bool, bool> callback);

        /// <summary>
        /// Show the error in the form with the provided text as the error.
        /// </summary>
        /// <param name="errorText">The error text to display.</param>
        /// <param name="report">Report is the delegate to send the report to the TFS reporting service. <see cref="ReportException"/></param>
        /// <returns> False if cancel button was pushed.</returns>
        bool ShowException(string errorText, ReportException report);

        /// <summary>
        /// This method will be called if a exception occurs during the ingestion into the TFS Exception report service.
        /// </summary>
        /// <param name="deliveryException">The exception causing failure</param>
        /// <param name="message">The message to display to the user.</param>
        void ShowDeliveryFailure(string message, System.Exception deliveryException);


        void UnRegister();
    }
}
