import { inject, factory, computedFrom } from 'aurelia-framework';
import { Router, RouteConfig } from 'aurelia-router';
import { bindable } from 'aurelia-templating';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { DaaSAPI } from '../../services/api/daas-api';
import { Database, DatabaseServerKind, ProvisioningAction, ProvisioningStatus } from '../../services/api/daas-models';
import { ToastService } from '../../services/toast/toast-service';

import { ConfirmDialog } from '../dialogs/confirm';
import { ViewModel } from '../common/view-model';

/**
 * Component for the Database detail view.
 */
@inject(DaaSAPI, Router, ToastService)
export class DatabaseDetail extends ViewModel {
    private databaseId: string;

    @bindable public database: Database | null = null;
    @bindable public confirmDialog: ConfirmDialog;

    /**
     * Create a new Database detail view model.
     * 
     * @param api The DaaS API client.
     * @param router The Aurelia router service.
     * @param toastService The toast-display service.
     */
    constructor(private api: DaaSAPI, private router: Router, toastService: ToastService) {
        super(toastService)
    }

    /**
     * Does the database exist?
     */
    @computedFrom('database')
    public get hasDatabase(): boolean {
        return this.database !== null;
    }

    /**
     * Is the database ready for use?
     */
    @computedFrom('database')
    public get isDatabaseReady(): boolean {
        return this.database !== null && this.database.status == ProvisioningStatus.Ready;
    }

    /**
     * The database connection string.
     */
    public get connectionString(): string | null {
        if (!this.database || !this.database.serverPublicFQDN || !this.database.serverPublicPort)
            return null;

        if (this.database.serverKind === DatabaseServerKind.SqlServer)
            return `Data Source=tcp:${this.database.serverPublicFQDN},${this.database.serverPublicPort};Initial Catalog=${this.database.name};User=${this.database.databaseUser};Password=<your password>`;
        else if (this.database.serverKind === DatabaseServerKind.RavenDB)
            return `http://${this.database.serverPublicFQDN}:${this.database.serverPublicPort}/`;
        else
            return null;
    }

    /**
     * Delete the database.
     */
    public async destroyDatabase(): Promise<void> {
        if (!this.database)
            return;

        const confirmed = await this.confirmDialog.show('Delete database',
            `Delete database '${this.database.name}'?`
        );
        if (!confirmed)
            return;

        const tenantId = this.database.tenantId;

        await this.runBusy(async () => {
            await this.api.deleteDatabase(this.databaseId);

            this.router.navigateToRoute('tenantDatabases', {
                tenantId: tenantId
            });
        });
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: RouteParams, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;
        this.databaseId = params.databaseId;
        
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
     * Load tenant and server details.
     */
    private async load(isReload: boolean): Promise<void> {
        this.database = await this.api.getDatabase(this.databaseId);
        
        if (!this.database) {
            this.routeConfig.title = 'Database not found';
            this.toastService.showWarning(
                `Database not found with Id ${this.databaseId}.`
            );
        } else {
            this.routeConfig.title = this.database.name;
        }

        if (this.database && this.database.action !== ProvisioningAction.None) {
            this.pollHandle = window.setTimeout(() => this.load(true), 2000);
        }
    }
}

/**
 * Route parameters for the Database detail view.
 */
interface RouteParams
{
    /**
     * The database Id.
     */
    databaseId: string;
}
