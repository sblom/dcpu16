ALL_TESTS = tests/testsub tests/testset tests/testadd tests/testmul tests/testdiv tests/testmod tests/testshl tests/testshr tests/testand tests/testor tests/testxor

.PHONY = clean tests

%.ll: %.das compile-dcpu.py
	python compile-dcpu.py < $< > $@

%.bc: %.ll
	llvm-as $< -o - | opt -std-compile-opts -o $@

%.s: %.bc
	llc $< -o $@

%: %.s emulator.c
	gcc $^ -o $@

clean:
	rm -f *.bc *.ll *.s

tests: $(ALL_TESTS)
