/// <reference types="semantic-ui" />

import { bindable } from 'aurelia-framework';
import * as $ from 'jquery';
import 'semantic';

import { DatabaseServer, Database } from '../../api/daas-api';

const noAction = () => {};

export class DatabaseActionsMenu {
    @bindable private rootElement: Element | null = null;
    
    @bindable public database: Database | null = null;
    @bindable public label: string | null = null;
    @bindable public disabled: boolean = false;
    @bindable public destroyClicked: () => void = noAction;

    constructor() {}

    public attached(): void {
        if (this.rootElement) {
            $(this.rootElement).dropdown();
        }
    }

    public get canDestroy(): boolean {
        if (this.destroyClicked === noAction)
            return false; // No handler.

        if (!this.database)
            return false;

        switch (this.database.status) {
            case 'Ready':
            case 'Error':
                return true;
            default:
                return false;
        }
    }
}
