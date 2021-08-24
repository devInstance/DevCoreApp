import { HttpClient } from "@angular/common/http";
import { Inject } from "@angular/core";
import { Observable } from "rxjs";
import { WeatherForecast } from "../../model/weather-forecast";
import { IFetchDataApi } from "./api/fetchdata-api.interface";

export class FetchDataApi implements IFetchDataApi {

    constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string)
    {
//        super();
    }

    getData(): Observable<WeatherForecast[]> {
        console.error("FetchDataApi.getList() " + this.baseUrl);
        return this.http.get<WeatherForecast[]>(this.baseUrl + 'weatherforecast');
    }
}
