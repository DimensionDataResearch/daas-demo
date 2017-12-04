import { bindable } from 'aurelia-framework';
import { computedFrom } from 'aurelia-binding';

import { DatabaseServer, ProvisioningStatus } from '../../services/api/daas-models';

export class ServerActions
{
    @bindable public server: DatabaseServer;

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
