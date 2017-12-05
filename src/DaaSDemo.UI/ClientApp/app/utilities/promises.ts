/**
 * Determine whether a value represents a Promise.
 * 
 * @param value The value to examine.
 * 
 * @returns true (type-guard), if the value is a Promise; otherwise, false.
 */
export function isPromise<T>(value: any): value is Promise<T> {
    return typeof value['then'] === 'function';
}
