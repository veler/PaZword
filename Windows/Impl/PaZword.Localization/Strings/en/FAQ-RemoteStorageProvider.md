### Frequently Asked Questions

#### Is it mandatory to select a cloud storage service?

Not at all, but it is highly recommended.

#### What does a cloud storage service bring?

It allows PaZword to do a backup of your vault in the cloud. This is useful to retrieve your data after formatting your current device, or when using PaZword on another device.

#### Why using my personal cloud storage service instead of a PaZword's server?

PaZword is an OpenSource software made by a developer in his spare time. Servers have a cost, and the developer doesn't wish to have to handle the cost of the traffic and the security of his own the server. The developer also believes that users might be suspicious about how are treated their personal vault. Therefore, relying on existing safe personal cloud storage services is a good balance between cost and security.

#### Is it safe?

PaZword encrypts all your data through your recovery key on the local machine and on the cloud storage service. Data cannot be read without this recovery key.

#### Where is my recovery key stored?

Your recovery key is stored in the [Windows Credential Manager](https://support.microsoft.com/en-us/help/4026814/windows-accessing-credential-manager) as long as your settings allow it (you can change them later).
While PaZword may keep the recovery key in the Windows Credential Manager, it is important that you keep a copy of it in case it gets deleted. The recovery key won't be store on the personal cloud storage service and the PassowrdZanager developer doesn't keep it.

#### How to remove my vault from the personal cloud storage service?

You can remove your data from the cloud storage service at any time by disabling the synchronization in the settings of PaZword, and then by deleting the files from the application folder on your personal cloud storage service.
For example, on Microsoft OneDrive and DropBox, you will find your vault data in the folder _/Applications/PaZword_.

#### I have more questions

You may find an answer on your favorite search engine on internet, or by contacting the developer [here](https://www.velersoftware.com/contact.php).