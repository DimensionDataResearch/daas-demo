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
    
    public tenant: Tenant | null = null;
    public server: Server | null = null;
    public newServer: NewServer | null = null;
    
    /**
     * Create a new Tenant detail view component.
     * 
     * @param api The DaaS API client.
     */
    constructor(private api: DaaSAPI) { }

    /**
     * Does the tenant currently have a server (or are we in the process of adding one)?
     */
    public get hasServer(): boolean {
        return this.server !== null || this.addingServer;
    }

    /**
     * Is a server currently being added?
     */
    public get addingServer(): boolean {
        return this.newServer !== null;
    }

    /**
     * Show the server creation form.
     */
    public showCreateServerForm(): void {
        this.newServer = {
            name: null,
            adminPassword: null
        };
    }

    /**
     * Hide the server creation form.
     */
    public hideCreateServerForm(): void {
        this.newServer = null;
    }

    /**
     * Request creation of a new server.
     */
    public async createServer(): Promise<void> {
        if (this.newServer === null || this.newServer.name === null || this.newServer.adminPassword === null)
            return;

        await this.api.deployTenantServer(
            this.tenantId,
            this.newServer.name,
            this.newServer.adminPassword
        );

        this.hideCreateServerForm();

        await this.load();
    }

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
 * Represents the form values for creating a server.
 */
export interface NewServer {
    name: string | null;
    adminPassword: string | null;
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
