import { bindable } from 'aurelia-framework';
import { Database } from '../api/daas-api';

export class DatabaseProvisioningStatus {
    @bindable public database: Database;

    constructor() {}

    public get isReady(): boolean {
        return this.database && this.database.status == 'Ready' && !this.isActionInProgress;
    }

    public get isActionInProgress(): boolean {
        return this.database && this.database.action !== 'None';
    }

    public get hasError(): boolean {
        return this.database && this.database.status === 'Error';
    }
}
