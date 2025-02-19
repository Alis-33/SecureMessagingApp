# Secure Messaging App

This project is a secure messaging application built in .NET with a simple console interface. The application uses RSA encryption (with OAEP and SHA256) to protect message content and AES to secure the private key. A master password processed through PBKDF2 (with a random salt and high iterations) derives the encryption key. The system supports basic account management (registration, login, deletion) and contact management to save public keys, IP addresses, and ports for reuse. Currently, the solution functions on a local network as a prototype.

## Features

- **Encryption:**
    - RSA with OAEP (SHA256) encrypts messages.
    - The private key is protected with AES, using a PBKDF2-derived key.

- **Account Management:**
    - Checks for existing user data at startup.
    - Supports login, new account creation, and account deletion.

- **Contacts Management:**
    - Allows storage and editing of recipient details (public key, IP address, port).

- **Messaging:**
    - Serializes messages to JSON and encrypts them before transmission.
    - Uses TCP with a simple length-prefixed protocol for sending and receiving.

## Usage

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/yourusername/secure-messaging-app.git
   ```

1. **Open the Project:** Open the solution in your preferred .NET IDE (e.g., JetBrains Rider on macOS).
2. **Build and Run:**

```bash
dotnet run
```
3. **Follow On-Screen Instructions:** Create an account or log in, manage contacts, and send messages as prompted.
   Limitations & Future Enhancements
4. **Default Port:** The application uses port 5000 by default. If this port is unavailable, you can change it in the `Program.cs` file.

##  Current Limitations:

1. The application is designed for LAN use.
2. Relying on a fixed IP is problematic in environments with dynamic IP assignment (e.g., DHCP).
3. As a peer-to-peer system, identity verification is minimal; any device could claim a false identity.
* **Future Enhancements:**
    * **Central Relay Server:** Adding a central server could support Internet communication without manual router configuration.
    * **Service Discovery:** Implement protocols like mDNS or Bonjour to manage changing IP addresses, though this introduces identity verification challenges.
    * **Identity Verification:** To address trust issues, potential solutions include:
        * A circle of trust, where users verify each other through mutual contacts.
        * A trusted third-party system, such as PKI.
        * Out-of-band verification of public key fingerprints.
          While this prototype demonstrates the core principles of secure messaging over a local network, these improvements would be necessary for deployment in a real-world environment.
