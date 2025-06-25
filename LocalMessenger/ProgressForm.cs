using System;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class ProgressForm : Form
    {


        public ProgressForm(string fileName, long totalSize)
        {
            this.totalSize = totalSize;
            InitializeComponent();
            InitializeCustomComponents(fileName);
        }

        private void InitializeCustomComponents(string fileName)
        {
            lblStatus.Text = $"File: {fileName}";
        }

        public void UpdateProgress(long bytesSent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<long>(UpdateProgress), bytesSent);
                return;
            }

            int percentage = (int)((bytesSent * 100) / totalSize);
            progressBar.Value = Math.Min(percentage, 100);
            lblProgress.Text = $"{percentage}% ({bytesSent / 1024 / 1024} MB / {totalSize / 1024 / 1024} MB)";
        }
    }
}