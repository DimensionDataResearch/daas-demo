import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';

@inject(HttpClient)
export class DatabaseList {
    public databases: Database[];

    constructor(http: HttpClient) {
        http.fetch('api/data/sample/weather-forecasts')
            .then(result => result.json() as Promise<Database[]>)
            .then(data => {
                this.databases = data;
            });
    }
}

interface Database {
    dateFormatted: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}
