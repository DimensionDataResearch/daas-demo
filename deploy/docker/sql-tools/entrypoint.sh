#!/bin/bash

set -euo pipefail

if [ -f 'run.sql' ]; then
    rm run.sql
fi

if [ -f 'secrets.sql' ]; then
    cat secrets.sql >> run.sql
fi

if [ -f 'script.sql' ]; then
    cat script.sql >> run.sql
fi

if [ ! -f 'run.sql' ]; then
    echo "Not found: run.sql (add secrets.sql and / or script.sql to ) $PWD."
    
    return 1;
fi

echo 'Running SQLCMD...'

sqlcmd -U "$SQL_USER" -P "$SQL_PASSWORD" -S "$SQL_HOST" -d "$SQL_DATABASE" -i "$PWD/run.sql"

echo 'Done.'
