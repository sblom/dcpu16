using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dcpu16.VM
{
  public class Machine
  {
    public enum Regs: byte { A, B, C, X, Y, Z, I, J };
    public ushort[] regs = new ushort[8];
    public ushort[] ram = new ushort[0x10000];
    public ushort pc, sp, o;
    public bool skip;
  }

  public class Processor
  {
    private Machine machine;
    public static ushort WORD_SIZE = 16;
    public static ushort MAX_VAL = (ushort)((1 << WORD_SIZE) - 1);
    public static ushort MIN_VAL = 0;

    public Processor() : this(new Machine()) {}

    public Processor(Machine machine)
    {
      this.machine = machine;
    }

    public void DoCycle()
    {
      ushort instruction = machine.ram[machine.pc++];
      byte oooo = (byte)(instruction & 0xf);
      byte aaaaaa = (byte)((instruction >> 4) & 0x3f);
      byte bbbbbb = (byte)((instruction >> 10) & 0x3f);

      switch (oooo)
      {
        case 0x00: extended(aaaaaa, bbbbbb);
          break;
        case 0x01: dispatch(aaaaaa, bbbbbb, set);
          break;
        case 0x02: dispatch(aaaaaa, bbbbbb, add);
          break;
        case 0x03: dispatch(aaaaaa, bbbbbb, sub);
          break;
        case 0x04: dispatch(aaaaaa, bbbbbb, mul);
          break;
        case 0x05: dispatch(aaaaaa, bbbbbb, div);
          break;
        case 0x06: dispatch(aaaaaa, bbbbbb, mod);
          break;
        case 0x07: dispatch(aaaaaa, bbbbbb, shl);
          break;
        case 0x08: dispatch(aaaaaa, bbbbbb, shr);
          break;
        case 0x09: dispatch(aaaaaa, bbbbbb, and);
          break;
        case 0x0a: dispatch(aaaaaa, bbbbbb, or);
          break;
        case 0x0b: dispatch(aaaaaa, bbbbbb, xor);
          break;
        case 0x0c: dispatch(aaaaaa, bbbbbb, ife);
          break;
        case 0x0d: dispatch(aaaaaa, bbbbbb, ifn);
          break;
        case 0x0e: dispatch(aaaaaa, bbbbbb, ifg);
          break;
        case 0x0f: dispatch(aaaaaa, bbbbbb, ifb);
          break;
      }
    }

    delegate ushort operation(ref ushort a, Func<ushort> bget = null);
    void dispatch(byte a, byte b, operation op)
    {
      route(a, op, () => route(b, get));
    }

    ushort route(byte aaaaaa, operation op, Func<ushort> bget = null)
    {
      if (machine.skip)
        op = noop;

      if ((aaaaaa & 0x20) != 0) {
        ushort literal = (ushort)(aaaaaa & 0x1f);
        return op(ref literal, bget);
      }
      else
      {
        byte loctype = (byte)((aaaaaa >> 3) & 0x03);
        byte reg = (byte)(aaaaaa & 0x07);

        switch (loctype)
        {
          case 0x00:
            return op(ref machine.regs[reg], bget);
          case 0x01:
            return op(ref machine.ram[machine.regs[reg]], bget);
          case 0x02:
            return op(ref machine.ram[machine.regs[reg] + machine.ram[machine.pc++]], bget);
          case 0x03:
            switch (reg)
            {
              case 0x00:
                return op(ref machine.ram[machine.sp++], bget);
              case 0x01:
                return op(ref machine.ram[machine.sp], bget);
              case 0x02:
                return op(ref machine.ram[--machine.sp], bget);
              case 0x03:
                return op(ref machine.sp, bget);
              case 0x04:
                return op(ref machine.pc, bget);
              case 0x05:
                return op(ref machine.o, bget);
              case 0x06:
                return op(ref machine.ram[machine.ram[machine.pc++]], bget);
              case 0x07:
                return op(ref machine.ram[machine.pc++], bget);
              default:
                throw new InvalidOperationException();
            }
          default:
            throw new InvalidOperationException();
        }
      }
    }

    void extended(byte a, byte o) { }

    #region Special ops that aren't actually in the CPU instruction set.
    ushort get(ref ushort a, Func<ushort> bget)
    {
      return a;
    }
    ushort noop(ref ushort a, Func<ushort> bget)
    {
      bget();
      return 0;
    }
    #endregion

    #region Machine operation implementations.
    ushort set(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget();
      a = b;
      return 0;
    }

    ushort add(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget(); 
      uint c = (uint)(a + b);
      a = (ushort)(c & MAX_VAL);
      machine.o = (ushort)(c >> 16);
      return 0;
    }

    ushort sub(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget();
      int c = a - b;
      a = (ushort)(c & MAX_VAL);
      machine.o = (ushort)(c >> 16);
      return 0;
    }

    ushort mul(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget();
      uint c = (uint)(a * b);
      a = (ushort)(c & MAX_VAL);
      machine.o = (ushort)(c >> 16);
      return 0;
    }

    ushort div(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget();
      if (b == 0)
      {
        a = 0;
        machine.o = 0;
      }
      else
      {
        uint c = (uint)(a << 16);
        a = (ushort)((a / b) >> 16);
        machine.o = (ushort)((c / b) & MAX_VAL);
      }
      return 0;
    }

    ushort mod(ref ushort a, Func<ushort> bget) { return 0; }
    ushort shl(ref ushort a, Func<ushort> bget) { return 0; }
    ushort shr(ref ushort a, Func<ushort> bget) { return 0; }
    ushort and(ref ushort a, Func<ushort> bget) { return 0; }
    ushort or(ref ushort a, Func<ushort> bget) { return 0; }
    ushort xor(ref ushort a, Func<ushort> bget) { return 0; }
    ushort ife(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget();
      machine.skip = !(a == b);
      return 0;
    }
    ushort ifn(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget();
      machine.skip = !(a != b);
      return 0;
    }
    ushort ifg(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget();
      machine.skip = !(a > b);
      return 0;
    }
    ushort ifb(ref ushort a, Func<ushort> bget)
    {
      ushort b = bget();
      machine.skip = !((a & b) == 0);
      return 0;
    }
    #endregion

    public static void Main(String[] args) {
      ushort[] sample = {0x7c01, 0x0030, 0x7de1, 0x1000, 0x0020, 0x7803, 0x1000, 0xc00d,
        0x7dc1, 0x001a, 0xa861, 0x7c01, 0x2000, 0x2161, 0x2000, 0x8463,
        0x806d, 0x7dc1, 0x000d, 0x9031, 0x7c10, 0x0018, 0x7dc1, 0x001a,
        0x9037, 0x61c1, 0x7dc1, 0x001a, 0x0000, 0x0000, 0x0000, 0x0000
        };

      Machine m1 = new Machine();
      for (int i = 0; i < sample.Length; ++i)
      {
        m1.ram[i] = sample[i];
      }

      Processor p1 = new Processor(m1);

      while (true)
      {
        p1.DoCycle();
      }

      ushort a = 0xffff;
      ushort b = 1;

      ushort copyA = a;
      Processor p = new Processor();
      p.add (ref a, ()=>b);
      Console.WriteLine(copyA + " + " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 5;
      b = 10;
      copyA = a;
      p.add (ref a, ()=>b);
      Console.WriteLine(copyA + " + " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 10;
      b = 5;
      copyA = a;
      p.sub (ref a, ()=>b);
      Console.WriteLine(copyA + " - " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 5;
      b = 10;
      copyA = a;
      p.sub (ref a, ()=>b);
      Console.WriteLine(copyA + " - " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xffff;
      b = 0x5;
      copyA = a;
      p.mul (ref a, ()=>b);
      Console.WriteLine(copyA + " * " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xffff;
      b = 0xffff;
      copyA = a;
      p.mul (ref a, ()=>b);
      Console.WriteLine(copyA + " * " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf000;
      b = 2;
      copyA = a;
      p.div (ref a, ()=>b);
      Console.WriteLine(copyA + " / " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf001;
      b = 2;
      copyA = a;
      p.div (ref a, ()=>b);
      Console.WriteLine(copyA + " / " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf001;
      b = 14;
      copyA = a;
      p.div (ref a, ()=>b);
      Console.WriteLine(copyA + " / " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);
    }
  }
}
