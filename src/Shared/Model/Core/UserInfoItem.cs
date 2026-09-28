using DevInstance.WebServiceToolkit.Common.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model.Core;

public class UserInfoItem : IModelItem
{
    public string Id { get; set; }

    public bool IsAuthenticated { get; set; }

    public string UserName { get; set; }

    public Dictionary<string, string> ExposedClaims { get; set; }
}
