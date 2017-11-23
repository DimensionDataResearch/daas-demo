import { inject, computedFrom, PLATFORM, bindable, factory } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { RouteConfig } from 'aurelia-router';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { DaaSAPI, Server, Database, Tenant, ProvisioningAction } from '../../api/daas-api';
import { ConfirmDialog } from '../../dialogs/confirm';
import { NewDatabase } from './forms/new';

@inject(DaaSAPI, NewInstance.of(ValidationController))
export class TenantDatabaseList {
    private routeConfig: RouteConfig;
    private tenantId: string;

    private pollHandle: number = 0;

    @bindable public isLoading: boolean = false;
    @bindable public tenant: Tenant | null = null;
    @bindable public servers: Server[] = [];
    @bindable public databases: Database[] | null = null;
    @bindable public newDatabase: NewDatabase | null = null;
    @bindable public errorMessage: string | null = null;

    @bindable private confirmDialog: ConfirmDialog

    /**
     * Create a new tenant databases view model.
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
     * Does the database creation form have any validation errors?
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
     * Does the tenant have at least one database?
     */
    @computedFrom('databases')
    public get hasDatabase(): boolean {
        return this.databases !== null && this.databases.length !== 0;
    }

    /**
     * Is a database currently being added?
     */
    @computedFrom('newDatabase')
    public get addingDatabase(): boolean {
        return this.newDatabase !== null;
    }

    /**
     * Should the "tenant has no databases." message be displayed?
     */
    @computedFrom('databases', 'hasDatabase', 'addingDatabase')
    public get shouldShowNoDatabasesMessage(): boolean {
        return !this.isLoading && !this.hasDatabase && !this.addingDatabase;
    }

    /**
     * Show the database creation form.
     */
    public showCreateDatabaseForm(): void {
        this.newDatabase = new NewDatabase();
    }

    /**
     * Hide the database creation form.
     */
    public hideCreateDatabaseForm(): void {
        this.newDatabase = null;
    }

    /**
     * Request creation of a new database.
     */
    public async createDatabase(): Promise<void> {
        if (this.newDatabase === null)
            return;

        if (this.validationController.errors.length)
            return;

        if (this.newDatabase.serverId == null || this.newDatabase.name == null || this.newDatabase.user == null || this.newDatabase.password == null)
            return;

        this.clearError();

        try {
            await this.api.createDatabase(
                this.newDatabase.serverId,
                this.newDatabase.name,
                this.newDatabase.user,
                this.newDatabase.password
            );
        }
        catch (error) {
            this.showError(error as Error);

            return;
        }

        this.hideCreateDatabaseForm();

        await this.load(true);
    }

    /**
     * Destroy the specified database.
     * 
     * @param database The database to destroy.
     */
    public async destroyDatabase(database: Database): Promise<void> {
        this.clearError();

        try {
            if (!this.confirmDialog)
                return;

            const confirm = await this.confirmDialog.show('Delete Database',
                `Delete database "${database.name}"?`
            );
            if (!confirm)
                return;

            await this.api.deleteDatabase(database.id);
        }
        catch (error) {
            this.showError(error as Error);

            return;
        }

        await this.load(true);
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
        if (this.pollHandle !== 0) {
            window.clearTimeout(this.pollHandle);
            this.pollHandle = 0;
        }
    }

    /**
     * Load tenant and database details.
     * 
     * @param isReload Is this a reload, rather than the initial load?
     */
    private async load(isReload: boolean): Promise<void> {
        this.clearError();
        
        if (!isReload)
            this.isLoading = true;

        try
        {
            const tenantRequest = this.api.getTenant(this.tenantId);
            const serversRequest = this.api.getTenantServers(this.tenantId);
            const databasesRequest = this.api.getTenantDatabases(this.tenantId);
    
            this.tenant = await tenantRequest;
            
            if (this.tenant != null)
                this.routeConfig.title = `${this.tenant.name} - Databases`;
            else
                this.routeConfig.title = 'Tenant not found';
    
            this.servers = await serversRequest;
            this.databases = await databasesRequest;

            if (this.databases && this.databases.find(database => database.action != ProvisioningAction.None)) {
                this.pollHandle = window.setTimeout(() => this.load(true), 2000);
            }
        } catch (error) {
            this.showError(error as Error);
        }
        finally
        {
            if (!isReload)
                this.isLoading = false;
        }
    }

    /**
     * Delete a database.
     * 
     * @param databaseId The database.
     */
    private async deleteDatabase(database: Database): Promise<void> {
        await this.api.deleteDatabase(database.id);
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
 * Route parameters for the Tenant databases view.
 */
interface RouteParams
{
    /**
     * The tenant Id.
     */
    id: string;
}
