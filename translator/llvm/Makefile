ALL_TESTS = tests/testsub tests/testset tests/testadd tests/testmul tests/testdiv

.PHONY = clean tests

%.ll: %.das compile-dcpu.py
	python compile-dcpu.py < $< > $@

%.bc: %.ll
	llvm-as $< -o $@

%.s: %.bc
	llc $< -o $@

%: %.s emulator.c
	gcc $^ -o $@

clean:
	rm -f *.bc *.ll *.s

tests: $(ALL_TESTS)
