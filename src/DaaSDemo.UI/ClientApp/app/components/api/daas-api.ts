import { inject, singleton } from 'aurelia-framework';
import { HttpClient, json } from 'aurelia-fetch-client';
import { ServerProvisioningStatus } from '../status/server-provisioning-status';

/**
 * Client for the Database-as-a-Service API.
 */
@singleton()
@inject(HttpClient)
export class DaaSAPI
{
    private configured: Promise<void>;

    /**
     * Create a new DaaS API client.
     * 
     * @param http An HTTP client.
     */
    constructor(private http: HttpClient)
    {
        this.configured = this.configure();
    }

    /**
     * Get information about all users.
     * 
     * @returns The users, sorted by name.
     */
    public async getUsers(): Promise<User[]> {
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
     * @param ensureUpToDate Ensure that the results are as up-to-date as possible?
     * 
     * @returns The servers.
     */
    public async getTenantServers(tenantId: string, ensureUpToDate: boolean = false): Promise<DatabaseServer[]> {
        await this.configured;

        let requestURI = `tenants/${tenantId}/servers`;
        if (ensureUpToDate)
            requestURI += '?ensureUpToDate=true';

        const response = await this.http.fetch(requestURI);
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
     * @param ensureUpToDate Ensure that the results are as up-to-date as possible?
     * 
     * @returns The databases, or null if the tenant does not have a server.
     */
    public async getTenantDatabases(tenantId: string, ensureUpToDate: boolean = false): Promise<Database[] | null> {
        await this.configured;

        let requestURI = `tenants/${tenantId}/databases`;
        if (ensureUpToDate)
            requestURI += '?ensureUpToDate=true';

        const response = await this.http.fetch(requestURI);
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
     * Create a tenant.
     * 
     * @param name The tenant name.
     * 
     * @returns The Id of the new tenant.
     */
    public async createTenant(name: string): Promise<string> {
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
        await this.configured;

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
            `Failed to create database '${name}' on server '${serverId}': ${errorResponse.message || 'Unknown error.'}`
        );
    }

    /**
     * Delete a database.
     * 
     * @param databaseId The database Id.
     */
    public async deleteDatabase(databaseId: string): Promise<void> {
        await this.configured;

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
        const endPointsResponse = await this.http.fetch('end-points');
        if (!endPointsResponse.ok)
            throw new Error('Failed to retrieve configuration for DaaS API end-points.');

        const body = await endPointsResponse.json();
        const endPoints = body as EndPoints;

        this.http.configure(request =>
            request.withBaseUrl(endPoints.api).withDefaults({
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-AuthX-Scope': 'daas_api' // Pseudo-header used to trigger automatic acquisition of access tokens
                }
            })
        );
    }
}

/**
 * End-point configuration for the DaaS UI.
 */
export interface EndPoints {
    /**
     * The DaaS API end-point.
     */
    api: string;

    /**
     * The identity server (STS) base address.
     */
    identityServer: string;
}

export interface User {
    id: string;
    name: string;
    emailAddress: string;
    isLockedOut: boolean;
}

/**
 * Represents a DaaS Tenant.
 */
export interface Tenant
{
    /**
     * The tenant Id.
     */
    id: string;
    
    /**
     * The tenant name.
     */
    name: string;
}

/**
 * Represents a DaaS SQL Server instance.
 */
export interface DatabaseServer
{
    /**
     * The server Id.
     */
    id: string;
    
    /**
     * The server name.
     */
    name: string;

    /**
     * The type of server.
     */
    kind: DatabaseServerKind;

    /**
     * The fully-qualified domain name (if any) on which the server is externally accessible.
     */
    publicFQDN?: string | null;
    
    /**
     * The TCP port (if any) on which the server is externally accessible.
     */
    publicPort?: number | null;

    /**
     * The Id of the tenant that owns the server.
     */
    tenantId: string;

    /**
     * The name of the tenant that owns the server.
     */
    tenantName: string;

    /**
     * The currently-requested action (if any) for the server.
     */
    action: ProvisioningAction;

    /**
     * The server provovisioning phase (if any).
     */
    phase: ServerProvisioningPhase;

    /**
     * The server status.
     */
    status: ProvisioningStatus;
}

/**
 * Represents an instance of an SQL Server database.
 */
export interface Database {
    /**
     * The database Id.
     */
    id: string;
    
    /**
     * The database name.
     */
    name: string;

    /**
     * The name of the user assigned to the database.
     */
    databaseUser: string;

    /**
     * The Id of the server that hosts the database.
     */
    serverId: string;

    /**
     * The name of the server that hosts the database.
     */
    serverName: string;

    /**
     * The kind of server that hosts the database.
     */
    serverKind: DatabaseServerKind;

    /**
     * The server's fully-qualified public domain name (if available).
     */
    serverPublicFQDN: string | null;

    /**
     * The server's public TCP port (if available).
     */
    serverPublicPort: number | null;

    /**
     * The Id of the tenant that owns the database.
     */
    tenantId: string;

    /**
     * The name of the tenant that owns the database.
     */
    tenantName: string;

    /**
     * The database's currently-requested provisioning action (if any).
     */
    action: ProvisioningAction;

    /**
     * The database provisioning status.
     */
    status: ProvisioningStatus;
}

/**
 * The provisioning status of a resource.
 */
export enum ProvisioningStatus {
    /**
     * Resource provisioning is pending.
     */
    Pending = 'Pending',

    /**
     * Resource is ready for use.
     */
    Ready = 'Ready',

    /**
     * Resource is being provisioned.
     */
    Provisioning = 'Provisioning',

    /**
     * Resource is being de-provisioned.
     */
    Deprovisioning = 'Deprovisioning',

    /**
     * Resource is being reconfigured.
     */
    Reconfiguring = 'Reconfiguring',

    /**
     * Resource state is invalid.
     */
    Error = 'Error',

    /**
     * Resource has been de-provisioned.
     */
    Deprovisioned = 'Deprovisioned'
}

/**
 * A provisioning action to be performed for a resource.
 */
export enum ProvisioningAction {
    /**
     * No provisioning action.
     */
    None = 'None',

    /**
     * Provision resource(s).
     */
    Provision = 'Provision',

    /**
     * De-provision resource(s).
     */
    Deprovision = 'Deprovision',

    /**
     * Reconfigure resource(s).
     */
    Reconfigure = 'Reconfigure'
}

/**
 * Represents a phase in server provisioning / reconfiguration / de-provisioning.
 */
export enum ServerProvisioningPhase
{
    /**
     * No provisioning phase is currently active.
     */
    None = 'None',

    /**
     * Server storage.
     */
    Storage = 'Storage',

    /**
     * Security configuration (e.g. credentials, firewall rules).
     */
    Security = 'Security',

    /**
     * The server instance.
     */
    Instance = 'Instance',

    /**
     * The server's internal network connectivity.
     */
    Network = 'Network',

    /**
     * The server's monitoring infrastructure.
     */
    Monitoring = 'Monitoring',

    /**
     * The server's configuration.
     */
    Configuration = 'Configuration',

    /**
     * The server's external network connectivity.
     */
    Ingress = 'Ingress',

    /**
     * The server's current action has been completed.
     */
    Done = 'Done'
}

/**
 * Well-known kinds of database server.
 */
export enum DatabaseServerKind
{
    /**
     * An unknown server kind.
     */
    Unknown = 'Unknown',

    /**
     * Microsoft SQL Server.
     */
    SqlServer = 'SqlServer',

    /**
     * RavenDB.
     */
    RavenDB = 'RavenDB'
}

/**
 * Represents an event relating to a DatabaseServer.
 */
export interface DatabaseServerEvent {
    /**
     * The date / time that the event occurred.
     */
    timestamp: string;

    /**
     * The kind of event represented by the DatabaseServerEvent.
     */
    kind: DatabaseServerEventKind;

    /**
     * Messages (if any) associated with the event.
     */
    messages: string[];
}

/**
 * Represents a provisioning event relating to a DatabaseServer.
 */
export interface DatabaseServerProvisioningEvent extends DatabaseServerEvent {
    /**
     * The requested action.
     */
    action: ProvisioningAction;

    /**
     * The server's current provisioning phase (if any) when the event was raised.
     */
    phase: ServerProvisioningPhase;

    /**
     * The server's current status when the event was raised.
     */
    status: ServerProvisioningStatus;
}

/**
 * Represents an event indicating that a DatabaseServer's ingress details have changed.
 */
export interface DatabaseServerIngressChangedEvent extends DatabaseServerEvent {
    /**
     * The server's current fully-qualified public domain name (if any) when the event was raised.
     */
    publicFQDN: string | null;

    /**
     * The server's current public TCP port (if any) when the event was raised.
     */
    publicPort: number | null;
}

/**
 * A well-known kind of DatabaseServerEvent.
 */
export enum DatabaseServerEventKind {
    /**
     * A provisioning-related event.
     */
    Provisioning = 'Provisioning',

    /**
     * Event indicating that a server's ingress details have changed.
     */
    IngressChanged = 'IngressChanged'
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
