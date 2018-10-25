using System;
using System.Collections.Generic;

namespace Application.Models
{
    public partial class ThirdPartyPublicKeys
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Key { get; set; }
        public byte[] Signature { get; set; }
    }
}
