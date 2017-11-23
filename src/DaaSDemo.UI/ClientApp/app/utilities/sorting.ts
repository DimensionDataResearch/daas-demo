import { Server } from '../components/api/daas-api';

/**
 * Represents a named item.
 */
export interface NamedItem {
    name: string;
}

/**
 * Sort the items in the specified array by name.
 * 
 * @param items The items to sort.
 * 
 * @return 
 */
export function sortByName<T extends NamedItem>(items: T[]): T[] {
    items.sort((item1, item2) => {
        const name1 = item1.name.toUpperCase();
        const name2 = item2.name.toUpperCase();

        if (name1 < name2)
            return -1;

        if (name1 > name2)
            return 1;

        return 0;
    });

    return items;
}
