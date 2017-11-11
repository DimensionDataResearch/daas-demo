import { inject } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Tenant  } from '../api/daas-api';

/**
 * Component for the Tenant detail view.
 */
@inject(DaaSAPI)
export class TenantDetails {
    private routeConfig: RouteConfig;
    
    public tenant: Tenant | null;
    
    /**
     * Create a new Tenant detail view component.
     * 
     * @param api The DaaS API client.
     */
    constructor(private api: DaaSAPI) { }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public async activate(params: RouteParams, routeConfig: RouteConfig): Promise<void> {
        this.routeConfig = routeConfig;

        this.tenant = await this.api.getTenant(params.id);
        if (this.tenant != null)
            this.routeConfig.title = this.tenant.name;
        else
            this.routeConfig.title = 'Tenant not found';
      }
}

/**
 * Route parameters for the Tenant detail view.
 */
interface RouteParams
{
    /**
     * The tenant Id.
     */
    id: number;
}
