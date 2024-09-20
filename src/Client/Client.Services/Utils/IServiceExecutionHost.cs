using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Services.Utils;

public interface IServiceExecutionHost
{
    string ErrorMessage { get; set; }
    bool IsError { get; set; }
    bool InProgress { get; set; }

    Task ShowLoginModalAsync();
    void StateHasChanged();
}
