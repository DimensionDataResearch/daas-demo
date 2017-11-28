import { inject, computedFrom, bindable } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { RouteConfig } from 'aurelia-router';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { DaaSAPI, Database, Tenant, Server } from '../../api/daas-api';

@inject(DaaSAPI, NewInstance.of(ValidationController))
export class NewDatabaseForm {
    @bindable public servers: Server[] = [];
    @bindable public newDatabase: NewDatabase | null = null;

    @bindable public createClicked: (newDatabase: NewDatabase) => void;
    @bindable public cancelClicked: () => void;

    /**
     * Create a new tenant database creation view model.
     * 
     * @param api The DaaS API client.
     */
    constructor(private api: DaaSAPI, public validationController: ValidationController) {
        this.createClicked = (newDatabase) => {};
        this.cancelClicked = () => {};
    }
}

/**
 * Represents the form values for creating a database.
 */
export class NewDatabase {
    public serverId: string | null;
    public name: string | null = null;
    public user: string | null = null;
    public password: string | null = null;

    constructor(serverId?: string) {
        this.serverId = serverId || null;
    }
}

ValidationRules
    .ensure('serverId').displayName('Server')
        .required()
    .ensure('name').displayName('Database name')
        .required()
        .minLength(5)
    .ensure('user').displayName('User name')
        .required()
        .minLength(4)
    .ensure('password').displayName('Password')
        .required()
        .minLength(6)
    .on(NewDatabase);
