import { inject, computedFrom } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Database } from '../api/daas-api';

@inject(DaaSAPI)
export class DatabaseList {
    private routeConfig: RouteConfig;

    public databases: Database[] = [];
    public isLoading: boolean = false;

    constructor(private api: DaaSAPI) { }

    /**
     * Are there no databases defined in the system?
     */
    @computedFrom('isLoading', 'databases')
    public get hasNoDatabases(): boolean {
        return !this.isLoading && this.databases.length == 0;
    }

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
        this.isLoading = true;

        try
        {
            this.databases = await this.api.getDatabases();
        }
        finally {
            this.isLoading = false;
        }
    }
}
