import { inject, computedFrom, bindable } from 'aurelia-framework';
import { Logger, getLogger } from 'aurelia-logging';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI } from '../../services/api/daas-api';
import { DatabaseServer, Database } from '../../services/api/daas-models';

import { ConfirmDialog } from '../dialogs/confirm';
import { sortByName } from '../../utilities/sorting';

import { NewDatabase } from './forms/new';

const log: Logger = getLogger('DatabaseList');

@inject(DaaSAPI)
export class DatabaseList {
    private routeConfig: RouteConfig;
    
    @bindable public servers: DatabaseServer[] = [];
    @bindable public databases: Database[] = [];
    @bindable public newDatabase: NewDatabase | null = null;
    @bindable public errorMessage: string | null = null;
    public isLoading: boolean = false;

    @bindable private confirmDialog: ConfirmDialog

    /**
     * Create a new database list view model.
     * 
     * @param api The DaaS API client.
     */
    constructor(private api: DaaSAPI) { }

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
    @computedFrom('newDatabase')
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
                await this.api.getDatabases()
            );
        } catch (error) {
            this.showError(error as Error);
        }
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
    public activate(params: any, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;

        this.load();
    }

    /**
     * Load tenant details.
     */
    private async load(): Promise<void> {
        this.isLoading = true;

        try
        {
            const serversRequest = this.api.getServers();
            const databasesRequest = this.api.getDatabases();

            this.servers = await serversRequest;
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
        log.error('Unexpected error: ', error);
        
        this.errorMessage = (error.message as string || 'Unknown error.').split('\n').join('<br/>');
    }
}
