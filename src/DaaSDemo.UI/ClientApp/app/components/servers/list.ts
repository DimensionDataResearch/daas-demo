import { inject, computedFrom, bindable } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Server } from '../api/daas-api';

@inject(DaaSAPI)
export class ServerList {
    private routeConfig: RouteConfig;

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

        this.load();
    }

    /**
     * Load tenant details.
     */
    private async load(): Promise<void> {
        this.isLoading = true;

        try
        {
            this.servers = await this.api.getServers();
        }
        catch (error) {
            this.showError(error as Error)
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
