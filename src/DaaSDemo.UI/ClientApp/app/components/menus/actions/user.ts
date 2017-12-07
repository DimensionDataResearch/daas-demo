/// <reference types="semantic-ui" />

import { bindable } from 'aurelia-framework';
import * as $ from 'jquery';
import 'semantic';

import { User } from '../../../services/api/daas-models';

const noAction = () => {};

export class UserActionsMenu {
    @bindable private rootElement: Element | null = null;
    
    @bindable public user: User | null = null;
    @bindable public label: string | null = null;
    @bindable public disabled: boolean = false;
    @bindable public destroyClicked: () => void = noAction;

    constructor() {}

    public attached(): void {
        if (this.rootElement) {
            $(this.rootElement).dropdown();
        }
    }

    public get canDelete(): boolean {
        if (this.destroyClicked === noAction)
            return false; // No handler.

        if (!this.user)
            return false;

        return !this.user.isSuperUser;
    }
}
