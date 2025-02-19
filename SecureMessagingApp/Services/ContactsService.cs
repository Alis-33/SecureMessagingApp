using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SecureMessagingApp.Models;

namespace SecureMessagingApp.Services
{
    public class ContactsService
    {
        private const string ContactsFile = "contacts.json";
        private List<Contact> _contacts;

        public ContactsService()
        {
            LoadContacts();
        }

        public List<Contact> GetContacts()
        {
            return _contacts;
        }

        public void AddOrUpdateContact(Contact contact)
        {
            var existing = _contacts.FirstOrDefault(c => c.Name.Equals(contact.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                // Update existing contact
                existing.PublicKey = contact.PublicKey;
                existing.IPAddress = contact.IPAddress;
                existing.Port = contact.Port;
            }
            else
            {
                _contacts.Add(contact);
            }
            SaveContacts();
        }

        public void DeleteContact(string contactName)
        {
            var contact = _contacts.FirstOrDefault(c => c.Name.Equals(contactName, StringComparison.OrdinalIgnoreCase));
            if (contact != null)
            {
                _contacts.Remove(contact);
                SaveContacts();
            }
        }

        private void LoadContacts()
        {
            if (File.Exists(ContactsFile))
            {
                string json = File.ReadAllText(ContactsFile);
                _contacts = JsonSerializer.Deserialize<List<Contact>>(json) ?? new List<Contact>();
            }
            else
            {
                _contacts = new List<Contact>();
            }
        }

        private void SaveContacts()
        {
            string json = JsonSerializer.Serialize(_contacts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ContactsFile, json);
        }
    }
}
