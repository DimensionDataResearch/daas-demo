import { bindable } from 'aurelia-framework';
import { Server } from '../api/daas-api';

export class ServerProvisioningStatus {
    @bindable public server: Server;

    constructor() {}

    public get isReady(): boolean {
        return this.server && this.server.status == 'Ready';
    }

    public get isActionInProgress(): boolean {
        return this.server && this.server.action !== 'None';
    }

    public get hasError(): boolean {
        return this.server && this.server.status === 'Error';
    }
}
