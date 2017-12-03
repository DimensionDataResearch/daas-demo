import { inject, bindable } from 'aurelia-framework';
import { EventAggregator, Subscription } from 'aurelia-event-aggregator';
import { HttpClient } from 'aurelia-fetch-client';
import { RouteConfig } from 'aurelia-router';

import { UserManager, User } from 'oidc-client';

import { EndPoints } from '../api/daas-api';
import { createUserManager, AuthXManager } from '../authx/authenticator';

/**
 * The view model for the default (home) view.
 */
@inject(AuthXManager, EventAggregator, HttpClient)
export class Home {
    private subscriptions: Subscription[] = [];

    @bindable public signedIn: boolean = false;
    @bindable public message: string;
    
    /**
     * Create a new home view model.
     * 
     * @param authxManager The authentication / authorisation service.
     * @param eventAggregator The event-aggregator service.
     * @param http An HTTP client.
     */
    constructor(
        private authxManager: AuthXManager,
        private eventAggregator: EventAggregator,
        private http: HttpClient
    ) { }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public async activate(params: any, routeConfig: RouteConfig): Promise<void> {
        const endPointsResponse = await this.http.fetch('end-points');
        if (!endPointsResponse.ok)
            throw new Error('Failed to retrieve configuration for DaaS API end-points.');

        const body = await endPointsResponse.json();
        const endPoints = body as EndPoints;

        console.log(`Will use IdentityServer4 at '${endPoints.identityServer}'.`);

        this.authxManager.initialize(
            endPoints.identityServer,
            'daas-ui-dev',  // Client Id
            [ 'roles' ]     // Additional claims
        );

        this.subscriptions.push(
            this.eventAggregator.subscribe('AuthX.UserLoaded', async () => {
                const userClaims = await this.authxManager.getUserClaims();
                if (userClaims) {
                    this.signedIn = true;
                    this.message = 'OIDC profile: ' + JSON.stringify(userClaims, null, '  ');
                }
            }),
            this.eventAggregator.subscribe('AuthX.UserUnloaded', () => {
                this.signedIn = false;
                this.message = 'Not logged in.';
            }),
            this.eventAggregator.subscribe('AuthX.UserSignedOut', () => {
                this.signedIn = false;
                this.message = 'Not logged in.';
            })
        );
        
        this.load();
    }

    /**
     * Trigger sign-in.
     */
    public async signin(): Promise<void> {
        await this.authxManager.signin();
    }

    /**
     * Trigger sign-out.
     */
    public async signout(): Promise<void> {
        await this.authxManager.signout();
    }

    /**
     * Load initial AuthX state.
     */
    private async load(): Promise<void> {
        const userClaims = await this.authxManager.getUserClaims();
        if (userClaims) {
            this.signedIn = true;
            this.message = 'OIDC profile (cached): ' + JSON.stringify(userClaims, null, '  ');
        } else {
            this.signedIn = false;
            this.message = 'Not logged in.';
        }
    }
}
