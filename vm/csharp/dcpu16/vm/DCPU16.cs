using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dcpu16.VM
{
  public class Machine
  {
    enum Regs: byte { A, B, C, X, Y, Z, I, J };
    ushort[] regs = new ushort[8];
    ushort[] ram = new ushort[0x10000];
    ushort pc, sp, o;
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

    }
  }  
}
