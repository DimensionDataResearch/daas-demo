import { bindable } from 'aurelia-framework';

import { Tenant, Server } from '../api/daas-api';

export class ServerActions
{
    @bindable public tenant: Tenant;
    @bindable public server: Server;

    @bindable public destroyServerClicked: () => void;

    public get isServerReady(): boolean {
        return this.server !== null && this.server.status === 'Ready';
    }

    constructor() {
        console.log('SERVER_ACTIONS');
    }

    private onDestroyServerClicked(): void {
        if (this.destroyServerClicked)
            this.destroyServerClicked();
    }

}
