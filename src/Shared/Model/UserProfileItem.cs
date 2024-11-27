using DevInstance.BlazorToolkit.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model;

public class UserProfileItem : ModelItem
{
    public string Email { get; set; }
    public string Name { get; set; }
    public DateTime CreateDate { get; set; }
}
