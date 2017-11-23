import { inject, computedFrom, bindable } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { ConfirmDialog } from '../dialogs/confirm';
import { DaaSAPI, Database } from '../api/daas-api';
import { sortByName } from '../../utilities/sorting';

@inject(DaaSAPI)
export class DatabaseList {
    private routeConfig: RouteConfig;

    @bindable public databases: Database[] = [];
    @bindable public errorMessage: string | null = null;
    public isLoading: boolean = false;

    @bindable private confirmDialog: ConfirmDialog

    constructor(private api: DaaSAPI) { }

    /**
     * Are there no databases defined in the system?
     */
    @computedFrom('isLoading', 'databases')
    public get hasNoDatabases(): boolean {
        return !this.isLoading && this.databases.length === 0;
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
            this.databases = await this.api.getDatabases();
        } catch (error) {
            this.showError(error as Error);
        }
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
            this.databases = sortByName(
                await this.api.getDatabases()
            );
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
