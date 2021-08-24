import { HttpClient } from "@angular/common/http";
import { Inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { WeatherForecast } from "../model/weather-forecast";
import { Subscribable } from "../utils/subscribable";
import { IFetchDataApi } from "./net/api/fetchdata-api.interface";

@Injectable({
  providedIn: 'root'
})
export class FetchDataService {

    public forecasts: Subscribable<WeatherForecast[]> = new Subscribable<WeatherForecast[]>();

    constructor(private api: IFetchDataApi) {
    }

    getList(): Observable<WeatherForecast[]> {

        console.error("FetchDataService.getList()");

    //    this.api.getData().subscribe(result => {
    //        console.error("FetchDataApi.getList() received data");
    //        this.forecasts.set(result);
    //    }, error => console.error(error));

    //    return this.forecasts.value;

        return this.api.getData();
    }
}
