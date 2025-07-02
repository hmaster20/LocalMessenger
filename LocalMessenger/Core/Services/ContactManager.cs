using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LocalMessenger.Core.Models;
using LocalMessenger.Utilities;

namespace LocalMessenger.Core.Services
{
    public class ContactManager
    {
        private readonly Dictionary<string, string> _contactIPs;
        private readonly Dictionary<string, DateTime> _lastHelloTimes;
        private readonly HashSet<string> _blinkingContacts;
        private readonly ImageList _statusIcons;
        private readonly ListView _lstContacts;
        private readonly string _myLogin;
        private readonly string _myName;
        private string _myStatus;

        public ContactManager(ListView lstContacts, string myLogin, string myName, string myStatus)
        {
            _lstContacts = lstContacts;
            _myLogin = myLogin;
            _myName = myName;
            _myStatus = myStatus;
            _contactIPs = new Dictionary<string, string>();
            _lastHelloTimes = new Dictionary<string, DateTime>();
            _blinkingContacts = new HashSet<string>();
            _statusIcons = new ImageList { ImageSize = new Size(16, 16) };
            _statusIcons.Images.Add("Online", Properties.Resources.Online);
            _statusIcons.Images.Add("Offline", Properties.Resources.Offline);
            _lstContacts.SmallImageList = _statusIcons;
            AddCurrentUser();
        }

        public void AddCurrentUser()
        {
            _lstContacts.Items.Add(new ListViewItem($"{_myLogin} ({_myName}, {_myStatus})") { ImageKey = _myStatus == "Online" ? "Online" : "Offline" });
            Logger.Log($"Added current user to contacts: {_myLogin}");
        }

        public void UpdateStatus(string newStatus)
        {
            _myStatus = newStatus;
            UpdateContactList();
        }

        public void HandleUdpMessage(string message, string remoteIP)
        {
            try
            {
                var parts = message.Split('|');
                if (parts.Length == 5 && parts[0] == "HELLO")
                {
                    var sender = parts[1];
                    var name = parts[2];
                    var status = parts[3];
                    var publicKey = Convert.FromBase64String(parts[4]);

                    if (sender != _myLogin)
                    {
                        _contactIPs[sender] = remoteIP;
                        _lastHelloTimes[sender] = DateTime.Now;
                        var contactString = $"{sender} ({name}, {status})";
                        var existingItem = _lstContacts.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text.StartsWith(sender));
                        if (existingItem != null)
                        {
                            existingItem.Text = contactString;
                            existingItem.ImageKey = status == "Online" ? "Online" : "Offline";
                            Logger.Log($"Updated contact: {sender} (Name: {name}, Status: {status}, IP: {remoteIP})");
                        }
                        else
                        {
                            var newItem = new ListViewItem(contactString) { ImageKey = status == "Online" ? "Online" : "Offline" };
                            _lstContacts.Items.Add(newItem);
                            Logger.Log($"Added contact: {sender} (Name: {name}, Status: {status}, IP: {remoteIP})");
                        }
                        _lstContacts.Invalidate();
                    }
                    else
                    {
                        Logger.Log($"Ignored own HELLO message from {remoteIP}");
                    }
                }
                else
                {
                    Logger.Log($"Invalid HELLO message format from {remoteIP}: {message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error parsing UDP message from {remoteIP}: {ex.Message}");
            }
        }

        public void UpdateContactList()
        {
            for (int i = 0; i < _lstContacts.Items.Count; i++)
            {
                var item = _lstContacts.Items[i];
                var login = item.Text.Split(' ')[0];
                if (login == _myLogin)
                {
                    item.Text = $"{_myLogin} ({_myName}, {_myStatus})";
                    item.ImageKey = _myStatus == "Online" ? "Online" : "Offline";
                }
            }
            _lstContacts.Invalidate();
            Logger.Log("Updated contact list with current user status");
        }

        public void CheckContactTimeouts()
        {
            var now = DateTime.Now;
            foreach (var contact in _lastHelloTimes.Keys.ToList())
            {
                if ((now - _lastHelloTimes[contact]).TotalSeconds > 60)
                {
                    var item = _lstContacts.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text.StartsWith(contact));
                    if (item != null)
                    {
                        item.Text = item.Text.Replace("Online", "Offline");
                        item.ImageKey = "Offline";
                        Logger.Log($"Contact {contact} marked as Offline due to timeout.");
                    }
                    _lastHelloTimes.Remove(contact);
                }
            }
            _lstContacts.Invalidate();
        }

        public string GetContactIP(string contactLogin)
        {
            return _contactIPs.ContainsKey(contactLogin) ? _contactIPs[contactLogin] : contactLogin;
        }

        public void AddBlinkingContact(string contact)
        {
            _blinkingContacts.Add(contact);
        }

        public void RemoveBlinkingContact(string contact)
        {
            _blinkingContacts.Remove(contact);
        }

        public bool IsBlinkingContact(string contact)
        {
            return _blinkingContacts.Contains(contact);
        }
    }
}