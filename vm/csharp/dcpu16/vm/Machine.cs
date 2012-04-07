using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dcpu16.VM
{
  public class Machine
  {
    public enum Regs : byte { A, B, C, X, Y, Z, I, J };
    public ushort[] regs = new ushort[8];
    public ushort[] ram = new ushort[0x10000];
    public ushort pc, sp, o;
    public bool skip;
  }
}
