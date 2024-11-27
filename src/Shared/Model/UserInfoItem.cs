using DevInstance.BlazorToolkit.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model;

public class UserInfoItem : ModelItem
{
    public bool IsAuthenticated { get; set; }

    public string UserName { get; set; }

    public Dictionary<string, string> ExposedClaims { get; set; }
}
