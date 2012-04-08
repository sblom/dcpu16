; no overflow
SET A, 10
ADD A, 20
OUT A
OUT O

; max int, no overflow
SET A, 0x7FFF
ADD A, 0x8000
OUT A
OUT O

; overflow
SET A, 0x8000
ADD A, 0x8001
OUT A
OUT O
