{ Benchmark output buffering. }

{ Iterations: } 100000000 {100M}

l:0[$l;=~\1+\]["Line"32,$.10,]#

{
    Combination of printing characters, decimals, and string literals.

    Buffer size is 64. The following three cases benchmark performance
    where buffering is implemented only for
    chars, chars and strings, or numbers and strings, respectively.

    | Buffer  | User  | System | Total  |
    |---------|-------|--------|--------|
    | char    | 8.20s | 18.02s | 26.22s |
    | +string | 6.95s | 13.50s | 20.45s |
    | +number | 3.16s |  0.98s |  4.14s |

    Since line length is very short, the buffer size of 64 is only really used
    if everything, including integers is buffered. This reduces the syscall count
    from 200M (flush before and after decimals) to 21,980,050 at a buffer size of 64.

    In v0.0.8, consequent pop/push commands are replaced with peek/replace.
    This just prevents the stack counter from being decremented and incremented.

    These runs were done with full buffering, at different buffer sizes:

    | BufSize | User   | System | Total   | v0.0.8 |
    |---------|--------|--------|---------|--------|
    |       1 | 10.08s | 17.73s | 27.818s |        |
    |       2 |  8.56s | 17.62s | 26.181s |        |
    |       3 |  8.16s | 17.90s | 26.064s |        |
    |       4 |  8.22s | 17.93s | 26.154s |        |
    |       5 |  7.49s | 13.52s | 21.014s |        |
    |       6 |  6.22s |  8.55s | 14.773s |        |
    |       7 |  5.16s |  9.23s | 14.393s |        |
    |       8 |  5.52s |  8.94s | 14.462s |        |
    |       9 |  5.91s |  9.22s | 15.130s |        |
    |      10 |  6.67s |  8.87s | 15.143s |        |
    |      11 |  6.22s |  8.87s | 15.094s |        |
    |      12 |  6.49s |  8.57s | 15.060s |        |
    |      13 |  6.12s |  8.36s | 14.486s |        |
    |      14 |  3.99s |  4.75s |  8.743s |        |
    |      16 |  4.39s |  4.58s |  8.967s |        |
    |      18 |  4.57s |  4.46s |  9.038s |        |
    |      20 |  4.46s |  4.58s |  9.044s |        |
    |      22 |  4.73s |  4.03s |  8.766s |        |
    |      24 |  3.89s |  3.13s |  7.023s |        |
    |      26 |  4.13s |  2.79s |  6.923s |        |
    |      28 |  3.29s |  2.31s |  5.602s |        |
    |      30 |  3.87s |  2.10s |  5.974s |        |
    |      32 |  3.75s |  2.25s |  5.998s |        |
    |      36 |  3.94s |  1.98s |  5.927s |        |
    |      40 |  3.43s |  1.62s |  5.052s |        |
    |      44 |  3.24s |  1.48s |  4.728s |        |
    |      48 |  3.16s |  1.62s |  4.780s |        |
    |      56 |  2.80s |  1.23s |  4.034s |        |
    |      64 |  3.11s |  1.06s |  4.171s | 4.157s |
    |      72 |  2.75s |  0.94s |  3.694s |        |
    |      80 |  2.61s |  0.81s |  3.420s |        |
    |      96 |  2.59s |  0.65s |  3.245s |        |
    |     112 |  2.44s |  0.61s |  3.048s |        |
    |     128 |  2.33s |  0.55s |  2.879s | 2.811s |
    |     144 |  2.56s |  0.38s |  2.936s |        |
    |     160 |  2.28s |  0.44s |  2.724s |        |
    |     192 |  2.21s |  0.41s |  2.626s |        |
    |     224 |  2.30s |  0.31s |  2.616s |        |
    |     256 |  2.26s |  0.37s |  2.628s | 2.274s |
    |     320 |  2.27s |  0.24s |  2.507s |        |
    |     384 |  2.21s |  0.13s |  2.340s |        |
    |     512 |  2.14s |  0.11s |  2.248s | 2.059s |
    |     768 |  2.08s |  0.08s |  2.165s |        |
    |    1024 |  2.07s |  0.07s |  2.139s | 1.913s |
    |    2048 |  2.05s |  0.02s |  2.073s | 1.859s |
    |    4096 |  2.03s |  0.00s |  2.029s | 1.821s |
    |    8192 |  2.03s |  0.00s |  2.031s | 1.838s |
    |   16384 |  2.04s |  0.00s |  2.039s |        |
    |   32768 |  2.01s |  0.01s |  2.017s |        |
    |   65536 |  2.00s |  0.00s |  1.999s |        |
}