using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Caliburn.Micro;
using Exceptionless;

namespace TestExceptionLess
{
    public class ShellViewModel : PropertyChangedBase
    {
        string name;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => CanSayHello);
            }
        }

        public bool CanSayHello
        {
            get { return !string.IsNullOrWhiteSpace(Name); }
        }

        public void SayHello()
        {
            MessageBox.Show(string.Format("Hello {0}!", Name)); //Don't do this in real life :)
        }

        public void ThrowException()
        {
            try
            {
                throw new Exception("This is an exception Test");
            }
            catch (Exception ex)
            {
                
                ex.ToExceptionless()
                    .MarkAsCritical()
                    .AddTags("Test", "By", "Jack")
                    .Submit();
            }
            
        }

        public void ThrowExceptionUnhandled()
        {
            throw new Exception("This is an unhandled exception Test");

        }
    }
}
