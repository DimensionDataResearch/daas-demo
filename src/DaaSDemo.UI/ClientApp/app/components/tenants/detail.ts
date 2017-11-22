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
    private tenantId: number;
    private pollHandle: number = 0;
    
    @bindable public loading: boolean = false;
    @bindable public tenant: Tenant | null = null;
    @bindable public server: Server | null = null;
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
        return this.server !== null && this.server.status === ProvisioningStatus.Ready;
    }

    /**
     * Is a provisioning action currently in progress for the tenant's server?
     */
    @computedFrom('server')
    public get isServerActionInProgress(): boolean {
        return this.server !== null && this.server.action !== ProvisioningAction.None;
    }

    /**
     * The current provisioning phase (if any) for the tenant's server.
     */
    @computedFrom('server')
    public get serverPhaseDescription(): string | null {
        if (!this.server || !this.isServerActionInProgress)
            return null;

        switch (this.server.phase) {
            case ServerProvisioningPhase.None:
                return 'Waiting';
                case ServerProvisioningPhase.Storage:
                return 'Storage';
            case ServerProvisioningPhase.Instance:
                return 'Server Instance';
            case ServerProvisioningPhase.Network:
                return 'Internal Network';
            case ServerProvisioningPhase.Monitoring:
                return 'Monitoring';
            case ServerProvisioningPhase.Configuration:
                return 'Server Configuration';
            case ServerProvisioningPhase.Ingress:
                return 'External Network';
            default:
                return this.server.phase;
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

        this.server = null;

        await this.api.deployTenantSqlServer(
            this.tenantId,
            this.newServer.name,
            this.newServer.adminPassword
        );

        await this.load(true);

        this.hideCreateServerForm();
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
            const serverRequest = this.api.getTenantServer(this.tenantId);

            this.tenant = await tenantRequest;
            this.server = await serverRequest;

            if (!this.tenant) {
                this.routeConfig.title = 'Tenant not found';
                this.errorMessage = `Tenant not found with Id ${this.tenantId}.`;
            } else {
                this.routeConfig.title = this.tenant.name;
            }

            if (this.server && this.server.action !== ProvisioningAction.None) {
                this.pollHandle = window.setTimeout(() => this.load(true), 1000);
            }
        } catch (error) {
            this.showError(error as Error);
        }
        finally {
            if (!isReload)
                this.loading = false;
        }
    }

    /**
     * Destroy the tenant's server.
     */
    private async destroyServer(): Promise<void> {
        this.clearError();
        
        try {
            if (!this.tenant || !this.server || !this.confirmDialog)
                return;

            const confirm = await this.confirmDialog.show('Destroy Server',
                `Delete server "${this.server.name}"?`
            );
            if (!confirm)
                return;

            await this.api.destroyTenantServer(
                this.tenant.id
            );
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
    private clearError(): void {
        this.errorMessage = null;
    }

    /**
     * Show an error message.
     * 
     * @param error The error to show.
     */
    private showError(error: Error): void {
        console.log(error);
        
        this.errorMessage = (error.message as string || 'Unknown error.').split('\n').join('<br/>');
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
