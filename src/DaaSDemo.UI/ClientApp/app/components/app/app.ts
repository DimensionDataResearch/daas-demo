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
        },{
            route: 'tenants',
            name: 'tenants',
            settings: { icon: 'user' },
            moduleId: PLATFORM.moduleName('../tenants/list'),
            nav: true,
            title: 'Tenants'
        }, {
            route: 'tenants/:tenantId',
            name: 'tenant',
            moduleId: PLATFORM.moduleName('../tenants/detail'),
            title: 'Tenant'
        }, {
            route: 'tenants/:tenantId/databases',
            name: 'tenantDatabases',
            moduleId: PLATFORM.moduleName('../tenants/databases/list'),
            title: 'Databases'
        }, {
            route: 'servers',
            name: 'servers',
            settings: { icon: 'server' },
            moduleId: PLATFORM.moduleName('../servers/list'),
            nav: true,
            title: 'Servers'
        }, {
            route: 'servers/:serverId',
            name: 'server',
            moduleId: PLATFORM.moduleName('../servers/detail'),
            title: 'Server'
        }, {
            route: 'servers/:serverId/databases',
            name: 'serverDatabases',
            moduleId: PLATFORM.moduleName('../databases/list-for-server'),
            title: 'Databases (server)'
        }, {
            route: 'servers/:serverId/events',
            name: 'serverEvents',
            moduleId: PLATFORM.moduleName('../servers/events'),
            title: 'Events (server)'
        }, {
            route: 'databases',
            name: 'databases',
            settings: { icon: 'database' },
            moduleId: PLATFORM.moduleName('../databases/list'),
            nav: true,
            title: 'Databases'
        }, {
            route: 'databases/:databaseId',
            name: 'database',
            settings: { icon: 'database' },
            moduleId: PLATFORM.moduleName('../databases/detail'),
            title: 'Database'
        }, {
            route: 'users',
            name: 'users',
            settings: { icon: 'user', roles: [ 'User' ] },
            moduleId: PLATFORM.moduleName('../users/list'),
            nav: true,
            title: 'Users'
        }]);

        this.router = router;
    }
}
