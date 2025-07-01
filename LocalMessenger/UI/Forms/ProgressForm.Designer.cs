using System.Windows.Forms;

namespace LocalMessenger.UI.Forms
{
    partial class ProgressForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblProgress;
        private long totalSize;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.Size = new System.Drawing.Size(400, 150);
            this.Text = "File Transfer Progress";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            lblStatus = new Label
            {
                Text = "File: ",
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

        #endregion
    }
}