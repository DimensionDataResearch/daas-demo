import { inject, singleton, NewInstance } from 'aurelia-framework';
import { HttpClient, json, Interceptor, RequestInit } from 'aurelia-fetch-client';

import { ConfigService, Configuration } from '../config/config-service';
import {
    User,
    Tenant,
    DatabaseServer,
    DatabaseServerKind,
    DatabaseServerEvent,
    Database
} from './daas-models';
import { AuthenticationInterceptor } from '../authx/auth-interceptor';

/**
 * Client for the Database-as-a-Service API.
 */
@singleton()
@inject(NewInstance.of(HttpClient), ConfigService, AuthenticationInterceptor)
export class DaaSAPI
{
    private _lastETagInterceptor: LastETagInterceptor = new LastETagInterceptor();
    private _authenticationInterceptor: AuthenticationInterceptor;
    private _configured: Promise<void>;

    /**
     * Create a new DaaS API client.
     * 
     * @param http An HTTP client.
     */
    constructor(private http: HttpClient, private configService: ConfigService, authenticationInterceptor: AuthenticationInterceptor)
    {
        this._authenticationInterceptor = authenticationInterceptor;
        this._configured = this.configure();
    }

    /**
     * The last ETag (if any) returned by a DaaS API call.
     */
    public get lastETag(): number {
        return this._lastETagInterceptor.lastEtag;
    }

    /**
     * Get information about all users.
     * 
     * @returns The users, sorted by name.
     */
    public async getUsers(): Promise<User[]> {
        await this._configured;

        const response = await this.http.fetch('admin/users');
        const body = await response.json();

        return body as User[];
    }

    /**
     * Get information about all tenants.
     * 
     * @returns The tenants, sorted by name.
     */
    public async getTenants(): Promise<Tenant[]> {
        await this._configured;

        const response = await this.http.fetch('tenants');
        const body = await response.json();

        return body as Tenant[];
    }

    /**
     * Get information about all servers.
     * 
     * @returns The tenants, sorted by name.
     */
    public async getServers(): Promise<DatabaseServer[]> {
        await this._configured;

        const response = await this.http.fetch('servers');
        const body = await response.json();

        return body as DatabaseServer[];
    }

    /**
     * Get information about all databases.
     * 
     * @returns The databases, sorted by server and then name.
     */
    public async getDatabases(): Promise<Database[]> {
        await this._configured;

        const response = await this.http.fetch('databases');
        const body = await response.json();

        return body as Database[];
    }

    /**
     * Get information about a specific database.
     * 
     * @param databaseId The database Id.
     * @returns The database, or null if no database exists with the specified Id.
     */
    public async getDatabase(databaseId: string): Promise<Database | null> {
        await this._configured;

        const response = await this.http.fetch(`databases/${databaseId}`);
        const body = await response.json();

        if (response.ok)
            return body as Database;

        if (response.status === 404)
        {
            const notFound = body as NotFoundResponse;
            if (notFound.entityType == 'DatabaseServer')
                return null;
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to retrieve details for database with Id ${databaseId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Get information about a specific user.
     * 
     * @param userId The user Id.
     * @returns The user, or null if no user exists with the specified Id.
     */
    public async getUser(userId: string): Promise<User | null> {
        await this._configured;

        const response = await this.http.fetch(`users/${userId}`);
        const body = await response.json();

        if (response.ok)
            return body as User;

        if (response.status === 404)
        {
            const notFound = body as NotFoundResponse;
            if (notFound.entityType == 'User')
                return null;
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to retrieve details for user with Id ${userId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Get information about a specific tenant.
     * 
     * @param tenantId The tenant Id.
     * @returns The tenant, or null if no tenant exists with the specified Id.
     */
    public async getTenant(tenantId: string): Promise<Tenant | null> {
        await this._configured;

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
     * Get information about a specific database server.
     * 
     * @param serverId The server Id.
     * @returns The server, or null if no server exists with the specified Id.
     */
    public async getServer(serverId: string): Promise<DatabaseServer | null> {
        await this._configured;

        const response = await this.http.fetch(`servers/${serverId}`);
        const body = await response.json();

        if (response.ok)
            return body as DatabaseServer;

        if (response.status === 404)
        {
            const notFound = body as NotFoundResponse;
            if (notFound.entityType == 'DatabaseServer')
                return null;
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to retrieve details for server with Id ${serverId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Get the event stream for a specific database server.
     * 
     * @param serverId The server Id.
     * 
     * @returns The server events.
     */
    public async getServerEvents(serverId: string): Promise<DatabaseServerEvent[]> {
        await this._configured;

        const response = await this.http.fetch(`servers/${serverId}/events`);
        const body = await response.json();

        if (response.ok)
            return body as DatabaseServerEvent[];

        if (response.status === 404)
        {
            const notFound = body as NotFoundResponse;
            if (notFound.entityType == 'DatabaseServer')
                throw new Error(`Server not found with Id ${serverId}.`);
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to retrieve events for server with Id ${serverId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Get information about all databases hosted by the specified server.
     * 
     * @returns The databases.
     */
    public async getServerDatabases(serverId: string): Promise<Database[]> {
        await this._configured;

        const response = await this.http.fetch(`servers/${serverId}/databases`);
        const body = await response.json();

        if (response.ok)
            return body as Database[];

        if (response.status === 404)
        {
            const notFound = body as NotFoundResponse;
            if (notFound.entityType == 'DatabaseServer')
                throw new Error(`Server not found with Id '${serverId}'.`);
        }

        const errorResponse = body as ApiResponse;
        
        throw new Error(
            `Failed to retrieve databases for server with Id ${serverId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Get information about a tenant's database servers.
     * 
     * @param tenantId The tenant Id.
     * 
     * @returns The servers.
     */
    public async getTenantServers(tenantId: string): Promise<DatabaseServer[]> {
        await this._configured;

        const response = await this.http.fetch(`tenants/${tenantId}/servers`);
        const body = await response.json();

        if (response.ok) {
            return body as DatabaseServer[];
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to retrieve details for servers owned by tenant with Id ${tenantId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Get information about a tenant's SQL server instance.
     * 
     * @param tenantId The Id of the tenant that owns the server.
     * 
     * @returns The databases, or null if the tenant does not have a server.
     */
    public async getTenantDatabases(tenantId: string): Promise<Database[] | null> {
        await this._configured;

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
     * Create a user.
     * 
     * @param name The user's name (for display purposes).
     * @param email The user's email address.
     * @param password The user's password.
     * @param passwordConfirmation Confirm the user's password.
     * @param isAdmin Grant the user administrative rights?
     * 
     * @returns The Id of the new user.
     */
    public async createUser(name: string, email: string, password: string, passwordConfirmation:string, isAdmin: boolean): Promise<string> {
        await this._configured;

        const response = await this.http.fetch('users', {
            method: 'POST',
            body: json({
                name: name,
                email: email,
                password: password,
                passwordConfirmation: passwordConfirmation,
                isAdmin: isAdmin
            })
        });
        
        const body = await response.json();
        if (response.ok) {
            const userCreated = body as TenantCreated;

            return userCreated.id;
        }

        if (response.status == 400 && !body.reason) {
            throw createInvalidModelError(body as InvalidModelResponse);
        }
        
        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to create user '${name}': ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Create a tenant.
     * 
     * @param name The tenant name.
     * 
     * @returns The Id of the new tenant.
     */
    public async createTenant(name: string): Promise<string> {
        await this._configured;

        const response = await this.http.fetch('tenants', {
            method: 'POST',
            body: json({
                name: name
            })
        });
        
        const body = await response.json();
        if (response.ok) {
            const tenantCreated = body as TenantCreated;

            return tenantCreated.id;
        }
        
        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to create tenant '${name}': ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Deploy a new SQL Server instance.
     * 
     * @param tenantId The Id of the tenant that will own the server.
     * @param name The server name.
     * @param adminPasword The server administrative password.
     * 
     * @returns The Id of the new server.
     */
    public async deploySqlServer(tenantId: string, name: string, adminPassword: string): Promise<string> {
        await this._configured;

        const response = await this.http.fetch('servers/create/sql', {
            method: 'POST',
            body: json({
                tenantId: tenantId,
                name: name,
                kind: DatabaseServerKind.SqlServer,
                adminPassword: adminPassword,
                sizeMB: 600 // TODO: Expose this via the UI.
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
     * Deploy a new RavenDB instance.
     * 
     * @param tenantId The Id of the tenant that will own the server.
     * @param name The server name.
     * 
     * @returns The Id of the new server.
     */
    public async deployRavenServer(tenantId: string, name: string): Promise<string> {
        await this._configured;

        const response = await this.http.fetch('servers/create/ravendb', {
            method: 'POST',
            body: json({
                tenantId: tenantId,
                name: name,
                kind: DatabaseServerKind.RavenDB,
                sizeMB: 600 // TODO: Expose this via the UI.
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
     * Destroy a database server.
     * 
     * @param serverId The server Id.
     */
    public async destroyServer(serverId: string): Promise<void> {
        await this._configured;

        const response = await this.http.fetch(`servers/${serverId}`, {
            method: 'DELETE'
        });
        
        if (response.ok) {
            return
        }

        const body = await response.json();
        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to delete server for tenant with Id ${serverId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Reconfigure a database server.
     * 
     * @param serverId The server Id.
     */
    public async reconfigureServer(serverId: string): Promise<void> {
        await this._configured;

        const response = await this.http.fetch(`servers/${serverId}/reconfigure`, {
            method: 'POST'
        });
        
        if (response.ok) {
            return
        }

        const body = await response.json();
        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to reconfigure server ${serverId}: ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Create a database for a tenant.
     * 
     * @param serverId The Id of the server that will host the database.
     * @param name The database name.
     * @param user The database user name.
     * @param password The database user password.
     */
    public async createDatabase(serverId: string, name: string, user: string, password: string): Promise<string> {
        await this._configured;

        const response = await this.http.fetch(`databases`, {
            method: 'POST',
            body: json({
                serverId: serverId,
                name: name,
                sizeMB: 200, // TODO: Expose this via the UI.
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
            throw createInvalidModelError(body as InvalidModelResponse);
        }

        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to create database '${name}' on server '${serverId}': ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Delete a database.
     * 
     * @param databaseId The database Id.
     */
    public async deleteDatabase(databaseId: string): Promise<void> {
        await this._configured;

        const response = await this.http.fetch(`databases/${databaseId}`, {
            method: 'DELETE'
        });
        
        if (response.ok || response.status === 400) {
            return
        }

        const body = await response.json();
        const errorResponse = body as ApiResponse;

        throw new Error(
            `Failed to delete database with Id '${databaseId}': ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Configure the DaaS API client.
     */
    private async configure(): Promise<void> {
        const configuration: Configuration = await this.configService.getConfiguration();
        
        this.http.configure(request =>
            request.withBaseUrl(configuration.api.endPoint).withDefaults({
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            })
            .withInterceptor(this._authenticationInterceptor)
            .withInterceptor(this._lastETagInterceptor)
        );
    }
}

/**
 * The name of the "X-Results-UpTo-ETag" header.
 */
export const ResultsUpToETagHeader = 'X-Results-UpTo-ETag';

/**
 * Interceptor that tracks the last ETag returned by the DaaS API and adds it to outgoing API requests using "X-Results-UpTo-ETag" header.
 */
export class LastETagInterceptor implements Interceptor {
    /**
     * The last ETag  (if any) returned by an API call.
     */
    public lastEtag: number = 0;

    /**
     * Create a new LastETagInterceptor.
     */
    constructor() { }

    /**
     * Intercept an outgoing request and add the "X-Results-UpTo-ETag" header.
     * 
     * @param request The outgoing request.
     */
    public request(request: Request): Request {
        if (this.lastEtag) {
            request.headers.set(ResultsUpToETagHeader,
                `"${this.lastEtag}"` // ETag header value has to be quoted, so we do the same for our custom header to be consistent
            );
        }

        return request;
    }

    /**
     * Intercept an outgoing response, extracting the latest ETag from the "ETag" header (if present).
     * 
     * @param response The incoming response.
     */
    public response(response: Response): Response {
        const etagHeader = response.headers.get('ETag');
        if (etagHeader && etagHeader[0] == '"' && etagHeader[etagHeader.length - 1] == '"') {
            const etagValue = etagHeader.substring(1, etagHeader.length - 1);
            this.lastEtag = Number.parseInt(etagValue);
        }

        return response;
    }
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
    id: string;
    name: string;
}

interface TenantCreated extends ApiResponse {
    id: string;
    name: string;
}

interface UserCreated extends ApiResponse {
    id: string;
    displayName: string;
    emailAddress: string;
}

interface DatabaseCreated extends ApiResponse {
    id: string;
    name: string;
}

/**
 * Represents the error response returned by the DaaS API with a 404 status code.
 */
interface NotFoundResponse extends ApiResponse {
    /**
     * The Id of the entity that was not found.
     */
    id: string;

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

/**
 * Create a new Error to represent an InvalidModelResponse.
 * 
 * @param invalidModelResponse The InvalidModelResponse containing model-validation error messages.
 */
function createInvalidModelError(invalidModelResponse: InvalidModelResponse): Error {
    let errorMessage: string = 'Invalid request:\n';
    for (const propertyName of Object.getOwnPropertyNames(invalidModelResponse)) {
        const validationMessages: string[] = invalidModelResponse[propertyName];
        for (const validationMessage of validationMessages) {
            errorMessage += `- ${propertyName}: ${validationMessage}`;
            errorMessage += '\n';
        }
    }

    return new Error(errorMessage);
}
