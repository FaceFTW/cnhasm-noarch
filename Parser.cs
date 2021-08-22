using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace chnasm {
    public class Parser {

        public static readonly string[] OpStrings = { "JMP", "JEQ", "JNE", "LOAD", "STORE", "JPRC", "JRET", " ",  "XORI", " ", "ORI",
            "ANDI", " ", " ", " ", "ADDI", "ADD", "SUB", "LSL", "LSR", "AND", "OR", "NOT", "XOR", " ", " ", "SLT", " ", " ", " ",
            "RDIO", "WRIO", " "};

        private SymbolTable _symTable;
        private ushort[] _asmBin;
        public ushort[] _kernBin;
        public ushort[] _progBin;
        private const string OPCODE_REGEX = @"([a-zA-z]{2,5})";
        private const string RTYPE_SREG_REGEX = @"([a-zA-z]+)([\t\n\v\f\r ]+)(\$(?:[0-9A-Za-z_]+))";
        private const string RTYPE_REG_REGEX = @"([a-zA-z]+)([\t\n\v\f\r ]+)(\$(?:[0-9A-Za-z_]+))((?>[\t\n\v\f\r ]?),(?>[ \t\n\v\f\r]*))(\$(?:[0-9A-Za-z_]+))((?>[\t\n\v\f\r ]*),(?>[\t\n\v\f\r ]*))(\$(?:[0-9A-Za-z_]+))";
        private const string RTYPE_IMM_REGEX = @"([a-zA-z]+)([\t\n\v\f\r ]*)(\$(?:[0-9A-Za-z_]+))((?>[\t\n\v\f\r ]*),(?>[\t\n\v\f\r ]*))(\$(?:[0-9A-Za-z_]+))((?>[\t\n\v\f\r ]*),(?>[\t\n\v\f\r ]*))(0x[0]{0,1}[0-9a-fA-F]|-{0,1}[0-9]{1,2})";
        private const string ITYPE_REGEX = @"([a-zA-z]+)([\t\n\v\f\r ]*)(\$(?:[0-9A-Za-z_]+))([\t\n\v\f\r ]*,[\t\n\v\f\r ]*)(0x[0-9a-fA-F]{1,2}|-{0,1}[0-9]{1,3})";
        private const string JTYPE_REGEX = @"([a-zA-z]{3,4})([\t\n\v\f\r ]*)(0[xX][0-9a-fA-F]{1,4}|[-]{0,1}[0-9]{1,3})";
        private const string JTYPE_LABEL_REGEX = @"([a-zA-z]{3,4})([\t\n\v\f\r ]+)([a-zA-Z_][0-9A-Za-z_]+)";
        private const int PROG_SIZE = 896;          //In Bytes
        private const int KERN_SIZE = 128;          //In Bytes
        private const int WORD_SIZE = 2;            //In Bytes
        private const ushort KERN_BASE_ADDR = 0x0000;
        private const ushort PROG_BASE_ADDR = 0x0080;
        public List<string> AsmText;
        public List<string> _kernText;
        public List<string> _progText;

        public ushort[] AsmBin { get { return this._asmBin; } }

        public Parser(List<string> _list) {
            this._symTable = new SymbolTable();
            this._asmBin = new ushort[(KERN_SIZE + PROG_SIZE) / WORD_SIZE];
            this._kernBin = new ushort[KERN_SIZE / WORD_SIZE];
            this._progBin = new ushort[PROG_SIZE / WORD_SIZE];
            this.AsmText = _list;
            this._kernText = new List<string>();
            this._progText = new List<string>();
        }

        public void RemoveNonCodeData() {
            //First - Remove Whitespaces
            bool removed = false;
            for(int i = 0; i < this.AsmText.Count; i++) {
                if(removed && i > 0) { i--; removed = false; }
                if(String.IsNullOrWhiteSpace(this.AsmText[i])) {
                    this.AsmText.RemoveAt(i);
                    removed = true;
                    if(i > 0) { i--; }

                }

            }

            //Second - Remove Line Comments
            //We also get to clear up some leading whitespace too :)
            for(int i = 0; i < this.AsmText.Count; i++) {
                this.AsmText[i] = this.AsmText[i].Trim();
                if(this.AsmText[i].StartsWith('#')) {
                    //KILL
                    this.AsmText.RemoveAt(i);
                }
            }

            //Now we have removed the useless garbagio
        }

        public void FindSymbols() {
            //Formatting should have occured earlier
            //See if we have a colon at the end of the string


            ushort currentAddr = KERN_BASE_ADDR;
            //Start at Kernel Code
            for(int i = 0; i < this._kernText.Count; i++) {
                if(this._kernText[i].EndsWith(':')) {
                    //Target Marked
                    Symbol sym = new Symbol();
                    sym.Address = currentAddr;
                    sym.Name = this._kernText[i].Substring(0, this._kernText[i].IndexOf(":", StringComparison.Ordinal));
                    this._symTable.AddSymbol(sym);
                    this._kernText.RemoveAt(i);
                    if(i > 0) { i--; }
                } else {
                    currentAddr += 2;
                }
            }

            currentAddr = PROG_BASE_ADDR;
            //Start at Kernel Code
            for(int i = 0; i < this._progText.Count; i++) {
                if(this._progText[i].Trim().EndsWith(':')) {
                    //Target Marked
                    Symbol sym = new Symbol {
                        Address = currentAddr,
                        Name = this._progText[i].Remove(this._progText[i].IndexOf(":", StringComparison.Ordinal))
                    };
                    this._symTable.AddSymbol(sym);
                    this._progText.RemoveAt(i);
                    if(i > 0) { i--; }
                } else {
                    currentAddr += 2;
                }
            }
        }

        public Instruction ParseAsmLine(string _asm_code, ushort _addr) {
            string opstring = "";
            Match match = Regex.Match(_asm_code, OPCODE_REGEX);

            //1 - Determine the opcode
            if(match.Success) {
                opstring = match.Groups[0].Value;
            } else {
                //throw a hissy fit at the programmer
                throw new BadSyntaxExcepiton(_asm_code, _addr);
            }

            InstructionOp opEnum = InstructionOp.INVALID;        //Placeholder
            for(int i = 0; i < OpStrings.Length; i++) {
                if(opstring.ToUpper().Equals(OpStrings[i])) { opEnum = (InstructionOp)i; break; }
            }
            //Just in case we couldn't figure out what the programmer wanted
            if(opEnum == InstructionOp.INVALID) { throw new InvalidOpExcepiton(opstring, _addr); }



            //Now we need to populate the struct
            Instruction inst = new Instruction(opEnum, _addr);
            switch(opEnum) {

                //These are all J-Types
                case InstructionOp.JMP:
                case InstructionOp.JEQ:
                case InstructionOp.JNE:
                case InstructionOp.JPRC:
                    match = Regex.Match(_asm_code, JTYPE_REGEX);
                    if(match.Success) {
                        inst.InstrType = InstructionType.JTYPE;
                        if(match.Groups[3].Value.StartsWith("0x") || match.Groups[3].Value.StartsWith("0X")) {
                            //This is a hex number, do some nice formatting stuff for C# to eat it
                            short temp = short.Parse(match.Groups[3].Value.Trim().Substring(2), NumberStyles.HexNumber);
                            //temp = (short)(~temp + 1);
                            inst.ImmJmp = temp;
                        } else { inst.ImmJmp = (short.Parse(match.Groups[3].Value)); }
                        //A bit of immediate size checking 
                        if(inst.ImmJmp > 1023 || inst.ImmJmp < -1024) { throw new LargeImmediateException(_asm_code, _addr); }
                    } else {
                        //Check if it references a label
                        match = Regex.Match(_asm_code, JTYPE_LABEL_REGEX);
                        if(match.Success) {
                            //Don't check label validity right now, we do that in the encode stage
                            inst.InstrType = InstructionType.JTYPE_LBL;
                            inst.Label = match.Groups[3].Value.Trim();
                        } else { throw new BadSyntaxExcepiton(_asm_code, _addr); }
                    }
                    break;

                //These are all I-Types
                case InstructionOp.LOAD:
                case InstructionOp.STORE:
                case InstructionOp.ADDI:
                case InstructionOp.ANDI:
                case InstructionOp.ORI:
                case InstructionOp.XORI:
                    match = Regex.Match(_asm_code, ITYPE_REGEX);
                    if(match.Success) {
                        inst.InstrType = InstructionType.ITYPE;
                        inst.DstReg = match.Groups[3].Value.Trim();
                        if(match.Groups[5].Value.StartsWith("0x") || match.Groups[5].Value.StartsWith("0X")) {
                            //This is a hex number, do some nice formatting stuff for C# to eat it
                            sbyte temp = sbyte.Parse(match.Groups[5].Value.Trim().Substring(2), NumberStyles.HexNumber);
                            // temp = (sbyte)(~temp + 1);
                            inst.Imm8Bit = temp;
                        } else { inst.Imm8Bit = (sbyte.Parse(match.Groups[5].Value)); }
                        if(inst.Imm8Bit > 127 || inst.Imm8Bit < -128) { throw new LargeImmediateException(_asm_code, _addr); }
                    } else { throw new BadSyntaxExcepiton(_asm_code, _addr); }
                    break;

                //These are R-Types (Either Reg/Imm)
                case InstructionOp.ADD:
                case InstructionOp.SUB:
                case InstructionOp.AND:
                case InstructionOp.OR:
                case InstructionOp.XOR:
                case InstructionOp.SLT:
                    match = Regex.Match(_asm_code, RTYPE_REG_REGEX);
                    if(match.Success) {
                        inst.InstrType = InstructionType.RTYPE_REG;
                        inst.DstReg = match.Groups[3].Value.Trim();
                        inst.SrcReg1 = match.Groups[5].Value.Trim();
                        inst.SrcReg2 = match.Groups[7].Value.Trim();
                    } else {
                        //Check if it uses the immediate form
                        match = Regex.Match(_asm_code, RTYPE_IMM_REGEX);
                        if(match.Success) {
                            inst.InstrType = InstructionType.RTYPE_IMM;
                            inst.DstReg = match.Groups[3].Value.Trim();
                            inst.SrcReg1 = match.Groups[5].Value.Trim();
                            if(match.Groups[7].Value.StartsWith("0x") || match.Groups[7].Value.StartsWith("0X")) {
                                //This is a hex number, do some nice formatting stuff for C# to eat it
                                inst.Imm4Bit = byte.Parse(match.Groups[7].Value.Trim().Substring(2), NumberStyles.HexNumber);
                            } else { inst.Imm4Bit = (byte.Parse(match.Groups[7].Value)); }

                            if(inst.Imm4Bit > 15) { throw new LargeImmediateException(_asm_code, _addr); }
                        } else { throw new BadSyntaxExcepiton(_asm_code, _addr); }
                    }
                    break;

                //These are explicitly R-types in immediate form
                case InstructionOp.LSL:
                case InstructionOp.LSR:
                    match = Regex.Match(_asm_code, RTYPE_IMM_REGEX);
                    if(match.Success) {
                        inst.InstrType = InstructionType.RTYPE_IMM;
                        inst.DstReg = match.Groups[3].Value.Trim();
                        inst.SrcReg1 = match.Groups[5].Value.Trim();
                        //Check if we are dealing with hex or dec
                        if(match.Groups[7].Value.StartsWith("0x") || match.Groups[7].Value.StartsWith("0X")) {
                            //This is a hex number, do some nice formatting stuff for C# to eat it
                            inst.Imm4Bit = byte.Parse(match.Groups[7].Value.Trim().Substring(2), NumberStyles.HexNumber);
                        } else { inst.Imm4Bit = (byte.Parse(match.Groups[7].Value.Trim())); }


                        if(inst.Imm4Bit > 15) { throw new LargeImmediateException(_asm_code, _addr); }
                    } else {
                        throw new BadSyntaxExcepiton(_asm_code, _addr);
                    }
                    break;

                //These are special R-Types with only one register operand
                case InstructionOp.RDIO:
                case InstructionOp.WRIO:
                case InstructionOp.NOT:
                case InstructionOp.JRET:
                    match = Regex.Match(_asm_code, RTYPE_SREG_REGEX);
                    if(match.Success) {
                        inst.InstrType = InstructionType.RTYPE_SINGLEREG;
                        inst.DstReg = match.Groups[3].Value;
                    } else { throw new BadSyntaxExcepiton(_asm_code, _addr); }
                    break;

                //Catch-all for documented/unsupported instructions
                case InstructionOp.INVALID:
                    throw new InvalidOpExcepiton(opstring, _addr);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //Instruction struct should be properly populated
            return inst;
        }

        public ushort EncodeAsm(Instruction _inst) {
            ushort machineCode = 0;
            byte dstRegIdx;
            byte srcReg1Idx;



            //First, Put the opcode in (Top 5 MSB)
            //byte opBin = (byte)_inst.OpcodeEnum;
            machineCode = (ushort)(((byte)_inst.OpcodeEnum << 11));

            //le epic instruction parsing switch tree
            switch(_inst.InstrType) {
                case InstructionType.RTYPE_SINGLEREG:
                    dstRegIdx = EncodeRegIndex(_inst.DstReg);
                    machineCode = (ushort)((machineCode | (dstRegIdx << 8)) & 0x0000FFFF);
                    break;
                case InstructionType.RTYPE_REG:
                    dstRegIdx = EncodeRegIndex(_inst.DstReg);
                    machineCode = (ushort)((machineCode | (dstRegIdx << 8)) & 0x0000FFFF);
                    srcReg1Idx = EncodeRegIndex(_inst.SrcReg1);
                    machineCode = (ushort)((machineCode | (srcReg1Idx << 5)) & 0x0000FFFF);
                    machineCode = (ushort)((machineCode | 0x00000010) & 0x0000FFFF);
                    byte srcReg2Idx = EncodeRegIndex(_inst.SrcReg2);
                    machineCode = (ushort)((machineCode | (srcReg2Idx << 1)) & 0x0000FFFF);
                    break;
                case InstructionType.RTYPE_IMM:
                    dstRegIdx = EncodeRegIndex(_inst.DstReg);
                    machineCode = (ushort)((machineCode | (dstRegIdx << 8)) & 0x0000FFFF);
                    srcReg1Idx = EncodeRegIndex(_inst.SrcReg1);
                    machineCode = (ushort)((machineCode | (srcReg1Idx << 5)) & 0x0000FFFF);
                    machineCode = (ushort)((machineCode & 0xFFFFFFEF) & 0x0000FFFF);
                    machineCode = (ushort)((machineCode | ((_inst.Imm4Bit & 0x0000000F))) & 0x0000FFFF);
                    break;
                case InstructionType.ITYPE:
                    dstRegIdx = EncodeRegIndex(_inst.DstReg);
                    machineCode = (ushort)((machineCode | (dstRegIdx << 8)) & 0x0000FFFF);
                    machineCode = (ushort)((machineCode | ((_inst.Imm8Bit & 0x000000FF))) & 0x0000FFFF);
                    break;
                case InstructionType.JTYPE:
                    //We have an immediate to mask
                    short immMask = (short)(((_inst.ImmJmp >> 1)) & 0x07FF);
                    machineCode = (ushort)(machineCode | immMask);
                    break;
                case InstructionType.JTYPE_LBL:
                    ushort symAddr = this._symTable.GetSymbolAddr(_inst.Label);
                    short offset = (short)(symAddr - _inst.Address);
                    //Because this immediate is dynamically generated, check if its too big
                    if(offset > 1023 || offset < -1024) { throw new FarLabelException(_inst.Label, _inst.Address, symAddr); }
                    //MASK TIME
                    offset = (short)(((offset >> 1)) & 0x07FF);
                    machineCode = (ushort)(machineCode | offset);
                    break;
                case InstructionType.INVALID:
                    //How did I Get Here?
                    //(Talking Heads- Once in a Lifetime)
                    throw new InvalidOpExcepiton("ERROR", _inst.Address);
                default:
                    throw new ArgumentOutOfRangeException();
            }


            return machineCode;
        }

        public void DoParse() {
            ushort currAddr = KERN_BASE_ADDR;
            for(int i = 0; i < this._kernText.Count; i++) {
                Instruction inst = ParseAsmLine(this._kernText[i], currAddr);
                this._kernBin[i] = EncodeAsm(inst);
                currAddr += 2;
                Console.WriteLine($"Address: 0x{inst.Address:X4}, Instruction: {this._kernText[i]}, Encoded: {Convert.ToString(this._kernBin[i], 2).PadLeft(16, '0')}");
            }

            //Now add this bad boy to the full binary
            for(int i = 0; i < this._kernBin.Length; i++) {
                this._asmBin[i] = this._kernBin[i];
            }

            currAddr = PROG_BASE_ADDR;
            for(int i = 0; i < this._progText.Count; i++) {
                Instruction inst = ParseAsmLine(this._progText[i], currAddr);
                this._progBin[i] = EncodeAsm(inst);
                currAddr += 2;
                Console.WriteLine($"Address: 0x{inst.Address:X4}, Instruction: {this._progText[i]}, Encoded: {Convert.ToString(this._progBin[i], 2).PadLeft(16, '0')}");
            }

            ////Now add this bad boy to the full binary
            for(int i = 0; i < this._kernBin.Length; i++) {
                this._asmBin[i + (PROG_SIZE / WORD_SIZE)] = this._progBin[i];
            }

        }


        private byte EncodeRegIndex(string _reg) {
            byte regIdx = 0;

            //Check if it is a GP register ($r0-$r4)
            if(_reg.StartsWith("$r")) {
                if(_reg.Equals("$ra")) {
                    regIdx = (byte)RegSel.RA_REG;
                } else {
                    regIdx = Byte.Parse(_reg.Substring(2, 1));
                }
            } else if(_reg.Equals("$sp")) {
                regIdx = (byte)RegSel.SP_REG;
            } else if(_reg.Equals("$fp")) {
                regIdx = (byte)RegSel.FP_REG;
            }
            return regIdx;
        }


        public void FindKernCode() {
            //Find the first instance of .ktext
            int index = this.AsmText.IndexOf(".ktext", 0) + 1;
            //this._asmText.RemoveAt(index);

            //Now iterate until we hit EOF or .text
            while(index < this.AsmText.Count) {
                if(this.AsmText[index].Equals(".text")) {
                    break;
                } else {
                    this._kernText.Add(this.AsmText[index]);
                    index++;
                }
            }
        }

        public void FindProgCode() {
            //Find the first instance of .ktext
            int index = this.AsmText.IndexOf(".text", 0) + 1;
            // this._asmText.RemoveAt(index);

            //Now iterate until we hit EOF or .text
            while(index < this.AsmText.Count) {
                if(this.AsmText[index].Equals(".text")) {
                    break;
                } else {
                    this._progText.Add(this.AsmText[index]);
                    index++;
                }
            }
        }
    }
}