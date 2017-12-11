import { inject, NewInstance } from 'aurelia-framework';
import { computedFrom } from 'aurelia-binding';
import { Logger, getLogger } from 'aurelia-logging';
import { RouteConfig } from 'aurelia-router';
import { bindable } from 'aurelia-templating';
import { ValidationController } from 'aurelia-validation';

import { DaaSAPI } from '../../services/api/daas-api';
import { Tenant } from '../../services/api/daas-models';

import { NewTenant } from './forms/new';

const log: Logger = getLogger('TenantList');

/**
 * View model for the tenant list view.
 */
@inject(DaaSAPI, NewInstance.of(ValidationController))
export class TenantList {
    private routeConfig: RouteConfig;

    @bindable public isLoading: boolean = false;
    @bindable public tenants: Tenant[] = [];
    @bindable public newTenant: NewTenant | null = null;
    @bindable public errorMessage: string | null = null;

    /**
     * Create a new tenant list view model.
     * 
     * @param api The DaaS API client.
     * @param validationController The validation controller for the current context.
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
     * Is there at least one tenant?
     */
    @computedFrom('tenants')
    public get hasTenant(): boolean {
        return this.tenants.length !== 0;
    }

    /**
     * Is a tenant currently being added?
     */
    @computedFrom('newTenant')
    public get addingTenant(): boolean {
        return this.newTenant !== null;
    }

    /**
     * Show the tenant creation form.
     */
    public showCreateTenantForm(): void {
        this.newTenant = new NewTenant();
    }

    /**
     * Hide the tenant creation form.
     */
    public hideCreateTenantForm(): void {
        this.newTenant = null;
    }

    /**
     * Request creation of a new tenant.
     */
    public async createTenant(): Promise<void> {
        if (this.newTenant === null)
            return;

        if (this.newTenant.name == null)
            return;

        this.clearError();

        try {
            await this.api.createTenant(
                this.newTenant.name
            );
        }
        catch (error) {
            this.showError(error as Error);

            return;
        }

        await this.load(true);

        this.hideCreateTenantForm();
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
     * Load tenants.
     * 
     * @param isReload Is this a reload, rather than the initial load?
     */
    private async load(isReload: boolean): Promise<void> {
        this.clearError();
        
        if (!isReload)
            this.isLoading = true;

        try
        {
            this.tenants = await this.api.getTenants();
        } catch (error) {
            this.showError(error as Error);
        }
        finally
        {
            if (!isReload)
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
