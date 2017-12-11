import * as $ from 'jquery';
import 'semantic';

import { inject, bindable, bindingBehavior } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { Logger, getLogger } from 'aurelia-logging';
import { Router, NavModel } from 'aurelia-router';
import { User } from 'oidc-client';

import { AuthService } from '../../services/authx/auth-service';

const log: Logger = getLogger('NavMenu');

/**
 * Model for the navigation menu.
 */
@inject(EventAggregator, AuthService)
export class NavMenu {
    @bindable public router: Router | null = null;
    @bindable public user: User | null = null;
    @bindable public adminDropDown: Element | null = null;

    /**
     * Create a new navigation menu model.
     * 
     * @param eventAggregator The Aurelia event-aggregator service.
     * @param authService The authentication / authorisation service.
     */
    constructor(private eventAggregator: EventAggregator, private authService: AuthService) {
        this.eventAggregator.subscribe('AuthX.UserLoaded', (user: User) => {
            log.debug('User loaded.', user);

            this.user = user;
        });
        this.eventAggregator.subscribe('AuthX.UserUnloaded', () => {
            log.debug('User unloaded.');

            this.user = null;
        });
    }

    /**
     * Models for the main navigation menu.
     */
    public get mainNavigation(): NavModel[] {
        return this.getNavModels();
    }

    /**
     * Models for the administrative navigation menu.
     */
    public get adminNavigation(): NavModel[] {
        return this.getNavModels('admin');
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

    /**
     * Called when the component is detached from the DOM.
     */
    public detached(): void {
        if (this.adminDropDown) {
            $(this.adminDropDown).dropdown('destroy');
        }
    }

    /**
     * Called when the adminDropDown element binding is changed.
     * 
     * @param newValue The new adminDropDown element (if any).
     * @param oldValue The old adminDropDown element (if any).
     */
    private adminDropDownChanged(newValue: Element, oldValue: Element) {
        if (oldValue) {
            $(oldValue).dropdown('destroy');
        }

        if (newValue) {
            $(newValue).dropdown();
        }
    }

    /**
     * Get navigation models for the specified menu group (or the main menu group).
     * 
     * @param menuGroup An optional menu group name (if not specified, models for the main menu will be returned).
     */
    private getNavModels(menuGroup?: string): NavModel[] {
        if (!this.router || !this.router.navigation) {
            return [];
        }

        return this.router.navigation.filter(navModel => {
            if (!navModel.settings) {
                return true;
            }

            return navModel.settings.menuGroup === menuGroup;
        });
    }
}
