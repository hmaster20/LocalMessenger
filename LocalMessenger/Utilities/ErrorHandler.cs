using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalMessenger.Utilities
{
    public static class ErrorHandler
    {
        public static void Handle(Exception ex, string context)
        {
            Logger.Log($"Error in {context}: {ex.Message}, StackTrace: {ex.StackTrace}");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
