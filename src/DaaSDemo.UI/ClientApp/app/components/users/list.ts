import { inject, NewInstance } from 'aurelia-framework';
import { computedFrom } from 'aurelia-binding';
import { RouteConfig } from 'aurelia-router';
import { bindable } from 'aurelia-templating';
import { ValidationController } from 'aurelia-validation';

import { DaaSAPI } from '../../services/api/daas-api';
import { User } from '../../services/api/daas-models';

import { NewUser } from './forms/new';

/**
 * View model for the user list view.
 */
@inject(DaaSAPI, NewInstance.of(ValidationController))
export class UserList {
    private routeConfig: RouteConfig;

    @bindable public isLoading: boolean = false;
    @bindable public users: User[] = [];
    @bindable public newUser: NewUser | null = null;
    @bindable public errorMessage: string | null = null;

    /**
     * Create a new user list view model.
     * 
     * @param api The DaaS API client.
     * @param validationController The validation controller for the current context.
     */
    constructor(private api: DaaSAPI, public validationController: ValidationController) { }

    /**
     * Has an error occurred?
     */
    @computedFrom('errorMessage')
    public get hasError(): boolean {
        return this.errorMessage !== null;
    }

    /**
     * Is there at least one user?
     */
    @computedFrom('users')
    public get hasUser(): boolean {
        return this.users.length !== 0;
    }

    /**
     * Is a user currently being added?
     */
    @computedFrom('newUser')
    public get addingUser(): boolean {
        return this.newUser !== null;
    }

    /**
     * Show the user creation form.
     */
    public showCreateUserForm(): void {
        this.newUser = new NewUser();
    }

    /**
     * Hide the user creation form.
     */
    public hideCreateUserForm(): void {
        this.newUser = null;
    }

    /**
     * Request creation of a new user.
     */
    public async createUser(): Promise<void> {
        if (this.newUser === null)
            return;

        if (this.newUser.displayName == null || this.newUser.email == null || this.newUser.password == null || this.newUser.passwordConfirmation == null)
            return;

        this.clearError();

        try {
            await this.api.createUser(
                this.newUser.displayName,
                this.newUser.email,
                this.newUser.password,
                this.newUser.isAdmin
            );
        }
        catch (error) {
            this.showError(error as Error);

            return;
        }

        await this.load(true);

        this.hideCreateUserForm();
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: any, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;

        this.load(false);
    }

    /**
     * Load users.
     * 
     * @param isReload Is this a reload, rather than the initial load?
     */
    private async load(isReload: boolean): Promise<void> {
        this.clearError();
        
        if (!isReload)
            this.isLoading = true;

        try
        {
            this.users = await this.api.getUsers();
        } catch (error) {
            this.showError(error as Error);
        }
        finally
        {
            if (!isReload)
                this.isLoading = false;
        }
    }

    /**
     * Clear the current error message (if any).
     */
    private clearError(): void {
        this.errorMessage = null;
    }

    /**
     * Show an error message.
     * 
     * @param error The error to show.
     */
    private showError(error: Error): void {
        console.log(error);
        
        this.errorMessage = (error.message as string || 'Unknown error.').split('\n').join('<br/>');
    }
}
