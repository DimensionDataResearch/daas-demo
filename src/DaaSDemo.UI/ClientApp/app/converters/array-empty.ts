import { valueConverter, inject } from 'aurelia-framework';

@valueConverter('arrayEmpty')
export class ArrayEmptyValueConverter {
    constructor() { }

    public toView(value: any[]): boolean {
        return Array.isArray(value) && !value.length;
    }
}
