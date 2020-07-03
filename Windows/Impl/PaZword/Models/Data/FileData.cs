using Newtonsoft.Json;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Core;
using PaZword.Core.Json;
using System;
using System.Security;
using System.Threading.Tasks;

namespace PaZword.Models.Data
{
    /// <summary>
    /// Represents the data for a file.
    /// </summary>
    internal sealed class FileData : AccountData, IUpgradableAccountData
    {
        [SecurityCritical]
        [JsonProperty(nameof(FileName))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _fileName;

        [SecurityCritical]
        [JsonProperty(nameof(FileExtension))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _fileExtension;

        [SecurityCritical]
        [JsonProperty(nameof(Base64Thumbnail))]
        private string _base64Thumbnail;

        /// <summary>
        /// Gets or sets the name of file.
        /// </summary>
        [JsonIgnore]
        internal SecureString FileName
        {
            get => EncryptionProvider.DecryptString(_fileName.ToUnsecureString(), string.Empty).ToSecureString();
            set => _fileName = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: false /* on purpose */), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the extension of file.
        /// </summary>
        [JsonIgnore]
        internal SecureString FileExtension
        {
            get => EncryptionProvider.DecryptString(_fileExtension.ToUnsecureString(), string.Empty).ToSecureString();
            set => _fileExtension = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the base64 string representation of the file's icon.
        /// </summary>
        /// <remarks>
        /// This is not a <see cref="SecureString"/>. The reason why is that the thumbnail of
        /// <see cref="Constants.DataFileThumbnailSize"/> pixels, encoded in RGBA (32 bit/px),
        /// might be heavier than 13KB (which corresponds to the size limit of <see cref="SecureString"/>).
        /// So exceptionally we keep this data in a plain string.
        /// It should be relatively acceptable considering that the string is still encrypted
        /// and that it won't expose user data very often.
        /// </remarks>
        [JsonIgnore]
        internal string Base64Thumbnail
        {
            // TODO: Being able to split the data in an array of SecureString.
            get => EncryptionProvider.DecryptString(_base64Thumbnail, string.Empty);
            set => _base64Thumbnail = EncryptionProvider.EncryptString(value, string.Empty);
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="FileData"/> class.
        /// </summary>
        public FileData()
            : base()
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="FileData"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the account</param>
        internal FileData(Guid id)
            : base(id)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                _fileName?.Dispose();
                _fileExtension?.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool ExactEquals(AccountData other)
        {
            return
                other is FileData fileData
                && fileData.Id == Id
                && fileData._fileName.IsEqualTo(_fileName)
                && fileData._fileExtension.IsEqualTo(_fileExtension)
                && string.Equals(fileData._base64Thumbnail, _base64Thumbnail, StringComparison.Ordinal);
        }

        public Task UpgradeAsync(int oldVersion, int targetVersion)
        {
            if (oldVersion == 1)
            {
                // In Version 1, there was a vulnerability in the encryption engine.
                // Let's fix it by decrypting and re-encrypting all data.

#pragma warning disable CA2245 // Do not assign a property to itself.
                FileName = FileName;
                FileExtension = FileExtension;
#pragma warning restore CA2245 // Do not assign a property to itself.
            }

            return Task.CompletedTask;
        }
    }
}
