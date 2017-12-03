import { inject, computedFrom, bindable } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { RouteConfig } from 'aurelia-router';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { DaaSAPI, User } from '../../../services/api/daas-api';

@inject(DaaSAPI, NewInstance.of(ValidationController))
export class NewUserForm {
    @bindable public newUser: NewUser | null = null;

    @bindable public createClicked: (newUser: NewUser) => void;
    @bindable public cancelClicked: () => void;

    /**
     * Create a new user user creation view model.
     * 
     * @param api The DaaS API client.
     * @param validationController The validation controller for the current context.
     */
    constructor(private api: DaaSAPI, public validationController: ValidationController) {
        this.createClicked = (newUser) => {};
        this.cancelClicked = () => {};
    }
}

/**
 * Represents the form values for creating a user.
 */
export class NewUser {
    name: string | null = null;

    // TODO: Add e-mail address with validation.
}

ValidationRules
    .ensure('name').displayName('User name')
        .required()
        .minLength(5)
    .on(NewUser);
