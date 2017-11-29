import { inject, computedFrom, bindable } from 'aurelia-framework';
import { Router, RouteConfig } from 'aurelia-router';

import { DaaSAPI, DatabaseServer, ProvisioningAction } from '../api/daas-api';
import { ConfirmDialog } from '../dialogs/confirm';
import { sortByName } from '../../utilities/sorting';

@inject(DaaSAPI, Router)
export class ServerList {
    private routeConfig: RouteConfig;
    private pollHandle: number = 0;

    @bindable public servers: DatabaseServer[] = [];
    @bindable public errorMessage: string | null = null;
    @bindable public isLoading: boolean = false;

    @bindable private confirmDialog: ConfirmDialog

    constructor(private api: DaaSAPI, private router: Router) { }

    /**
     * Are there no servers defined in the system?
     */
    @computedFrom('isLoading', 'servers')
    public get hasNoServers(): boolean {
        return !this.isLoading && this.servers.length == 0;
    }

    /**
     * Has an error occurred?
     */
    @computedFrom('errorMessage')
    public get hasError(): boolean {
        return this.errorMessage !== null;
    }

    /**
     * Refresh the server list.
     */
    public async refreshServerList(): Promise<void> {
        this.clearError();

        try {
            this.servers = await this.api.getServers();
        } catch (error) {
            this.showError(error as Error);
        }
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: any, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;

        this.load(false);
    }

    /**
     * Called when the component is deactivated.
     */
    public deactivate(): void {
        if (this.pollHandle) {
            window.clearTimeout(this.pollHandle);
            this.pollHandle = 0;
        }
    }

    /**
     * Load tenant details.
     * 
     * @param isReload False if this is the initial load.
     */
    private async load(isReload: boolean): Promise<void> {
        if (!isReload)
            this.isLoading = true;

        try
        {
            this.servers = sortByName(
                await this.api.getServers()
            );

            if (this.servers && this.servers.find(server => server.action != ProvisioningAction.None)) {
                this.pollHandle = window.setTimeout(() => this.load(true), 2000);
            } else {
                this.pollHandle = 0;
            }
        }
        catch (error) {
            this.showError(error as Error)
        }
        finally {
            if (!isReload)
                this.isLoading = false;
        }
    }

    /**
     * Repair a server.
     * 
     * @param server The server to repair.
     */
    public async repairServer(server: DatabaseServer): Promise<void> {
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
     * Show a server's databases.
     * 
     * @param server The target server.
     */
    public showDatabases(server: DatabaseServer): void {
        // Cheat, for now.
        this.router.navigateToRoute('serverDatabases', {
            serverId: server.id
        });
    }

    /**
     * Destroy a server.
     * 
     * @param server The server to destroy.
     */
    public async destroyServer(server: DatabaseServer): Promise<void> {
        this.clearError();
        
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
