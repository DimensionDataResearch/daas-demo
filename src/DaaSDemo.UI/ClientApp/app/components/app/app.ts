import { Aurelia, PLATFORM, inject } from 'aurelia-framework';
import { Router, RouterConfiguration } from 'aurelia-router';

export class App {
    router: Router;

    configureRouter(config: RouterConfiguration, router: Router) {
        config.title = 'Database-as-a-Service Demo';
        config.map([{
            route: [ '', 'home' ],
            name: 'home',
            settings: {
                icon: 'home'
            },
            moduleId: PLATFORM.moduleName('../home/home'),
            nav: true,
            title: 'Home'
        }, {
            route: 'servers',
            name: 'servers',
            settings: {
                icon: 'server',
                roles: [ 'User' ]
            },
            moduleId: PLATFORM.moduleName('../servers/list'),
            nav: true,
            title: 'Servers'
        }, {
            route: 'servers/:serverId',
            name: 'server',
            settings: {
                roles: [ 'User' ]
            },
            moduleId: PLATFORM.moduleName('../servers/detail'),
            title: 'Server'
        }, {
            route: 'servers/:serverId/databases',
            name: 'serverDatabases',
            settings: {
                roles: [ 'User' ]
            },
            moduleId: PLATFORM.moduleName('../databases/list-for-server'),
            title: 'Databases (server)'
        }, {
            route: 'servers/:serverId/events',
            name: 'serverEvents',
            settings: {
                roles: [ 'User' ]
            },
            moduleId: PLATFORM.moduleName('../servers/events'),
            title: 'Events (server)'
        }, {
            route: 'databases',
            name: 'databases',
            settings: {
                icon: 'database',
                roles: [ 'User' ]
            },
            moduleId: PLATFORM.moduleName('../databases/list'),
            nav: true,
            title: 'Databases'
        }, {
            route: 'databases/:databaseId',
            name: 'database',
            settings: {
                roles: [ 'User' ]
            },
            moduleId: PLATFORM.moduleName('../databases/detail'),
            title: 'Database'
        }, {
            route: 'admin/tenants',
            name: 'tenants',
            settings: {
                icon: 'world',
                menuGroup: 'admin',
                roles: [ 'Administrator' ]
            },
            moduleId: PLATFORM.moduleName('../tenants/list'),
            nav: true,
            title: 'Tenants'
        }, {
            route: 'admin/tenants/:tenantId',
            name: 'tenant',
            settings: {
                roles: [ 'Administrator' ]
            },
            moduleId: PLATFORM.moduleName('../tenants/detail'),
            title: 'Tenant'
        }, {
            route: 'admin/tenants/:tenantId/databases',
            name: 'tenantDatabases',
            settings: {
                roles: [ 'Administrator' ]
            },
            moduleId: PLATFORM.moduleName('../tenants/databases/list'),
            title: 'Databases'
        }, {
            route: 'admin/users',
            name: 'users',
            settings: {
                icon: 'user',
                menuGroup: 'admin',
                roles: [ 'Administrator' ]
            },
            moduleId: PLATFORM.moduleName('../users/list'),
            nav: true,
            title: 'Users'
        }, {
            route: 'admin/users/:userId',
            name: 'user',
            settings: {
                roles: [ 'Administrator' ]
            },
            moduleId: PLATFORM.moduleName('../users/detail'),
            title: 'User'
        }]);

        this.router = router;
    }
}
