using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace BunAPI
{
    /// <summary>
    /// A definition for a file stored on BunnyCDN
    /// </summary>
    public class BunFile
    {
        /// <summary>
        /// The unique identifier given to the file when it was uploaded.
        /// </summary>
        public string Guid { get; set; }
        /// <summary>
        /// The storage zone this file resides in.
        /// </summary>
        public string StorageZoneName { get; set; }
        /// <summary>
        /// The full "path" for this file using virtual folders.
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// The name of this object. This is the name used for API operations.
        /// </summary>
        public string ObjectName { get; set; }
        /// <summary>
        /// The length of this file in bytes.
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// The date of the last write operation on this file.
        /// </summary>
        public DateTime LastChanged { get; set; }
        /// <summary>
        /// True if this file is actually a directory.
        /// </summary>
        public bool IsDirectory { get; set; }
        /// <summary>
        /// The BunnyCDN server this file resides on.
        /// </summary>
        public int ServerId { get; set; }
        /// <summary>
        /// The ID for the user who owns this file.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The date this file was created.
        /// </summary>
        public DateTime DateCreated { get; set; }
        /// <summary>
        /// The ID for the storage zone this file resides in.
        /// </summary>
        public int StorageZoneId { get; set; }
    }
}
