import sys
from pyparsing import *

class Opcode(object):
  def __init__(self, opcode):
    self._opcode = opcode

  def __repr__(self):
    return 'Opcode(' + self._opcode + ')'

  def to_das(self):
    return self._opcode

class SETOpcode(Opcode):
  def __init__(self):
    super(SETOpcode, self).__init__('SET')

  def __repr__(self):
    return 'SETOpcode()'

  def to_llvm(self, out, arguments):
    out.write_line('store i16 %s, i16* %s' % (arguments[1].to_llvm(out), arguments[0].to_llvm_store(out)))

class OUTOpcode(Opcode):
  def __init__(self):
    super(OUTOpcode, self).__init__('OUT')

  def __repr__(self):
    return 'OUTOpcode()'

  def to_llvm(self, out, arguments):
    out.write_line('call void @output(i16 %s)' % (arguments[0].to_llvm(out)))

class DBGOpcode(Opcode):
  def __init__(self):
    super(DBGOpcode, self).__init__('DBG')

  def __repr__(self):
    return 'DBGOpcode()'

  def to_llvm(self, out, arguments):
    out.write_line('call void @debug(%struct.VMState* %state)')

class SUBOpcode(Opcode):
  def __init__(self):
    super(SUBOpcode, self).__init__('SUB')

  def __repr__(self):
    return 'SUBOpcode()'

  def to_llvm(self, out, arguments):
    arg0 = arguments[0].to_llvm(out)
    arg1 = arguments[1].to_llvm(out)
    tmp1 = out.temp_variable()
    tmp2 = out.temp_variable()
    tmp3 = out.temp_variable()
    tmp4 = out.temp_variable()
    out.write_line('%s = zext i16 %s to i32' % (tmp1, arg0))
    out.write_line('%s = zext i16 %s to i32' % (tmp2, arg1))
    out.write_line('%s = sub i32 %s, %s' % (tmp3, tmp1, tmp2))
    out.write_line('%s = trunc i32 %s to i16' % (tmp4, tmp3))
    out.write_line('store i16 %s, i16* %s' % (tmp4, arguments[0].to_llvm_store(out)))

    # underflow
    tmp5 = out.temp_variable()
    tmp6 = out.temp_variable()
    out.write_line('%s = lshr i32 %s, 16' % (tmp5, tmp3))
    out.write_line('%s = trunc i32 %s to i16' % (tmp6, tmp5))
    out.write_line('store i16 %s, i16* %%O' % tmp6)

class JSROpcode(Opcode):
  def __init__(self):
    super(JSROpcode, self).__init__('JSR')

  def __repr__(self):
    return 'JSROpcode()'

  def to_llvm(self, out, arguments):
    out.write_line('call void @%s(%%struct.VMState* %%state)' % arguments[0].label())

class IFNOpcode(Opcode):
  def __init__(self):
    super(IFNOpcode, self).__init__('IFN')

  def __repr__(self):
    return 'IFNOpcode()'

  def to_llvm(self, out, arguments):
    arg1 = arguments[0].to_llvm(out)
    arg2 = arguments[1].to_llvm(out)
    tmp1 = out.temp_variable()
    out.write_line('%s = icmp ne i16 %s, %s' % (tmp1, arg1, arg2))
    label1 = out.label()
    label2 = out.label()
    out.write_line('br i1 %s, label %%%s, label %%%s' % (tmp1, label1, label2))
    out.write_line('%s:' % label1)
    def post_condition():
      out.write_line('br label %%%s' % label2)
      out.write_line('%s:' % label2)
    return post_condition

class SHLOpcode(Opcode):
  def __init__(self):
    super(SHLOpcode, self).__init__('SHL')

  def __repr__(self):
    return 'SHLOpcode()'

  def to_llvm(self, out, arguments):
    arg0 = arguments[0].to_llvm(out)
    arg1 = arguments[1].to_llvm(out)
    tmp1 = out.temp_variable()
    tmp2 = out.temp_variable()
    tmp3 = out.temp_variable()
    tmp4 = out.temp_variable()
    out.write_line('%s = zext i16 %s to i32' % (tmp1, arg0))
    out.write_line('%s = zext i16 %s to i32' % (tmp2, arg1))
    out.write_line('%s = shl i32 %s, %s' % (tmp3, tmp1, tmp2))
    out.write_line('%s = trunc i32 %s to i16' % (tmp4, tmp3))
    out.write_line('store i16 %s, i16* %s' % (tmp4, arguments[0].to_llvm_store(out)))

    # overflow
    tmp5 = out.temp_variable()
    tmp6 = out.temp_variable()
    out.write_line('%s = lshr i32 %s, 16' % (tmp5, tmp3))
    out.write_line('%s = trunc i32 %s to i16' % (tmp6, tmp5))
    out.write_line('store i16 %s, i16* %%O' % tmp6)

class Register(object):
  def __init__(self, register, offset):
    self._register = register
    self._offset = offset

  def __repr__(self):
    return 'Register(' + repr(self._register) + ', ' + repr(self._offset) + ')'

  def register(self):
    return self._register

  def offset(self):
    return self._offset

  def extra_length(self):
    return 0

  def to_das(self):
    return self._register

  def to_llvm(self, out):
    tmp = out.temp_variable()
    out.write_line('%s = load i16* %%%s' % (tmp, self._register))
    return tmp

  def to_llvm_store(self, out):
    return '%%%s' % self._register

class Number(object):
  def __init__(self, num):
    self._num = num

  def __repr__(self):
    return 'Number(' + repr(self._num) + ')'

  def extra_length(self):
    return 0

  def to_das(self):
    return str(self._num)

  def to_llvm(self, out):
    return str(self._num)

class Label(object):
  def __init__(self, label):
    self._label = label

  def __repr__(self):
    return 'Label(' + repr(self._label) + ')'

  def extra_length(self):
    return 0

  def to_das(self):
    return self._label

  def label(self):
    return self._label

class Addition(object):
  def __init__(self, number, register):
    self._number = number
    self._register = register

  def __repr__(self):
    return 'Addition(' + repr(self._number) + ', ' + repr(self._register) + ')'

  def extra_length(self):
    return 1

  def to_das(self):
    return self._number.to_das() + '+' + self._register.to_das()

  def to_llvm(self, out):
    arg1 = self._number.to_llvm(out)
    arg2 = self._register.to_llvm(out)
    tmp1 = out.temp_variable()
    out.write_line('%s = add i16 %s, %s' % (tmp1, arg1, arg2))
    return tmp1

class Dereference(object):
  def __init__(self, argument):
    self._argument = argument

  def __repr__(self):
    return 'Dereference(' + repr(self._argument) + ')'

  def extra_length(self):
    return self._argument.extra_length()

  def to_das(self):
    return '[' + self._argument.to_das() + ']'

  def to_llvm(self, out):
    tmp1 = out.temp_variable()
    tmp2 = out.temp_variable()
    out.write_line('%s = getelementptr i16* %%memory, i16 %s' % \
                   (tmp1, self._argument.to_llvm(out)))
    out.write_line('%s = load i16* %s' % (tmp2, tmp1))
    return tmp2

  def to_llvm_store(self, out):
    tmp = out.temp_variable()
    out.write_line('%s = getelementptr i16* %%memory, i16 %s' % \
                   (tmp, self._argument.to_llvm(out)))
    return tmp

class Instruction(object):
  def __init__(self, label, opcode, arguments):
    self._label = label
    self._opcode = opcode
    self._arguments = arguments
    self._pc = 0

  def __repr__(self):
    args = []
    if (self._label):
      args += ['label=' + repr(self._label)]
    args += ['opcode=' + repr(self._opcode)]
    args += ['arguments=[' + ', '.join([repr(x) for x in self._arguments]) + ']']
    return 'Instruction(' + ', '.join(args) + ')'

  def opcode(self):
    return self._opcode

  def arguments(self):
    return self._arguments

  def label(self):
    return self._label

  def pc(self):
    return self._pc

  def set_pc(self, pc):
    self._pc = pc

  def length(self):
    return 1 + sum([x.extra_length() for x in self._arguments])

  def jump_label(self):
    if self._is_set_PC() and not self.is_return():
      return self._arguments[1].label()
    return None

  def is_return(self):
    return self._is_set_PC() and self._arguments[1] == Pop

  def _is_set_PC(self):
    return isinstance(self._opcode, SETOpcode) and isinstance(self._arguments[0], Register) and \
      self._arguments[0].register() == 'PC'

  def to_llvm(self, out):
    out.write_line('')
    out.write_line('; %s' % self.to_das())
    if self._label is not None:
      out.write_line('br label %%%s' % self._label)
      out.write_line('%s:' % self._label)
    out.write_line('store i16 %d, i16* %%PC' % self._pc)
    if self.jump_label() is not None:
      out.write_line('br label %%%s' % self.jump_label())
      return True, self.jump_label(), None
    elif self.is_return():
      out.write_line('ret void')
      return True, None, None
    else:
      return False, None, self._opcode.to_llvm(out, self._arguments)

  def to_das(self):
    return self._opcode.to_das() + ' ' + ', '.join([x.to_das() for x in self._arguments])

class Pop(object):
  def __repr__(self):
    return 'Pop()'

  def to_das(self):
    return 'POP'

  def extra_length(self):
    return 0
Pop = Pop()

class Peek(object):
  def __repr__(self):
    return 'Peek()'

  def to_das(self):
    return 'PEEK'

  def extra_length(self):
    return 0
Peek = Peek()

class Push(object):
  def __repr__(self):
    return 'Push()'

  def to_das(self):
    return 'PUSH'

  def extra_length(self):
    return 0
Push = Push()

class Program(object):
  def __init__(self, instructions):
    self._instructions = instructions
    self._link()

  def __repr__(self):
    return 'Program([\n' + ',\n'.join([repr(x) for x in self._instructions]) + '])'

  def _make_label_map(self):
    return dict([(x[1].label(), x[0]) for x in enumerate(self._instructions) if x[1].label() is not None])

  def _identify_function_labels(self):
    return set([x.arguments()[0].label() for x in self._instructions if isinstance(x.opcode(), JSROpcode)])

  def _link(self):
    pc = 0
    for instruction in self._instructions:
      instruction.set_pc(pc)
      pc += instruction.length()
    self._label_map = self._make_label_map()
    self._function_starts = self._identify_function_labels()
    function_labels = {}

  def _to_llvm_block(self, index, out):
    referenced_labels = set()
    post_conditions = []
    first = True
    for instruction in self._instructions[index:]:
      if not first and instruction.label() is not None:
        referenced_labels.add(instruction.label())
        break

      stop, label, post_condition = instruction.to_llvm(out)
      done = stop and len(post_conditions) == 0

      if post_condition is None:
        for post_condition in post_conditions:
          post_condition()
        post_conditions = []
      else:
        post_conditions = [post_condition] + post_conditions

      if label is not None:
        referenced_labels.add(label)

      if done:
        break

      first = False

    return referenced_labels

  def _to_llvm_function(self, name, index, out):
    rendered_labels = set()
    pending_labels = set([index])

    start_label = self._instructions[index].label()
    if start_label is not None:
      rendered_labels.add(start_label)

    out.write_line('define void @%s (%%struct.VMState* nocapture %%state) nounwind {' % name)
    out.indent()
    for register in registers.values():
      out.write_line('%%%s = getelementptr %%struct.VMState* %%state, i32 0, i32 0, i32 %s' % (register.register(), register.offset()))
    out.write_line('%memory = getelementptr %struct.VMState* %state, i32 0, i32 1, i32 0')

    func_out = LLVM_Function_Out(out)

    while len(pending_labels) > 0:
      sorted_labels = list(pending_labels)
      sorted_labels.sort(key=lambda (x): (isinstance(x, int) and -1) or self._label_map[x])
      label = sorted_labels[0]
      rendered_labels.add(label)
      if isinstance(label, basestring):
        index = self._label_map[label]
      else:
        index = label
      pending_labels = pending_labels.union(self._to_llvm_block(index, func_out))
      pending_labels = pending_labels.difference(rendered_labels)

    out.write_line('')
    out.write_line('ret void')
    out.dedent()
    out.write_line('}')

  def to_llvm(self, out):
    out.write_line('%struct.VMState = type { [11 x i16], [65536 x i16] }')
    out.write_line('declare void @output(i16)')
    out.write_line('declare void @debug(%struct.VMState* nocapture) nounwind')
    self._to_llvm_function('runMachine', 0, out)
    for label in self._function_starts:
      self._to_llvm_function(label, self._label_map[label], out)

class LLVM_Out(object):
  def __init__(self, f):
    self._f = f;
    self._func_counter = 0
    self._indent = ''

  def write_line(self, s):
    print >>self._f, self._indent + s

  def indent(self):
    self._indent += '  '

  def dedent(self):
    self._indent = self._indent[:-2]

  def func(self):
    result = '%%func%d' % self._func_counter
    self._temp_counter += 1
    return result

class LLVM_Function_Out(object):
  def __init__(self, out):
    self._out = out;
    self._temp_counter = 0
    self._label_counter = 0

  def write_line(self, s):
    self._out.write_line(s)

  def indent(self):
    self._out.indent()

  def dedent(self):
    self._out.dedent()

  def temp_variable(self):
    result = '%%tmp%d' % self._temp_counter
    self._temp_counter += 1
    return result

  def label(self):
    result = 'label%d' % self._label_counter
    self._label_counter += 1
    return result

opcodes = {
  'SET': SETOpcode(),
  'OUT': OUTOpcode(),
  'DBG': DBGOpcode(),
  'SUB': SUBOpcode(),
  'IFN': IFNOpcode(),
  'JSR': JSROpcode(),
  'SHL': SHLOpcode(),
}

registers = {
  'A': Register('A', 0),
  'B': Register('B', 1),
  'C': Register('C', 2),
  'X': Register('X', 3),
  'Y': Register('Y', 4),
  'Z': Register('Z', 5),
  'I': Register('I', 6),
  'J': Register('J', 7),
  'SP': Register('SP', 8),
  'PC': Register('PC', 9),
  'O': Register('O', 10),
}

ParserElement.setDefaultWhitespaceChars(' \t')

OPCODE = Or([Literal(x) for x in opcodes.keys()])
OPCODE.setParseAction(lambda s,l,t: opcodes[t[0]])

REGISTER = Or([Literal(x) for x in registers.keys()])

COMMENT = Suppress(';' + CharsNotIn('\n')*(0,1))

REGISTER_ARGUMENT = REGISTER
REGISTER_ARGUMENT.setParseAction(lambda s,l,t: registers[t[0]])

HEX_ARGUMENT = '0x' + Word(nums)
HEX_ARGUMENT.setParseAction(lambda s,l,t: Number(int(t[1], 16)))

DEC_ARGUMENT = Word(nums)
DEC_ARGUMENT.setParseAction(lambda s,l,t: Number(int(t[0])))

NUMERIC_ARGUMENT = HEX_ARGUMENT ^ DEC_ARGUMENT

ADD_ARGUMENT = NUMERIC_ARGUMENT + '+' + REGISTER
ADD_ARGUMENT.setParseAction(lambda s,l,t: Addition(t[0], t[2]))

BASIC_ARGUMENT = ADD_ARGUMENT ^ REGISTER ^ NUMERIC_ARGUMENT

DEREFERENCED_ARGUMENT = '[' + BASIC_ARGUMENT + ']'
DEREFERENCED_ARGUMENT.setParseAction(lambda s,l,t: Dereference(t[1]))

LABEL_ARGUMENT = Word(alphas)
LABEL_ARGUMENT.setParseAction(lambda s,l,t: Label(t[0]))

POP_ARGUMENT = Literal('POP')
POP_ARGUMENT.setParseAction(lambda s,l,t: Pop)

PEEK_ARGUMENT = Literal('PEEK')
PEEK_ARGUMENT.setParseAction(lambda s,l,t: Peek)

PUSH_ARGUMENT = Literal('PUSH')
PUSH_ARGUMENT.setParseAction(lambda s,l,t: Push)

ARGUMENT = DEREFERENCED_ARGUMENT ^ BASIC_ARGUMENT ^ \
           POP_ARGUMENT ^ PEEK_ARGUMENT ^ PUSH_ARGUMENT ^ LABEL_ARGUMENT

LABEL = Word(':', alphas)

INSTRUCTION = LABEL*(0,1) + OPCODE + (ARGUMENT + (Suppress(',') + ARGUMENT)*(0,))*(0,1)
def make_instruction(s,l,t):
  if isinstance(t[0], basestring):
    return Instruction(t[0][1:], t[1], t[2:])
  else:
    return Instruction(None, t[0], t[1:])
INSTRUCTION.setParseAction(make_instruction)

LINE = INSTRUCTION*(0,1) + COMMENT*(0,1) + Suppress(Literal('\n'))

PROGRAM = LINE*(0,)
PROGRAM.setParseAction(lambda s,l,t: Program(t))

program = PROGRAM.parseFile(sys.stdin)
program[0].to_llvm(LLVM_Out(sys.stdout))
