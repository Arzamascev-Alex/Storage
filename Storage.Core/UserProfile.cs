using System;
using System.Collections.Generic;
using System.Text;

namespace Storage.Core
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string UserName {  get; set; } = string.Empty;
        public DateTime CreatedAt {  get; set; }
    }
}
