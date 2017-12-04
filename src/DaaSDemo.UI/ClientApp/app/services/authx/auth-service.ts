import { inject } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { UserManager, User } from 'oidc-client';

import { ConfigService, Configuration } from '../config/config-service';

/**
 * The DaaS application authentication manager.
 */
@inject(EventAggregator, ConfigService)
export class AuthService {
    private _userManager: UserManager | null = null;
    private _initialized: Promise<void>;
    private _user: User | null;

    constructor(private eventAggregator: EventAggregator, private configService: ConfigService) {
        this._initialized = this.initialize();
    }

    /**
     * Is the user currently signed in?
     */
    public get isSignedIn(): boolean {
        return !!this._user;
    }

    /**
     * The current user information (if signed in).
     */
    public get user(): User | null {
        return this._user;
    }

    /**
     * Is the AuthX service ready for use?
     */
    public get ready(): Promise<void> {
        return this._initialized;
    }

    /**
     * Determine whether the user is a member of the specified role.
     * 
     * @param roleName The role's display name (e.g. "Administrator").
     * 
     * @returns true, if the user is a member of the role; otherwise, false.
     */
    public async isInRole(roleName: string): Promise<boolean> {
        await this.ready;

        if (!this.user) {
            return false;
        }

        const roles = this.user.profile.roles as string[];
        if (!roles) {
            return false;
        }

        return roles.indexOf(roleName) !== -1;
    }

    /**
     * Sign in.
     */
    public async signin(): Promise<User | null> {
        await this.ready;

        if (!this._userManager) {
            throw new Error('AuthManager has not been initialised.');
        }

        this._user = await this._userManager.signinPopup();

        return this.user;
    }

    /**
     * Sign out.
     */
    public async signout(): Promise<void> {
        await this.ready;

        if (!this._userManager) {
            throw new Error('AuthManager has not been initialised.');
        }

        await this._userManager.signoutPopup();
        this._user = null;
    }

    /**
     * Initialise the AuthManager.
     */
    private async initialize(): Promise<void> {
        const configuration: Configuration = await this.configService.getConfiguration();

        const authority = configuration.identity.authority;
        const clientId = configuration.identity.clientId;
        const additionalScopes: string[] = (configuration.identity.additionalScopes || '').split(';');

        this._userManager = createUserManager(authority, clientId, additionalScopes);
        this._userManager.events.addAccessTokenExpired(() => {
            this.eventAggregator.publish('AuthX.TokenExpired');
        });
        this._userManager.events.addSilentRenewError((error: Error) => {
            this.eventAggregator.publish('AuthX.SilentRenewError', error);
        });
        this._userManager.events.addUserLoaded(async () => {
            if (!this._userManager)
                return;

            const user = await this._userManager.getUser();
            if (user) {
                this.eventAggregator.publish('AuthX.UserLoaded', user);
            }
        });
        this._userManager.events.addUserUnloaded(() => {
            this.eventAggregator.publish('AuthX.UserUnloaded');
        });
        this._userManager.events.addUserSignedOut(() => {
            this.eventAggregator.publish('AuthX.UserSignedOut');
        });

        // Manually trigger initial sign-in event.
        this._user = await this._userManager.getUser();
        if (this._user) {
            this.eventAggregator.publish('AuthX.UserLoaded', this.user);
        }
    }
}

/**
 * Create an OIDC user manager.
 * 
 * @param authority The base address of the STS.
 * @param client_id The OIDC client Id representing the DaaS UI.
 * @param additionalScopes Additional default scopes (if any) to include with token requests.
 */
export function createUserManager(authority: string, client_id: string, additionalScopes: string[] = []): UserManager {
    const baseAddress: string = window.location.protocol + '//' + window.location.host;

    const scopes: string[] = [ 'openid', 'profile' ];
    scopes.splice(scopes.length, 0, ...additionalScopes);

    const userManager = new UserManager({
        authority: authority,        
        client_id: client_id,

        // We use a pop-up window for the initial sign-in.
        popup_redirect_uri: baseAddress + '/oidc/signin/popup',
        popup_post_logout_redirect_uri: baseAddress + '/oidc/signout/popup',
        post_logout_redirect_uri: baseAddress + '/oidc/signout/popup',
        popupWindowFeatures: 'menubar=no,location=yes,toolbar=no,width=546,height=602,left=450,top=86;resizable=yes',

        // Automatically renew tokens before they expire using silent sign-in (hidden iframe).
        automaticSilentRenew: true,
        checkSessionInterval: 10000,
        silent_redirect_uri: window.location.protocol + '//' + window.location.host + '/oidc/signin/silent',
    
        // Defaults needed for silent_renew
        response_type: 'id_token token',
        scope: scopes.join(' '),
    
        // Automatically query the user profile service.
        loadUserInfo: true,
    
        // Revoke (reference) access tokens when logging out
        revokeAccessTokenOnSignout: true,
    
        // Don't store OIDC protocol claims
        filterProtocolClaims: true
    });

    return userManager;
}
