using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    // We're passing the second operand as a Lazy<ushort> because we
    // need the side effects that come from calling route(bbbbbb, get) to
    // be deferred until after route(aaaaaa, op) is fully resolved and
    // has had its side effects (SP or PC changes) committed.
    delegate ushort operation(ref ushort a, Lazy<ushort> b = null);
    void dispatch(byte aaaaaa, byte bbbbbb, operation op)
    {
      var blazy = new Lazy<ushort>(() => route(bbbbbb, get));
      route(aaaaaa, op, blazy);

      // Make sure that we called blazy.Value (for its side effects).
      System.Diagnostics.Debug.Assert(blazy.IsValueCreated);
    }

    ushort route(byte aaaaaa, operation op, Lazy<ushort> blazy = null)
    {
      // IMPORTANT: op will be set to noop only for the primary op
      // in an instruction, the flag will have already been cleared
      // in time for the bget() phase.
      if (machine.skip)
      {
        op = noop;
        machine.skip = false;
      }

      if ((aaaaaa & 0x20) != 0) {
        ushort literal = (ushort)(aaaaaa & 0x1f);
        return op(ref literal, blazy);
      }
      else
      {
        byte loctype = (byte)((aaaaaa >> 3) & 0x03);
        byte reg = (byte)(aaaaaa & 0x07);

        switch (loctype)
        {
          case 0x00:
            return op(ref machine.regs[reg], blazy);
          case 0x01:
            return op(ref machine.ram[machine.regs[reg]], blazy);
          case 0x02:
            return op(ref machine.ram[machine.regs[reg] + machine.ram[machine.pc++]], blazy);
          case 0x03:
            switch (reg)
            {
              case 0x00:
                return op(ref machine.ram[machine.sp++], blazy);
              case 0x01:
                return op(ref machine.ram[machine.sp], blazy);
              case 0x02:
                return op(ref machine.ram[--machine.sp], blazy);
              case 0x03:
                return op(ref machine.sp, blazy);
              case 0x04:
                return op(ref machine.pc, blazy);
              case 0x05:
                return op(ref machine.o, blazy);
              case 0x06:
                return op(ref machine.ram[machine.ram[machine.pc++]], blazy);
              case 0x07:
                return op(ref machine.ram[machine.pc++], blazy);
              default:
                throw new InvalidOperationException();
            }
          default:
            throw new InvalidOperationException();
        }
      }
    }

    void extended(byte oooooo, byte aaaaaa)
    {
      switch (oooooo)
      {
        case 0x01: route(aaaaaa, jsr);
          break;
        default:
          throw new InvalidOperationException();
      }
    }

    #region Special ops that aren't actually in the CPU instruction set.
    ushort get(ref ushort a, Lazy<ushort> blazy)
    {
      Debug.Assert(blazy == null);
      return a;
    }
    ushort noop(ref ushort a, Lazy<ushort> b)
    {
      // Get blazy.Value for its side effects.
      ushort bval = b.Value;
      return 0;
    }
    #endregion

    #region Machine operation implementations.
    ushort set(ref ushort a, Lazy<ushort> b)
    {
      a = b.Value;
      return 0;
    }

    ushort add(ref ushort a, Lazy<ushort> b)
    {
      uint c = (uint)(a + b.Value);
      a = (ushort)(c & ushort.MaxValue);
      machine.o = (ushort)(c >> 16);
      return 0;
    }

    ushort sub(ref ushort a, Lazy<ushort> b)
    {
      int c = a - b.Value;
      a = (ushort)(c & ushort.MaxValue);
      machine.o = (ushort)(c >> 16);
      return 0;
    }

    ushort mul(ref ushort a, Lazy<ushort> b)
    {
      uint c = (uint)(a * b.Value);
      a = (ushort)(c & ushort.MaxValue);
      machine.o = (ushort)(c >> 16);
      return 0;
    }

    ushort div(ref ushort a, Lazy<ushort> b)
    {
      if (b.Value == 0)
      {
        a = 0;
        machine.o = 0;
      }
      else
      {
        uint c = (uint)(a << 16);
        a = (ushort)((a / b.Value) >> 16);
        machine.o = (ushort)((c / b.Value) & ushort.MaxValue);
      }
      return 0;
    }

    ushort mod(ref ushort a, Lazy<ushort> b)
    {
      uint c = (uint)(a << 16);
      a = (ushort)((c / b.Value) >> 16);
      machine.o = (ushort)((c / b.Value) & ushort.MaxValue);
      return 0;
    }

    ushort shl(ref ushort a, Lazy<ushort> b)
    {
      uint c = (uint)(a << b.Value);
      a = (ushort)(c & ushort.MaxValue);
      machine.o = (ushort) (c >> 16);
      return 0;
    }

    ushort shr(ref ushort a, Lazy<ushort> b)
    {
      uint c = (uint)((a << 16) >> b.Value);
      a = (ushort)(c >> 16);
      machine.o = (ushort)(c & ushort.MaxValue);
      return 0;
    }

    ushort and(ref ushort a, Lazy<ushort> b)
    {
      a = (ushort)(a & b.Value);
      return 0;
    }

    ushort or(ref ushort a, Lazy<ushort> b)
    {
      a = (ushort)(a | b.Value);
      return 0;
    }

    ushort xor(ref ushort a, Lazy<ushort> b)
    {
      a = (ushort)(a ^ b.Value);
      return 0;
    }

    ushort ife(ref ushort a, Lazy<ushort> b)
    {
      machine.skip = !(a == b.Value);
      return 0;
    }

    ushort ifn(ref ushort a, Lazy<ushort> b)
    {
      machine.skip = !(a != b.Value);
      return 0;
    }

    ushort ifg(ref ushort a, Lazy<ushort> b)
    {
      machine.skip = !(a > b.Value);
      return 0;
    }

    ushort ifb(ref ushort a, Lazy<ushort> b)
    {
      machine.skip = !((a & b.Value) == 0);
      return 0;
    }
    #endregion

    #region Extended instructions.
    ushort jsr(ref ushort a, Lazy<ushort> b)
    {
      Debug.Assert(b == null);
      machine.ram[--machine.sp] = machine.pc;
      machine.pc = a;
      return 0;
    }
    #endregion

    public static void Main(String[] args) {
      ushort[] sample = {
        0x7c01, 0x0030, 0x7de1, 0x1000, 0x0020, 0x7803, 0x1000, 0xc00d,
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

      while (m1.pc != 0x1a)
      {
        p1.DoCycle();
      }

      ushort a = 0xffff;
      ushort b = 1;

      ushort copyA = a;
      Processor p = new Processor();
      p.add (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " + " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 5;
      b = 10;
      copyA = a;
      p.add (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " + " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 10;
      b = 5;
      copyA = a;
      p.sub (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " - " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 5;
      b = 10;
      copyA = a;
      p.sub (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " - " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xffff;
      b = 0x5;
      copyA = a;
      p.mul (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " * " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xffff;
      b = 0xffff;
      copyA = a;
      p.mul (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " * " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf000;
      b = 2;
      copyA = a;
      p.div (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " / " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf001;
      b = 2;
      copyA = a;
      p.div (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " / " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf001;
      b = 14;
      copyA = a;
      p.div (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " / " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0x8000;
      b = 2;
      copyA = a;
      p.mod (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " % " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0x8001;
      b = 2;
      copyA = a;
      p.mod (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " % " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);


      a = 0xf001;
      b = 2;
      copyA = a;
      p.shl (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " << " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf007;
      b = 2;
      copyA = a;
      p.shr (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " >> " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf007;
      b = 0x0f08;
      copyA = a;
      p.and (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " & " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf007;
      b = 0x0f09;
      copyA = a;
      p.or (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " | " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);

      a = 0xf007;
      b = 0x0f09;
      copyA = a;
      p.xor (ref a, new Lazy<ushort>(()=>b));
      Console.WriteLine(copyA + " xor " + b + " = " + a);
      Console.WriteLine ("overflow: " + p.machine.o);
    }
  }
}
