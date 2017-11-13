import { inject, factory, transient, computedFrom } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { RouteConfig } from 'aurelia-router';
import { bindable } from 'aurelia-templating';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { DaaSAPI, Tenant, Server  } from '../api/daas-api';

/**
 * Component for the Tenant detail view.
 */
@inject(DaaSAPI, NewInstance.of(ValidationController))
export class TenantDetails {
    private routeConfig: RouteConfig;
    private tenantId: number;
    
    @bindable public loading: boolean = false;
    @bindable public tenant: Tenant | null = null;
    @bindable public server: Server | null = null;
    @bindable public errorMessage: string | null = null;
    @bindable public newServer: NewServer | null = null;
    
    /**
     * Create a new Tenant detail view component.
     * 
     * @param api The DaaS API client.
     */
    constructor(private api: DaaSAPI, public validationController: ValidationController) { }

    /**
     * Has an error occurred?
     */
    @computedFrom('errorMessage')
    public get hasError(): boolean {
        return this.errorMessage !== null;
    }

    /**
     * Does the tenant creation form have any validation errors?
     */
    public get hasValidationErrors(): boolean {
        return this.validationController.errors.length !== 0;
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
        if (this.hasValidationErrors)
            return;

        if (this.newServer === null)
            return;

        if (this.newServer.name === null || this.newServer.adminPassword === null)
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
export class NewServer {
    name: string | null = null;
    adminPassword: string | null = null;
}

ValidationRules
    .ensure('name').displayName('Database name')
        .required()
        .minLength(5)
    .ensure('password').displayName('Administrator password')
        .required()
        .minLength(6)
    .on(NewServer);

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
