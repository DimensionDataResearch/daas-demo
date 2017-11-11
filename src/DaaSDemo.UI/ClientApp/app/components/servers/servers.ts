import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';

@inject(HttpClient)
export class ServerList {
    public servers: Server[];

    constructor(private http: HttpClient) {
        http.fetch('api/data/sample/weather-forecasts')
            .then(result => result.json() as Promise<Server[]>)
            .then(data => {
                this.servers = data;
            });
    }
}

interface Server {
    dateFormatted: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}
