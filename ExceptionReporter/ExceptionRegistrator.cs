using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using Inmeta.Exception.Reporter;
using Inmeta.Exception.ReportUI.WPF;
using Kongsberg.Nemo.ExceptionReporter.Properties;
using Osiris.Exception.Reporter;

namespace Kongsberg.Nemo.ExceptionReporter
{
    public static class ExceptionRegistrator
    {
        //store previous exception to avoid recursive reporting
        private static Exception _previousException;

        internal static bool _tryContinueAfterException = true;
        internal static bool _showExitAppWindow = true;
        internal static ReportForm _form;
        private static readonly object _syncObject = new object();

        /// <summary>
        ///     The application name.
        /// </summary>
        internal static string ApplicationName { get; set; }

        /// <summary>
        ///     Get Reporter, SAME AS NEMO, but more robust.
        /// </summary>
        internal static string Reporter
        {
            get
            {
                try
                {
                    //same as NEMO, but with exception handling.
                    WindowsIdentity currentNtUser = WindowsIdentity.GetCurrent();
                    string customer = string.IsNullOrEmpty(CustomerName) ? "" : CustomerName + "\\";
                    string user = currentNtUser != null ? currentNtUser.Name : "n/a";

                    return string.Format("{0}{1}", customer, user);
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
        ///     Get VERSION, SAME AS NEMO, but more robust.
        /// </summary>
        internal static string Version
        {
            get
            {
                string version;
                try
                {
                    //SAME AS NEMO, but with exception handling
                    version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
                catch (Exception privEx)
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
        private static Exception TheException { get; set; }

        public static string CustomerName { get; set; }

        /// <summary>
        ///     Call this to register the exception trapper.
        ///     This function should be the first function called in your application, it MUST be called before any forms are
        ///     created.
        ///     Good  practice is to call it before Application.Run().
        /// </summary>
        public static void Register(bool tryContinueAfterException = true, bool showExitAppWindow = true)
        {
            Contract.Ensures(_form != null);

            _tryContinueAfterException = tryContinueAfterException;
            _showExitAppWindow = showExitAppWindow;

            //get the registered form type.
            _form = new ReportForm();
            _form.RegisterExceptionEvents(OnException);

            //For nemo allways use KongsbergNemo.
            ApplicationName = "KongsbergNemo";
        }


        /// <summary>
        ///     Call this to register the exception trapper when you have a diffent application name than the default "Kongsberg.Nemo"
        ///     This function should be the first function called in your application, it MUST be called before any forms are
        ///     created.
        ///     Good  practice is to call it before Application.Run().
        /// </summary>
        public static void Register(string applicationName, bool tryContinueAfterException = true,
            bool showExitAppWindow = true)
        {
            Register(tryContinueAfterException,showExitAppWindow);
            ApplicationName = applicationName;
        }

        /// <summary>
        /// </summary>
        /// <param name="use"></param>
        public static void UseReportGUI(bool use)
        {
            Settings.Default.SetUseReportingUI = use;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static bool OnException(Exception e, bool isTerminating)
        {
            if (ExceptionReporting != null)
                ExceptionReporting(e, EventArgs.Empty);

            bool fromSTA = true;

            //avoid recursive reporting when IsTerminating is true, since we have registered both Main Form and AppDomain with unhandled exceptions
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                if (_previousException != null && e != null && _previousException.ToString() == e.ToString())
                {
                    ReportLogger.LogInfo(new ArgumentException("Trying to report on the same excpetion.", e).ToString());
                    //same as previous
                    return _tryContinueAfterException;
                }

                _previousException = e;
            }

            ReportLogger.LogInfo("Received exception  (isTerminating = " + isTerminating + ")");

            //set the excpetion, SAME as NEMO (private static field excpetion)
            TheException = e;

            try
            {
#if (RELEASE)
    //use ExceptionRegistrator.UseReportGUI to control if UI is to be used or not.
                if (!Kongsberg.Nemo.ExceptionReporter.Properties.Settings.Default.UseReportingUI)
                {
                    ReportExceptionWithNoGUI(Reporter, Version, ApplicationName, e);
                    return _tryContinueAfterException;
                }
#endif
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
                    //Same as NEMO project:
                    string errorText = CreateExceptionText(e);

                    //show the error to the user and collect a description of the error from the user.
                    try
                    {
                        //show the exception using the registered IReportForm.
                        //provide the text to display: Same as NEMO project, but now the form is not part of this assembly so we generate it and provide it to the form.
                        //again same as NEMO project.
                        //if return value is false -> cancel
                        if (!_form.ShowException(errorText,
                            //this callback is to be used when the form click on the Send button.    
                            //same as NEMO btnPost_Click
                            DoPost))
                        {
                            //cancel report exception to log
                            LogToFile(e);
                        }
                    }
                    catch (Exception ex)
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

        private static void LogToFile(Exception e)
        {
            var report = new TFSExceptionReport(ApplicationName, Reporter, Reporter, e, Version,
                "Exception reported w/o description");

            ReportLogger.LogToFile(report);
        }

        public static void ReportInSTA(Exception e, bool isTerminating)
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
                //Same as NEMO:
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
                Exception result = report.Post();

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
                    catch (Exception ex)
                    {
                        //failed to show delivery failure... just log 
                        ReportLogger.LogExceptionsDuringDelivery(
                            new InvalidOperationException("Failed to show delivery exception", ex));
                    }
                }
            }
            catch (Exception ex)
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
        ///     This code is the same as from Nemo project, but more robust.
        /// </summary>
        internal static string CreateExceptionText(Exception e)
        {
            Contract.Requires(e != null);

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

        private static string FormStringFromException(Exception ex)
        {
            string name = ex.GetType().Name;

            if (name.Contains("ParseMessageException"))
                return string.Format("{0}\n{1}", ex.Message, ex.StackTrace ?? String.Empty);

            return string.Format("Application: {0}\nException message: {1}\nType: {2}\n{3}",
                Assembly.GetEntryAssembly().GetName().Name, ex.Message, ex.GetType(), ex.StackTrace ?? String.Empty);
        }

        public static Exception GetMostInnerException(Exception e)
        {
            Exception temp = e;

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