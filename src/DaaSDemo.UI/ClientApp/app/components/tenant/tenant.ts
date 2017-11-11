import { inject, factory, transient, computedFrom } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Tenant, Server  } from '../api/daas-api';

/**
 * Component for the Tenant detail view.
 */
@inject(DaaSAPI)
export class TenantDetails {
    private routeConfig: RouteConfig;
    private tenantId: number;
    
    public loading: boolean = false;
    public tenant: Tenant | null = null;
    public server: Server | null = null;
    public errorMessage: string | null = null;
    public newServer: NewServer | null = null;
    
    /**
     * Create a new Tenant detail view component.
     * 
     * @param api The DaaS API client.
     */
    constructor(private api: DaaSAPI) { }

    /**
     * Has an error occurred?
     */
    @computedFrom('errorMessage')
    public get hasError(): boolean {
        return this.errorMessage !== null;
    }

    /**
     * Does the tenant exist?
     */
    @computedFrom('tenant')
    public get hasTenant(): boolean {
        return this.tenant !== null;
    }

    /**
     * Does the tenant currently have a server?
     */
    @computedFrom('server')
    public get hasServer(): boolean {
        return this.server !== null;
    }

    /**
     * Is the tenant's server ready for use?
     */
    @computedFrom('server')
    public get isServerReady(): boolean {
        return this.server !== null && this.server.status === 'Ready';
    }

    /**
     * Is a server currently being added?
     */
    @computedFrom('newServer')
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

        this.server = null;

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
        this.errorMessage = null;
        this.loading = true;

        try {
            const tenantRequest = this.api.getTenant(this.tenantId);
            const serverRequest = this.api.getTenantServer(this.tenantId);

            this.tenant = await tenantRequest;
            this.server = await serverRequest;

            if (this.tenant === null)
            {
                this.routeConfig.title = 'Tenant not found';
                this.errorMessage = `Tenant not found with Id ${this.tenantId}.`;
            }
            else
            {
                this.routeConfig.title = this.tenant.name;
            }
        } catch (error) {
            console.log(error);

            this.errorMessage = error.message;
        }
        finally {
            this.loading = false;
        }
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
