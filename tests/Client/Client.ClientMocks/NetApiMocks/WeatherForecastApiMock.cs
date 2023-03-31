using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Net.NetApiMocks
{
    internal class WeatherForecastApiMock : IWeatherForecastApi
    {
        public Task<WeatherForecastItem?> AddAsync(WeatherForecastItem payload)
        {
            throw new NotImplementedException();
        }

        public Task<WeatherForecastItem?> GetAsync(string id, ItemFields? fields)
        {
            throw new NotImplementedException();
        }

        public Task<ModelList<WeatherForecastItem>?> GetItemsAsync(int? top, int? page, ItemFilters? filters, ItemQueries? query, ItemFields? fields)
        {
            throw new NotImplementedException();
        }

        public Task<WeatherForecastItem?> RemoveAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<WeatherForecastItem?> UpdateAsync(string id, WeatherForecastItem payload)
        {
            throw new NotImplementedException();
        }
    }
}
