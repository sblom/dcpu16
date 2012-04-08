; no overflow
SET A, 10
MUL A, 5
OUT A
OUT O

; max int, no overflow
SET A, 0x7FFF
MUL A, 2
OUT A
OUT O

; overflow
SET A, 0x8001
MUL A, 0x8000
OUT A
OUT O
