import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';

@inject(HttpClient)
export class TenantList {
    public tenants: Tenant[];

    constructor(private http: HttpClient) {
        http.configure(request =>
            request.withBaseUrl(
                'http://kr-cluster.tintoy.io:31200/api/v1/'
            )
            .withDefaults({
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            })
        );

        http.fetch('tenants')
            .then(result => result.json() as Promise<Tenant[]>)
            .then(data => {
                this.tenants = data;
            });
    }
}

/**
 * Represents basic information about a tenant.
 */
interface Tenant {
    /**
     * The tenant Id.
     */
    id: number,

    /**
     * The tenant name.
     */
    name: string
}
