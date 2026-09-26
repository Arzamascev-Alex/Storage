using System;
using System.Collections.Generic;
using System.Text;
using Storage.Serialization;

namespace Storage.Core
{
    [GenerateBinarySerializer]
    public partial class UserProfile
    {
        public int Id { get; set; }
        public string UserName {  get; set; } = string.Empty;
        public DateTime CreatedAt {  get; set; }
    }
}
