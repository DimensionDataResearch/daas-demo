import { inject, NewInstance } from 'aurelia-framework';
import { computedFrom } from 'aurelia-binding';
import { RouteConfig, Router } from 'aurelia-router';
import { bindable } from 'aurelia-templating';

import { ViewModel } from '../common/view-model';
import { DaaSAPI } from '../../services/api/daas-api';
import { User } from '../../services/api/daas-models';

/**
 * View model for the user detail view.
 */
@inject(DaaSAPI, Router)
export class UserDetail extends ViewModel {
    private userId: string;

    @bindable public user: User | null = null;

    /**
     * Create a new user list view model.
     * 
     * @param api The DaaS API client.
     * @param router The Aurelia router service.
     */
    constructor(private api: DaaSAPI, private router: Router) {
        super();
    }

    /**
     * Does the user exist?
     */
    @computedFrom('user')
    public get hasUser(): boolean {
        return !!this.user;
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: RouteParams, routeConfig: RouteConfig): void {
        super.activate(params, routeConfig);

        this.userId = params.userId;
        this.load();
    }

    public async deleteUser(): Promise<void> {
        await this.runLoadingAsync(async () => {
            await this.api.deleteUser(this.userId);
            await this.router.navigateToRoute('users');
        });
    }

    /**
     * Load the user's details.
     */
    private async load(): Promise<void> {
        await this.runLoadingAsync(async () => {
            this.user = await this.api.getUser(this.userId);

            if (this.user) {
                this.routeConfig.title = `User (${this.user.name})`;
            }
        });
    }
}

/**
 * Route parameters for the user detail view.
 */
export interface RouteParams {
    userId: string;
}
