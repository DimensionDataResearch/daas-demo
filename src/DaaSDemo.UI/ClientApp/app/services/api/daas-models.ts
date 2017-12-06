/**
 * Represents a DaaS application user.
 */
export interface User {
    id: string;
    name: string;
    emailAddress: string;
    isLockedOut: boolean;
    idAdmin: boolean;
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
    status: ProvisioningStatus;
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
