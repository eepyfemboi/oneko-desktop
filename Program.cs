using System;
using System.Windows.Forms;

namespace oneko
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new OnekoForm());
        }
    }
}
