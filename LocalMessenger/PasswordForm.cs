using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class PasswordForm : Form
    {
        public string Password { get; private set; }

        public PasswordForm()
        {
            InitializeComponent();
        }


    }
}