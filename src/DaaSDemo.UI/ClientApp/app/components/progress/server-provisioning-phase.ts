/// <reference types="semantic-ui" />

import { bindable, inject, computedFrom } from 'aurelia-framework';
import * as $ from 'jquery';
import 'semantic';

import { DatabaseServer, ServerProvisioningPhase } from '../../services/api/daas-models';

export class ServerProvisioningPhaseProgress {
    @bindable private progressBarElement: Element
    
    @bindable public server: DatabaseServer | null;

    /**
     * Called when the server has been updated.
     * 
     * @param oldValue The old value of the "server" field.
     * @param newValue The new value of the "server" field.
     */
    public serverChanged(oldValue: DatabaseServer, newValue: DatabaseServer): void {
        this.update();
    }

    /**
     * The server's current provisioning phase.
     */
    @computedFrom('server')    
    public get currentPhase(): ServerProvisioningPhase {
        if (!this.server)
            return ServerProvisioningPhase.None;

        return this.server.phase;
    }

    /**
     * If an action is in progress for the server, its percentage completion.
     */
    @computedFrom('server')
    public get actionPercentComplete(): number {
        switch (this.currentPhase) {
            case ServerProvisioningPhase.Storage:
                return 20;
            case ServerProvisioningPhase.Security:
                return 30;
            case ServerProvisioningPhase.Instance:
                return 40;
            case ServerProvisioningPhase.Network:
                return 50;
            case ServerProvisioningPhase.Monitoring:
                return 60;
            case ServerProvisioningPhase.Configuration:
                return 80;
            case ServerProvisioningPhase.Ingress:
                return 100;
            default:
                return 0;
        }
    }

    /**
     * Update the progress bar.
     */
    public update(): void {
        $(this.progressBarElement).progress('set percent', this.actionPercentComplete);
    }

    /**
     * Called when the component is attached to the DOM.
     */
    private attached(): void {
        $(this.progressBarElement).progress('set percent', this.actionPercentComplete);
    }

    /**
     * Called when the component is detached from the DOM.
     */
    private detached(): void {
        $(this.progressBarElement).progress('destroy');
    }
}
