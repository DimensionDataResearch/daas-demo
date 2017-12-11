import { inject, computedFrom, bindable } from 'aurelia-framework';
import { Logger, getLogger } from 'aurelia-logging';
import { Router, RouteConfig } from 'aurelia-router';

import { dateDiff, DatePart } from '../../utilities/date-and-time';

import { DaaSAPI } from '../../services/api/daas-api';
import { DatabaseServer, DatabaseServerEvent } from '../../services/api/daas-models';

// TODO: Create separate components to render various event types.

const Log: Logger = getLogger('ServerEvents');

/**
 * View model for the server events view.
 */
@inject(DaaSAPI)
export class ServerEvents {
    private routeConfig: RouteConfig
    private serverId: string;

    @bindable public server: DatabaseServer | null = null;
    @bindable public events: DatabaseServerEvent[] = [];

    @bindable public errorMessage: string | null = null;
    @bindable public isLoading: boolean = false;
    @bindable public isRefreshing: boolean = false;

    constructor(private api: DaaSAPI) { }

    /**
     * Has an error occurred?
     */
    @computedFrom('errorMessage')
    public get hasError(): boolean {
        return this.errorMessage !== null;
    }

    /**
     * Are there any events for the target server?
     */
    public get hasEvents(): boolean {
        return this.events.length !== 0;
    }

    /**
     * Format an event's timestamp for display.
     * 
     * @param event The target event.
     */
    public formatTimestamp(event: DatabaseServerEvent): string {
        const now = new Date();
        const timestamp = new Date(event.timestamp);

        const weeks = dateDiff(DatePart.Weeks, timestamp, now);
        if (weeks == 1)
            return weeks + ' week ago';
        else if (weeks > 1)
            return weeks + ' weeks ago';

        const days = dateDiff(DatePart.Days, timestamp, now);
        if (days == 1)
            return days + ' day ago';
        else if (days > 1)
            return days + ' days ago';

        const hours = dateDiff(DatePart.Hours, timestamp, now);
        if (hours == 1)
            return hours + ' hour ago';
        else if (hours > 1)
            return hours + ' hours ago';

        const minutes = dateDiff(DatePart.Minutes, timestamp, now);
        if (minutes == 1)
            return minutes + ' minute ago';
        else if (minutes > 1)
            return minutes + ' minutes ago';

        const seconds = dateDiff(DatePart.Seconds, timestamp, now);
        if (seconds == 1)
            return seconds + ' second ago';
        else if (seconds > 1)
            return seconds + ' seconds ago';

        return 'Just now';
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: RouteParams, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;
        this.serverId = params.serverId;

        this.load(false);
    }

    /**
     * Load tenant details.
     * 
     * @param isReload False if this is the initial load.
     */
    private async load(isReload: boolean): Promise<void> {
        if (!isReload)
            this.isLoading = true;
        else
            this.isRefreshing = true;

        try
        {
            const serverRequest = this.api.getServer(this.serverId);
            const eventsRequest = this.api.getServerEvents(this.serverId);

            this.server = await serverRequest;
            if (this.server) {
                this.routeConfig.title = `Events (${this.server.name})`;
            } else {
                this.routeConfig.title = 'Events (server)';
            }

            this.events = await eventsRequest;
        }
        catch (error) {
            this.showError(error as Error)
        }
        finally {
            if (!isReload)
                this.isLoading = false;
                else
                this.isRefreshing = false;
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
        Log.error('Unexpected error: ', error);
        
        this.errorMessage = (error.message as string || 'Unknown error.').split('\n').join('<br/>');
    }
}

/**
 * Route parameters for the server events view.
 */
interface RouteParams {
    serverId: string;
}
