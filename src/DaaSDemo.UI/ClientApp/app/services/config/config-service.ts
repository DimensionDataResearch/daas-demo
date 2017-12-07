import { inject, singleton } from 'aurelia-framework';
import { HttpClient } from 'aurelia-fetch-client';

/**
 * The configuration-management service.
 */
@singleton()
@inject(HttpClient)
export class ConfigService {
    private _initialized: Promise<void>;
    private _configuration: Configuration;

    constructor(private http: HttpClient) {
        this._initialized = this.initialize();
    }

    public async getConfiguration(): Promise<Configuration> {
        await this._initialized;

        return this._configuration;
    }

    private async initialize(): Promise<void> {
        const endPointsResponse = await this.http.fetch('config');
        if (!endPointsResponse.ok)
            throw new Error('Failed to retrieve configuration for DaaS API end-points.');

        const body = await endPointsResponse.json();
        this._configuration = body as Configuration;
    }
}

export interface Configuration {
    api: ApiConfiguration;
    identity: IdentityConfiguration;
}

export interface ApiConfiguration {
    endPoint: string;
}

export interface IdentityConfiguration {
    authority: string;
    clientId: string;
    additionalScopes: string;
}
