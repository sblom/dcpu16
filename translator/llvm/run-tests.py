import os
import os.path
import re
import sys
import subprocess

tests_dir = os.path.join(os.path.dirname(sys.argv[0]), 'tests')
tests = []

expected_file_re = re.compile(r'(.+)-expected.txt')
for f in os.listdir(tests_dir):
  m = re.match(expected_file_re, f)
  if m is not None:
    tests.append(m.group(1))

failures = 0
for test in tests:
  print 'Running %s ...' % test
  f = open(os.path.join(tests_dir, test + '-expected.txt'))
  expected = f.read()
  f.close()

  actual = subprocess.check_output([os.path.join(tests_dir, test)])
  if actual != expected:
    print 'Test case %s failed. Expected:' % test
    print expected
    print 'Actual:'
    print actual
    print ''
    failures += 1

print 'Total failures: %d' % failures
if failures > 0:
  sys.exit(1)
