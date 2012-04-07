.PHONY = clean

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
