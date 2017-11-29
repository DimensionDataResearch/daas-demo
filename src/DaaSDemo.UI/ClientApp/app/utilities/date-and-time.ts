export enum DatePart {
    Weeks = 'Weeks',
    Days = 'Days',
    Hours = 'Hours',
    Minutes = 'Minutes',
    Seconds = 'Seconds'
}

export function dateDiff(datePart: DatePart, fromDate: Date, toDate: Date) {	
    return Math.floor(
        (toDate.valueOf() - fromDate.valueOf()) / divideBy[datePart]
    );
}

interface DivideBy {
    [key: string]: number;
}
const divideBy: DivideBy = {
    'Weeks':   604800000, 
    'Days':    86400000, 
    'Hours':   3600000, 
    'Minutes': 60000, 
    'Seconds': 1000
};

