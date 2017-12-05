import { inject, bindable, bindingBehavior } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { Router } from 'aurelia-router';
import { User } from 'oidc-client';

import { AuthService } from '../../services/authx/auth-service';

/**
 * Model for the navigation menu.
 */
@inject(EventAggregator, AuthService)
export class NavMenu {
    @bindable public router: Router
    @bindable public user: User | null = null;
    
    /**
     * Create a new navigation menu model.
     * 
     * @param authService The authentication / authorisation service.
     */
    constructor(private eventAggregator: EventAggregator, private authService: AuthService) {
        this.eventAggregator.subscribe('AuthX.UserLoaded', (user: User) => {
            console.log('User loaded.', user);

            this.user = user;
        });
        this.eventAggregator.subscribe('AuthX.UserUnloaded', () => {
            console.log('User unloaded.');

            this.user = null;
        });
    }

    /**
     * Is the user currently signed in?
     */
    public get isSignedIn(): boolean {
        return this.authService.isSignedIn;
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
}
