import { inject } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Database } from '../api/daas-api';

@inject(DaaSAPI)
export class DatabaseList {
    private routeConfig: RouteConfig;

    public databases: Database[] = [];
    public noDatabases: boolean = false;

    constructor(private api: DaaSAPI) { }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: any, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;

        this.load();
    }

    /**
     * Load tenant details.
     */
    private async load(): Promise<void> {
        this.databases = await this.api.getDatabases();
        this.noDatabases = !this.databases.length;
    }
}
