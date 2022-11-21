using System.Net.Http;

namespace DevInstance.DevCoreApp.Client.Net
{
    public class ApiBase
    {
        protected readonly HttpClient httpClient;
        public ApiBase(HttpClient http)
        {
            httpClient = http;
        }
    }
}
