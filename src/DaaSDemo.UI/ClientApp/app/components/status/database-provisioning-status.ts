import { bindable } from 'aurelia-framework';
import { Database, ProvisioningAction, ProvisioningStatus } from '../api/daas-api';

export class DatabaseProvisioningStatus {
    @bindable public database: Database;

    constructor() {}

    public get isReady(): boolean {
        return this.database && this.database.status == ProvisioningStatus.Ready && !this.isActionInProgress;
    }

    public get isActionInProgress(): boolean {
        return this.database && this.database.action !== ProvisioningAction.None;
    }

    public get hasError(): boolean {
        return this.database && this.database.status === ProvisioningStatus.Error;
    }
}
