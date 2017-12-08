import { inject, factory, transient, computedFrom } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { Router, RouteConfig } from 'aurelia-router';
import { bindable } from 'aurelia-templating';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { ToastService } from '../../services/toast/toast-service';
import { DaaSAPI } from '../../services/api/daas-api';
import { Tenant, DatabaseServer, DatabaseServerKind, ProvisioningAction } from '../../services/api/daas-models';

import { sortByName } from '../../utilities/sorting';
import { ViewModel } from '../common/view-model';

import { ConfirmDialog } from '../dialogs/confirm';
import { ServerProvisioningPhaseProgress } from '../progress/server-provisioning-phase';

/**
 * Component for the Tenant detail view.
 */
@inject(DaaSAPI, ToastService, Router, NewInstance.of(ValidationController))
export class TenantDetail extends ViewModel {
    private tenantId: string;

    @bindable public tenant: Tenant | null = null;
    @bindable public servers: DatabaseServer[] = [];
    @bindable public errorMessage: string | null = null;
    @bindable public newServer: NewServer | null = null;

    @bindable private confirmDialog: ConfirmDialog
    
    /**
     * Create a new Tenant detail view model.
     * 
     * @param api The DaaS API client.
     * @param toastService The toast-display service.
     */
    constructor(private api: DaaSAPI, toastService: ToastService, private router: Router, public validationController: ValidationController) {
        super(toastService);
    }

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
        return !!this.servers.find(server => server.action !== ProvisioningAction.None);
    }

    /**
     * Is a server currently being added?
     */
    @computedFrom('newServer')
    public get addingServer(): boolean {
        return this.newServer !== null;
    }

    /**
     * Should the password field be displayed?
     */
    @computedFrom('newServer')
    public get showPasswordField(): boolean {
        return this.newServer !== null && this.newServer.kind == DatabaseServerKind.SqlServer;
    }

    /**
     * Refresh the list of servers.
     */
    public async refreshServerList(): Promise<void> {
        await this.runBusy(
            () => this.load()
        );
    }

    /**
     * Show the server creation form.
     */
    public showCreateServerForm(): void {
        this.newServer = new NewServer();
        this.newServer.kind = DatabaseServerKind.SqlServer; // The default.
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

        if (this.newServer.name === null)
            return;

        try {
            let serverId: string;
            switch (this.newServer.kind) {
                case DatabaseServerKind.SqlServer: {
                    serverId = await this.api.deploySqlServer(
                        this.tenantId,
                        this.newServer.name,
                        this.newServer.adminPassword || ''
                    );
    
                    break;
                }
                case DatabaseServerKind.RavenDB: {
                    serverId = await this.api.deployRavenServer(
                        this.tenantId,
                        this.newServer.name
                    );
    
                    break;
                }
                default: {
                    throw new Error(`Unsupported server kind (${this.newServer.kind}).`);
                }
            }
            
            this.router.navigateToRoute('server', { serverId: serverId });
        } catch (error) {
            this.showError(error as Error);
        }
    }

    /**
     * Show the server's databases.
     * 
     * @param server The target server.
     */
    public showDatabases(server: DatabaseServer): void {
        this.router.navigateToRoute('serverDatabases', {
            serverId: server.id
        });
    }

    /**
     * Show the server's event stream.
     * 
     * @param server The target server.
     */
    public showEvents(server: DatabaseServer): void {
        this.router.navigateToRoute('serverEvents', {
            serverId: server.id
        });
    }

    /**
     * Reconfigure / repair a server.
     */
    public async reconfigureServer(server: DatabaseServer): Promise<void> {
        this.toastService.dismissAll();
        
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

        await this.runBusyAsync(
            () => this.load()
        );
    }

    /**
     * Destroy a server.
     */
    public async destroyServer(server: DatabaseServer): Promise<void> {
        this.toastService.dismissAll();
        
        try {
            const confirm = await this.confirmDialog.show('Destroy Server',
                `Destroy server "${server.name}"?`
            );
            if (!confirm)
                return;

            await this.api.destroyServer(server.id);
        }
        catch (error) {
            this.showError(error as Error);

            return;
        }

        await this.runBusyAsync(
            () => this.load()
        );
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: RouteParams, routeConfig: RouteConfig): void {
        super.activate(params, routeConfig);
        
        this.tenantId = params.tenantId;
        
        this.runLoadingAsync(
            () => this.load()
        );
    }

    /**
     * Load tenant and server details.
     */
    private async load(): Promise<void> {
        this.toastService.dismissAll();
        
        const tenantRequest = this.api.getTenant(this.tenantId);
        const serversRequest = this.api.getTenantServers(this.tenantId);

        this.tenant = await tenantRequest;
        this.servers = sortByName(
            await serversRequest
        );

        if (!this.tenant) {
            this.routeConfig.title = 'Tenant not found';
            this.errorMessage = `Tenant not found with Id ${this.tenantId}.`;
        } else {
            this.routeConfig.title = this.tenant.name;
        }

        if (this.areServerActionsInProgress) {
            this.pollHandle = window.setTimeout(() => this.load(), 2000);
        }
    }
}

/**
 * Represents the form values for creating a server.
 */
export class NewServer {
    name: string | null = null;
    kind: DatabaseServerKind;
    adminPassword: string | null = null;
}

ValidationRules
    .ensure<NewServer, string>('name').displayName('Database name')
        .required()
        .minLength(5)
    .ensure<string>('adminPassword').displayName('Administrator password')
        .satisfies((adminPassword, newServer) => {
            if (!newServer || newServer.kind !== DatabaseServerKind.SqlServer)
                return true;
            
            return !!adminPassword;
        }).withMessage('Administrator password is required for SQL Server')
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
    tenantId: string;
}
