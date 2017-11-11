import { inject, transient, TransientRegistration } from 'aurelia-framework';
import { HttpClient } from 'aurelia-fetch-client';

/**
 * Client for the Database-as-a-Service API.
 */
@transient()
@inject(HttpClient)
export class DaaSAPI
{
    /**
     * Create a new DaaS API client.
     * 
     * @param http An HTTP client.
     */
    constructor(private http: HttpClient)
    {
        http.configure(request =>
            request.withBaseUrl(
                'http://kr-cluster.tintoy.io:31200/api/v1/'
            )
            .withDefaults({
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            })
        );
    }

    /**
     * Get all tenants.
     * 
     * @returns The tenants, sorted by name.
     */
    public async getTenants(): Promise<Tenant[]> {
        const response = await this.http.fetch('tenants');
        const body = await response.json();

        return body as Tenant[];
    }

    /**
     * Get a tenant by Id.
     * 
     * @returns The tenant, or null if no tenant exists with the specified Id.
     */
    public async getTenant(id: number): Promise<Tenant | null> {
        const response = await this.http.fetch(`tenants/${id}`);
        if (response.ok) {
            const body = await response.json();
    
            return body as Tenant;
        }

        if (response.status == 400)
            return null;

        throw new Error(`Cannot find tenant with Id ${id}.`);
    }
}

/**
 * Represents a DaaS Tenant.
 */
export interface Tenant
{
    /**
     * The tenant Id.
     */
    id: number,
    
    /**
     * The tenant name.
     */
    name: string
}
