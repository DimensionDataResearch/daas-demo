import { bindable } from 'aurelia-framework';
import { DatabaseServer, ProvisioningAction, ProvisioningStatus } from '../../services/api/daas-api';

export class ServerProvisioningStatus {
    @bindable public server: DatabaseServer;

    constructor() {}

    public get isReady(): boolean {
        return this.server && this.server.status == ProvisioningStatus.Ready;
    }

    public get isActionInProgress(): boolean {
        return this.server && this.server.action !== ProvisioningAction.None;
    }

    public get hasError(): boolean {
        return this.server && this.server.status === ProvisioningStatus.Error;
    }
}
