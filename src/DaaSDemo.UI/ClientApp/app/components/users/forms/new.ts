import { inject, computedFrom, bindable } from 'aurelia-framework';
import { NewInstance } from 'aurelia-dependency-injection';
import { RouteConfig } from 'aurelia-router';
import { ValidationRules, ValidationController } from 'aurelia-validation';

import { DaaSAPI } from '../../../services/api/daas-api';

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
    email: string | null = null;
    password: string | null = null;
    passwordConfirmation: string | null = null;
}

ValidationRules
    .ensure<NewUser, string>('email').displayName('Email address')
        .required()
        .email()
    .ensure('password').displayName('Password')
        .required()
        .minLength(5)
    .ensure('passwordConfirmation').displayName('Password confirmation')
        .required()
        .minLength(5)
        .satisfies(
            (passwordConfirmation: string, newUser: NewUser) => passwordConfirmation == newUser.password
        ).withMessage('Passwords must match')
    .on(NewUser);
