'a'b'c'd'e'f10 { [a,b,c,d,e,f,\n] }
1000000[$0=~][7[$0=~][$1+ø,1-]#%1-]#% { print "abcdef\n" a million times }

{
    Results on my System (WSL2, 5900X)
    | Buffer Size | Execution time |
    |-------------|----------------|
    |           1 |          0.50s |
    |           2 |          0.49s |
    |           3 |          0.45s |
    |           4 |          0.46s |
    |           5 |          0.46s |
    |           6 |          0.44s |
    |           7 |          0.45s |
    |           8 |          0.44s |
    |          12 |          0.44s |
    |          16 |          0.44s |
    |          24 |          0.44s |
    |          32 |          0.44s |
}
