'a'b'c'd'e'f10 { [a,b,c,d,e,f,\n] }
1000000[$0=~][7[$0=~][$1+ø,1-]#%1-]#% { print "abcdef\n" a million times }

{
    Results on my System (WSL2, 5900X)
    | Buffer Size | Interpreter | User   | System | Total  |
    |-------------|-------------|--------|--------|--------|
    |           1 |      (3.5s) | 0.222s | 0.311s | 0.534s |
    |           2 |      (3.5s) | 0.115s | 0.157s | 0.273s |
    |           3 |      (3.5s) | 0.086s | 0.114s | 0.200s |
    |           4 |      (3.5s) | 0.093s | 0.070s | 0.163s |
    |           5 |      (3.5s) | 0.077s | 0.062s | 0.139s |
    |           6 |      (3.5s) | 0.072s | 0.049s | 0.121s |
    |           7 |      (3.5s) | 0.063s | 0.046s | 0.109s |
    |           8 |      (3.5s) | 0.063s | 0.040s | 0.103s |
    |          12 |      (3.5s) | 0.058s | 0.024s | 0.082s |
    |          16 |      (3.5s) | 0.053s | 0.019s | 0.073s |
    |          24 |      (3.5s) | 0.050s | 0.013s | 0.064s |
    |          32 |      (3.5s) | 0.050s | 0.008s | 0.058s |
    |          64 |      (3.5s) | 0.046s | 0.004s | 0.051s |
    |          96 |      (3.5s) | 0.045s | 0.003s | 0.048s |
    |         128 |      (3.5s) | 0.044s | 0.002s | 0.047s |
    |        1024 |      (3.5s) | 0.043s | 0.001s | 0.043s |
    | flush on \n |      (3.5s) | 0.068s | 0.046s | 0.114s |

    This example just writes characters. However, the current compiler does not buffer output when printing decimals or string literals, only for characters.
    As these results show, implementing buffering for all string sources would be very much worth the effort.

    Running .\bin\Release\net6.0\FalseDotNet.Cli.exe to interpret takes about 3.5s,
    so the assembled code is a lot faster than that at least. Of course, this is not the fastest
    interpreter there is, it's just a lot better for debugging and testing.

    After changing the stack pointer to not be stored in memory and loaded for every command, execution time changed from 0.44s to 0.07s. Wtf
}
