import { inject } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Database, Tenant } from '../api/daas-api';

@inject(DaaSAPI)
export class TenantDatabaseList {
    private routeConfig: RouteConfig;
    private tenantId: number;

    public tenant: Tenant | null;
    public databases: Database[] | null;
    public loaded: boolean = false;

    /**
     * Create a new Server databases view component.
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
     * Load tenant and database details.
     */
    private async load(): Promise<void> {
        const tenantRequest = this.api.getTenant(this.tenantId);
        const databasesRequest = this.api.getTenantDatabases(this.tenantId);

        this.tenant = await tenantRequest;
        
        if (this.tenant != null)
            this.routeConfig.title = `${this.tenant.name} - Databases`;
        else
            this.routeConfig.title = 'Tenant not found';

        this.databases = await databasesRequest;

        this.loaded = (this.tenant != null && this.databases != null);
    }

    /**
     * Delete a database.
     * 
     * @param databaseId The database Id.
     */
    private async deleteDatabase(databaseId: number): Promise<void> {
        await this.api.deleteTenantDatabase(this.tenantId, databaseId);
        await this.load();
    }
}

/**
 * Route parameters for the Tenant databases view.
 */
interface RouteParams
{
    /**
     * The tenant Id.
     */
    id: number;
}
