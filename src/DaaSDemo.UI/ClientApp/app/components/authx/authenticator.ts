import { inject } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { Interceptor, HttpClient } from 'aurelia-fetch-client';
import { Log as OIDCLog, UserManager, UserManagerSettings, User } from 'oidc-client';

/**
 * The DaaS client-side authentication / authorisation manager.
 */
@inject(EventAggregator)
export class AuthXManager {
    private userManager: UserManager | null = null;

    /**
     * Create a new DaaS client-side authentication / authorisation manager.
     * 
     * @param eventAggregator The Aurelia event-aggregator service.
     */
    constructor(private eventAggregator: EventAggregator) {}

    /**
     * Initialise the AuthX manager.
     * 
     * @param authority The base address of the STS.
     * @param client_id The OIDC client Id representing the DaaS UI.
     * @param additionalScopes Additional default scopes (if any) to include with token requests.
     */
    public initialize(authority: string, client_id: string, additionalScopes: string[] = []): void {
        if (this.userManager !== null) {
            throw new Error('AuthXManager has already been initialised.');
        }

        this.userManager = createUserManager(authority, client_id, additionalScopes);
        
        this.userManager.events.addAccessTokenExpired(() => {
            this.eventAggregator.publish('AuthX.TokenExpired');
        });
        this.userManager.events.addSilentRenewError((error: Error) => {
            this.eventAggregator.publish('AuthX.SilentRenewError', error);
        });
        this.userManager.events.addUserLoaded((user: User) => {
            this.eventAggregator.publish('AuthX.UserLoaded', user);
        });
        this.userManager.events.addUserUnloaded(() => {
            this.eventAggregator.publish('AuthX.UserLoaded');
        });
        this.userManager.events.addUserSignedOut(() => {
            this.eventAggregator.publish('AuthX.UserSignedOut');
        });
    }

    /**
     * Add an Interceptor to the HTTP client that automatically requests access tokens.\
     * 
     * @param httpClient The HTTP client to configure.
     */
    public addAuthenticator(httpClient: HttpClient): void {
        if (this.userManager === null) {
            throw new Error('AuthXManager has not been initialised.');
        }

        httpClient.interceptors.push(
            new AuthenticationInterceptor(this.userManager)
        );
    }

    /**
     * Get claims associated with the current user.
     */
    public async getUserClaims(): Promise<UserClaims> {
        if (!this.userManager)
            throw new Error('AuthXManager has not been initialised.');

        const user = await this.userManager.getUser();
        
        const userClaims: UserClaims = {};
        
        return Object.assign(userClaims, user.profile); // Clone
    }
}

/**
 * Represents user claims, keyed by claim name.
 */
export interface UserClaims {
    [claim: string]: string;
}

/**
 * An Interceptor for HTTP clients that automatically requests access tokens for outgoing requests.
 */
export class AuthenticationInterceptor implements Interceptor {
    /**
     * Create a new AuthenticationInterceptor.
     * 
     * @param userManager The OIDC user manager.
     */
    constructor(private userManager: UserManager) {}

    /**
     * Called when an outgoing request is being processed by the client.
     * 
     * @param request The outgoing request.
     */
    public async request(request: Request): Promise<Request> {
        const scope = request.headers.get('X-AuthX-Scope');
        if (!scope) {
            return request;
        }

        let user = await this.userManager.getUser();
        if (!user)
            throw new Error('Cannot call API (not logged in).');

        request.headers.set('Authorization', 'Bearer: ' + user.access_token);
        request.headers.delete('X-AuthX-Scope');

        return request;
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
        popupWindowFeatures: 'menubar=no,location=yes,toolbar=no,width=640,height=480,left=300,top=200;resizable=yes',        
        
        post_logout_redirect_uri: baseAddress + '/',

        // Automatically renew tokens before they expire using silent sign-in (hidden iframe).
        automaticSilentRenew: true,
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
