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
  }

  public class Processor
  {
    private Machine machine;

    public Processor()
      : this(new Machine())
    {}

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

    delegate void operation(ref ushort loca, ref ushort locb);
    void dispatch(byte a, byte b, operation op)
    {
      ushort bval = 0;
      route(b, ref bval, get);
      route(a, ref bval, op);
    }

    void get(ref ushort a, ref ushort b)
    {
      b = a;
    }

    void route(byte aaaaaa, ref ushort bval, operation op)
    {

      if ((aaaaaa & 0x20) != 0) {
        ushort literal = (ushort)(aaaaaa & 0x1f);
        op(ref literal, ref bval);
      }
      else
      {
        byte loctype = (byte)((aaaaaa >> 3) & 0x03);
        byte reg = (byte)(aaaaaa & 0x07);

        switch (loctype)
        {
          case 0x00:
            op(ref machine.regs[reg], ref bval);
            break;
          case 0x01:
            op(ref machine.ram[machine.regs[reg]], ref bval);
            break;
          case 0x02:
            op(ref machine.ram[machine.regs[reg] + machine.ram[machine.pc++]], ref bval);
            break;
          case 0x03:
            switch (reg)
            {
              case 0x00:
                op(ref machine.ram[machine.sp++], ref bval);
                break;
              case 0x01:
                op(ref machine.ram[machine.sp], ref bval);
                break;
              case 0x02:
                op(ref machine.ram[--machine.sp], ref bval);
                break;
              case 0x03:
                op(ref machine.sp, ref bval);
                break;
              case 0x04:
                op(ref machine.pc, ref bval);
                break;
              case 0x05:
                op(ref machine.o, ref bval);
                break;
              case 0x06:
                op(ref machine.ram[machine.ram[machine.pc++]], ref bval);
                break;
              case 0x07:
                op(ref machine.ram[machine.pc++], ref bval);
                break;
            }
            break;
        }
      }

      byte type = (byte)((aaaaaa >> 3) & 0x3);
      byte loc = (byte)(aaaaaa & 0x7);
      if (type == 0)
      {
        switch(loc){}
      }
    }   

    void extended(byte a, byte o) { }
    void set(ref ushort loca, ref ushort locb) {}
    void add(ref ushort loca, ref ushort locb) { }
    void sub(ref ushort loca, ref ushort locb) { }
    void mul(ref ushort loca, ref ushort locb) { }
    void div(ref ushort loca, ref ushort locb) { }
    void mod(ref ushort loca, ref ushort locb) { }
    void shl(ref ushort loca, ref ushort locb) { }
    void shr(ref ushort loca, ref ushort locb) { }
    void and(ref ushort loca, ref ushort locb) { }
    void or(ref ushort loca, ref ushort locb) { }
    void xor(ref ushort loca, ref ushort locb) { }
    void ife(ref ushort loca, ref ushort locb) { }
    void ifn(ref ushort loca, ref ushort locb) { }
    void ifg(ref ushort loca, ref ushort locb) { }
    void ifb(ref ushort loca, ref ushort locb) { }
  }
}
