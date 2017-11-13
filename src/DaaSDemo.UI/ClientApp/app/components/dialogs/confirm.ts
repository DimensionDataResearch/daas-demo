/// <reference types="semantic-ui" />

import { bindable, inject } from 'aurelia-framework';
import * as $ from 'jquery';
import 'semantic';

import { Database } from '../api/daas-api';

const noAction = () => { };

export class ConfirmDialog {
    private rootElement: Element;
    private promise: Promise<boolean> | null;
    
    private title: string | null;
    private message: string | null;

    constructor() { }

    public show(title: string, message: string): Promise<boolean> {
        if (this.promise)
            return this.promise;

        return this.promise = new Promise<boolean>((resolve, reject) => {
            if (!title || !message)
                throw new Error('Must supply a title and message for confirmation dialog.');

            this.title = title;
            this.message = message;

            $(this.rootElement)
                .modal('setting', 'onApprove', () => {
                    resolve(true);

                    this.promise = null;
                })
                .modal('setting', 'onDeny', () => {
                    resolve(false);
                    
                    this.promise = null;
                })
                .modal('show');
        });
    }
}
