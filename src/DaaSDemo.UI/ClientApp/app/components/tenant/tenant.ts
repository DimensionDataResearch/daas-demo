import { inject } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Tenant, Server  } from '../api/daas-api';

/**
 * Component for the Tenant detail view.
 */
@inject(DaaSAPI)
export class TenantDetails {
    private routeConfig: RouteConfig;
    private tenantId: number;
    
    public tenant: Tenant | null;
    public server: Server | null;
    
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
    public activate(params: RouteParams, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;
        this.tenantId = params.id;
        
        this.load();
    }

    /**
     * Load tenant and server details.
     */
    private async load(): Promise<void> {
        const tenantRequest = this.api.getTenant(this.tenantId);
        const serverRequest = this.api.getTenantServer(this.tenantId);

        this.tenant = await tenantRequest;
        this.server = await serverRequest;
        
        if (this.tenant != null)
            this.routeConfig.title = this.tenant.name;
        else
            this.routeConfig.title = 'Tenant not found';
    }

    /**
     * Destroy the tenant's server.
     */
    private async destroyServer(): Promise<void> {
        await this.api.destroyTenantServer(this.tenantId);
        await this.load();
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
