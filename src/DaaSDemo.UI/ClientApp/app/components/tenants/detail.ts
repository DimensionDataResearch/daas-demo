import { inject, factory, transient, computedFrom } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { RouteConfig } from 'aurelia-router';
import { bindable } from 'aurelia-templating';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { ConfirmDialog } from '../dialogs/confirm';
import { DaaSAPI, Tenant, Server, ProvisioningAction, ProvisioningStatus, ServerProvisioningPhase  } from '../api/daas-api';
import { ServerProvisioningPhaseProgress } from '../progress/server-provisioning-phase';

/**
 * Component for the Tenant detail view.
 */
@inject(DaaSAPI, NewInstance.of(ValidationController))
export class TenantDetail {
    private routeConfig: RouteConfig;
    private tenantId: string;
    private pollHandle: number = 0;
    
    @bindable public loading: boolean = false;
    @bindable public tenant: Tenant | null = null;
    @bindable public servers: Server[] = [];
    @bindable public errorMessage: string | null = null;
    @bindable public newServer: NewServer | null = null;

    @bindable private confirmDialog: ConfirmDialog
    
    /**
     * Create a new Tenant detail view model.
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
     * Does the tenant currently have at least one server?
     */
    @computedFrom('servers')
    public get hasServer(): boolean {
        return this.servers.length !== 0;
    }

    /**
     * Are any actions in progress for the tenant's servers?
     */
    @computedFrom('servers')
    public get areServerActionsInProgress(): boolean {
        const serverWithActionInProgress = this.servers.find(server => server.action !== ProvisioningAction.None);
        if (serverWithActionInProgress) {
            console.log('areServerActionsInProgress: true', serverWithActionInProgress);

            return true;
        } else {
            console.log('areServerActionsInProgress: false');

            return false;
        }
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

        await this.api.deploySqlServer(
            this.tenantId,
            this.newServer.name,
            this.newServer.adminPassword
        );

        this.hideCreateServerForm();

        await this.load(true);
    }

    /**
     * Reconfigure / repair a server.
     */
    public async reconfigureServer(server: Server): Promise<void> {
        this.clearError();
        
        try {
            const confirm = await this.confirmDialog.show('Repair Server',
                `Repair server "${server.name}"?`
            );
            if (!confirm)
                return;

            await this.api.reconfigureServer(server.id);
        }
        catch (error) {
            this.showError(error as Error);

            return;
        }

        await this.load(true);
    }

    /**
     * Destroy a server.
     */
    public async destroyServer(server: Server): Promise<void> {
        this.clearError();
        
        try {
            const confirm = await this.confirmDialog.show('Destroy Server',
                `Delete server "${server.name}"?`
            );
            if (!confirm)
                return;

            await this.api.destroyServer(server.id);
        }
        catch (error) {
            this.showError(error as Error);

            return;
        }

        await this.load(true);
    }

    /**
     * Clear the current error message (if any).
     */
    public clearError(): void {
        this.errorMessage = null;
    }

    /**
     * Show an error message.
     * 
     * @param error The error to show.
     */
    public showError(error: Error): void {
        console.log(error);
        
        this.errorMessage = (error.message as string || 'Unknown error.').split('\n').join('<br/>');
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
        
        this.load(false);
    }

    /**
     * Called when the component is deactivated.
     */
    public deactivate(): void {
        if (this.pollHandle != 0) {
            window.clearTimeout(this.pollHandle);
            this.pollHandle = 0;
        }
    }

    /**
     * Load tenant and server details.
     */
    private async load(isReload: boolean): Promise<void> {
        this.clearError();
        
        if (isReload)
            this.pollHandle = 0;
        else
            this.loading = true;

        try {
            const tenantRequest = this.api.getTenant(this.tenantId);
            const serversRequest = this.api.getTenantServers(this.tenantId);

            this.tenant = await tenantRequest;
            this.servers = await serversRequest;

            if (!this.tenant) {
                this.routeConfig.title = 'Tenant not found';
                this.errorMessage = `Tenant not found with Id ${this.tenantId}.`;
            } else {
                this.routeConfig.title = this.tenant.name;
            }

            if (this.areServerActionsInProgress) {
                this.pollHandle = window.setTimeout(() => this.load(true), 2000);
            }
        } catch (error) {
            this.showError(error as Error);
        }
        finally {
            if (!isReload)
                this.loading = false;
        }
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
    id: string;
}
