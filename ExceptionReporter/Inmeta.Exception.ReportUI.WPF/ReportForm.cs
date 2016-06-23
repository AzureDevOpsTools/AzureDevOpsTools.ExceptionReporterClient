using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Kongsberg.Nemo.ExceptionReporter.Properties;
using Osiris.Exception.Reporter;
using Application = System.Windows.Application;

namespace Inmeta.Exception.ReportUI.WPF
{
    public class ReportForm : IReportForm
    {
        /// <summary>
        /// objects used for sync locking. 
        /// </summary>
        private object syncRoot = new object();

        public void RegisterExceptionEvents(Func<System.Exception, bool, bool> callback)
        {
            //How to handle unhandled excpetions in WPF:
            //see: http://msdn.microsoft.com/en-us/library/system.windows.application.dispatcherunhandledexception.aspx
            //but unhandled 
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException +=
                    (sender, args) =>
                        {
                            lock (this.syncRoot)
                            {
                                //True: application will continue.
                                //False: default unhandled exception processing
                                args.Handled = CanBeSafelySkipped(args) || callback(args.Exception, !args.Handled);
                            }
                        };
            }

            //catch exceptions.
            System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            //since WPF DispatcherUnhandledException do not hook on child thread, register with Appdomain unhandled Exceptions register with Appdomain 
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) =>
                    {
                        lock (this.syncRoot)
                        {
                            if (!callback(args.ExceptionObject as System.Exception, args.IsTerminating))
                            {
                                if (Application.Current != null)
                                    Application.Current.Shutdown();
                                else 
                                    System.Windows.Forms.Application.Exit();
                            }
                        }
                    };
            // some apps has it's own app domain exception registator, and the only way to check it is "try"
        }

        /// <summary>
        /// Supresses some exceptions temporary.
        /// Rarely on close we have "Exception message: Dispatcher processing has been suspended, but messages are still being processed.Type: System.InvalidOperationException"
        /// which is not important and can be safely skipped. It is unclear why we have it for now.
        /// </summary>
        private static bool CanBeSafelySkipped(DispatcherUnhandledExceptionEventArgs args)
        {
            return args.Exception is InvalidOperationException && 
                args.Exception.Message.Contains("Dispatcher processing has been suspended, but messages are still being processed.");
        }

        internal ReportFormUI Window {get; set;} 

        public bool ShowException(string errorText, ReportException report)
        {
            bool posted = false;
            Window = new ReportFormUI();
            Window.txtError.Text = errorText;
            Window.btnPost.Click += 
                (sender, args) =>
                {
                    try
                    {
                        var text = Window.txtDescription == null ? "" : Window.txtDescription.Text;
                        report(text);
                        posted = true;
                        Window.Close();
                    }
                    catch (System.Exception)
                    {
                        //failure 
                        //Nothing relevant to report.
                    }
                };

            Window.Topmost = true;
            Window.ShowDialog();

            Window = null;
            return posted;
        }

        public void ShowDeliveryFailure(string message, System.Exception deliveryException)
        {
            System.Windows.MessageBox.Show(
                Resources.URL + Resources.Colon + ServiceSettings.ServiceUrl + Environment.NewLine + message, 
                Resources.DeliveryFailure, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void UnRegister()
        {
            //nothing to do unregister.
        }
    }
}
