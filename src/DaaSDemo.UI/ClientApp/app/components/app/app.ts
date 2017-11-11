import { Aurelia, PLATFORM, inject } from 'aurelia-framework';
import { Router, RouterConfiguration } from 'aurelia-router';

export class App {
    router: Router;

    configureRouter(config: RouterConfiguration, router: Router) {
        config.title = 'Database-as-a-Service Demo';
        config.map([{
            route: [ '', 'home' ],
            name: 'home',
            settings: { icon: 'home' },
            moduleId: PLATFORM.moduleName('../home/home'),
            nav: true,
            title: 'Home'
        }, {
            route: 'tenants',
            name: 'tenants',
            settings: { icon: 'user' },
            moduleId: PLATFORM.moduleName('../tenants/tenants'),
            nav: true,
            title: 'Tenants'
        }, {
            route: 'tenants/:id',
            name: 'tenant',
            moduleId: PLATFORM.moduleName('../tenant/tenant'),
            title: 'Tenant'
        }, {
            route: 'tenants/:id/databases',
            name: 'tenantDatabases',
            moduleId: PLATFORM.moduleName('../tenant-databases/tenant-databases'),
            title: 'Databases'
        }, {
            route: 'servers',
            name: 'servers',
            settings: { icon: 'server' },
            moduleId: PLATFORM.moduleName('../servers/servers'),
            nav: true,
            title: 'Servers'
        }, {
            route: 'databases',
            name: 'databases',
            settings: { icon: 'database' },
            moduleId: PLATFORM.moduleName('../databases/databases'),
            nav: true,
            title: 'Databases'
        }]);

        this.router = router;
    }
}
