import { inject, bindable } from 'aurelia-framework';
import { createUserManager, AuthXManager } from '../authx/authenticator';
import { UserManager, User } from 'oidc-client';
import { EventAggregator, Subscription } from 'aurelia-event-aggregator';

/**
 * The view model for the default (home) view.
 */
@inject(AuthXManager, EventAggregator)
export class Home {
    private subscriptions: Subscription[] = [];

    @bindable public signedIn: boolean = false;
    @bindable public message: string;
    
    /**
     * Create a new Home view model.
     */
    constructor(private authxManager: AuthXManager, private eventAggregator: EventAggregator) {
        this.authxManager.initialize(
            'http://localhost:5060',
            'daas-ui-dev',
            [ 'roles' ]
        );

        this.subscriptions.push(
            this.eventAggregator.subscribe('AuthX.UserLoaded', async () => {
                console.log('User loaded.');

                const userClaims = await this.authxManager.getUserClaims();
                if (userClaims) {
                    this.signedIn = true;
                    this.message = 'OIDC profile: ' + JSON.stringify(userClaims, null, '  ');
                }
            }),
            this.eventAggregator.subscribe('AuthX.UserUnloaded', () => {
                console.log('User unloaded.');
                
                this.signedIn = false;
                this.message = 'Not logged in.';
            }),
            this.eventAggregator.subscribe('AuthX.UserSignedOut', () => {
                console.log('User signed out.');

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
