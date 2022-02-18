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
    |          64 |          0.44s |
    |          96 |          0.44s |
    |         128 |          0.43s |
    |        1024 |          0.43s |
    | flush on \n |          0.44s |

    The write syscall seems to be very fast, any buffer size above 8
    seems to be useless.
    Keeping FlushOnNewline on won't really come at a cost,
    but makes the program output nicer to read.

    Running .\bin\Release\net6.0\FalseDotNet.Cli.exe to interpret takes about 3.5s,
    so the assembled code is at least not slower than the interpreter.
}
