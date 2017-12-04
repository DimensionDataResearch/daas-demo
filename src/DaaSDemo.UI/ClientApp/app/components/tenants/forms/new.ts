import { inject, computedFrom, bindable } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { RouteConfig } from 'aurelia-router';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { DaaSAPI } from '../../../services/api/daas-api';

@inject(DaaSAPI, NewInstance.of(ValidationController))
export class NewTenantForm {
    @bindable public newTenant: NewTenant | null = null;

    @bindable public createClicked: (newTenant: NewTenant) => void;
    @bindable public cancelClicked: () => void;

    /**
     * Create a new tenant tenant creation view model.
     * 
     * @param api The DaaS API client.
     * @param validationController The validation controller for the current context.
     */
    constructor(private api: DaaSAPI, public validationController: ValidationController) {
        this.createClicked = (newTenant) => {};
        this.cancelClicked = () => {};
    }
}

/**
 * Represents the form values for creating a tenant.
 */
export class NewTenant {
    name: string | null = null;
}

ValidationRules
    .ensure('name').displayName('Tenant name')
        .required()
        .minLength(5)
    .on(NewTenant);
