#  CipherChat-System

**CipherChat-System** is a secure, real-time LAN communication application built using **C# (.NET 6)** and **Windows Forms**. It is designed to provide encrypted messaging and file sharing capabilities over a local network, demonstrating core concepts of Network Programming and Cybersecurity.

![Status](https://img.shields.io/badge/Status-Completed-success)
![Security](https://img.shields.io/badge/Encryption-AES-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)

##  Overview

In an era where data privacy is paramount, CipherChat-System ensures that communication between clients remains confidential. By utilizing TCP/IP sockets for stable connections and AES algorithms for encryption, users can chat and share documents securely within an organization or a home network.

## ✨ Key Features

* **🛡️ End-to-End Encryption:** All text messages are encrypted/decrypted instantly to prevent packet sniffing.
* **📨 Real-Time Messaging:** Low-latency communication using `TcpClient` and `TcpListener`.
* **📁 Secure File Transfer:** Support for sending various file formats (PDF, DOCX, TXT, Images) with automatic saving.
* **🖼️ Rich User Interface:**
    * Modern chat bubbles (WhatsApp-style).
    * Emoji picker support.
    * Visual indicators for sent/received messages.
    * "Press Enter to Send" functionality.
* **💾 Auto-Save:** Received files are automatically organized and saved in a dedicated local folder.

## 🛠️ Tech Stack

* **Language:** C#
* **Framework:** .NET 6.0 (Desktop Runtime)
* **UI:** Windows Forms (WinForms) with GDI+ Graphics
* **Networking:** System.Net.Sockets
* **Data Handling:** Newtonsoft.Json
* **Security:** System.Security.Cryptography

## 📸 Screenshots

| Login Interface | Chat Interface | File Transfer |
| :---: | :---: | :---: |
| <img width="1803" height="697" alt="1" src="https://github.com/user-attachments/assets/4813f87a-8041-40bb-8123-b3f7797c6d64" />
 | <img width="1810" height="700" alt="2" src="https://github.com/user-attachments/assets/0d262ab2-b89a-4ae4-a2fb-38ac0a87bb7c" />
 | <img width="1288" height="745" alt="3" src="https://github.com/user-attachments/assets/8cf45589-01b9-41cf-9b75-f419f3a3906a" />
 | <img width="1808" height="692" alt="4" src="https://github.com/user-attachments/assets/7006b626-0af7-4a7f-8256-9b7887939426" />

## ⚙️ Installation & Usage

### Prerequisites
* Windows OS (10 or 11)
* [.NET Desktop Runtime 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

### How to Run
1.  **Clone the repository:**
   ```bash
    git clone [https://github.com/mohamedramzial/CipherChat-System.git)
    ```
2.  **Start the Server:**
    * Run `ServerApp.exe`.
    * Ensure firewall permissions are granted if prompted.
3.  **Start the Client:**
    * Run `ClientApp.exe` on any machine in the same network.
    * Enter your **Username** and the **Server's IP Address**.
    * Click **Connect** and start chatting!

## 👤 Author

**Mohammed**

* Eng Cybersecurity  & Developer*

---
*This project was developed for educational purposes to demonstrate secure socket programming.*

