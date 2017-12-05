import { inject } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { Interceptor } from 'aurelia-fetch-client';
import { User } from 'oidc-client';

import { AuthService } from './auth-service';

/**
 * An Interceptor that adds an access token to outgoing requests.
 */
@inject(EventAggregator, AuthService)
export class AuthenticationInterceptor implements Interceptor {
    private user: User | null = null;

    constructor(private eventAggregator: EventAggregator, private authService: AuthService) {
        this.authService.loadUser().then(user => {
            this.user = user;
        }).catch(loadUserFailed => {
            console.log('AuthenticationInterceptor: Failed to load user.', loadUserFailed)
        });
        this.eventAggregator.subscribe('AuthX.UserLoaded', (user: User) => {
            this.user = user;
        });
        this.eventAggregator.subscribe('AuthX.UserUnloaded', () => {
            this.user = null;
        });
    }

    /**
     * Add the access token (if available) to an outgoing request.
     * 
     * @param request The outgoing request.
     */
    public request(request: Request): Request {
        if (this.user && this.user.access_token)
            request.headers.set('Authorization', 'Bearer ' + this.user.access_token);

        return request;
    }
}
