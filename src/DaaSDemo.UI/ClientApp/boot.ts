import 'isomorphic-fetch';
import { Aurelia, PLATFORM } from 'aurelia-framework';
import { setLevel, logLevel, addAppender } from 'aurelia-logging';
import { ConsoleAppender } from 'aurelia-logging-console'; 
import { HttpClient } from 'aurelia-fetch-client';

import { AuthService } from './app/services/authx/auth-service';
import { ConfigService } from './app/services/config/config-service';

declare const IS_DEV_BUILD: boolean; // The value is supplied by Webpack during the build

export function configure(aurelia: Aurelia) {
    aurelia.use.standardConfiguration();

    aurelia.use.plugin(
        PLATFORM.moduleName('aurelia-validation')
    );

    if (IS_DEV_BUILD) {
        aurelia.use.developmentLogging();
    } else {
        setLevel(logLevel.info);
        addAppender(
            new ConsoleAppender()
        );
    }

    new HttpClient().configure(config => {
        const baseUrl = document.getElementsByTagName('base')[0].href;
        config.withBaseUrl(baseUrl);
    });

    aurelia.start().then(() => {
        aurelia.setRoot(
            PLATFORM.moduleName('app/components/app/app')
        );
    });
}
