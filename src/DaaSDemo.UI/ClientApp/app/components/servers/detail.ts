/// <reference types="semantic-ui" />

import { inject, factory, transient, computedFrom, bindable } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { RouteConfig } from 'aurelia-router';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import * as $ from 'jquery';
import 'semantic';

import { DaaSAPI, Server, ProvisioningAction, ServerProvisioningPhase, ProvisioningStatus  } from '../api/daas-api';

/**
 * Component for the Server detail view.
 */
@inject(DaaSAPI, NewInstance.of(ValidationController))
export class ServerDetail {
    private routeConfig: RouteConfig;
    private serverId: number = 0;
    private pollHandle: number = 0;
    
    public loading: boolean = false;
    public server: Server | null = null;
    public errorMessage: string | null = null;

    @bindable public progressBar: Element;
    
    /**
     * Create a new Server detail view model.
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
     * Does the server exist?
     */
    @computedFrom('server')
    public get hasServer(): boolean {
        return this.server !== null;
    }

    /**
     * Is the server ready for use?
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
     * If an action is in progress for the server, its percentage completion.
     */
    @computedFrom('server')
    public get actionPercentComplete(): number {
        if (this.server === null)
            return 0;

        switch (this.server.phase) {
            case ServerProvisioningPhase.Instance:
                return 25;
            case ServerProvisioningPhase.Network:
                return 50;
            case ServerProvisioningPhase.Configuration:
                return 75;
            case ServerProvisioningPhase.Ingress:
                return 100;
            default:
                return 0;
        }
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: RouteParams, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;
        this.serverId = params.id;
        
        this.load(false);
    }

    /**
     * Stop polling the DaaS API to keep the server status up-to-date.
     */
    private stopPolling(): void {
        if (this.pollHandle === 0)
            return;

        window.clearInterval(this.pollHandle);
        this.pollHandle = 0;
    }

    /**
     * Load server and server details.
     */
    private async load(isReload: boolean): Promise<void> {
        this.errorMessage = null;
        
        if (!isReload)
            this.loading = true;

        try {
            this.server = await this.api.getServer(this.serverId);

            if (this.server === null)
            {
                this.routeConfig.title = 'Server not found';
                this.errorMessage = `Server not found with Id ${this.serverId}.`;
            }
            else
            {
                this.routeConfig.title = this.server.name;
                if (this.server.action != ProvisioningAction.None)
                    this.startPolling();
                else
                    this.stopPolling();

                if (this.progressBar)
                    $(this.progressBar).progress('set percent', this.actionPercentComplete);
            }
        } catch (error) {
            console.log(error);

            this.errorMessage = error.message;
        }
        finally {
            if (!isReload)
                this.loading = false;
        }
    }

    /**
     * Start polling the DaaS API to keep the server status up-to-date.
     */
    private startPolling(): void {
        if (this.pollHandle !== 0)
            return;

        this.pollHandle = window.setInterval(() => this.load(true), 4000);
    }
}

/**
 * Represents the form values for creating a server.
 */
export class NewServer {
    /**
     * The server's name.
     */
    name: string | null = null;

    /**
     * The server's administrator ("sa") password.
     */
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
 * Route parameters for the Server detail view.
 */
interface RouteParams
{
    /**
     * The server Id.
     */
    id: number;
}
