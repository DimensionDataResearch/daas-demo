import { computedFrom, bindable } from 'aurelia-framework';
import { Logger, getLogger } from 'aurelia-logging';
import { RouteConfig } from 'aurelia-router';

import { ToastService } from '../../services/toast/toast-service';

const log: Logger = getLogger('ViewModel');

/**
 * The base class for view models.
 */
export abstract class ViewModel {
    private _isLoading: boolean = false;
    private _isBusy: boolean = false;
    
    protected routeConfig: RouteConfig;
    protected pollHandle: number = 0;

    /**
     * Has an error occurred?
     */
    @bindable public hasError: boolean;

    protected constructor(protected toastService: ToastService) { }

    /**
     * Is the view loading?
     */
    public get isLoading(): boolean {
        return this._isLoading;
    }

    /**
     * Is the view busy?
     */
    public get isBusy(): boolean {
        return this._isBusy;
    }

    /**
     * Called when the component is activated.
     * 
     * @param params Route parameters.
     * @param routeConfig The configuration for the currently-active route.
     */
    public activate(params: any, routeConfig: RouteConfig): void {
        this.routeConfig = routeConfig;
    }

    /**
     * Called when the component is deactivated.
     */
    public deactivate(): void {
        if (this.pollHandle != 0) {
            window.clearTimeout(this.pollHandle);
            this.pollHandle = 0;
        }
    }

    /**
     * Execute an action, ensuring that isLoading is true while it runs.
     * 
     * @param action The action to execute.
     */
    protected runLoading(action: () => void): void {
        this.toastService.dismissAll();

        try {
            this._isLoading = true;

            action();

            this.clearError();
        } catch (error) {
            this.showError(error as Error);
        } finally {
            this._isLoading = false;
        }
    }

    /**
     * Execute an action, ensuring that isLoading is true while it runs.
     * 
     * @param action The action to execute.
     */
    protected async runLoadingAsync(action: () => Promise<void>): Promise<void> {
        this.toastService.dismissAll();
        
        try {
            this._isLoading = true;

            await action();

            this.clearError();
        } catch (error) {
            this.showError(error as Error);
        } finally {
            this._isLoading = false;
        }
    }

    /**
     * Execute an action, ensuring that isBusy is true while it runs.
     * 
     * @param action The action to execute.
     */
    protected runBusy(action: () => void): void {
        try {
            this._isBusy = true;

            action();

            this.clearError();
        } catch (error) {
            this.showError(error as Error);
        } finally {
            this._isBusy = false;
        }
    }

    /**
     * Execute an action, ensuring that isBusy is true while it runs.
     * 
     * @param action The action to execute.
     */
    protected async runBusyAsync(action: () => Promise<void>): Promise<void> {
        try {
            this._isBusy = true;

            await action();

            this.clearError();
        } catch (error) {
            this.showError(error as Error);
        } finally {
            this._isBusy = false;
        }
    }

    /**
     * Clear the view model's error status.
     */
    protected clearError(): void {
        this.hasError = false;
    }

    /**
     * Show an error message.
     * 
     * @param error The error to show.
     */
    protected showError(error: Error): void {
        log.error('Unexpected error: ', error);
        
        this.hasError = true;
        this.toastService.showError(
            (error.message as string || 'Unknown error.').split('\n').join('<br/>'),
        );
    }
}

function isPromise<T>(mightBePromise: any): mightBePromise is Promise<T> {
    return typeof mightBePromise.then === 'function';
}
