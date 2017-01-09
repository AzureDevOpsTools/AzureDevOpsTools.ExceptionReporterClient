using System;
using System.Runtime.Serialization;
using System.Windows;

namespace TestApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
           
        }

        private void Bang(object sender, RoutedEventArgs e)
        {
            throw new DeliberateException();
        }
    }


    [Serializable]
    public class DeliberateException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public DeliberateException()
        {
        }

        public DeliberateException(string message) : base(message)
        {
        }

        public DeliberateException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DeliberateException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
