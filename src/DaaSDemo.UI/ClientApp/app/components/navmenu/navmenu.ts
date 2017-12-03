import { inject, bindable, bindingBehavior } from 'aurelia-framework';
import { Router } from 'aurelia-router';
import { User } from 'oidc-client';

import { AuthService } from '../../services/authx/auth-service';

/**
 * Model for the navigation menu.
 */
@inject(AuthService)
export class NavMenu {
    @bindable public router: Router
    
    /**
     * Create a new navigation menu model.
     * 
     * @param authService The authentication / authorisation service.
     */
    constructor(private authService: AuthService) { }

    /**
     * Is the user currently signed in?
     */
    public get isSignedIn(): boolean {
        return this.authService.isSignedIn;
    }

    /**
     * The user information (if currently signed in).
     */
    public get user(): User | null {
        return this.authService.user;
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
