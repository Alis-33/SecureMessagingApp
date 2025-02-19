using System;
using System.Threading.Tasks;
using SecureMessagingApp.Services;
using SecureMessagingApp.Models;

namespace SecureMessagingApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Secure Messaging App ===");

            // Initialize core services that don't require login
            var encryptionService = new RSAEncryptionService();
            var accountService = new AccountService(encryptionService);
            var contactsService = new ContactsService();
            var networkService = new NetworkService(5000);

            string masterPassword = "";
            string senderName = "";

            // Account management: Check if an account exists.
            if (accountService.AccountExists())
            {
                Console.WriteLine("Account detected. Please login.");
                bool loggedIn = false;
                int attempts = 0;
                while (!loggedIn && attempts < 3)
                {
                    Console.Write("Enter your master password: ");
                    string inputPassword = Console.ReadLine();

                    if (accountService.ValidateMasterPassword(inputPassword))
                    {
                        loggedIn = true;
                        masterPassword = inputPassword;
                    }
                    else
                    {
                        Console.WriteLine("Invalid master password.");
                        attempts++;
                        if (attempts < 3)
                        {
                            Console.WriteLine("Options: 1. Try again  2. Delete account");
                            string option = Console.ReadLine();
                            if (option == "2")
                            {
                                Console.Write("Are you sure you want to delete your account? This will remove your keys and contacts. (yes/no): ");
                                string confirm = Console.ReadLine();
                                if (confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
                                {
                                    accountService.DeleteAccount();
                                    Console.WriteLine("Account deleted. Please restart the application to create a new account.");
                                    return;
                                }
                            }
                        }
                    }
                }
                if (!loggedIn)
                {
                    Console.WriteLine("Too many failed attempts. Exiting...");
                    return;
                }
            }
            else
            {
                Console.WriteLine("No account found. Create a new account.");
                Console.Write("Enter a new master password: ");
                masterPassword = Console.ReadLine();
                accountService.CreateAccount(masterPassword);
                Console.Write("Enter your sender name: ");
                senderName = Console.ReadLine();
            }

            // If account exists and login succeeded, ask for sender name if not provided earlier.
            if (string.IsNullOrEmpty(senderName))
            {
                Console.Write("Enter your sender name: ");
                senderName = Console.ReadLine();
            }

            // Start network service listener.
            var listenerTask = networkService.StartListenerAsync();

            // Create the messaging service.
            var messagingService = new MessagingService(encryptionService, networkService, masterPassword, senderName);
            messagingService.OnNewMessage += message =>
            {
                Console.WriteLine("\n=== New Message Received ===");
                Console.WriteLine($"From: {message.Sender}");
                Console.WriteLine($"Time: {message.Timestamp}");
                Console.WriteLine($"Message: {message.Content}");
                Console.WriteLine("============================\n");
            };

            // Main menu loop.
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nMain Menu Options:");
                Console.WriteLine("1. Send a message");
                Console.WriteLine("2. View my public key");
                Console.WriteLine("3. Manage contacts");
                Console.WriteLine("4. Delete account");
                Console.WriteLine("5. Exit");
                Console.Write("Select an option: ");
                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await SendMessageFlow(messagingService, contactsService);
                        break;
                    case "2":
                        Console.WriteLine("\n=== Your Public Key ===");
                        Console.WriteLine(encryptionService.GetPublicKey());
                        Console.WriteLine("========================\n");
                        break;
                    case "3":
                        ManageContactsFlow(contactsService);
                        break;
                    case "4":
                        Console.Write("Are you sure you want to delete your account? (yes/no): ");
                        string confirm = Console.ReadLine();
                        if (confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
                        {
                            accountService.DeleteAccount();
                            Console.WriteLine("Account deleted. Exiting application...");
                            exit = true;
                        }
                        break;
                    case "5":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Try again.");
                        break;
                }
            }

            networkService.StopListener();
            await listenerTask;
        }

        // Flow for sending a message.
        static async Task SendMessageFlow(MessagingService messagingService, ContactsService contactsService)
        {
            Console.WriteLine("\nSend Message Options:");
            Console.WriteLine("1. Enter recipient details manually");
            Console.WriteLine("2. Select from saved contacts");
            Console.Write("Select an option: ");
            string sendOption = Console.ReadLine();

            string recipientIp = "";
            int recipientPort = 5000;
            string recipientPublicKeyXml = "";

            if (sendOption == "1")
            {
                Console.Write("Enter recipient's IP address: ");
                recipientIp = Console.ReadLine();
                Console.Write("Enter recipient's port (default 5000): ");
                string portInput = Console.ReadLine();
                if (!string.IsNullOrEmpty(portInput))
                {
                    int.TryParse(portInput, out recipientPort);
                }
                Console.WriteLine("Paste the recipient's public key (XML format): ");
                recipientPublicKeyXml = Console.ReadLine();
            }
            else if (sendOption == "2")
            {
                var contacts = contactsService.GetContacts();
                if (contacts.Count == 0)
                {
                    Console.WriteLine("No contacts saved.");
                    return;
                }
                Console.WriteLine("Select a contact:");
                for (int i = 0; i < contacts.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {contacts[i].Name} (IP: {contacts[i].IPAddress}, Port: {contacts[i].Port})");
                }
                Console.Write("Enter contact number: ");
                string contactChoice = Console.ReadLine();
                if (int.TryParse(contactChoice, out int index) && index >= 1 && index <= contacts.Count)
                {
                    var contact = contacts[index - 1];
                    recipientIp = contact.IPAddress;
                    recipientPort = contact.Port;
                    recipientPublicKeyXml = contact.PublicKey;
                }
                else
                {
                    Console.WriteLine("Invalid contact selection.");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Invalid option.");
                return;
            }

            Console.Write("Enter your message: ");
            string content = Console.ReadLine();
            var message = new Message { Content = content };

            await messagingService.SendMessageWithPublicKeyAsync(message, recipientPublicKeyXml, recipientIp, recipientPort);
        }

        // Flow for managing contacts.
        static void ManageContactsFlow(ContactsService contactsService)
        {
            bool back = false;
            while (!back)
            {
                Console.WriteLine("\nContacts Management:");
                Console.WriteLine("1. View contacts");
                Console.WriteLine("2. Add/Edit contact");
                Console.WriteLine("3. Delete contact");
                Console.WriteLine("4. Back to main menu");
                Console.Write("Select an option: ");
                string option = Console.ReadLine();
                switch (option)
                {
                    case "1":
                        var contacts = contactsService.GetContacts();
                        if (contacts.Count == 0)
                        {
                            Console.WriteLine("No contacts saved.");
                        }
                        else
                        {
                            Console.WriteLine("Saved Contacts:");
                            foreach (var contact in contacts)
                            {
                                Console.WriteLine($"Name: {contact.Name}, IP: {contact.IPAddress}, Port: {contact.Port}");
                            }
                        }
                        break;
                    case "2":
                        Console.Write("Enter contact name: ");
                        string name = Console.ReadLine();
                        Console.Write("Enter contact's IP address: ");
                        string ip = Console.ReadLine();
                        Console.Write("Enter contact's port (default 5000): ");
                        string portInput = Console.ReadLine();
                        int port = 5000;
                        if (!string.IsNullOrEmpty(portInput))
                        {
                            int.TryParse(portInput, out port);
                        }
                        Console.WriteLine("Paste the contact's public key (XML format): ");
                        string publicKey = Console.ReadLine();

                        var contactToSave = new Contact { Name = name, IPAddress = ip, Port = port, PublicKey = publicKey };
                        contactsService.AddOrUpdateContact(contactToSave);
                        Console.WriteLine("Contact saved.");
                        break;
                    case "3":
                        Console.Write("Enter contact name to delete: ");
                        string deleteName = Console.ReadLine();
                        contactsService.DeleteContact(deleteName);
                        Console.WriteLine("Contact deleted if it existed.");
                        break;
                    case "4":
                        back = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }
    }
}
