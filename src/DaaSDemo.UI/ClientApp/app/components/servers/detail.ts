/// <reference types="semantic-ui" />

import { inject, factory, transient, computedFrom, bindable } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { Router, RouteConfig } from 'aurelia-router';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import * as $ from 'jquery';
import 'semantic';

import { ConfirmDialog } from '../dialogs/confirm';
import { DaaSAPI, Server, ProvisioningAction, ServerProvisioningPhase, ProvisioningStatus, DatabaseServerKind  } from '../api/daas-api';
import { ServerProvisioningPhaseProgress } from '../progress/server-provisioning-phase';

/**
 * Component for the Server detail view.
 */
@inject(Router, DaaSAPI, NewInstance.of(ValidationController))
export class ServerDetail {
    private routeConfig: RouteConfig;
    private serverId: string;
    private pollHandle: number = 0;
    
    public loading: boolean = false;
    public server: Server | null = null;
    public errorMessage: string | null = null;

    @bindable private progressBar: ServerProvisioningPhaseProgress
    @bindable private confirmDialog: ConfirmDialog
    
    /**
     * Create a new Server detail view model.
     * 
     * @param router The router service.
     * @param api The DaaS API client.
     * @param validationController The validation controller for the current context.
     */
    constructor(private router: Router, private api: DaaSAPI, public validationController: ValidationController) { }

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
     * Show the server's databases.
     */
    public showDatabases(): void {
        if (!this.server)
            return;

        // Cheat, for now.
        this.router.navigateToRoute('tenantDatabases', {
            id: this.server.tenantId
        });
    }

    /**
     * Destroy the server.
     */
    public async destroyServer(): Promise<void> {
        if (this.server === null)
            return;

        this.clearError();
        
        try {
            const confirm = await this.confirmDialog.show('Destroy Server',
                `Destroy server "${this.server.name}"?`
            );
            if (!confirm)
                return;

            await this.api.destroyServer(this.server.id);
        }
        catch (error) {
            this.showError(error as Error);

            return;
        }

        this.router.navigate('servers');
    }

    /**
     * Repair the server.
     */
    public async repairServer(): Promise<void> {
        if (this.server === null)
            return;

        this.clearError();
        
        try {
            const confirm = await this.confirmDialog.show('Repair Server',
                `Repair server "${this.server.name}"?`
            );
            if (!confirm)
                return;

            await this.api.reconfigureServer(this.server.id);
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
        this.serverId = params.id;
        
        this.load(false);
    }

    /**
     * Called when the component is deactivated.
     */
    public deactivate(): void {
        if (this.pollHandle != 0) {
            window.clearTimeout(this.pollHandle);
            this.pollHandle = 0;
        }
    }

    /**
     * Clear the current error message (if any).
     */
    public clearError(): void {
        this.errorMessage = null;
    }

    /**
     * Show an error message.
     * 
     * @param error The error to show.
     */
    public showError(error: Error): void {
        console.log(error);
        
        this.errorMessage = (error.message as string || 'Unknown error.').split('\n').join('<br/>');
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
                if (isReload) {
                    this.router.navigate('servers');

                    return;
                }

                this.routeConfig.title = 'Server not found';
                this.errorMessage = `Server not found with Id ${this.serverId}.`;
            }
            else
            {
                this.routeConfig.title = this.server.name;
                if (this.server.action != ProvisioningAction.None)
                {
                    this.pollHandle = window.setTimeout(() => this.load(true), 2000);

                    if (this.progressBar)
                        this.progressBar.update();
                }
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
}

/**
 * Represents the form values for creating a server.
 */
export class NewServer {
    /**
     * The server name.
     */
    name: string | null = null;

    /**
     * The kind of server to create.
     */
    kind: DatabaseServerKind;

    /**
     * The server administrative password.
     */
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
 * Route parameters for the Server detail view.
 */
interface RouteParams
{
    /**
     * The server Id.
     */
    id: string;
}
