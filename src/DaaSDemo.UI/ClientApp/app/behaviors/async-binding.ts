import { bindingBehavior } from 'aurelia-framework';

import { isPromise } from '../utilities/promises';

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
                console.log('AsyncBindingBehavior: busyUpdateTarget', busyValue);
                binding.originalupdateTarget(busyValue || null);
                
                value.then(resolvedValue => {
                    console.log('AsyncBindingBehavior: resolvedUpdateTarget', resolvedValue)
                    binding.originalupdateTarget(resolvedValue);
                });
            } else {
                console.log('AsyncBindingBehavior: originalUpdateTarget', busyValue);
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
