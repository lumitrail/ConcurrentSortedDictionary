# ConcurrentSortedDictionary
## Sutdy on Concurrency
### My policy
1. To read while reading can be concurrently done. = Reading doesn't block reading.
2. To write while reading is blocked by reading. = Reading blocks writing.
3. To read while writing is blocked by writing. = Writing blocks reading.
4. To write while writing is blocked by preceeding writing. = Writing blocks writing.

