import { inject } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { UserManager, User } from 'oidc-client';

import { EndPoints } from '../api/daas-api'
import { HttpClient } from 'aurelia-fetch-client';

/**
 * The DaaS application authentication manager.
 */
@inject(EventAggregator)
export class AuthManager {
    private _userManager: UserManager | null = null;
    private _identityServerBaseAddress: string | null = null;
    private _initialized: Promise<void>;
    private _user: User | null;

    constructor(private eventAggregator: EventAggregator) {
        this._initialized = this.initialize();
    }

    /**
     * The current user information (if signed in).
     */
    public get user(): User | null {
        return this._user;
    }

    /**
     * Is the user currently signed in?
     */
    public get isSignedIn(): boolean {
        return this._user !== null;
    }

    /**
     * Initialise the AuthManager.
     */
    async initialize(): Promise<void> {
        const http = new HttpClient();

        const endPointsResponse = await http.fetch('end-points');
        if (!endPointsResponse.ok)
            throw new Error('Failed to retrieve configuration for DaaS API end-points.');

        const body = await endPointsResponse.json();
        const endPoints = body as EndPoints;

        this._identityServerBaseAddress = endPoints.identityServer;

        this._userManager = createUserManager(
            this._identityServerBaseAddress, // authority
            'daas-ui-dev',                  // client_id
            ['daas_api_v1']                 // additionalScopes
        );
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
            this.eventAggregator.publish('AuthX.UserLoaded');
        });
        this._userManager.events.addUserSignedOut(() => {
            this.eventAggregator.publish('AuthX.UserSignedOut');
        });
    }

    /**
     * Sign in.
     */
    public async signin(): Promise<User | null> {
        await this._initialized;

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
        await this._initialized;

        if (!this._userManager) {
            throw new Error('AuthManager has not been initialised.');
        }

        await this._userManager.signoutPopup();
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

    const scopes: string[] = [
        'openid',
        'profile'
    ];
    scopes.splice(0, 0, ...additionalScopes);

    const userManager = new UserManager({
        authority: authority,        
        client_id: client_id,

        // We use a pop-up window for the initial sign-in.
        popup_redirect_uri: baseAddress + '/oidc/signin/popup',
        popup_post_logout_redirect_uri: baseAddress + '/oidc/signout/popup',
        post_logout_redirect_uri: baseAddress + '/oidc/signout/popup',
        popupWindowFeatures: 'menubar=no,location=yes,toolbar=no,width=700,height=933,left=300,top=200;resizable=yes',

        // Automatically renew tokens before they expire using silent sign-in (hidden iframe).
        // automaticSilentRenew: true,
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
