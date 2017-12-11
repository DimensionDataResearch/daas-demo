import { bindingBehavior } from 'aurelia-framework';
import { getLogger, log } from 'aurelia-logging';

import { isPromise } from '../utilities/promises';

const log: log = getLogger('AsyncBindingBehavior');

/**
 * Asynchronous binding behaviour.
 */
@bindingBehavior('async')
export class AsyncBindingBehavior {
    constructor() {}

    /**
     * Bind the value.
     * 
     * @param binding The target binding.
     * @param source The binding source.
     * @param busyValue An optional value to bind with until the promise is resolved.
     */
    bind(binding: any, source: any, busyValue?: any) {
        binding.originalupdateTarget = binding.updateTarget;

        // Override the binding-update function.
        binding.updateTarget = (value: any) => {
            if (isPromise(value)) {
                log.debug('AsyncBindingBehavior: busyUpdateTarget', busyValue);
                binding.originalupdateTarget(busyValue || null);
                
                value.then(resolvedValue => {
                    log.debug('AsyncBindingBehavior: resolvedUpdateTarget', resolvedValue)
                    binding.originalupdateTarget(resolvedValue);
                });
            } else {
                log.debug('AsyncBindingBehavior: originalUpdateTarget', busyValue);
                binding.originalupdateTarget(value);
            }
        };
    }

    /**
     * Unbind the value.
     * 
     * @param binding The target binding.
     */
    unbind(binding: any) {
        binding.updateTarget = binding.originalupdateTarget;
        binding.originalupdateTarget = null;
    }
}
