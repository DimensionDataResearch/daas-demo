import { inject, transient, TransientRegistration } from 'aurelia-framework';
import { HttpClient, json } from 'aurelia-fetch-client';

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
     * Get information about all tenants.
     * 
     * @returns The tenants, sorted by name.
     */
    public async getTenants(): Promise<Tenant[]> {
        const response = await this.http.fetch('tenants');
        const body = await response.json();

        return body as Tenant[];
    }

    /**
     * Get information about all databases.
     * 
     * @returns The databases, sorted by server and then name.
     */
    public async getDatabases(): Promise<Database[]> {
        const response = await this.http.fetch('databases');
        const body = await response.json();

        return body as Database[];
    }

    /**
     * Get information about a specific tenant.
     * 
     * @param tenantId The tenant Id.
     * @returns The tenant, or null if no tenant exists with the specified Id.
     */
    public async getTenant(tenantId: number): Promise<Tenant | null> {
        const response = await this.http.fetch(`tenants/${tenantId}`);
        const body = await response.json();

        if (response.ok)
            return body as Tenant;

        if (response.status === 404)
        {
            const notFound = body as NotFoundResponse;
            if (notFound.entityType == 'DatabaseServer')
                return null;
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to retrieve details for tenant with Id ${tenantId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Get information about a tenant's SQL server instance.
     * 
     * @param tenantId The Id of the tenant that owns the server.
     * @returns The server, or null if the tenant does not have a server.
     */
    public async getTenantServer(tenantId: number): Promise<Server | null> {
        const response = await this.http.fetch(`tenants/${tenantId}/server`);
        const body = await response.json();

        if (response.ok) {
            return body as Server;
        }

        if (response.status === 404)
        {
            const notFound = body as NotFoundResponse;
            if (notFound.entityType == 'DatabaseServer')
                return null;
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to retrieve details for server owned by tenant with Id ${tenantId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Get information about a tenant's SQL server instance.
     * 
     * @param tenantId The Id of the tenant that owns the server.
     * @returns The databases, or null if the tenant does not have a server.
     */
    public async getTenantDatabases(tenantId: number): Promise<Database[] | null> {
        const response = await this.http.fetch(`tenants/${tenantId}/databases`);
        const body = await response.json();

        if (response.ok) {
            return body as Database[];
        }

        if (response.status == 404)
        {
            const notFound = body as NotFoundResponse;
            if (notFound.entityType == 'DatabaseServer')
                return null;
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to retrieve databases owned by tenant with Id ${tenantId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Deploy a tenant's database server.
     * 
     * @param tenantId The tenant Id.
     * @param databaseId The database Id.
     * 
     * @returns The Id of the new server.
     */
    public async deployTenantServer(tenantId: number, name: string, adminPassword: string): Promise<number> {
        const response = await this.http.fetch(`tenants/${tenantId}/server`, {
            method: 'POST',
            body: json({
                name: name,
                adminPassword: adminPassword
            })
        });
        
        const body = await response.json();
        if (response.ok) {
            const serverCreated = body as ServerCreated;

            return serverCreated.id;
        }
        
        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to create server for tenant with Id ${tenantId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Destroy a tenant's database server.
     * 
     * @param tenantId The tenant Id.
     */
    public async destroyTenantServer(tenantId: number): Promise<void> {
        const response = await this.http.fetch(`tenants/${tenantId}/server`, {
            method: 'DELETE'
        });
        
        if (response.ok || response.status === 404) {
            return
        }

        const body = await response.json();
        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to delete server for tenant with Id ${tenantId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Create a database for a tenant.
     * 
     * @param tenantId The tenant Id.
     * @param name The database name.
     * @param user The database user name.
     * @param password The database user password.
     */
    public async createTenantDatabase(tenantId: number, name: string, user: string, password: string): Promise<number> {
        const response = await this.http.fetch(`tenants/${tenantId}/databases`, {
            method: 'POST',
            body: json({
                tenantId: tenantId,
                name: name,
                databaseUser: user,
                databasePassword: password
            })
        });
        
        const body = await response.json();

        if (response.ok) {
            const databaseCreated = body as DatabaseCreated;
            
            return databaseCreated.id;
        }

        if (response.status == 400) {
            const invalidModelResponse = body as InvalidModelResponse;

            let validationMessages: string = 'Invalid request:\n';
            for (const propertyName of Object.getOwnPropertyNames(invalidModelResponse)) {
                const errorMessages: string[] = invalidModelResponse[propertyName];
                for (const errorMessage of errorMessages) {
                    validationMessages += `- ${propertyName}: ${errorMessage}`;
                    validationMessages += '\n';
                }
            }

            throw new Error(validationMessages);
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to create database for tenant with Id ${tenantId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Delete a tenant's database.
     * 
     * @param tenantId The tenant Id.
     * @param databaseId The database Id.
     */
    public async deleteTenantDatabase(tenantId: number, databaseId: number): Promise<void> {
        const response = await this.http.fetch(`tenants/${tenantId}/databases/${databaseId}`, {
            method: 'DELETE'
        });
        
        if (response.ok || response.status === 400) {
            return
        }

        const body = await response.json();
        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to delete database with Id ${databaseId} owned by tenant with Id ${tenantId}: ${errorResponse.message || 'Unknown error.'}`
        );
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
    id: number;
    
    /**
     * The tenant name.
     */
    name: string;
}

/**
 * Represents a DaaS SQL Server instance.
 */
export interface Server
{
    /**
     * The server Id.
     */
    id: number;
    
    /**
     * The server name.
     */
    name: string;

    /**
     * The Id of the tenant that owns the server.
     */
    tenantId: number;

    /**
     * The currently-requested action (if any) for the server.
     */
    action?: string | null;

    /**
     * The server status.
     */
    status?: string | null;

    /**
     * The fully-qualified domain name (if any) on which the server is externally accessible.
     */
    publicFQDN?: string | null;

    /**
     * The TCP port (if any) on which the server is externally accessible.
     */
    publicPort?: number | null;
}

/**
 * Represents an instance of an SQL Server database.
 */
export interface Database {
    /**
     * The database Id.
     */
    id: number;
    
    /**
     * The database name.
     */
    name: string;

    /**
     * The database's currently-requested provisioning action (if any).
     */
    action: string;

    /**
     * The database provisioning status.
     */
    status: string;

    /**
     * The database connection string (if available).
     */
    connectionString: string | null;
}

/**
 * Represents a generic response from the DaaS API.
 */
interface ApiResponse {
    /**
     * An informational message describing the error.
     */
    message?: string;
}

interface ServerCreated extends ApiResponse {
    id: number;
    name: string;
}

interface DatabaseCreated extends ApiResponse {
    id: number;
    name: string;
}

/**
 * Represents the error response returned by the DaaS API with a 404 status code.
 */
interface NotFoundResponse extends ApiResponse {
    /**
     * The Id of the entity that was not found.
     */
    id: number;

    /**
     * The type of entity that was not found.
     */
    entityType: string;
}

/**
 * Represents the error response returned when 
 */
interface InvalidModelResponse {
    /**
     * Get the validation messages for the specified property.
     */
    [propertyName: string]: string[];
}
