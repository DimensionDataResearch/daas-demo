import { bindable } from 'aurelia-framework';

import { Server } from '../api/daas-api';

export class ServerConnectivity
{
    @bindable public server: Server;

    constructor() {}
}
