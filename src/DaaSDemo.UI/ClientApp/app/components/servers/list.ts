import { inject, computedFrom, bindable } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Server, ProvisioningAction } from '../api/daas-api';

@inject(DaaSAPI)
export class ServerList {
    private routeConfig: RouteConfig;
    private pollHandle: number = 0;

    @bindable public servers: Server[] = [];
    @bindable public errorMessage: string | null = null;
    public isLoading: boolean = false;

    constructor(private api: DaaSAPI) { }

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
            this.servers = await this.api.getServers();

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
    public async repairServer(server: Server): Promise<void> {
        await this.api.reconfigureServer(server.id);
        await this.load(true);
    }

    /**
     * Destroy a server.
     * 
     * @param server The server to destroy.
     */
    public async destroyServer(server: Server): Promise<void> {
        await this.api.destroyServer(server.id);
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
