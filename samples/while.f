{ Example of a nested while loop. }

1[$4=~][
	$.':,' ,
	1[$3=~][
		$. ',, ' ,
		1+
	]#.
	10,
	1+
]#%

"Done."10,

{-- Expected output --}
{
    1: 1, 2, 3
    2: 1, 2, 3
    3: 1, 2, 3
    Done.
}