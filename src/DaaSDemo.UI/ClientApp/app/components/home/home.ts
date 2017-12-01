import { inject, bindable } from 'aurelia-framework';
import { createUserManager } from '../authx/authenticator';
import { UserManager, User } from 'oidc-client';

/**
 * The view model for the default (home) view.
 */
export class Home {
    private userManager: UserManager

    @bindable public message: string;
    
    /**
     * Create a new Home view model.
     */
    constructor() {
        this.userManager = createUserManager(
            'http://localhost:5060',
            'daas-ui-dev',
            [ 'roles' ]
        );
        this.userManager.events.addUserLoaded((user: User) => {
            console.log('User loaded!', user);

            this.message = 'OIDC profile: ' + JSON.stringify(user.profile);
        });
    }

    /**
     * Trigger login via pop-up.
     */
    public async login(): Promise<void> {
        await this.userManager.signinPopup();
    }
}
