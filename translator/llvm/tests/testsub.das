; no overflow
SET A, 30
SUB A, 10
OUT A
OUT O

; no overflow, return 0
SET A, 30
SUB A, 30
OUT A
OUT O

; overflow
SET A, 30
SUB A, 40
OUT A
OUT O
