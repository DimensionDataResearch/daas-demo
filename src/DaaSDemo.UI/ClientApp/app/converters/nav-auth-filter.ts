import { valueConverter, inject } from 'aurelia-framework';
import { AuthService } from '../services/authx/auth-service';
import { NavModel } from 'aurelia-router';

import { User } from 'oidc-client';

@valueConverter('navAuthFilter')
export class NavigationAuthorizationFilter {
    constructor() { }

    public toView(navModels: NavModel[], user: User): NavModel[] {
        console.log('NavigationAuthorizationFilter: filtering navModels for user.', navModels, user);

        const userRoles: string[] = (user && user.profile) ? user.profile.role : [];
        console.log('NavigationAuthorizationFilter: userRoles = ', userRoles);

        function userHasRole(role: string): boolean {
            return userRoles.indexOf(role) !== -1;
        }

        const filteredNavModels = navModels.filter(navModel => {
            const requiredRoles: string[] = navModel.settings.roles;
            if (!requiredRoles)
                return true;

            for (const requiredRole of requiredRoles) {
                console.log(`NavigationAuthorizationFilter: checking user role '${requiredRole}' for nav item '${navModel.title}'.`);

                if (!userHasRole(requiredRole)) {
                    console.log(`NavigationAuthorizationFilter: filtering out nav item '${navModel.title}' because user does not have role '${requiredRole}'.`);

                    return false;
                }
            }

            return true;
        });

        console.log('NavigationAuthorizationFilter: filtered nav models = ', filteredNavModels);

        return filteredNavModels;
    }
}
