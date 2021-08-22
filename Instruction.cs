using System;
using System.Collections.Generic;
using System.Text;

namespace chnasm {

    public struct Instruction {


        public Instruction(InstructionOp _op, ushort _addr, InstructionType _type) {
            //Initialize as a blank instruction effectively
            this.OpcodeEnum = _op;
            this.Address = _addr;
            this.InstrType = _type;

            //These parameters should always be zero/empty by default
            this.DstReg = "";
            this.SrcReg1 = "";
            this.SrcReg2 = "";
            this.Imm4Bit = 0;
            this.Imm8Bit = 0;
            this.ImmJmp = 0;
            this.Label = "";
        }

        public Instruction(InstructionOp _op, ushort _addr) {
            //Initialize as a blank instruction effectively
            OpcodeEnum = _op;
            Address = _addr;
            this.InstrType = InstructionType.INVALID;

            //These parameters should always be zero/empty by default
            this.DstReg = "";
            this.SrcReg1 = "";
            this.SrcReg2 = "";
            this.Imm4Bit = 0;
            this.Imm8Bit = 0;
            this.ImmJmp = 0;
            this.Label = "";
        }

        public ushort Address { get; set; }

        public InstructionOp OpcodeEnum { get; set; }

        public InstructionType InstrType { get; set; }

        public string DstReg { get; set; }

        public string SrcReg1 { get; set; }

        public string SrcReg2 { get; set; }

        public byte Imm4Bit { get; set; }

        public sbyte Imm8Bit { get; set; }

        public short ImmJmp { get; set; }

        public string Label { get; set; }
    }

    public enum InstructionOp : byte {
        JMP = 0,
        JEQ,
        JNE,
        LOAD,
        STORE,
        JPRC,
        JRET,
        XORI=8,
        ORI=10,
        ANDI=11,
        ADDI=15,
        ADD,
        SUB,
        LSL,
        LSR,
        AND,
        OR,
        NOT,
        XOR,
        SLT=26,
        RDIO=30,
        WRIO=31,
        INVALID
    }

    public enum InstructionType {
        RTYPE_SINGLEREG = 0,
        RTYPE_REG = 1,
        RTYPE_IMM = 2,
        ITYPE = 3,
        JTYPE = 4,
        JTYPE_LBL = 5,
        INVALID = 6
    }

    public enum RegSel {
        GP_REG_0 = 0,
        GP_REG_1,
        GP_REG_2,
        GP_REG_3,
        GP_REG_4,
        SP_REG,
        FP_REG,
        RA_REG
    }
}