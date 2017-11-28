import { bindable } from 'aurelia-framework';

import { Tenant, Server, ProvisioningStatus } from '../api/daas-api';
import { computedFrom } from 'aurelia-binding';

export class ServerActions
{
    @bindable public server: Server;

    @bindable public destroyServerClicked: () => void;

    /**
     * Is the server ready for use?
     */
    @computedFrom('server')
    public get isServerReady(): boolean {
        return this.server !== null && this.server.status === ProvisioningStatus.Ready;
    }

    /**
     * Is the server in an error state?
     */
    @computedFrom('server')
    public get isServerErrored(): boolean {
        return this.server !== null && this.server.status === ProvisioningStatus.Error;
    }

    /**
     * Can the server be destroyed?
     */
    @computedFrom('server')
    public get canDestroyServer(): boolean {
        return this.isServerReady || this.isServerErrored;
    }

    constructor() { }

    private onDestroyServerClicked(): void {
        if (this.destroyServerClicked)
            this.destroyServerClicked();
    }

}
