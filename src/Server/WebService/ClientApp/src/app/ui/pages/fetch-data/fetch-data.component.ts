import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { WeatherForecast } from '../../../model/weather-forecast';
import { FetchDataService } from '../../../services/fetchdata.service';

@Component({
    selector: 'app-fetch-data',
    templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
    public forecasts: WeatherForecast[];

    constructor(service: FetchDataService) {
        service.getList().subscribe(result => {
            console.error("FetchDataApi.getList() received data");
            this.forecasts = result;
        });
    }
}
