# benchmark-sqlite3-bulk-insert

## Usage:

`PS> .\BenchApp.exe --help`
```
Benchmark of sqlite3 bulk inserts with different pragmas.

-i|--batchSizeIncrement Optional with default '5000'. Growth of 'batchSizeBase'
                        parameter in each test set run.
-b|--batchSizeBase      Optional with default '5000'. Minimal number of inserts
                        in a transaction, will be incremented by
                        'batchSizeIncrement' in each test set run.
-t|--tableCount         Optional with default '10'. Number of tables to use for
                        inserts.
-s|--string             Optional with default 'string'. Value to insert (along
                        with sequential integer primary key).
-r|--rowCount           Optional with default '5000000'. Total number of rows
                        to insert in each test.
```


## Publish, Run with default parameters, Clean-up artifacts:
`PS> ./publish-run-cleanup.ps1`

```
TDB
```

## Info:

  * https://sqlite.org/pragma.html
  * https://stackoverflow.com/Questions/364017/faster-bulk-inserts-in-sqlite3
