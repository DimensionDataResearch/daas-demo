import { computedFrom } from 'aurelia-framework';
import { RouteConfig } from 'aurelia-router';

/**
 * The base class for view models.
 */
export abstract class ViewModel {
    private _isLoading: boolean = false;
    private _isBusy: boolean = false;
    
    protected routeConfig: RouteConfig;
    protected pollHandle: number = 0;
    
    public errorMessage: string | null = null;

    /**
     * Has an error occurred?
     */
    @computedFrom('errorMessage')
    public get hasError(): boolean {
        return this.errorMessage !== null;
    }

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
        this.clearError();

        try {
            this._isLoading = true;

            action();
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
        this.clearError();

        try {
            this._isLoading = true;

            await action();
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
        this.clearError();

        try {
            this._isBusy = true;

            action();
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
        this.clearError();

        try {
            this._isBusy = true;

            await action();
        } catch (error) {
            this.showError(error as Error);
        } finally {
            this._isBusy = false;
        }
    }

    /**
     * Clear the current error message (if any).
     */
    protected clearError(): void {
        this.errorMessage = null;
    }

    /**
     * Show an error message.
     * 
     * @param error The error to show.
     */
    protected showError(error: Error): void {
        console.log(error);
        
        this.errorMessage = (error.message as string || 'Unknown error.').split('\n').join('<br/>');
    }
}

function isPromise<T>(mightBePromise: any): mightBePromise is Promise<T> {
    return typeof mightBePromise.then === 'function';
}
