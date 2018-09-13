using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using AzureDevOpsTools.ExceptionReporter;
using AzureDevOpsTools.Exception.ReportUI.WPF;

namespace AzureDevOpsTools.ExceptionReporter
{
    /// <summary>
    /// Use to enable registration of handler
    /// </summary>
    public static class ExceptionRegistrator
    {
        //store previous exception to avoid recursive reporting
        private static System.Exception _previousException;

        internal static bool _tryContinueAfterException = true;
        internal static bool _showExitAppWindow = true;
        internal static ReportForm _form;
        private static readonly object _syncObject = new object();
        private static bool useReportGUI = true;

        /// <summary>
        /// Can be set to true to disable posting to the production TFS server.
        /// Useful if used for testing.
        /// </summary>
        public static bool DoNotSend { get; set; } 

        /// <summary>
        ///     The application name.
        /// </summary>
        internal static string ApplicationName { get; set; }

        /// <summary>
        /// </summary>
        internal static string Reporter
        {
            get
            {
                try
                {
                    var currentNtUser = WindowsIdentity.GetCurrent();
                    string customer = string.IsNullOrEmpty(CustomerName) ? "" : CustomerName + "\\";
                    string user = currentNtUser != null ? currentNtUser.Name : "n/a";

                    return $"{customer}{user}";
                }
                catch (SecurityException privEx)
                {
                    const string temp = "Insufficient privileges to extract the current user.";

                    ReportLogger.LogExceptionsDuringDelivery(
                        new UnauthorizedAccessException(temp, privEx));

                    return temp;
                }
            }
        }

        /// <summary>
        /// </summary>
        public static string Version
        {
            get
            {
                string version;
                try
                {
                    version = Assembly.GetCallingAssembly().GetName().Version.ToString();
                }
                catch (System.Exception privEx)
                {
                    version =
                        "Insufficient privileges to extract the correct assembly version";
                    ReportLogger.LogExceptionsDuringDelivery(
                        new UnauthorizedAccessException(version, privEx));
                }

                return version;
            }
        }

        /// <summary>
        ///     The exception being reported.
        /// </summary>
        private static System.Exception TheException { get; set; }

        /// <summary>
        /// Customers name
        /// </summary>
        public static string CustomerName { get; set; }

        /// <summary>
        ///     Call this to register the exception trapper.
        ///     This function should be the first function called in your application, it MUST be called before any forms are
        ///     created.
        ///     Good  practice is to call it before Application.Run().
        /// </summary>
        public static void Register(bool tryContinueAfterException = true, bool showExitAppWindow = true)
        {
            _tryContinueAfterException = tryContinueAfterException;
            _showExitAppWindow = showExitAppWindow;

            //get the registered form type.
            _form = new ReportForm();
            _form.RegisterExceptionEvents(OnException);

            ApplicationName = "AzureDevOpsTools.TestApp";
        }

        /// <summary>
        ///     Use this method for better performance results in pure Windows forms application
        ///     Call this to register the exception trapper.
        ///     This function should be the first function called in your application, it MUST be called before any forms are
        ///     created.
        ///     Good  practice is to call it before Application.Run().
        /// </summary>
        public static void RegisterWinFormsApplication(bool tryContinueAfterException = true, bool showExitAppWindow = true)
        {
            _tryContinueAfterException = tryContinueAfterException;
            _showExitAppWindow = showExitAppWindow;

            //get the registered form type.
            _form = new ReportForm();
            _form.RegisterWindowsFormsExceptionEvents(OnException);

            ApplicationName = "AzureDevOpsTools.TestApp";
        }


        /// <summary>
        ///     Call this to register the exception trapper when you have a diffent application name than the default "AzureDevOpsTools"
        ///     This function should be the first function called in your application, it MUST be called before any forms are
        ///     created.
        ///     Good  practice is to call it before Application.Run().
        ///     Application Name should uniquely define your application, and will also be used as the folder name.  Avoid spaces and special characters
        /// </summary>
        public static void Register(string applicationName, bool tryContinueAfterException = true,
            bool showExitAppWindow = true)
        {
            Register(tryContinueAfterException,showExitAppWindow);
            ApplicationName = applicationName;
        }

        /// <summary>
        /// Use normally to suppress use of GUI for services and other non-UI applications
        /// </summary>
        /// <param name="use"></param>
        public static void UseReportGUI(bool use)
        {
            useReportGUI = use;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static bool OnException(System.Exception e, bool isTerminating)
        {
            ExceptionReporting?.Invoke(e, EventArgs.Empty);
            var proc = Process.GetCurrentProcess();
            bool fromSTA = true;

            //avoid recursive reporting when IsTerminating is true, since we have registered both Main Form and AppDomain with unhandled exceptions
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                if (_previousException != null && e != null && _previousException.ToString() == e.ToString())
                {
                    ReportLogger.LogInfo(new ArgumentException("Trying to report on the same exception.", e).ToString());
                    //same as previous
                    return _tryContinueAfterException;
                }

                _previousException = e;
            }

            ReportLogger.LogInfo("Received exception  (isTerminating = " + isTerminating + ")");

            TheException = e;

            try
            {
                //use ExceptionRegistrator.UseReportGUI to control if UI is to be used or not.
                if (!useReportGUI)
                {
                    ReportExceptionWithNoGUI(Reporter, Version, ApplicationName, e);
                    return false;
                }
                // XAML issue with MTA threads.
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    LogToFile(e);
                    ReportInSTA(e, isTerminating);
                    fromSTA = false;
                    return _tryContinueAfterException;
                }

                //only report one exception at the time.
                lock (_syncObject)
                {
                    string errorText = CreateExceptionText(e);

                    //show the error to the user and collect a description of the error from the user.
                    try
                    {
                        //show the exception using the registered IReportForm.
                        //if return value is false -> cancel
                        if (!_form.ShowException(errorText,
                            //this callback is to be used when the form click on the Send button.    
                            DoPost))
                        {
                            //cancel report exception to log
                            LogToFile(e);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        //log report exception 
                        var report = new TFSExceptionReport(ApplicationName, Reporter, Reporter, e, Version,
                            "THIS IS A AUTO GENERATED TEXT: Failed to show exception report.");

                        ReportLogger.LogToFile(report);

                        ReportLogger.LogExceptionsDuringDelivery(
                            new InvalidOperationException("Failed to show exception report.", ex));
                    }
                }
            }
            catch (ThreadAbortException terminate)
            {
                try
                {
                    _form.Window.Close();
                }
                catch
                {
                    //ignore...
                }
                ReportLogger.LogInfo(new ThreadStateException("Report form is terminating.", terminate).ToString());
            }
            finally
            {
                //we should inform the user that the application is about to terminate.
                // currently only support for WPF.
                if (_showExitAppWindow && isTerminating && fromSTA && Application.Current != null)
                {
                    try
                    {
                        var terminateWindow = new IsTerminatingWindow();
                        terminateWindow.Topmost = true;
                        terminateWindow.Show();

                        //sleep for 5000 seconds.
                        Thread.Sleep(5000);

                        terminateWindow.Close();
                    }
                    catch
                    {
                        //for now ignore ... this will happen if thread is MTA...but no more time to code. 
                    }
                }
            }

            return _tryContinueAfterException;
        }

        /// <summary>
        /// report W/O GUI. 
        /// </summary>
        /// <param name="currentNtUser"></param>
        /// <param name="version"></param>
        /// <param name="applicationName"></param>
        /// <param name="e"></param>
        internal static void ReportExceptionWithNoGUI(string currentNtUser, string version, string applicationName, System.Exception e)
        {
            try
            {
                //ignore result since we are not using any UI to provide user feedback.
                //any errors will be logged by the Post function.
                var report =
                    new TFSExceptionReport(
                        applicationName,
                        currentNtUser,
                        currentNtUser, e,
                        version,
                        "Exception reported w/o description");

                ReportLogger.LogToFile(report);

                report.Post();
            }
            catch (System.Exception ex)
            {
                ReportLogger.LogExceptionsDuringDelivery(new System.Exception("Failed to deliver exception (no GUI)", ex));
            }
        }

        public static string FindVersion()
        {
            return Version;
        }


        private static void LogToFile(System.Exception e)
        {
            var report = new TFSExceptionReport(ApplicationName, Reporter, Reporter, e, Version,
                "Exception reported w/o description");

            ReportLogger.LogToFile(report);
        }

        public static void ReportInSTA(System.Exception e, bool isTerminating)
        {
            ReportLogger.LogInfo("Need to spawn own STA thread.");
            var staReportFormThread = new Thread(() => OnException(e, isTerminating));

            staReportFormThread.SetApartmentState(ApartmentState.STA);
            staReportFormThread.Start();
            staReportFormThread.Join();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void DoPost(string description)
        {
            try
            {
                //create exception entity
                var report = new TFSExceptionReport
                    (
                    ApplicationName,
                    Reporter,
                    Reporter, TheException,
                    Version,
                    description);

                //log to file
                ReportLogger.LogToFile(report);

                //post to service. 
                var result = (!DoNotSend) ? report.Post() : null;

                ReportLogger.LogInfo("Result posted");
                //if error show to user.
                if (result != null)
                {
                    ReportLogger.LogExceptionsDuringDelivery(
                        new FileLoadException(
                            "Failed to deliver exception to url = '" + ServiceSettings.ServiceUrl + "'", result));
                    try
                    {
                        //failed to deliver exception display for user.
                        _form.ShowDeliveryFailure(result.Message, result);
                    }
                    catch (System.Exception ex)
                    {
                        //failed to show delivery failure... just log 
                        ReportLogger.LogExceptionsDuringDelivery(
                            new InvalidOperationException("Failed to show delivery exception", ex));
                    }
                }
            }
            catch (System.Exception ex)
            {
                ReportLogger.LogExceptionsDuringDelivery(
                    new FileLoadException("Exception during TFS exception report create or post", ex));
            }
        }

        /// <summary>
        ///     Should be called when the application is exiting. Added for interface symmetri.
        /// </summary>
        public static void UnRegister()
        {
            _form.UnRegister();
        }

        /// <summary>
        ///     Creates a string formed by exception and inner exceptions.
        /// </summary>
        internal static string CreateExceptionText(System.Exception e)
        {
            var errorText = new StringBuilder();
            errorText.Append(FormStringFromException(e));

            while (e.InnerException != null)
            {
                e = e.InnerException;
                string textToAppend = FormStringFromException(e);

                if (textToAppend.Length > 0)
                {
                    if (errorText.Length > 0)
                        errorText.Insert(0, textToAppend + Environment.NewLine + "Error: ");
                    else
                        errorText.Append(textToAppend);
                }
            }

            return errorText.ToString();
        }

        private static string FormStringFromException(System.Exception ex)
        {
            string name = ex.GetType().Name;

            if (name.Contains("ParseMessageException"))
                return $"{ex.Message}\n{ex.StackTrace ?? String.Empty}";

            return
                $"Application: {Assembly.GetEntryAssembly().GetName().Name}\nException message: {ex.Message}\nType: {ex.GetType()}\n{ex.StackTrace ?? String.Empty}";
        }

        public static System.Exception GetMostInnerException(System.Exception e)
        {
            System.Exception temp = e;

            while (temp.InnerException != null)
                temp = temp.InnerException;

            return temp;
        }


        public static void Configure(Tuple<Uri, string> exceptionConfiguration)
        {
            if (exceptionConfiguration != null)
            {
                ServiceSettings.ServiceUrl = exceptionConfiguration.Item1;
                CustomerName = exceptionConfiguration.Item2;
            }
        }

        public static event EventHandler ExceptionReporting;
    }
}