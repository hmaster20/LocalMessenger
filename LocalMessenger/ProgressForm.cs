using System;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class ProgressForm : Form
    {
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblProgress;
        private long totalSize;

        public ProgressForm(string fileName, long totalSize)
        {
            this.totalSize = totalSize;
            InitializeComponents(fileName);
        }

        private void InitializeComponents(string fileName)
        {
            this.Size = new System.Drawing.Size(400, 150);
            this.Text = "File Transfer Progress";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            lblStatus = new Label
            {
                Text = $"File: {fileName}",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(360, 20)
            };

            progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(360, 20),
                Minimum = 0,
                Maximum = 100
            };

            lblProgress = new Label
            {
                Text = "0%",
                Location = new System.Drawing.Point(10, 70),
                Size = new System.Drawing.Size(360, 20)
            };

            this.Controls.AddRange(new Control[] { lblStatus, progressBar, lblProgress });
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