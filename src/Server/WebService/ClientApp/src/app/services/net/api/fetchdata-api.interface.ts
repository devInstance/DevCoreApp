import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { WeatherForecast } from "../../../model/weather-forecast";

@Injectable({
    providedIn: 'root'
})
export abstract class IFetchDataApi {
    abstract getData(): Observable<WeatherForecast[]>;
}
