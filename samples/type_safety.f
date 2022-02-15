{ print ansi color code } [27,'[,.'m,]m:

10,32m;!"Here are some examples for type safety."       10,
34m;!"Pop Number as Number: "35m;! 34 35+.              10,
34m;!"Pop Lambda as Lambda: "35m;! 42[10*]!.            10,
34m;!"Pop Ref as Ref:       "35m;! [5/]d:345d;!.        10,
10,

{ References push the index into the variable buffer onto the stack. a=0, ..., z=25 }
[34m;!"Pop Ref as Int:       "35m;! x23+.                10,]a:

{ This is fine, since references are masked with 0x1f anyway.
  With full type safety, this is still forbidden, however. }
[34m;!"Pop Int as Ref:       "35m;! 123 5:6f;+.          10,]b:

{ Main code is 0, and there were a couple lambdas before this.
  This means, the ids should be 7 and 8.
  While not unsafe, this may yield ub in the case of the compiled code. }
[34m;!"Pop Lambda as Int:    "35m;! ["asd"].", "["xyz"]. 10,]c:

{ Index 4 should print "xyz". In the assembled program, however,
  That number is treated as an address and will segfault,
  and in other cases may yield ub. }
[34m;!"Pop Int as Lambda:    "35m;! '",8!'",             10,]d:

1 a; ?
1 b; ?
1 c; ?
1 d; ?