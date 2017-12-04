import { inject, computedFrom, bindable } from 'aurelia-framework';
import { Router, RouteConfig } from 'aurelia-router';

import { DaaSAPI } from '../../services/api/daas-api';
import { DatabaseServer, Database, ProvisioningAction } from '../../services/api/daas-models';

import { ConfirmDialog } from '../dialogs/confirm';
import { sortByName } from '../../utilities/sorting';

import { NewDatabase } from './forms/new';

@inject(DaaSAPI, Router)
export class DatabaseListForServer {
    private routeConfig: RouteConfig;
    private serverId: string;
    private pollHandle: number = 0;

    @bindable public server: DatabaseServer;
    @bindable public databases: Database[] = [];
    @bindable public newDatabase: NewDatabase | null = null;
    @bindable public errorMessage: string | null = null;
    public isLoading: boolean = false;

    @bindable private confirmDialog: ConfirmDialog

    /**
     * Create a new server-scoped database list view model.
     * 
     * @param api The DaaS API client.
     * @param router The router service.
     */
    constructor(private api: DaaSAPI, private router: Router) { }

    /**
     * Servers for the add-database form.
     */
    @computedFrom('server')
    public get servers(): DatabaseServer[] {
        return [ this.server ];
    }

    /**
     * Are there no databases defined in the system?
     */
    @computedFrom('isLoading', 'databases')
    public get hasNoDatabases(): boolean {
        return !this.isLoading && this.databases && this.databases.length === 0;
    }

    /**
     * Is the add-database form displayed?
     */
    public get addingDatabase(): boolean {
        return this.newDatabase !== null;
    }

    /**
     * Has an error occurred?
     */
    @computedFrom('errorMessage')
    public get hasError(): boolean {
        return this.errorMessage !== null;
    }

    /**
     * Refresh the database list.
     */
    public async refreshDatabaseList(): Promise<void> {
        this.clearError();

        try {
            this.databases = sortByName(
                await this.api.getServerDatabases(this.serverId)
            );

            if (this.databases.find(database => database.action !== ProvisioningAction.None)) {
                this.pollHandle = window.setTimeout(() => this.refreshDatabaseList(), 2000);
            } else {
                this.pollHandle = 0;
            }
        } catch (error) {
            this.showError(error as Error);
        }
    }

    /**
     * Show the database creation form.
     */
    public showCreateDatabaseForm(): void {
        this.newDatabase = new NewDatabase(this.serverId);
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

        await this.refreshDatabaseList();

        this.hideCreateDatabaseForm();
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

        await this.refreshDatabaseList();
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: RouteParams, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;
        if (!params.serverId) {
            this.router.navigateToRoute('databases')

            return;
        }

        this.serverId = params.serverId;

        this.load();
    }

    public deactivate(): void {
        if (this.pollHandle !== 0) {
            window.clearTimeout(this.pollHandle);
            this.pollHandle = 0;
        }
    }

    /**
     * Load tenant details.
     */
    private async load(): Promise<void> {
        this.isLoading = true;

        try
        {
            const serverRequest = this.api.getServer(this.serverId);
            const databasesRequest = this.serverId ? this.api.getServerDatabases(this.serverId) : this.api.getDatabases();

            const server = await serverRequest;
            if (!server)
                throw new Error(`Server not found with Id '${this.serverId}'.`);

            this.server = server;
            this.routeConfig.title = `Databases (${this.server.name})`;

            this.databases = sortByName(
                await databasesRequest
            );
        }
        catch (error) {
            this.showError(error);
        }
        finally {
            this.isLoading = false;
        }
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
 * Route parameters for the database list view.
 */
interface RouteParams {
    /**
     * If specified, then only databases for the specified server will be shown.
     */
    serverId: string;
}
