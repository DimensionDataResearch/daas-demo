import { bindable } from 'aurelia-framework';

import { Server } from '../../../api/daas-api';

export class ServerConnectivity
{
    @bindable public server: Server;

    constructor() {}

    /**
     * Is the server externally accessible?
     */
    public get isServerExternallyAccessible(): boolean {
        return this.server !== null && this.server.publicFQDN !== null;
    }
}
