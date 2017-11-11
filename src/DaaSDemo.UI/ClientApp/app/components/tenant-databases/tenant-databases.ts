import { inject, computedFrom } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

import { DaaSAPI, Database, Tenant } from '../api/daas-api';

@inject(DaaSAPI)
export class TenantDatabaseList {
    private routeConfig: RouteConfig;
    private tenantId: number;

    public loading: boolean = false;
    public tenant: Tenant | null = null;
    public databases: Database[] | null = null;
    public newDatabase: NewDatabase | null = null;
    public errorMessage: string | null = null;

    /**
     * Create a new Server databases view component.
     * 
     * @param api The DaaS API client.
     */
    constructor(private api: DaaSAPI) { }

    /**
     * Has an error occurred?
     */
    @computedFrom('errorMessage')
    public get hasError(): boolean {
        return this.errorMessage !== null;
    }

    /**
     * Does the tenant exist?
     */
    @computedFrom('tenant')
    public get hasTenant(): boolean {
        return this.tenant !== null;
    }

    /**
     * Does the tenant have at least one database?
     */
    @computedFrom('databases')
    public get hasDatabase(): boolean {
        return this.databases !== null && this.databases.length !== 0;
    }

    /**
     * Is a database currently being added?
     */
    @computedFrom('newDatabase')
    public get addingDatabase(): boolean {
        return this.newDatabase !== null;
    }

    /**
     * Show the database creation form.
     */
    public showCreateDatabaseForm(): void {
        this.newDatabase = {
            name: null,
            user: null,
            password: null
        };
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
        if (this.newDatabase === null || this.newDatabase.name === null || this.newDatabase.user === null || this.newDatabase.password == null)
            return;

        try {
            await this.api.createTenantDatabase(
                this.tenantId,
                this.newDatabase.name,
                this.newDatabase.user,
                this.newDatabase.password
            );
        }
        catch (error) {
            console.log(error);

            this.errorMessage = (error.message as string || 'Unknown error.').split('\n').join('<br/>');

            return;
        }

        this.hideCreateDatabaseForm();

        await this.load();
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: RouteParams, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;
        this.tenantId = params.id;

        this.load();
    }

    /**
     * Load tenant and database details.
     */
    private async load(): Promise<void> {
        this.loading = true;

        try
        {
            const tenantRequest = this.api.getTenant(this.tenantId);
            const databasesRequest = this.api.getTenantDatabases(this.tenantId);
    
            this.tenant = await tenantRequest;
            
            if (this.tenant != null)
                this.routeConfig.title = `${this.tenant.name} - Databases`;
            else
                this.routeConfig.title = 'Tenant not found';
    
            this.databases = await databasesRequest;
        } catch (error) {
            console.log(error);

            this.errorMessage = error.message;
        }
        finally
        {
            this.loading = false;
        }
    }

    /**
     * Delete a database.
     * 
     * @param databaseId The database Id.
     */
    private async deleteDatabase(databaseId: number): Promise<void> {
        await this.api.deleteTenantDatabase(this.tenantId, databaseId);
        await this.load();
    }
}

/**
 * Represents the form values for creating a database.
 */
export interface NewDatabase {
    name: string | null;
    user: string | null;
    password: string | null;
}

/**
 * Route parameters for the Tenant databases view.
 */
interface RouteParams
{
    /**
     * The tenant Id.
     */
    id: number;
}