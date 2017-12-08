import { singleton, inject } from 'aurelia-framework';
import { Logger, getLogger } from 'aurelia-logging';

import * as $ from 'jquery';
import * as toastr from 'toastr';
import { EventAggregator } from 'aurelia-event-aggregator';

const logger: Logger = getLogger('ToastService');

export interface ToastOptions {
    message: string;
    title?: string;
}

@singleton()
@inject(EventAggregator)
export class ToastService {
    constructor(private eventAggregator: EventAggregator) {
        toastr.options.closeButton = true;
        toastr.options.hideDuration = 500;
        toastr.options.timeOut = 1300;

        eventAggregator.subscribe('Toast.Success', (toastOptions: ToastOptions) => {
            this.showSuccess(toastOptions.message, toastOptions.title);
        });
        eventAggregator.subscribe('Toast.Info', (toastOptions: ToastOptions) => {
            this.showInfo(toastOptions.message, toastOptions.title);
        });
        eventAggregator.subscribe('Toast.Warning', (toastOptions: ToastOptions) => {
            this.showWarning(toastOptions.message, toastOptions.title);
        });
        eventAggregator.subscribe('Toast.Error', (toastOptions: ToastOptions) => {
            this.showError(toastOptions.message, toastOptions.title);
        });
    }

    public showSuccess(message: string, title?: string): void {
        toastr.success(message, title || 'Success');
    }

    public showInfo(message: string, title?: string): void {
        toastr.info(message, title || 'Information');
    }

    public showWarning(message: string, title?: string): void {
        toastr.warning(message, title || 'Warning');
    }

    public showError(message: string, title?: string): void {
        toastr.error(message, title || 'Error', {
            timeOut: 0
        });
    }

    public dismissAll(): void {
        toastr.clear();
    }
}
