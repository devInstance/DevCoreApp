using System;
using System.Collections.Generic;
using System.Text;

namespace DevInstance.SampleWebApp.Shared.Model
{
    public class UserInfoItem : ModelItem
    {
        public bool IsAuthenticated { get; set; }

        public string UserName { get; set; }

        public Dictionary<string, string> ExposedClaims { get; set; }
    }
}
