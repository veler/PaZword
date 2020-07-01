### Question fréquemment posées

#### Est-il obligatoire de choisir un service de stockage dans le cloud?

Pas du tout, mais c'est fortement recommandé.

#### Qu'est-ce qu'un service de stockage dans le cloud apporte?

Celà permet à PaZword de faire une sauvegarde de votre coffre-fort dans le cloud. C'est utile pour récupérer vos données après avoir formatté votre machine, ou lorsque vous utilisez PaZword sur une autre machine.

#### Pourquoi utiliser mon service de stockage dans le cloud personnel au lieu d'un server pour PaZword?

PaZword est un logiciel Open Source fait par un développeur dans son temps libre. Les servers ont un coût, et le développeur ne souhaite payer le prix du traffic et gérer la sécurité de son propre server. Le développeur pense également que les utilisateurs suspecterons la manière dont leur coffre-fort est géré. De ce fait, utiliser un service de stockage dans le cloud existant offre un bon compromis entre le coût et la sécurité.

#### Est-ce sécurisé?

PaZword chiffre toutes vos données via votre clé de récupération sur votre machine et dans le cloud. Les données ne peuvent être lu sans la clé de récupération.

#### Où est stocké ma clé de récupération?

Votre clé de récupération est stocké dans le [Windows Credential Manager](https://support.microsoft.com/en-us/help/4026814/windows-accessing-credential-manager) tant que vos paramètres le permettent (vous pouvez lez changer plus tard).
Même si PaZword conserve la clé de récupérer dans le Windows Credential Manager, il est important que vous conservew une copie de votre clé au cas ou elle soit supprimée. La clé de récupération ne sera pas stocké dans le service de stockage personnel dans le cloud et le développeur de PaZword ne la garde pas non plus.

#### Comment supprimer mon coffre-fort de mon service de stockage personnel dans le cloud?

Vous pouvez supprimer vos données dans le cloud à tout moment en désactivant la synchronisation dans les paramètres de PaZword, puis en supprimant les fichier depuis le dossier d'application de votre service de stockage personnel dans le cloud.
Par exemple, sur Microsoft OneDrive et Dropbox, vous trouverez votre les données de votre coffre-fort dans le dossier _/Applications/PaZword_.

#### J'ai d'autres questions

Vous pouvez trouver des réponses sur votre moteur de recherche favoris sur internet, ou bien en contactant le développeur [ici](https://www.velersoftware.com/contact.php).