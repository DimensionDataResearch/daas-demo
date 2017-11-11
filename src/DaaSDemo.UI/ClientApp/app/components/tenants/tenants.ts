import { inject } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Tenant  } from '../api/daas-api';

@inject(DaaSAPI)
export class TenantList {
    private routeConfig: RouteConfig;
    
    public tenants: Tenant[];
    public noTenants: boolean;
    
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
        this.tenants = await this.api.getTenants();
        this.noTenants = !this.tenants.length;
    }
}
