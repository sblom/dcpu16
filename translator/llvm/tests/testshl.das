; no overflow
SET A, 0x110
SHL A, 0x3
OUT A
OUT O

; overflow
SET A, 0x8001
SHL A, 0x4
OUT A
OUT O
