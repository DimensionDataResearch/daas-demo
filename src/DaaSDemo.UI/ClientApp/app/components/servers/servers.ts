import { inject, computedFrom } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Server } from '../api/daas-api';

@inject(DaaSAPI)
export class ServerList {
    private routeConfig: RouteConfig;

    public servers: Server[] = [];
    public isLoading: boolean = false;

    constructor(private api: DaaSAPI) { }

    /**
     * Are there no servers defined in the system?
     */
    @computedFrom('isLoading', 'servers')
    public get hasNoServers(): boolean {
        return !this.isLoading && this.servers.length == 0;
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
            this.servers = await this.api.getServers();
        }
        finally {
            this.isLoading = false;
        }
    }
}
