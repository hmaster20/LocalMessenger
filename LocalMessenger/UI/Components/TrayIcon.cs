using LocalMessenger.UI.Forms;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LocalMessenger.UI.Components
{
    public class TrayIconManager
    {
        private readonly NotifyIcon notifyIcon;
        private readonly MainForm mainForm;

        public TrayIconManager(MainForm form, Icon appIcon)
        {
            mainForm = form;
            notifyIcon = new NotifyIcon
            {
                Icon = appIcon, // Используем переданную иконку
                Visible = false,
                Text = "Local Messenger"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Восстановить", null, RestoreWindow);
            contextMenu.Items.Add("Выход", null, ExitApplication);
            notifyIcon.ContextMenuStrip = contextMenu;

            // Обработчики событий
            mainForm.Resize += OnFormResize;
            notifyIcon.DoubleClick += (s, e) => RestoreWindow(null, null);
        }

        /// <summary>
        /// Свернуть окно в трей
        /// </summary>
        private void OnFormResize(object sender, EventArgs e)
        {
            if (mainForm.WindowState == FormWindowState.Minimized)
            {
                mainForm.Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(1000, "Local Messenger", "Приложение свернуто в трей", ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// Восстановить окно из трея
        /// </summary>
        private void RestoreWindow(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            mainForm.Show();
            mainForm.WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// Завершить приложение
        /// </summary>
        private void ExitApplication(object sender, EventArgs e)
        {
            notifyIcon.Dispose();
            Application.Exit();
        }
    }
}