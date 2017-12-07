import { valueConverter, inject } from 'aurelia-framework';
import { AuthService } from '../services/authx/auth-service';
import { NavModel } from 'aurelia-router';

import { User } from 'oidc-client';

@valueConverter('navAuthFilter')
export class NavigationAuthorizationFilter {
    constructor() { }

    public toView(navModels: NavModel[], user: User): NavModel[] {
        const userProfile = (user && user.profile) ? user.profile : { roles: [] };

        // If the user only has a single role, then userProfile.role will be a string rather than an array of strings.
        const userRoles: string[] = isArray<string>(userProfile.role) ? userProfile.role : [ userProfile.role ];
        
        function userHasRole(role: string): boolean {
            return userRoles.indexOf(role) !== -1;
        }

        const filteredNavModels = navModels.filter(navModel => {
            const requiredRoles: string[] = navModel.settings.roles;
            if (!requiredRoles)
                return true;

            for (const requiredRole of requiredRoles) {
                if (!userHasRole(requiredRole)) {
                    return false;
                }
            }

            return true;
        });

        return filteredNavModels;
    }
}

function isArray<T>(value: any): value is Array<T> {
    return Array.isArray(value);
}
