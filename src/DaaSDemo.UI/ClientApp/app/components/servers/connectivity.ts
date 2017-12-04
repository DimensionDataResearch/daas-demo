import { bindable } from 'aurelia-framework';

import { DatabaseServer, DatabaseServerKind } from '../../services/api/daas-models';

export class ServerConnectivity
{
    @bindable public server: DatabaseServer | null;

    constructor() {}

    /**
     * Is the server externally accessible?
     */
    public get isServerExternallyAccessible(): boolean {
        return this.server !== null && this.server.publicFQDN !== null;
    }

    /**
     * Is the server RavenDB?
     */
    public get isRavenDB(): boolean {
        return this.server !== null && this.server.kind == DatabaseServerKind.RavenDB;
    }

    /**
     * Is the server SQL Server?
     */
    public get isSqlServer(): boolean {
        return this.server !== null && this.server.kind == DatabaseServerKind.SqlServer;
    }
}
