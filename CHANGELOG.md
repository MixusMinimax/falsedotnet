# Changes

## 0.0.1

Implement interpreter. No type safety.

## 0.0.2

Implement compiler. No type safety.
The FALSE stack is stored in a mmap-allocated memory area. It is not possibly to use the standard x86 stack using `push` and `pop`, because it is modified by `call` and `ret`, and is needed to maintain information for control structures such as `if` and `while`.

## 0.0.3

Buffer stdout for the compiled code. When printing characters one-by-one, they are stored in a buffer, and flushed if full or a newline is printed.

For now, other print sources like printing string literals and numbers results in a flush directly, as the write-syscall is not actually that slow. Performance increase stops at a buffer size of about 6 or 7.

## 0.0.4

Implement type safety. 

There are three levels of type safety: `{NONE, LAMBDA, FULL}`. Both `NONE` and `FULL` are self-explanatory, they either enforce types for stack elements or not.
Arguably, it is most important to verify the type when executing a stack element as a lambda, which is the only thing the `LAMBDA` safety-level enforces. This is usually a good balance between safety and efficiency.

It is important to keep in mind that references are masked to stay within `[0,32)`, so using an integer as a reference is not really a problem, which is why the `LAMBDA` safety is probably the appropriate level in most cases.

When type-safety is not `NONE`, type information is maintained on a separate stack as to not mess up alignment of the main FALSE stack.

## 0.0.5

There are three globals used throughout the program: `stack`, `stack_ptr`, and `type_stack`. Instead of using a stack pointer directly, `stack_ptr` actually represents the index into the stack, as it is used for both the data- and the type stack.

These globals used to be located in `.bss` and `.data`, and interacted with for every push and pop. This was of course very slow, but the plan was to optimize redundant loads and stores away later.

Instead, with this version, I use the registers `R12`-`R14` for these values and never write them to memory.

This has resulted in a 6.4x performance increase.

With these new results, it might be necessary to improve stdout buffering to also buffer decimal representations.

## 0.0.6

Implement ReadChar command: `^`.

Add `-i` argument to read input from a file instead of stdin, making testing easier. This works for both the interpreter and the compiler.

## 0.0.7

Buffer output from PrintString and PrintDecimal. If the printed string is longer than the buffer, the entire string is printed using the write syscall instead of copying it into the stdout buffer chunk by chunk. If the buffer is big enough but too full, the buffer is flushed before copying the string into the buffer.

In addition, the buffer is flushed on program exit, just like with the interpreter. For some reason, this was missing until now.

Stdin remains unbuffered, because we can't know how many characters to read in advance. It would be possible to analyze the program to know when to read input in chunks, i.e. the FALSE code `^^^` could just execute a single syscall. However, reading characters usually happens in loops, and in that case it would be way harder to predict how many characters to read.

I personally don't really see a reason to bother, since the important parts of the language are pretty fast now.

## 0.0.8

Consequent Pops and Pushes are replaced with Peek/Replace.

Example:

```asm
; Add top two numbers on the stack

; Before
; pop, pop, add, push
dec r12
mov rax, [r13+r12*8]
dec r12
mov rdx, [r13+r12*8]
add rax, rdx
mov [r13+r12*8], rax
inc r12

; After
; pop, peek, add, replace
dec r12
mov rax, [r13+r12*8]
mov rdx, [r13+(r12+-1)*8]
add rax, rdx
mov [r13+(r12+-1)*8], rax
```

This is especially interesting in the dup command `$`:

```asm
; Duplicate top stack element

; Before
; pop, push, push
dec r12
mov rax, [r13+r12*8]
mov [r13+r12*8], rax
inc r12
mov [r13+r12*8], rax
inc r12

; After
; peek, push
mov rax, [r13+(r12+-1)*8]
mov [r13+r12*8], rax
inc r12
```

This results in an amortized user-space improvement of about 7%-10% in the output.f example.