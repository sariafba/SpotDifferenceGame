using System;
using System.Windows.Forms;

namespace SpotTheDifference
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SpotTheDifferenceForm());
        }
    }
}