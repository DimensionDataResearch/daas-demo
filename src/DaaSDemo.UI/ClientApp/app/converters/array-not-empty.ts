import { valueConverter, inject } from 'aurelia-framework';

@valueConverter('arrayNotEmpty')
export class ArrayNotEmptyValueConverter {
    constructor() { }

    public toView(value: any[]): boolean {
        return Array.isArray(value) && !!value.length;
    }
}
