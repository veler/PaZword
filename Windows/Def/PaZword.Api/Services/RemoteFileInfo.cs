using System;
using System.Collections.Generic;

namespace PaZword.Api.Services
{
    /// <summary>
    /// Represents information about a file on a remote server.
    /// </summary>
    public struct RemoteFileInfo : IEquatable<RemoteFileInfo>
    {
        public static readonly RemoteFileInfo Empty = default;

        /// <summary>
        /// Gets the full path to the file on the server.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets the date where the file has been created.
        /// </summary>
        public DateTimeOffset CreatedDateTime { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="RemoteFileInfo"/> class.
        /// </summary>
        public RemoteFileInfo(string fullPath, DateTimeOffset createdDateTime)
        {
            FullPath = fullPath;
            CreatedDateTime = createdDateTime;
        }

        public override bool Equals(object obj)
        {
            return obj is RemoteFileInfo other && Equals(other);
        }

        public bool Equals(RemoteFileInfo other)
        {
            return FullPath == other.FullPath &&
                   CreatedDateTime.Equals(other.CreatedDateTime);
        }

        public override int GetHashCode()
        {
            int hashCode = -570407407;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FullPath);
            hashCode = hashCode * -1521134295 + CreatedDateTime.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(RemoteFileInfo left, RemoteFileInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RemoteFileInfo left, RemoteFileInfo right)
        {
            return !(left == right);
        }
    }
}
