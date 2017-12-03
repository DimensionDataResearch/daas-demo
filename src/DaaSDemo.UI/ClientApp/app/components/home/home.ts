import { inject, bindable } from 'aurelia-framework';
import { EventAggregator, Subscription } from 'aurelia-event-aggregator';
import { HttpClient } from 'aurelia-fetch-client';
import { RouteConfig } from 'aurelia-router';

import { UserManager, User } from 'oidc-client';

import { AuthService } from '../../services/authx/auth-service';

/**
 * The view model for the default (home) view.
 */
@inject(EventAggregator, HttpClient, AuthService)
export class Home {
    private subscriptions: Subscription[] = [];

    @bindable public message: string | null;
    
    /**
     * Create a new home view model.
     * 
     * @param eventAggregator The event-aggregator service.
     * @param http An HTTP client.
     * @param authService The authentication / authorisation service.
     */
    constructor(
        private eventAggregator: EventAggregator,
        private http: HttpClient,
        private authService: AuthService
    ) { }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public async activate(params: any, routeConfig: RouteConfig): Promise<void> {
        this.load();

        this.eventAggregator.subscribe('AuthX.UserLoaded', (user: User) => {
            this.message = JSON.stringify(user.profile, null, 4);
        });
        this.eventAggregator.subscribe('AuthX.UserUnloaded', () => {
            this.message = null;
        });
        this.eventAggregator.subscribe('AuthX.UserSignedOut', () => {
            this.message = null;
        });
    }

    /**
     * Trigger sign-in.
     */
    public async signin(): Promise<void> {
        await this.authService.signin();
    }

    /**
     * Trigger sign-out.
     */
    public async signout(): Promise<void> {
        await this.authService.signout();
    }

    /**
     * Load initial AuthX state.
     */
    private async load(): Promise<void> {
        const user = this.authService.user;
        if (user) {
            this.message = JSON.stringify(user.profile, null, 4);
        } else {
            this.message = null;
        }
    }
}
