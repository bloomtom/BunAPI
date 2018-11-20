using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace BunAPI
{
    public class BunFile
    {
        public string Guid { get; set; }
        public string StorageZoneName { get; set; }
        public string Path { get; set; }
        public string ObjectName { get; set; }
        public int Length { get; set; }
        public DateTime LastChanged { get; set; }
        public bool IsDirectory { get; set; }
        public int ServerId { get; set; }
        public string UserId { get; set; }
        public DateTime DateCreated { get; set; }
        public int StorageZoneId { get; set; }
    }
}
