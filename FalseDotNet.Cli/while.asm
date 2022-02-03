; Generated using the FALSE.NET compiler.
; =======================================


; Constants:
%define SYS_WRITE 1
%define SYS_EXIT 60
%define SYS_MMAP 9
%define PROT_NONE 0b0000
%define PROT_READ 0b0001
%define PROT_WRITE 0b0010
%define PROT_EXEC 0b1000
%define MAP_PRIVATE 0b00000010
%define MAP_ANONYMOUS 0b00100000


; Code:
    section .text
    global _start
    global main

_start:
main:
;===[SETUP START]===
    ; Allocate FALSE stack:
    mov rax, SYS_MMAP
    mov rdi, 0
    mov rsi, 0x00010000
    mov rdx, PROT_READ | PROT_WRITE
    mov r10, MAP_PRIVATE | MAP_ANONYMOUS
    mov r8, -1
    mov r9, 0
    syscall
    ; Check for mmap success
    cmp rax, -1
    jne skip_mmap_error
    ; Print the fact that mmap failed
    mov rax, SYS_WRITE
    mov rdi, 2
    lea rsi, [rel mmap_error]
    mov rdx, mmap_error_len
    syscall
    mov rax, SYS_EXIT
    mov rdi, 1
    syscall
skip_mmap_error:
    mov [rel stack], rax
;===[SETUP  END]===

    ; -- IntLiteral, 1; --
    mov rax, 0x00000001
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- Lambda, 1; --
    ; -- Lambda, 2; --
    ; -- WhileInit; --
    ; -- WhileCondition; --
    ; -- WhileBody; --
    ; -- Drop; --
    ; -- PrintString, 0; --
    mov rax, SYS_WRITE
    mov rdi, 1
    lea rsi, [rel str_000]
    mov rdx, len_000
    syscall
    ; -- IntLiteral, 10; --
    mov rax, 0x0000000a
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- OutputChar; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rax, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    push rax
    mov rax, SYS_WRITE
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 1
    syscall
    add rsp, 8
    ; -- Exit; --
    mov rax, SYS_EXIT
    mov rdi, 0
    syscall

_lambda_001:
    ; -- Dup; --
    ; -- IntLiteral, 4; --
    mov rax, 0x00000004
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- Eq; --
    ; -- Not; --
    ; -- Ret; --

_lambda_002:
    ; -- Dup; --
    ; -- OutputDecimal; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rdi, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    call print_decimal
    ; -- IntLiteral, 58; --
    mov rax, 0x0000003a
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- OutputChar; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rax, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    push rax
    mov rax, SYS_WRITE
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 1
    syscall
    add rsp, 8
    ; -- IntLiteral, 32; --
    mov rax, 0x00000020
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- OutputChar; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rax, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    push rax
    mov rax, SYS_WRITE
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 1
    syscall
    add rsp, 8
    ; -- IntLiteral, 1; --
    mov rax, 0x00000001
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- Lambda, 3; --
    ; -- Lambda, 4; --
    ; -- WhileInit; --
    ; -- WhileCondition; --
    ; -- WhileBody; --
    ; -- OutputDecimal; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rdi, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    call print_decimal
    ; -- IntLiteral, 10; --
    mov rax, 0x0000000a
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- OutputChar; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rax, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    push rax
    mov rax, SYS_WRITE
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 1
    syscall
    add rsp, 8
    ; -- IntLiteral, 1; --
    mov rax, 0x00000001
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- Add; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rax, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rdx, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    add rax, rdx
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- Ret; --

_lambda_003:
    ; -- Dup; --
    ; -- IntLiteral, 3; --
    mov rax, 0x00000003
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- Eq; --
    ; -- Not; --
    ; -- Ret; --

_lambda_004:
    ; -- Dup; --
    ; -- OutputDecimal; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rdi, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    call print_decimal
    ; -- IntLiteral, 44; --
    mov rax, 0x0000002c
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- OutputChar; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rax, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    push rax
    mov rax, SYS_WRITE
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 1
    syscall
    add rsp, 8
    ; -- IntLiteral, 32; --
    mov rax, 0x00000020
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- OutputChar; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rax, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    push rax
    mov rax, SYS_WRITE
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 1
    syscall
    add rsp, 8
    ; -- IntLiteral, 1; --
    mov rax, 0x00000001
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- Add; --
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rax, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    dec rcx
    mov rdx, [rbx,rcx*8]
    mov [rel stack_ptr], rcx
    add rax, rdx
    mov rbx, [rel stack]
    mov rcx, [rel stack_ptr]
    mov [rbx,rcx*8], rax
    inc rcx
    mov [rel stack_ptr], rcx
    ; -- Ret; --

; Converts rdi to decimal and writes to stdout.
print_decimal:
    mov rax, rdi
    neg rdi
    mov r11, 0
    mov rdx, -1
    cmp rax, 0
    cmovl r11, rdx
    cmovl rax, rdi
    mov rdi, 10
    lea r8, [rel string_buffer]
    mov rcx, 0x00000020
print_decimal_loop:
    dec rcx
    xor rdx, rdx
    div rdi
    add dl, '0'
    mov [r8,rcx], dl
    cmp rax, 0
    jne print_decimal_loop
    cmp r11, 0
    je print_decimal_skip
    dec rcx
    mov dl, '-'
    mov [r8,rcx], dl
print_decimal_skip:
    add r8, rcx
    mov rdx, 0x00000020
    sub rdx, rcx
    mov rax, SYS_WRITE
    mov rdi, 1
    mov rsi, r8
    syscall
    ret


; Uninitialized Globals:
    section .bss
stack: RESQ 1
string_buffer: RESB 32


; Globals:
    section .data
stack_ptr: DQ 0


; Constants:
    section .rodata
str_000: DB `Done.`
len_000: EQU $ - str_000

; Constant Constants:
mmap_error: DB `MMAP Failed! Exiting.\n`
mmap_error_len: EQU $ - mmap_error
